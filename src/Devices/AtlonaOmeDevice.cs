using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AtlonaOme.Config;
using AtlonaOme.JoinMaps;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.CrestronThread;
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
    public abstract class AtlonaOmeDevice : EssentialsBridgeableDevice, IDeviceInfoProvider
    {
        public string IpAddress { get; protected set; }
        public string MacAddress { get; protected set; }
        public string Hostname { get; protected set; }
        public string SerialNumber { get; protected set; }
        public string FirmwareVersion { get; protected set; }
        public string Model { get; protected set; }
        public const string Make = "Atlona";

        private static char _newLine = CrestronEnvironment.NewLine.ToCharArray().First();

        private bool activated;

        public readonly EndpointType EndpointType;

        protected List<Action> Polls { get; set; } 

        private List<Action> DeviceInfoPolls { get; set; } 

        public bool PowerIsOn { get; protected set; }

        private CTimer _pollRing;
        private CTimer _deviceInfoPollRing;

        private readonly string _connectionStringFromConfig;

        protected string LastCommand { get; set; }

        public StringFeedback ModelFeedback { get; protected set; }
        public StringFeedback MakeFeedback { get; protected set; }
        public FeedbackCollection<BoolFeedback> SyncFeedbacks { get; protected set; } 

        protected AtlonaOmeDevice(string key, string name, AtlonaOmeConfigObject config, IBasicCommunication comms, EndpointType endpointType) : base(key, name)
        {
            EndpointType = endpointType;
            _connectionStringFromConfig = config.Control.TcpSshProperties.Address;
            _receiveQueue = new GenericQueue(key + "-rxqueue");  // If you need to set the thread priority, use one of the available overloaded constructors.
            _transmitQueue = new GenericQueue(key + "-txqueue", 750, Thread.eThreadPriority.LowestPriority, 64);

            SerialNumber = config.DeviceSerialNumber;

            DeviceInfo = new DeviceInfo()
            {
                SerialNumber = String.IsNullOrEmpty(SerialNumber) ? "Unknown - Set in Config" : SerialNumber,
                MacAddress = String.IsNullOrEmpty(MacAddress) ? "Unknown - Set in Config" : MacAddress
            };

            _comms = comms;
            _commsMonitor = new GenericCommunicationMonitor(this, _comms, config.PollTimeMs, config.WarningTimeoutMs, config.ErrorTimeoutMs, PollAllStatus);

            OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
            StatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);
            ModelFeedback = new StringFeedback(() => Model);
            MakeFeedback = new StringFeedback(() => Make);


            DeviceInfoPolls = new List<Action>
            {
                GetFirmware,
                GetModel
            };

            BuildPolls();

            /*
            _pollRing = new CTimer(StatusPollRing, 0, Timeout.Infinite);
            _deviceInfoPollRing = new CTimer(InfoPollRing, 0, Timeout.Infinite);
            */

            var socket = _comms as ISocketStatus;
            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += socket_ConnectionChange;
            }

            // Only one of the below handlers should be necessary.  

            // _comms gather for any API that has a defined delimiter
            var commsGather = new CommunicationGather(_comms, CommsRxDelimiter);
            commsGather.LineReceived += Handle_LineRecieved;

            _commsMonitor.StatusChange += (s, a) =>
            {
                if (a == null) return;
                if (a.Status != MonitorStatus.IsOk) return;
                if (activated) ResolveHostData();
                //_deviceInfoPollRing.Reset(2500);

            };
            AddPostActivationAction(_comms.Connect);
            AddPostActivationAction(SetActive);
        }

        private void SetActive()
        {
            activated = true;
            if (!_commsMonitor.IsOnline) return;
            ResolveHostData();
        }

        private static string SanitizeIpAddress(string ipAddressIn)
        {
            var preSantitizedIp = ipAddressIn.TrimStart('0').TrimAll();
            Debug.Console(2, "IP Address To Sanitize = {0}", preSantitizedIp);
            try
            {
                var ipAddress = IPAddress.Parse(ipAddressIn.TrimStart('0').TrimAll());
                return ipAddress.ToString();
            }
            catch (Exception ex)
            {
                Debug.Console(0, "Unable to Santize Ip : {0}", ex.Message);
                return ipAddressIn;
            }
        }

        public string GetMacFromArpTable(string ipaddress)
        {
            Debug.Console(1, this, "GetMacFromArpTable for : {0}", ipaddress);
            var consoleResponse = string.Empty;
            var macAddress = !String.IsNullOrEmpty(MacAddress) ? MacAddress : "Unknown";
            var ipAddress = SanitizeIpAddress(ipaddress);
            if (!CrestronConsole.SendControlSystemCommand("showarptable", ref consoleResponse)) return macAddress;
            if (string.IsNullOrEmpty(consoleResponse)) return macAddress;

            Debug.Console(2, "ConsoleResponse of 'showarptable' : {0}{1}", "\n", consoleResponse);

            var myLines =
                consoleResponse.Split(_newLine).ToList().Where(o => (o.Contains(':') && !o.Contains("Type", StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var line in myLines)
            {
                var item = line;
                var seperator = item.Contains('\t') ? '\t' : ' ';
                var dataPoints = item.Split(seperator);
                if (dataPoints == null || dataPoints.Length < 2) continue;
                var lineIp = SanitizeIpAddress(dataPoints.First());
                if (lineIp != ipAddress) continue;
                Debug.Console(0, this, "dataPoints.Last() : {0}", dataPoints.Last());
                macAddress = dataPoints.Last();
                break;
            }
            MacAddress = macAddress;
            return macAddress;
        }

        public void ResolveHostData()
        {
            var searchString = _connectionStringFromConfig;
            if (string.IsNullOrEmpty(searchString)) return;
            var hostEntry = Dns.GetHostEntry(searchString);
            if (hostEntry == null) return;
            DeviceInfo.IpAddress = hostEntry.AddressList.First().ToString();
            DeviceInfo.HostName = hostEntry.HostName;
            DeviceInfo.MacAddress = GetMacFromArpTable(DeviceInfo.IpAddress);
        }

        private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            if (args.Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                _commsMonitor.Start();
                PollDeviceInfo();
            }
            if (args.Client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                _commsMonitor.Stop();
            }
            if (StatusFeedback != null)
                StatusFeedback.FireUpdate();
        }

        private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
        {
            const string telnetResponse = "\xFF\xFD\x01\xFF\xFD\x1F\xFF\xFB\x01\xFF\xFB\x03";
            try
            {
                if (args == null)
                {
                    Debug.Console(0, this, "No Args");
                }
                Debug.Console(1,this, "Line Received : {0}", args.Text.Trim());
                Debug.Console(2, this, "LastCommand : {0}", LastCommand.Trim());
                if (args.Text == telnetResponse) return;
                if (args.Text.Contains("TELNET.", StringComparison.OrdinalIgnoreCase)) return;
                if (args.Text.Contains("FAILED:", StringComparison.OrdinalIgnoreCase)) return;
                if (args.Text.Trim().Equals(LastCommand.Trim())) return;
                //ProcessBaseFeedbackMessage(args.Text);
                Debug.Console(2, _receiveQueue, "Enqueue : {0}", args.Text);
                _receiveQueue.Enqueue(new ProcessStringMessage(args.Text, ProcessBaseFeedbackMessage));
                //Debug.Console(0, _receiveQueue, "QueueCount : {0} - Capacity : {1}", _receiveQueue.QueueCount, _receiveQueue.QueueCapacity);

            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "LineReceived Error : {0}", ex.Message);
            }
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
        }

        private void ProcessBaseFeedbackMessage(string message)
        {
            try
            {
                Debug.Console(2, this, "Processing Response : {0}", message);
                if (LastCommand.Equals("Version", StringComparison.OrdinalIgnoreCase))
                {
                    DeviceInfo.FirmwareVersion = message;
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
                if (message.IndexOf("IP Addr:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ParseIpInformation(message);
                    return;
                }
                if (message.IndexOf("RHostName", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ParseHostnameInformation(message);
                    return;
                }
                ProcessFeedbackMessage(message);
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "ProcessBaseFeedbackMessage Error : {0}", ex.Message);
            }
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
            Debug.Console(2, _transmitQueue, "Cmd Enqueued : {0}", text);
            _transmitQueue.Enqueue(new ProcessStringMessage(text + CommsTxDelimiter, _comms.SendText));
            //Debug.Console(0, _transmitQueue, "QueueCount : {0} - Capacity : {1}",_transmitQueue.QueueCount, _transmitQueue.QueueCapacity);
           // _comms.SendText(string.Format("{0}{1}", text, CommsTxDelimiter));
        }

        private void PollDeviceInfo()
        {
            //_deviceInfoPollRing.Stop();
            foreach (var item in DeviceInfoPolls)
            {
                var pollAction = item;
                pollAction.Invoke();
            }
            /*
            var index = 0;
            if (indexObj != null)
            {
                var indexNullable = indexObj as int?;
                index = indexNullable == null ? 0 : (int) indexNullable;
            }
            if (index >= DeviceInfoPolls.Count)
            {
                _deviceInfoPollRing = new CTimer(InfoPollRing, 0, Timeout.Infinite);
                return;
            }
            DeviceInfoPolls[index].Invoke();
            var outIndex = index + 1;
            _deviceInfoPollRing = new CTimer(InfoPollRing, outIndex, 750);
             */
        }

        private void PollAllStatus()
        {
            foreach (var item in Polls)
            {
                var pollAction = item;
                pollAction.Invoke();
            }


            /*
            _pollRing.Stop();
            var index = 0;
            if (indexObj != null)
            {
                var indexNullable = indexObj as int?;
                index = indexNullable == null ? 0 : (int)indexNullable;
            }
            if (index >= Polls.Count)
            {
                _pollRing = new CTimer(StatusPollRing, 0, Timeout.Infinite);
                return;
            }
            Polls[index].Invoke();
            var outIndex = index + 1;
            _pollRing = new CTimer(StatusPollRing, outIndex, 750);
             */
        }


        public void GetIpConfig()
        {
            SendText("IPCFG");
        }

        public void GetModel()
        {
            SendText("Type");
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

        private readonly GenericQueue _transmitQueue;

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

            Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(2, this, "Linking to Atlona Endpoint: {0}", Name);


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

        public void ReportDeviceInfo()
        {
            if (DeviceInfo == null)
            {
                Debug.Console(0, this, "DeviceInfo is null");
                return;
            }
            Debug.Console(0, this, DeviceInfo.FirmwareVersion);
            Debug.Console(0, this, DeviceInfo.IpAddress);
            Debug.Console(0, this, DeviceInfo.HostName);
            Debug.Console(0, this, DeviceInfo.MacAddress);
            Debug.Console(0, this, DeviceInfo.SerialNumber);
        }

        #region IDeviceInfoProvider Members

        public DeviceInfo DeviceInfo { get; protected set; }

        public event DeviceInfoChangeHandler DeviceInfoChanged;

        private void OnDeviceInfoChanged()
        {
            var args = new DeviceInfoEventArgs(DeviceInfo);
            var changeEvent = DeviceInfoChanged;
            if (changeEvent == null) return;
            DeviceInfoChanged(this, args);

        }

        public void UpdateDeviceInfo()
        {
            PollDeviceInfo();
            //_deviceInfoPollRing.Reset(750);
        }

        #endregion


    }

    public enum EndpointType
    {
        Tx,
        Rx
    }

}
