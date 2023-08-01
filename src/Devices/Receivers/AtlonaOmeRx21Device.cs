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


namespace AtlonaOme.Devices.Receivers
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
    public class AtlonaOmeRx21Device : AtlonaOmeDevice, IHdBaseTInput1, IHdmiInput2, IRoutingFeedback, IAtlonaRoutingPoll
    {
        public ushort CurrentInput { get; private set; }
        private bool[] InputSync { get; set; }
        public BoolFeedback HdBaseTInput1SyncFeedback { get; private set; }
        public BoolFeedback HdmiInput2SyncFeedback { get; private set; }

        public IntFeedback AudioVideoSourceNumericFeedback { get; private set; }

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
                // ReSharper disable once ObjectCreationAsStatement
                new CTimer(o => action.Invoke(), 5000);
            }
        }


        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        public AtlonaOmeRx21Device(string key, string name, AtlonaOmeConfigObject config, IBasicCommunication comms)
            : base(key, name, config, comms, EndpointType.Rx)
        {
			Debug.Console(1, this, "Constructing new {0} instance", name);

            InputSync = new[]
            {
                false,
                false
            };

            AudioVideoSourceNumericFeedback = new IntFeedback(() => CurrentInput);
            HdBaseTInput1SyncFeedback = new BoolFeedback(() => InputSync[0]);
            HdmiInput2SyncFeedback = new BoolFeedback(() => InputSync[1]);

            SyncFeedbacks = new FeedbackCollection<BoolFeedback>
            {
		        HdBaseTInput1SyncFeedback,
		        HdmiInput2SyncFeedback
		    };

            InputPorts = new RoutingPortCollection<RoutingInputPort>();
            OutputPorts = new RoutingPortCollection<RoutingOutputPort>
		    {
                new RoutingOutputPort(RoutingPortNames.HdmiOut, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, "1", this)
		    };
            //Change DisplayPort to UsbC when Essentials updates
            AddRoutingInputPort(new RoutingInputPort("hdBaseTIn1", eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.DmCat, new Action(HdBaseTInput1), this), "x1AVx1");
            AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn2, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(HdmiInput2), this), "x2AVx1");
        }

        void AddRoutingInputPort(RoutingInputPort port, string fbMatch)
        {
            port.FeedbackMatchObject = fbMatch;
            InputPorts.Add(port);
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
                    var newMessage = message;
                    if (message.IndexOf(",", StringComparison.OrdinalIgnoreCase) > -1)
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
                if (message.IndexOf("inputstatus", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    ProcessInputStatus(message);
                }
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "ProcessFeedbackMessage : \"{1}\" :: [Rx21] Error : {0}", ex.Message, message);
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

        private void ProcessInputStatus(string message)
        {
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
            var newInput = InputPorts.FirstOrDefault(i => i.FeedbackMatchObject.Equals(message));
            if (newInput == null) return;
            var inputIndex = InputPorts.IndexOf(newInput);
            if (inputIndex <= -1) return;
            CurrentInput = (ushort)(inputIndex + 1);
            AudioVideoSourceNumericFeedback.FireUpdate();
        }

        public void HdBaseTInput1()
        {
            SendText("x1AVx1");
        }

        public void HdmiInput2()
        {
            SendText("x2AVx1");
        }
    }
}

