using System.Collections.Generic;
using VisualMaster.Api;
using VisualCutterForm.Legacy;

namespace VisualMaster.WorkFlow
{
    public interface ISerialRuntimeService
    {
        bool IsSerialOpen(string portName);
        void ConnectSerial(string portName, int baudRate);
        void OutputResult(string portName, object data);
        string GetSerialPortName(string deviceId);
        int GetSerialBaudRate(string deviceId);
        IReadOnlyDictionary<string, ISerialPort> SerialPorts { get; }
    }
}