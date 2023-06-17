using PepperDash.Essentials.Core;

namespace AtlonaOme
{
    public interface IUsbCInput1 : IInput1
    {
        void UsbCInput1();
        BoolFeedback UsbCInput1SyncFeedback { get; }
    }
    public interface IHdmiInput1 : IInput1
    {
        void HdmiInput1();
        BoolFeedback HdmiInput1SyncFeedback { get; }
    }
    public interface IHdmiInput2 : IInput2
    {
        void HdmiInput2();
        BoolFeedback HdmiInput2SyncFeedback { get; }
    }
    public interface IHdmiInput3 : IInput3
    {
        void HdmiInput3();
        BoolFeedback HdmiInput3SyncFeedback { get; }
    }

    public interface IHdBaseTInput1 : IInput1
    {
        void HdBaseTInput1();
        BoolFeedback HdBaseTInput1SyncFeedback { get; }
    }
    public interface IHdBaseTInput2 : IInput2
    {
        void HdBaseTInput2();
        BoolFeedback HdBaseTInput2SyncFeedback { get; }
    }

    public interface IInput1
    {
    }
    public interface IInput2
    {
    }
    public interface IInput3
    {
    }

    public interface IRoutingFeedback : IRoutingNumeric
    {
        IntFeedback AudioVideoSourceNumericFeedback { get;} 
    }
}