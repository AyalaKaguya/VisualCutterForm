using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Windows.Input;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI.ViewModels
{
    public sealed class UartDriverConfigViewModel : NotifyBase
    {
        private static readonly IReadOnlyList<string> BaudRateOptionsInternal = new[] { "9600", "19200", "38400", "57600", "115200" };
        private static readonly IReadOnlyList<string> DataBitsOptionsInternal = new[] { "5", "6", "7", "8" };
        private static readonly IReadOnlyList<string> ParityOptionsInternal = new[] { "None", "Odd", "Even", "Mark", "Space" };
        private static readonly IReadOnlyList<string> StopBitsOptionsInternal = new[] { "One", "Two", "OnePointFive" };
        private static readonly IReadOnlyList<string> HandshakeOptionsInternal = new[] { "None", "XOnXOff", "RequestToSend", "RequestToSendXOnXOff" };

        private string _portName;
        private string _baudRate;
        private string _dataBits;
        private string _parity;
        private string _stopBits;
        private string _handshake;
        private string _blockName;

        public event EventHandler ConfigChanged;
        public event EventHandler RealtimeRequested;

        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>();

    public IReadOnlyList<string> BaudRateOptions => BaudRateOptionsInternal;
    public IReadOnlyList<string> DataBitsOptions => DataBitsOptionsInternal;
    public IReadOnlyList<string> ParityOptions => ParityOptionsInternal;
    public IReadOnlyList<string> StopBitsOptions => StopBitsOptionsInternal;
    public IReadOnlyList<string> HandshakeOptions => HandshakeOptionsInternal;

        public ICommand RefreshPortsCommand { get; }

        public string PortName
        {
            get => _portName;
            set { if (SetField(ref _portName, value)) Notify(); }
        }

        public string BaudRate
        {
            get => _baudRate;
            set
            {
                var normalized = NormalizeOption(value, BaudRateOptionsInternal, "9600");
                if (SetField(ref _baudRate, normalized)) Notify();
            }
        }

        public string DataBits
        {
            get => _dataBits;
            set
            {
                var normalized = NormalizeOption(value, DataBitsOptionsInternal, "8");
                if (!SetField(ref _dataBits, normalized))
                    return;

                if (!string.Equals(_dataBits, "5", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(_stopBits, "OnePointFive", StringComparison.OrdinalIgnoreCase))
                {
                    _stopBits = "One";
                    OnPropertyChanged(nameof(StopBits));
                }

                Notify();
            }
        }

        public string Parity
        {
            get => _parity;
            set
            {
                var normalized = NormalizeOption(value, ParityOptionsInternal, "None");
                if (SetField(ref _parity, normalized)) Notify();
            }
        }

        public string StopBits
        {
            get => _stopBits;
            set
            {
                var normalized = NormalizeOption(value, StopBitsOptionsInternal, "One");
                if (string.Equals(normalized, "OnePointFive", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(_dataBits, "5", StringComparison.OrdinalIgnoreCase))
                {
                    normalized = "One";
                }

                if (SetField(ref _stopBits, normalized)) Notify();
            }
        }

        public string Handshake
        {
            get => _handshake;
            set
            {
                var normalized = NormalizeOption(value, HandshakeOptionsInternal, "None");
                if (SetField(ref _handshake, normalized)) Notify();
            }
        }

        public string BlockName
        {
            get => _blockName;
            set { if (SetField(ref _blockName, value)) Notify(); }
        }

        public UartDriverConfigViewModel(CommunicationDeviceConfig config)
        {
            RefreshPortsCommand = new RelayCommand(ExecuteRefreshPorts);
            LoadFrom(config);
        }

        public void LoadFrom(CommunicationDeviceConfig config)
        {
            if (config == null) return;

            var settings = config.DriverSettings ?? new Dictionary<string, string>();
            _portName  = GetSetting(settings, "PortName", "COM1");
            _baudRate  = NormalizeOption(GetSetting(settings, "BaudRate", "9600"), BaudRateOptionsInternal, "9600");
            _dataBits  = NormalizeOption(GetSetting(settings, "DataBits", "8"), DataBitsOptionsInternal, "8");
            _parity    = NormalizeOption(GetSetting(settings, "Parity", "None"), ParityOptionsInternal, "None");
            _stopBits  = NormalizeOption(GetSetting(settings, "StopBits", "One"), StopBitsOptionsInternal, "One");
            _handshake = NormalizeOption(GetSetting(settings, "Handshake", "None"), HandshakeOptionsInternal, "None");
            if (!string.Equals(_dataBits, "5", StringComparison.OrdinalIgnoreCase)
                && string.Equals(_stopBits, "OnePointFive", StringComparison.OrdinalIgnoreCase))
            {
                _stopBits = "One";
            }
            _blockName = config.Blocks?.FirstOrDefault()?.Name ?? "串口数据";

            ExecuteRefreshPorts();
            OnPropertyChanged(null);
        }

        public void ToDeviceConfig(CommunicationDeviceConfig config)
        {
            if (config == null) return;
            if (config.DriverSettings == null)
                config.DriverSettings = new Dictionary<string, string>();

            config.DriverSettings["PortName"]  = _portName ?? "COM1";
            config.DriverSettings["BaudRate"]  = _baudRate ?? "9600";
            config.DriverSettings["DataBits"]  = _dataBits ?? "8";
            config.DriverSettings["Parity"]    = _parity ?? "None";
            config.DriverSettings["StopBits"]  = _stopBits ?? "One";
            config.DriverSettings["Handshake"] = _handshake ?? "None";

            if (config.Blocks?.Count > 0)
            {
                config.Blocks[0].Name = _blockName ?? "串口数据";
                if (!string.IsNullOrWhiteSpace(_portName))
                    config.Blocks[0].Address = _portName;
            }
        }

        private void ExecuteRefreshPorts()
        {
            AvailablePorts.Clear();
            foreach (var port in SerialPort.GetPortNames())
                AvailablePorts.Add(port);
        }

        private void Notify() => ConfigChanged?.Invoke(this, EventArgs.Empty);

        private static string GetSetting(Dictionary<string, string> settings, string key, string fallback)
        {
            return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
        }

        private static string NormalizeOption(string value, IReadOnlyList<string> options, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var match = options.FirstOrDefault(option => string.Equals(option, value, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                    return match;
            }

            return fallback;
        }
    }
}
