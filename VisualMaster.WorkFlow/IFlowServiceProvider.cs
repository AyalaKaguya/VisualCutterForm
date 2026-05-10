using VisualMaster.Api;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VisualMaster.WorkFlow
{
    public interface IFlowServiceProvider
    {
        ImageFifo GetFifo(string serial);
        bool IsSerialOpen(string portName);
        void ConnectSerial(string portName, int baudRate);
        void OutputResult(string portName, object data);
        SerialSlot GetSerialSlot(string slotId);
        IReadOnlyDictionary<string, ISerialPort> SerialPorts { get; }
    }
}
