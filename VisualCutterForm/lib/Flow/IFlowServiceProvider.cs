using VisualMaster.Api;
using System.Threading.Tasks;
using VisualCutterForm.Lib.Flow.Data;

namespace VisualCutterForm.Lib.Flow
{
    public interface IFlowServiceProvider
    {
        string GetFirstActiveSerial();
        ImageFifo GetFifo(string serial);
        bool IsSerialOpen(string portName);
        void ConnectSerial(string portName, int baudRate);
        void OutputResult(string portName, object data);
        VisionController.CameraSlot GetCameraSlot(string serial);
        bool TriggerCommunication(string portName, SerialTriggerRule rule);
        void OnSerialDataReceived(string portName, string data);
    }
}
