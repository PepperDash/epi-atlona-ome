using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AtlonaOme.Config;
using AtlonaOme.JoinMaps;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.Queues;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Devices.Common.VideoCodec.Cisco;

namespace AtlonaOme.Devices
{
    public abstract class AtlonaOmeDevice : EssentialsBridgeableDevice, IDeviceInfoProvider, IHasPowerControlWithFeedback
    {
        public string IpAddress { get; protected set; }
        public string MacAddress { get; protected set; }
        public string Hostname { get; protected set; }
        public string SerialNumber { get; protected set; }
        public string FirmwareVersion { get; protected set; }
        public string Model { get; protected set; }
        public const string Make = "Atlona";

        public readonly EndpointType EndpointType;

        protected List<Action> Polls { get; set; } 

        private List<Action> DeviceInfoPolls { get; set; } 

        public bool PowerIsOn { get; protected set; }

        private CTimer _pollRing;
        private CTimer _deviceInfoPollRing;

        protected string LastCommand { get; set; }

        public StringFeedback ModelFeedback { get; protected set; }
        public StringFeedback MakeFeedback { get; protected set; }
        public FeedbackCollection<BoolFeedback> SyncFeedbacks { get; protected set; } 

        protected AtlonaOmeDevice(string key, string name, AtlonaOmeConfigObject config, IBasicCommunication comms, EndpointType endpointType) : base(key, name)
        {
            EndpointType = endpointType;
            _receiveQueue = new GenericQueue(key + "-rxqueue");  // If you need to set the thread priority, use one of the available overloaded constructors.

            ConnectFeedback = new BoolFeedback(() => Connect);
            OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
            StatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);
            ModelFeedback = new StringFeedback(() => Model);
            MakeFeedback = new StringFeedback(() => Make);

            SerialNumber = config.DeviceSerialNumber;
            MacAddress = config.DeviceMacAddress;

            DeviceInfoPolls = new List<Action>
            {
                GetIpConfig,
                GetFirmware,
                GetModel,
                GetHostname
            };

            BuildPolls();

            _pollRing = CreatePollTimer(Polls);
            _deviceInfoPollRing = CreatePollTimer(DeviceInfoPolls);

            _comms = comms;
            _commsMonitor = new GenericCommunicationMonitor(this, _comms, config.PollTimeMs, config.WarningTimeoutMs, config.ErrorTimeoutMs, () => _pollRing.Reset(750));

            var socket = _comms as ISocketStatus;
            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += socket_ConnectionChange;
                Connect = true;
            }

            // Only one of the below handlers should be necessary.  

            // _comms gather for any API that has a defined delimiter
            var commsGather = new CommunicationGather(_comms, CommsRxDelimiter);
            commsGather.LineReceived += Handle_LineRecieved;

