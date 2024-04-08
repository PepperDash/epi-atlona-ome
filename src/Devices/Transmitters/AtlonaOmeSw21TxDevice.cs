// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using System;
using System.Globalization;
using System.Linq;
using Crestron.SimplSharp;
using PepperDash.Core;
using AtlonaOme.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Routing;
using AtlonaOme.JoinMaps;


namespace AtlonaOme.Devices.Transmitters
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.
	/// </remarks>
	/// <example>
	/// "EssentialsPluginDeviceTemplate" renamed to "SamsungMdcDevice"
	/// </example>
    public class AtlonaOmeSw21TxDevice : AtlonaOmeDevice, ITxRoutingWithFeedback, IAtlonaRoutingPoll, IHdmiInput2, IUsbCInput1, IHasPowerControlWithFeedback
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        public ushort CurrentInput { get; private set; }
        private bool[] InputSync { get; set; }

        public BoolFeedback UsbCInput1SyncFeedback { get; private set; }
        public BoolFeedback HdmiInput2SyncFeedback { get; private set; }

        public IntFeedback VideoSourceNumericFeedback { get; private set; }
        public IntFeedback AudioSourceNumericFeedback { get; private set; }

        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }
	    public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

	    public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
	    {
	        RouteBySelector(inputSelector);
	    }

	    public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
	    {
	        if (input < 1) return;
	        var inputPort = InputPorts[input - 1];
	        if (inputPort == null) return;
	        var inputSelector = inputPort.Selector;
            RouteBySelector(inputSelector);
	    }

	    private void RouteBySelector(object selector)
	    {
	        var action = selector as Action;
	        if (action == null) return;

	        if (PowerIsOn)
	        {
	            action.Invoke();
	        }
	        else
	        {
	            PowerOn();
	            // ReSharper disable once ObjectCreationAsStatement
	            new CTimer(o => action.Invoke(), 5000);
	        }
	    }

	    public void UsbCInput1()
	    {
	        SendText("x1AVx1");
	    }
	    public void HdmiInput2()
	    {
	        SendText("x2AVx1");
	    }



		/// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		/// <param name="comms"></param>
        public AtlonaOmeSw21TxDevice(string key, string name, AtlonaOmeConfigObject config, IBasicCommunication comms)
			: base(key, name, config, comms, EndpointType.Tx)
		{
			Debug.Console(1, this, "Constructing new {0} instance", name);

            InputSync = new[]
            {
                false,
                false,
                false
            };

            VideoSourceNumericFeedback = new IntFeedback(() => CurrentInput);
            AudioSourceNumericFeedback = new IntFeedback(() => CurrentInput);
            UsbCInput1SyncFeedback = new BoolFeedback(() => InputSync[0]);
		    HdmiInput2SyncFeedback = new BoolFeedback(() => InputSync[1]);
            PowerIsOnFeedback = new BoolFeedback(() => PowerIsOn);


		    SyncFeedbacks = new FeedbackCollection<BoolFeedback>
		    {
		        UsbCInput1SyncFeedback,
		        HdmiInput2SyncFeedback
		    };

		    InputPorts = new RoutingPortCollection<RoutingInputPort>();
		    OutputPorts = new RoutingPortCollection<RoutingOutputPort>
		    {
                new RoutingOutputPort("HdBaseTOut", eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.DmCat, "1", this)
		    };
            //Change DisplayPort to UsbC when Essentials updates
            AddRoutingInputPort(new RoutingInputPort("usbCIn1", eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.DisplayPort, new Action(UsbCInput1), this ), "x1AVx1");
            AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn2, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(HdmiInput2), this ), "x2AVx1");

            Polls.Add(PollPower);
		}

        void AddRoutingInputPort(RoutingInputPort port, string fbMatch)
        {
            port.FeedbackMatchObject = fbMatch;
            InputPorts.Add(port);
        }

		public override void LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey, PepperDash.Essentials.Core.Bridges.EiscApiAdvanced bridge)
		{
			Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(2, this, "Linking to Atlona Endpoint: {0}", Name);

			var joinMap = new AtlonaTxJoinMap(joinStart);

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			if (bridge == null)
			{
				return;
			}

			bridge.AddJoinMap(Key, joinMap);

			trilist.SetUShortSigAction(joinMap.AudioVideoInput.JoinNumber,
				a => ExecuteNumericSwitch(a, 1, eRoutingSignalType.AudioVideo));
			VideoSourceNumericFeedback.LinkInputSig(
				trilist.UShortInput[joinMap.AudioVideoInput.JoinNumber]);
			
			UsbCInput1SyncFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Input1VideoSyncStatus.JoinNumber]);

			trilist.OnlineStatusChange += (s, a) =>
			{
				if (s == null) return;
				if (!a.DeviceOnLine) return;
				VideoSourceNumericFeedback.FireUpdate();
				UsbCInput1SyncFeedback.FireUpdate();
				HdmiInput2SyncFeedback.FireUpdate();
			};

			LinkEndpointToApi(this, trilist, joinMap);
		}

        /// <summary>
        /// This method should perform any necessary parsing of feedback messages from the device
        /// </summary>
        /// <param name="message"></param>
        protected override void ProcessFeedbackMessage(string message)
        {
            try
            {
                if (LastCommand.Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Console(0, this, "ProcessRouteResponse 1");
                    var newMessage = message;
                    if (message.IndexOf(",", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        newMessage = message.Substring(message.IndexOf(",", StringComparison.OrdinalIgnoreCase) + 1);
                    }
                    ProcessRouteResponse(newMessage);
                    return;
                }
                if (
                    LastCommand.First()
                        .ToString(CultureInfo.InvariantCulture)
                        .Equals("x", StringComparison.OrdinalIgnoreCase) &&
                    LastCommand[4].ToString(CultureInfo.InvariantCulture)
                        .Equals("x", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessRouteResponse(message);
                    return;
                }
                if (message.IndexOf("inputstatus", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ProcessInputStatus(message);
                }
                if (message.IndexOf("PWON", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    PowerIsOn = true;
                    return;
                }
                if (message.IndexOf("PWOFF", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    PowerIsOn = false;
                }

            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "ProcessFeedbackMessage : \"{1}\" :: [Sw21Tx] Error : {0}", ex.Message, message);
            }
        }

	    public void PollRouteStatus()
	    {
	        SendText("Status");
	    }

	    public void PollInputStatus()
	    {
	        SendText("InputStatus");
	    }

        public void PollPower()
        {
            SendText("PWSTA");
        }


	    private void ProcessInputStatus(string message)
	    {
	        if (message.IndexOf(" ", StringComparison.Ordinal) < 0) return;
	        const string prefix = "InputStatus";
	        var status = PullDataFromPrefix(prefix, message);
	        for (var i = 0; i < status.Length; i++)
	        {
	            InputSync[i] = status[i] == '1';
	        }
	        foreach (var feedback in SyncFeedbacks)
	        {
	            var fb = feedback;
                fb.FireUpdate();
	        }
	    }

	    private void ProcessRouteResponse(string message)
	    {
            try
            {
                var newInput = InputPorts.FirstOrDefault(i => i.FeedbackMatchObject.Equals(message));
                if (newInput == null) return;
                var inputIndex = InputPorts.IndexOf(newInput);
                if (inputIndex <= -1) return;
                CurrentInput = (ushort)(inputIndex + 1);
                VideoSourceNumericFeedback.FireUpdate();
                AudioSourceNumericFeedback.FireUpdate();
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "Error In Process Route Response : {0}", ex.Message);
                throw;
            }
	    }

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
}

