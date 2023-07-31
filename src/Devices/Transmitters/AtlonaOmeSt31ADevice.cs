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
    public class AtlonaOmeSt31ADevice : AtlonaOmeDevice, ITxRoutingWithFeedback, IAtlonaRoutingPoll, IHdmiInput2, IHdmiInput3, IUsbCInput1
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        public ushort CurrentInput { get; private set; }
        private bool[] InputSync { get; set; }
        public BoolFeedback UsbCInput1SyncFeedback { get; private set; }
        public BoolFeedback HdmiInput2SyncFeedback { get; private set; }
        public BoolFeedback HdmiInput3SyncFeedback { get; private set; }

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
	    public void HdmiInput3()
	    {
	        SendText("x3AVx1");
	    }



		/// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		/// <param name="comms"></param>
        public AtlonaOmeSt31ADevice(string key, string name, AtlonaOmeConfigObject config, IBasicCommunication comms)
			: base(key, name, config, comms, EndpointType.Tx)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);

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
		    HdmiInput3SyncFeedback = new BoolFeedback(() => InputSync[2]);

		    SyncFeedbacks = new FeedbackCollection<BoolFeedback>
		    {
		        UsbCInput1SyncFeedback,
		        HdmiInput2SyncFeedback,
		        HdmiInput3SyncFeedback
		    };

		    InputPorts = new RoutingPortCollection<RoutingInputPort>();
		    OutputPorts = new RoutingPortCollection<RoutingOutputPort>
		    {
                new RoutingOutputPort("HdBaseTOut", eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.DmCat, "1", this)
		    };
            //Change DisplayPort to UsbC when Essentials updates
            AddRoutingInputPort(new RoutingInputPort("usbCIn1", eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.DisplayPort, new Action(UsbCInput1), this ), "x1AVx1");
            AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn2, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(HdmiInput2), this ), "x2AVx1");
            AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn3, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(HdmiInput3), this), "x3AVx1");

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
                    Debug.Console(0, this, "ProcessRouteResponse 2");

                    ProcessRouteResponse(message);
                    return;
                }
                if (message.IndexOf("inputstatus", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.Console(0, this, "ProcessInputStatus");
                    ProcessInputStatus(message);
                }
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, "ProcessFeedbackMessage [St31A] Error : {0}", ex.Message);
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
    }
}

