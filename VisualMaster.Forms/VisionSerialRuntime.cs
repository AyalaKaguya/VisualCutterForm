using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VisualMaster.Api;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;

namespace VisualMaster.Forms
{
    internal sealed class VisionSerialRuntime : IDisposable
    {
        private readonly List<SerialDeviceConfig> _serialDevices;
        private readonly Dictionary<string, ISerialPort> _serialPorts;
        private readonly IReadOnlyDictionary<string, ISerialPort> _readOnlyPorts;
        private readonly CommunicationSystemConfig _communicationConfig;
        private readonly CommunicationManager _communicationManager;
        private readonly Action<Exception> _errorHandler;

        public VisionSerialRuntime(Action<Exception> errorHandler)
        {
            _serialDevices = new List<SerialDeviceConfig>();
            _serialPorts = new Dictionary<string, ISerialPort>(StringComparer.OrdinalIgnoreCase);
            _readOnlyPorts = new ReadOnlyDictionary<string, ISerialPort>(_serialPorts);
            _communicationConfig = new CommunicationSystemConfig();
            _communicationManager = new CommunicationManager();
            _communicationManager.LoadConfig(_communicationConfig);
            _errorHandler = errorHandler;
        }

        public IReadOnlyDictionary<string, ISerialPort> SerialPorts => _readOnlyPorts;
        public ISerialPort SerialPort => _serialPorts.Values.FirstOrDefault();
        public CommunicationManager CommunicationManager => _communicationManager;

        public SerialSlot GetSerialSlot(string slotId)
        {
            var device = GetSerialDeviceConfig(slotId);
            if (device == null) return null;

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

        public string GetSerialPortName(string slotId) => GetSerialDeviceConfig(slotId)?.PortName;
        public int GetSerialBaudRate(string slotId) => GetSerialDeviceConfig(slotId)?.BaudRate ?? 9600;

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
            if (string.IsNullOrEmpty(deviceId)) return null;
            return _serialDevices.FirstOrDefault(device => device.DeviceId == deviceId)?.Clone();
        }

        public SerialDeviceConfig AddSerialDevice(string displayName, string portName = null)
        {
            var device = new SerialDeviceConfig
            {
                DeviceId = Guid.NewGuid().ToString("N"),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "通信设备" : displayName.Trim(),
                PortName = string.IsNullOrWhiteSpace(portName) ? string.Empty : portName.Trim(),
            };
            _serialDevices.Add(device);
            SyncCommunicationConfig();
            return device.Clone();
        }

        public void RemoveSerialDevice(string deviceId)
        {
            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == deviceId);
            if (device == null) return;
            DisconnectSerialDevice(device.DeviceId);
            _serialDevices.Remove(device);
            SyncCommunicationConfig();
        }

