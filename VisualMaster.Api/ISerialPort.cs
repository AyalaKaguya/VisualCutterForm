using System;

namespace VisualMaster.Api
{
    public interface ISerialPort : IDisposable
    {
        string PortName { get; }
        int BaudRate { get; }
        bool IsOpen { get; }

        void Open();
        void Close();
        void Send(string data);
        void Send(byte[] data);
        void SendLine(string line);

        event EventHandler<string> DataReceived;
        event EventHandler<byte[]> RawDataReceived;
        event EventHandler<Exception> ErrorOccurred;
    }
}