            _commsMonitor.StatusChange += (s, a) =>
            {
                if (a == null) return;
                if (a.Status != MonitorStatus.IsOk) return;
                _deviceInfoPollRing.Reset(2500);
            };

        }
        

        private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            var telnetNegotation = new byte[] { 0xFF, 0xFE, 0x01, 0xFF, 0xFE, 0x21, 0xFF, 0xFC, 0x01, 0xFF, 0xFC, 0x03 };

            if (args.Client.IsConnected)
            {
                args.Client.SendBytes(telnetNegotation);
            }

            if (ConnectFeedback != null)
                ConnectFeedback.FireUpdate();

            if (StatusFeedback != null)
                StatusFeedback.FireUpdate();
        }

        private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
        {
            _receiveQueue.Enqueue(new ProcessStringMessage(args.Text, ProcessBaseFeedbackMessage));
        }

        /// <summary>
        /// This method should perform any necessary parsing of feedback messages from the device
        /// </summary>
        /// <param name="message"></param>
        protected abstract void ProcessFeedbackMessage(string message);

        protected void BuildPolls()
        {
            Polls = new List<Action>();
            var atlonaRoutingPoll = this as IAtlonaRoutingPoll;
            if (atlonaRoutingPoll != null)
            {
                Polls.Add(atlonaRoutingPoll.PollInputStatus);
                Polls.Add(atlonaRoutingPoll.PollRouteStatus);
            }
            Polls.Add(PollPower);
        }

        private void ProcessBaseFeedbackMessage(string message)
        {
            if (LastCommand.Equals("Version", StringComparison.OrdinalIgnoreCase))
            {
                FirmwareVersion = message;
                OnDeviceInfoChanged();
                return;
            }
            if (LastCommand.Equals("Type", StringComparison.OrdinalIgnoreCase))
            {
                Model = message;
                ModelFeedback.FireUpdate();
                MakeFeedback.FireUpdate();
                return;
            }
            if (message.IndexOf("IP Addr:", StringComparison.OrdinalIgnoreCase) > -1)
            {
                ParseIpInformation(message);
                return;
            }
            if (message.IndexOf("RHostName", StringComparison.OrdinalIgnoreCase) > -1)
            {
                ParseHostnameInformation(message);
                return;
            }
            if (message.IndexOf("PWON", StringComparison.OrdinalIgnoreCase) > -1)
            {
                PowerIsOn = true;
                PowerIsOnFeedback.FireUpdate();
                return;
            }
            if (message.IndexOf("PWOFF", StringComparison.OrdinalIgnoreCase) > -1)
            {
                PowerIsOn = false;
                PowerIsOnFeedback.FireUpdate();
                return;
            }
            ProcessFeedbackMessage(message);
        }

        private void ParseIpInformation(string message)
        {
            const string pattern = @"\S+$";
            var regex = new Regex(pattern);
            var matches = regex.Matches(message);
            if (matches.Count < 1) return;
            IpAddress = matches[0].Value;
            OnDeviceInfoChanged();
        }

        private void ParseHostnameInformation(string message)
        {
            const string prefix = "RHostName";
            Hostname = PullDataFromPrefix(prefix, message);
            OnDeviceInfoChanged();
        }

        /// <summary>
        /// Sends text to the device plugin comms
        /// </summary>
        /// <remarks>
        /// Can be used to test commands with the device plugin using the DEVPROPS and DEVJSON console commands
        /// </remarks>
        /// <param name="text">Command to be sent</param>		
        public void SendText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            LastCommand = text;
            _comms.SendText(string.Format("{0}{1}", text, CommsTxDelimiter));
        }

        /*
        /// <summary>
        /// Polls the device
        /// </summary>
        /// <remarks>
        /// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
        /// </remarks>
        public void Poll(List<Action> polls, CTimer timer)
        {
            if (timer != null) return;
            timer = new CTimer(o => ProcessPoll(polls, 0, timer), 0);
        }

        private void ProcessPoll(ICollection polls, int index, CTimer timer)
        {
            if (index >= polls.Count)
            {
                timer = null;
                return;
            }
            var accessor = index;
            var action = Polls.ElementAtOrDefault(accessor);
            if(action != null) action.Invoke();
            timer = new CTimer(o => ProcessPoll(polls, accessor + 1, timer), 750);
        }
         */

        public CTimer CreatePollTimer(List<Action> polls)
        {
            CTimer timer = null;
            timer = new CTimer(_ =>
            {
                polls.ForEach(p => p.Invoke());
                timer.Dispose();
            }, 0, Timeout.Infinite);

            return timer;
        }
        


        public void PollPower()
        {
            SendText("PWSTA");
        }

        public void GetIpConfig()
        {
            SendText("IPCFG");
        }

        public void GetModel()
        {
            SendText("Model");
        }

        public void GetFirmware()
        {
            SendText("Version");
        }

        public void GetHostname()
        {
            SendText("RHostname");
        }

        protected string PullDataFromPrefix(string prefix, string message)
        {
            return message.Substring(message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) + prefix.Length + 1);
        }


        /// <summary>
        /// Provides a queue and dedicated worker thread for processing feedback messages from a device.
        /// </summary>
        private readonly GenericQueue _receiveQueue;

        private readonly IBasicCommunication _comms;
        private readonly GenericCommunicationMonitor _commsMonitor;

        private const string CommsTxDelimiter = "\r";
        private const string CommsRxDelimiter = "\r\n";

        /// <summary>
        /// Connects/disconnects the comms of the plugin device
        /// </summary>
        /// <remarks>
        /// triggers the _comms.Connect/Disconnect as well as thee comms monitor start/stop
        /// </remarks>
        public bool Connect
        {
            get { return _comms.IsConnected; }
            set
            {
                if (value)
                {
                    _comms.Connect();
                    _commsMonitor.Start();
                }
                else
                {
                    _comms.Disconnect();
                    _commsMonitor.Stop();
                }
            }
        }

        /// <summary>
        /// Reports connect feedback through the bridge
        /// </summary>
        public BoolFeedback ConnectFeedback { get; private set; }

        /// <summary>
        /// Reports online feedback through the bridge
        /// </summary>
        public BoolFeedback OnlineFeedback { get; private set; }

        /// <summary>
        /// Reports socket status feedback through the bridge
        /// </summary>
        public IntFeedback StatusFeedback { get; private set; }

        public override void LinkToApi(BasicTriList trilist, uint joinStart,
            string joinMapKey, EiscApiAdvanced bridge)
        {
            JoinMapBaseAdvanced joinMap;
            var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

            var isSerialized = !String.IsNullOrEmpty(joinMapSerialized);

            Debug.Console(0, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, this, "Linking to Atlona Endpoint: {0}", Name);


            switch (EndpointType)
            {
                case(EndpointType.Tx):
                    joinMap =
                        isSerialized
                            ? JsonConvert.DeserializeObject<AtlonaTxJoinMap>(joinMapKey)
                            : new AtlonaTxJoinMap(joinStart);
                    LinkTxToApi(trilist, joinMap, bridge);
                    break;
                case(EndpointType.Rx):
                    joinMap = isSerialized
                        ? JsonConvert.DeserializeObject<AtlonaRxJoinMap>(joinMapKey)
                        : new AtlonaRxJoinMap(joinStart);
                    LinkRxToApi(trilist, joinMap, bridge);
                    break;
                default:
                    Debug.Console(0, this, "Endpoint type not set, unable to build bridge");
                    return;
            }
        }

        public void LinkRxToApi(BasicTriList trilist, JoinMapBaseAdvanced rxMap, EiscApiAdvanced bridge)
        {
            if(bridge != null)bridge.AddJoinMap(Key, rxMap);
            var joinMap = rxMap as AtlonaRxJoinMap;
            if (joinMap == null) return;
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            var rxRoutingWithFeedback = this as IRoutingFeedback;
            if (rxRoutingWithFeedback != null)
            {
                trilist.SetUShortSigAction(joinMap.AudioVideoInput.JoinNumber,
                    a => rxRoutingWithFeedback.ExecuteNumericSwitch(a, 1, eRoutingSignalType.AudioVideo));
                rxRoutingWithFeedback.AudioVideoSourceNumericFeedback.LinkInputSig(
                    trilist.UShortInput[joinMap.AudioVideoInput.JoinNumber]);
            }
            var hdmiInput2 = this as IHdmiInput2;
            if (hdmiInput2 != null) hdmiInput2.HdmiInput2SyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.InputSync.JoinNumber]);

            trilist.SetUshort(joinMap.HdcpSupportCapability.JoinNumber, 0);
            trilist.SetUshort(joinMap.HdcpInputPortCount.JoinNumber, 0);

            trilist.OnlineStatusChange += (s, a) =>
            {
                if (s == null) return;
                if (!a.DeviceOnLine) return;
                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
                trilist.SetUshort(joinMap.HdcpSupportCapability.JoinNumber, 0);
                trilist.SetUshort(joinMap.HdcpInputPortCount.JoinNumber, 0);
                if (rxRoutingWithFeedback != null) rxRoutingWithFeedback.AudioVideoSourceNumericFeedback.FireUpdate();
                if (hdmiInput2 != null) hdmiInput2.HdmiInput2SyncFeedback.FireUpdate();
            };

        }
        public void LinkTxToApi(BasicTriList trilist, JoinMapBaseAdvanced txMap, EiscApiAdvanced bridge)
        {
            if (bridge != null) bridge.AddJoinMap(Key, txMap);
            var joinMap = txMap as AtlonaTxJoinMap;
            if (joinMap == null) return;
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            var txRoutingWithFeedback = this as ITxRoutingWithFeedback;
            if (txRoutingWithFeedback != null)
            {
                trilist.SetUShortSigAction(joinMap.AudioVideoInput.JoinNumber,
                    a => txRoutingWithFeedback.ExecuteNumericSwitch(a, 1, eRoutingSignalType.AudioVideo));
                txRoutingWithFeedback.VideoSourceNumericFeedback.LinkInputSig(
                    trilist.UShortInput[joinMap.AudioVideoInput.JoinNumber]);
            }
            var usbCInput1 = this as IUsbCInput1;
            var hdmiInput1 = this as IHdmiInput1;
            var hdmiInput2 = this as IHdmiInput2;
            var hdmiInput3 = this as IHdmiInput3;
            if (usbCInput1 != null) usbCInput1.UsbCInput1SyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input1VideoSyncStatus.JoinNumber]);
            if (hdmiInput1 != null) hdmiInput1.HdmiInput1SyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input1VideoSyncStatus.JoinNumber]);
            if (hdmiInput2 != null) hdmiInput2.HdmiInput2SyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input2VideoSyncStatus.JoinNumber]);
            if (hdmiInput3 != null) hdmiInput3.HdmiInput3SyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input3VideoSyncStatus.JoinNumber]);

            trilist.SetUshort(joinMap.HdcpSupportCapability.JoinNumber, 0);
            trilist.SetUshort(joinMap.HdcpInputPortCount.JoinNumber, 0);

            trilist.OnlineStatusChange += (s, a) =>
            {
                if (s == null) return;
                if (!a.DeviceOnLine) return;
                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
                trilist.SetUshort(joinMap.HdcpSupportCapability.JoinNumber, 0);
                trilist.SetUshort(joinMap.HdcpInputPortCount.JoinNumber, 0);
                if(txRoutingWithFeedback!= null) txRoutingWithFeedback.VideoSourceNumericFeedback.FireUpdate();
                if (usbCInput1 != null) usbCInput1.UsbCInput1SyncFeedback.FireUpdate();
                if (hdmiInput1 != null) hdmiInput1.HdmiInput1SyncFeedback.FireUpdate();
                if (hdmiInput2 != null) hdmiInput2.HdmiInput2SyncFeedback.FireUpdate();
                if (hdmiInput3 != null) hdmiInput3.HdmiInput3SyncFeedback.FireUpdate();
            };
        }

        #region IDeviceInfoProvider Members

        public DeviceInfo DeviceInfo { get; protected set; }

        public event DeviceInfoChangeHandler DeviceInfoChanged;

        private void OnDeviceInfoChanged()
        {
            DeviceInfo = new DeviceInfo
            {
                IpAddress = IpAddress,
                HostName = Hostname,
                MacAddress = MacAddress,
                FirmwareVersion = FirmwareVersion,
                SerialNumber = SerialNumber
            };
            var args = new DeviceInfoEventArgs(DeviceInfo);
            var changeEvent = DeviceInfoChanged;
            if (changeEvent == null) return;
            DeviceInfoChanged(this, args);

        }

        public void UpdateDeviceInfo()
        {
            _deviceInfoPollRing.Reset(750);
        }

        #endregion

        #region IHasPowerControlWithFeedback Members

        public BoolFeedback PowerIsOnFeedback { get; protected set; }

        #endregion

        #region IHasPowerControl Members

        public void PowerOff()
        {
            SendText("PWOFF");
        }

        public void PowerOn()
        {
            SendText("PWON");
        }

        public void PowerToggle()
        {
            if (PowerIsOn)
            {
                PowerOff();
                return;
            }
            PowerOn();
        }

        #endregion

    }

    public enum EndpointType
    {
        Tx,
        Rx
    }

}