        public void UpdateSerialDevice(SerialDeviceConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.DeviceId)) return;

            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == config.DeviceId);
            if (device == null)
                _serialDevices.Add(config.Clone());
            else
            {
                bool portChanged = !string.Equals(device.PortName, config.PortName, StringComparison.OrdinalIgnoreCase);
                if (portChanged)
                    DisconnectSerialDevice(device.DeviceId);

                device.DisplayName = string.IsNullOrWhiteSpace(config.DisplayName) ? device.DisplayName : config.DisplayName.Trim();
                device.PortName = string.IsNullOrWhiteSpace(config.PortName) ? string.Empty : config.PortName.Trim();
                device.BaudRate = config.BaudRate;
                device.DataBits = config.DataBits;
                device.Parity = string.IsNullOrWhiteSpace(config.Parity) ? "None" : config.Parity;
                device.StopBits = string.IsNullOrWhiteSpace(config.StopBits) ? "One" : config.StopBits;
            }

            SyncCommunicationConfig();
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
                throw new InvalidOperationException($"Communication device {deviceId} not found.");
            ConnectSerial(device.PortName, device.BaudRate, device.DataBits, device.Parity, device.StopBits);
        }

        public void DisconnectSerialDevice(string deviceId)
        {
            var device = _serialDevices.FirstOrDefault(item => item.DeviceId == deviceId);
            if (device == null || string.IsNullOrWhiteSpace(device.PortName)) return;
            DisconnectSerial(device.PortName);
        }

        public void ConnectSerial(string portName, int baudRate = 9600, int dataBits = 8, string parity = "None", string stopBits = "One")
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new InvalidOperationException("Communication interface is empty.");

            EnsureSerialDeviceForPort(portName, baudRate, dataBits, parity, stopBits);
            SyncCommunicationConfig();

            var device = _serialDevices.First(d => string.Equals(d.PortName, portName, StringComparison.OrdinalIgnoreCase));
            _communicationManager.StartDeviceAsync(device.DeviceId).GetAwaiter().GetResult();
            if (!_serialPorts.ContainsKey(portName))
                _serialPorts[portName] = new CommunicationSerialPortView(_communicationManager, device, _errorHandler);
        }

        public void DisconnectSerial(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName)) return;

            var device = _serialDevices.FirstOrDefault(d => string.Equals(d.PortName, portName, StringComparison.OrdinalIgnoreCase));
            if (device != null)
                _communicationManager.StopDeviceAsync(device.DeviceId).GetAwaiter().GetResult();

            if (_serialPorts.TryGetValue(portName, out var port))
            {
                port.Dispose();
                _serialPorts.Remove(portName);
            }
        }

        public void DisconnectAllSerials()
        {
            foreach (var name in _serialPorts.Keys.ToList())
                DisconnectSerial(name);
        }

        public bool IsSerialOpen(string portName)
        {
            return _serialPorts.TryGetValue(portName, out var port) && port.IsOpen;
        }

        public void OutputResult(string portName, string result)
        {
            if (string.IsNullOrEmpty(portName))
                portName = _serialPorts.Keys.FirstOrDefault();
            if (string.IsNullOrEmpty(portName) || !_serialPorts.TryGetValue(portName, out var port))
                throw new InvalidOperationException("No communication device connected.");

            port.SendLine(result);
        }

        public void OutputResult(string portName, byte[] data)
        {
            if (string.IsNullOrEmpty(portName))
                portName = _serialPorts.Keys.FirstOrDefault();
            if (string.IsNullOrEmpty(portName) || !_serialPorts.TryGetValue(portName, out var port))
                throw new InvalidOperationException("No communication device connected.");

            port.Send(data);
        }

        public void OutputResult(string portName, object data)
        {
            if (data is string s) OutputResult(portName, s);
            else if (data is byte[] b) OutputResult(portName, b);
        }

        public void LoadSerialDevices(IEnumerable<SerialDeviceConfig> serialConfigs)
        {
            DisconnectAllSerials();
            _serialDevices.Clear();
            if (serialConfigs != null)
            {
                foreach (var config in serialConfigs)
                {
                    var clone = config?.Clone();
                    if (clone == null) continue;
                    if (string.IsNullOrWhiteSpace(clone.DeviceId))
                        clone.DeviceId = Guid.NewGuid().ToString("N");
                    if (string.IsNullOrWhiteSpace(clone.DisplayName))
                        clone.DisplayName = clone.PortName;
                    _serialDevices.Add(clone);
                }
            }
            SyncCommunicationConfig();
        }

        private void EnsureSerialDeviceForPort(string portName, int baudRate, int dataBits, string parity, string stopBits)
        {
            var existing = _serialDevices.FirstOrDefault(device => string.Equals(device.PortName, portName, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                _serialDevices.Add(new SerialDeviceConfig
                {
                    DeviceId = Guid.NewGuid().ToString("N"),
                    DisplayName = portName,
                    PortName = portName,
                    BaudRate = baudRate,
                    DataBits = dataBits,
                    Parity = string.IsNullOrWhiteSpace(parity) ? "None" : parity,
                    StopBits = string.IsNullOrWhiteSpace(stopBits) ? "One" : stopBits,
                });
                return;
            }

            existing.BaudRate = baudRate;
            existing.DataBits = dataBits;
            existing.Parity = string.IsNullOrWhiteSpace(parity) ? "None" : parity;
            existing.StopBits = string.IsNullOrWhiteSpace(stopBits) ? "One" : stopBits;
        }

        private void SyncCommunicationConfig()
        {
            var devices = _serialDevices.Select(ToCommunicationDevice).ToList();
            _communicationConfig.LoadFrom(devices);
            _communicationManager.LoadConfig(_communicationConfig);
        }

        private static CommunicationDeviceConfig ToCommunicationDevice(SerialDeviceConfig serial)
        {
            var interfaceName = string.IsNullOrWhiteSpace(serial.PortName) ? serial.DisplayName : serial.PortName;
            return new CommunicationDeviceConfig
            {
                DeviceId = serial.DeviceId,
                DisplayName = serial.DisplayName,
                DriverName = "UART",
                IsEnabled = true,
                DriverSettings = new Dictionary<string, string>
                {
                    ["PortName"] = serial.PortName ?? "",
                    ["BaudRate"] = serial.BaudRate.ToString(),
                    ["DataBits"] = serial.DataBits.ToString(),
                    ["Parity"] = string.IsNullOrWhiteSpace(serial.Parity) ? "None" : serial.Parity,
                    ["StopBits"] = string.IsNullOrWhiteSpace(serial.StopBits) ? "One" : serial.StopBits,
                },
                Blocks = new List<CommunicationBlockConfig>(),
            };
        }

        public void Dispose()
        {
            DisconnectAllSerials();
            _communicationManager.Dispose();
        }

        private sealed class CommunicationSerialPortView : ISerialPort
        {
            private readonly CommunicationManager _manager;
            private readonly SerialDeviceConfig _device;
            private readonly string _blockId;
            private readonly Action<Exception> _errorHandler;
            private bool _disposed;

            public CommunicationSerialPortView(CommunicationManager manager, SerialDeviceConfig device, Action<Exception> errorHandler)
            {
                _manager = manager;
                _device = device.Clone();
                _errorHandler = errorHandler;
                var driver = manager.Drivers.FirstOrDefault(d => d.DeviceId == device.DeviceId);
                _blockId = driver?.Blocks.FirstOrDefault()?.Config.BlockId;
                if (_blockId != null)
                    manager.SubscribeToBlock(_device.DeviceId, _blockId, OnBlockUpdated);
            }

            public string PortName => _device.PortName;
            public int BaudRate => _device.BaudRate;
            public bool IsOpen => !_disposed && _manager.Drivers.FirstOrDefault(d => d.DeviceId == _device.DeviceId)?.IsConnected == true;

            public event EventHandler<string> DataReceived;
            public event EventHandler<byte[]> RawDataReceived;
            public event EventHandler<Exception> ErrorOccurred;

            public void Open() { }
            public void Close() => Dispose();

            public void Send(string data)
            {
                Send(System.Text.Encoding.ASCII.GetBytes(data ?? ""));
            }

            public void Send(byte[] data)
            {
                try
                {
                    _manager.WriteBlockAsync(_device.DeviceId, _blockId, data ?? new byte[0]).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    _errorHandler?.Invoke(ex);
                }
            }

            public void SendLine(string line)
            {
                Send((line ?? "") + "\r\n");
            }

            private void OnBlockUpdated(object sender, CommunicationBlockUpdatedEventArgs e)
            {
                RawDataReceived?.Invoke(this, e.Data);
                DataReceived?.Invoke(this, System.Text.Encoding.ASCII.GetString(e.Data ?? new byte[0]));
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_blockId != null)
                    _manager.UnsubscribeFromBlock(_device.DeviceId, _blockId, OnBlockUpdated);
            }
        }
    }
}
