using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace VisualMaster.Communication
{
    public class SerialPortAdapter : ISerialPort
    {
        private readonly SerialPort _port;

        public string PortName => _port.PortName;
        public int BaudRate => _port.BaudRate;
        public bool IsOpen => _port.IsOpen;

        public event EventHandler<string> DataReceived;
        public event EventHandler<byte[]> RawDataReceived;
        public event EventHandler<Exception> ErrorOccurred;

        public SerialPortAdapter(string portName, int baudRate = 9600,
            Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _port.NewLine = "\r\n";
            _port.DataReceived += OnDataReceived;
            _port.ErrorReceived += OnErrorReceived;
        }

        public void Open()
        {
            if (_port.IsOpen) return;
            _port.Open();
        }

        public void Close()
        {
            if (!_port.IsOpen) return;
            _port.Close();
        }

        public void Send(string data)
        {
            if (!_port.IsOpen) throw new InvalidOperationException("Serial port is not open.");
            _port.Write(data);
        }

        public void Send(byte[] data)
        {
            if (!_port.IsOpen) throw new InvalidOperationException("Serial port is not open.");
            _port.Write(data, 0, data.Length);
        }

        public void SendLine(string line)
        {
            if (!_port.IsOpen) throw new InvalidOperationException("Serial port is not open.");
            _port.WriteLine(line);
        }

        public void Dispose()
        {
            _port.DataReceived -= OnDataReceived;
            _port.ErrorReceived -= OnErrorReceived;
            if (_port.IsOpen)
                _port.Close();
            _port.Dispose();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_port.BytesToRead > 0)
                {
                    var buffer = new byte[_port.BytesToRead];
                    _port.Read(buffer, 0, buffer.Length);
                    RawDataReceived?.Invoke(this, buffer);

                    string text = _port.Encoding.GetString(buffer);
                    DataReceived?.Invoke(this, text);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorOccurred?.Invoke(this,
                new InvalidOperationException($"Serial port error: {e.EventType}"));
        }
    }

    public static class SerialPortUtility
    {
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
