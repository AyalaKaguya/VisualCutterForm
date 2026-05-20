using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VisualMaster.Api;
using VisualMaster.Communication;

namespace VisualMaster.Forms
{
    internal sealed class VisionSerialRuntime : IDisposable
    {
        private readonly List<SerialDeviceConfig> _serialDevices;
        private readonly Dictionary<string, ISerialPort> _serialPorts;
        private readonly IReadOnlyDictionary<string, ISerialPort> _readOnlyPorts;
        private readonly Action<Exception> _errorHandler;

        public VisionSerialRuntime(Action<Exception> errorHandler)
        {
            _serialDevices = new List<SerialDeviceConfig>();
            _serialPorts = new Dictionary<string, ISerialPort>();
            _readOnlyPorts = new ReadOnlyDictionary<string, ISerialPort>(_serialPorts);
            _errorHandler = errorHandler;
        }

        public IReadOnlyDictionary<string, ISerialPort> SerialPorts => _readOnlyPorts;
        public ISerialPort SerialPort => _serialPorts.Values.FirstOrDefault();

        public SerialSlot GetSerialSlot(string slotId)
        {
            var device = GetSerialDeviceConfig(slotId);
            if (device == null)
                return null;

            return new SerialSlot
            {
                SlotId = device.DeviceId,
                SlotName = device.DisplayName,
                PortName = device.PortName,
                BaudRate = device.BaudRate,
                DataBits = device.DataBits,
                Parity = device.Parity,
                StopBits = device.StopBits,
            };
        }

        public string GetSerialPortName(string slotId)
        {
            return GetSerialSlot(slotId)?.PortName;
        }

        public int GetSerialBaudRate(string slotId)
        {
            return GetSerialSlot(slotId)?.BaudRate ?? 9600;
        }

        public List<SerialSlot> GetSerialSlots()
        {
            return _serialDevices.Select(device => GetSerialSlot(device.DeviceId)).Where(slot => slot != null).ToList();
        }

        public IReadOnlyList<SerialDeviceConfig> GetSerialDeviceConfigs()
        {
            return _serialDevices.Select(device => device.Clone()).ToList();
        }

        public SerialDeviceConfig GetSerialDeviceConfig(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return null;

            return _serialDevices.FirstOrDefault(device => device.DeviceId == deviceId)?.Clone();
        }

        public SerialDeviceConfig AddSerialDevice(string displayName, string portName = null)
        {
            var baseName = string.IsNullOrWhiteSpace(displayName) ? "通信设备" : displayName.Trim();
            var device = new SerialDeviceConfig
            {
                DeviceId = System.Guid.NewGuid().ToString(),
                DisplayName = baseName,
                PortName = string.IsNullOrWhiteSpace(portName) ? string.Empty : portName.Trim(),
            };
            _serialDevices.Add(device);
            return device.Clone();
        }

        public void RemoveSerialDevice(string deviceId)
        {
            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == deviceId);
            if (device == null)
                return;

            if (!string.IsNullOrWhiteSpace(device.PortName))
                DisconnectSerial(device.PortName);

            _serialDevices.Remove(device);
        }

