using VisualMaster.Api;
using System.Collections.Generic;
using System.Threading.Tasks;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.WorkFlow
{
    public interface IFlowServiceProvider
    {
        string GetFirstActiveSerial();
        ImageFifo GetFifo(string serial);
        bool IsSerialOpen(string portName);
        void ConnectSerial(string portName, int baudRate);
        void OutputResult(string portName, object data);
        CameraSlot GetCameraSlot(string serial);
        bool TriggerCommunication(string portName, SerialTriggerRule rule);
        void OnSerialDataReceived(string portName, string data);
        SerialSlot GetSerialSlot(string slotId);
        IReadOnlyDictionary<string, ISerialPort> SerialPorts { get; }
        string GetFirstSlotId();
    }
}