        public void UpdateSerialDevice(SerialDeviceConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.DeviceId))
                return;

            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == config.DeviceId);
            if (device == null)
            {
                _serialDevices.Add(config.Clone());
                return;
            }

            var portChanged = !string.Equals(device.PortName, config.PortName, System.StringComparison.OrdinalIgnoreCase);
            if (portChanged && !string.IsNullOrWhiteSpace(device.PortName))
                DisconnectSerial(device.PortName);

            device.DisplayName = string.IsNullOrWhiteSpace(config.DisplayName) ? device.DisplayName : config.DisplayName.Trim();
            device.PortName = string.IsNullOrWhiteSpace(config.PortName) ? string.Empty : config.PortName.Trim();
            device.BaudRate = config.BaudRate;
            device.DataBits = config.DataBits;
            device.Parity = string.IsNullOrWhiteSpace(config.Parity) ? "None" : config.Parity;
            device.StopBits = string.IsNullOrWhiteSpace(config.StopBits) ? "One" : config.StopBits;
        }

        public bool IsSerialDeviceConnected(string deviceId)
        {
            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == deviceId);
            return device != null && IsSerialOpen(device.PortName);
        }

        public void ConnectSerialDevice(string deviceId)
        {
            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == deviceId);
            if (device == null)
                throw new InvalidOperationException($"Serial device {deviceId} not found.");

            if (string.IsNullOrWhiteSpace(device.PortName))
                throw new InvalidOperationException("Serial device has no port assigned.");

            ConnectSerial(device.PortName, device.BaudRate, device.DataBits, device.Parity, device.StopBits);
        }

        public void DisconnectSerialDevice(string deviceId)
        {
            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == deviceId);
            if (device == null || string.IsNullOrWhiteSpace(device.PortName))
                return;

            DisconnectSerial(device.PortName);
        }

        public void ConnectSerial(string portName, int baudRate = 9600, int dataBits = 8, string parity = "None", string stopBits = "One")
        {
            EnsureSerialDeviceForPort(portName, baudRate, dataBits, parity, stopBits);

            if (_serialPorts.ContainsKey(portName))
                return;

            var sp = new SerialPortAdapter(portName, baudRate, ParseParity(parity), dataBits, ParseStopBits(stopBits));
            sp.ErrorOccurred += (s, e) => _errorHandler?.Invoke(e);
            sp.Open();
            _serialPorts[portName] = sp;
        }

        public void DisconnectSerial(string portName)
        {
            if (!_serialPorts.TryGetValue(portName, out var sp))
                return;

            sp.Close();
            sp.Dispose();
            _serialPorts.Remove(portName);
        }

        public void DisconnectAllSerials()
        {
            foreach (var name in _serialPorts.Keys.ToList())
                DisconnectSerial(name);
        }

        public bool IsSerialOpen(string portName)
        {
            return _serialPorts.TryGetValue(portName, out var sp) && sp.IsOpen;
        }

        public void OutputResult(string portName, string result)
        {
            if (string.IsNullOrEmpty(portName))
            {
                portName = _serialPorts.Keys.FirstOrDefault();
                if (portName == null)
                    throw new InvalidOperationException("No serial port connected.");
            }

            if (!_serialPorts.TryGetValue(portName, out var sp) || !sp.IsOpen)
                throw new InvalidOperationException($"Serial port {portName} is not connected.");

            sp.SendLine(result);
        }

        public void OutputResult(string portName, byte[] data)
        {
            if (string.IsNullOrEmpty(portName))
            {
                portName = _serialPorts.Keys.FirstOrDefault();
                if (portName == null)
                    throw new InvalidOperationException("No serial port connected.");
            }

            if (!_serialPorts.TryGetValue(portName, out var sp) || !sp.IsOpen)
                throw new InvalidOperationException($"Serial port {portName} is not connected.");

            sp.Send(data);
        }

        public void OutputResult(string portName, object data)
        {
            if (data is string s)
                OutputResult(portName, s);
            else if (data is byte[] b)
                OutputResult(portName, b);
        }

        public void LoadSerialDevices(IEnumerable<SerialDeviceConfig> serialConfigs)
        {
            DisconnectAllSerials();
            _serialDevices.Clear();

            if (serialConfigs == null)
                return;

            foreach (var config in serialConfigs)
            {
                var clone = config?.Clone();
                if (clone == null)
                    continue;

                if (string.IsNullOrWhiteSpace(clone.DeviceId))
                    clone.DeviceId = System.Guid.NewGuid().ToString();

                if (string.IsNullOrWhiteSpace(clone.DisplayName))
                    clone.DisplayName = clone.PortName;

                _serialDevices.Add(clone);

                if (string.IsNullOrWhiteSpace(clone.PortName))
                    continue;

                try
                {
                    ConnectSerial(clone.PortName, clone.BaudRate, clone.DataBits, clone.Parity, clone.StopBits);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            DisconnectAllSerials();
        }

        private static System.IO.Ports.Parity ParseParity(string parity)
        {
            switch (parity)
            {
                case "Even": return System.IO.Ports.Parity.Even;
                case "Odd": return System.IO.Ports.Parity.Odd;
                case "Mark": return System.IO.Ports.Parity.Mark;
                case "Space": return System.IO.Ports.Parity.Space;
                default: return System.IO.Ports.Parity.None;
            }
        }

        private static System.IO.Ports.StopBits ParseStopBits(string stopBits)
        {
            switch (stopBits)
            {
                case "Two": return System.IO.Ports.StopBits.Two;
                case "OnePointFive": return System.IO.Ports.StopBits.OnePointFive;
                default: return System.IO.Ports.StopBits.One;
            }
        }

        private void EnsureSerialDeviceForPort(string portName, int baudRate, int dataBits, string parity, string stopBits)
        {
            if (string.IsNullOrWhiteSpace(portName))
                return;

            var existing = _serialDevices.FirstOrDefault(device => string.Equals(device.PortName, portName, System.StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.BaudRate = baudRate;
                existing.DataBits = dataBits;
                existing.Parity = string.IsNullOrWhiteSpace(parity) ? "None" : parity;
                existing.StopBits = string.IsNullOrWhiteSpace(stopBits) ? "One" : stopBits;
                if (string.IsNullOrWhiteSpace(existing.DisplayName))
                    existing.DisplayName = portName;
                return;
            }

            _serialDevices.Add(new SerialDeviceConfig
            {
                DeviceId = System.Guid.NewGuid().ToString(),
                DisplayName = portName,
                PortName = portName,
                BaudRate = baudRate,
                DataBits = dataBits,
                Parity = string.IsNullOrWhiteSpace(parity) ? "None" : parity,
                StopBits = string.IsNullOrWhiteSpace(stopBits) ? "One" : stopBits,
            });
        }
    }
}