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

        public ICommand RefreshPortsCommand { get; }

        public string PortName
        {
            get => _portName;
            set { if (SetField(ref _portName, value)) Notify(); }
        }

        public string BaudRate
        {
            get => _baudRate;
            set { if (SetField(ref _baudRate, value)) Notify(); }
        }

        public string DataBits
        {
            get => _dataBits;
            set { if (SetField(ref _dataBits, value)) Notify(); }
        }

        public string Parity
        {
            get => _parity;
            set { if (SetField(ref _parity, value)) Notify(); }
        }

        public string StopBits
        {
            get => _stopBits;
            set { if (SetField(ref _stopBits, value)) Notify(); }
        }

        public string Handshake
        {
            get => _handshake;
            set { if (SetField(ref _handshake, value)) Notify(); }
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
            _baudRate  = GetSetting(settings, "BaudRate", "9600");
            _dataBits  = GetSetting(settings, "DataBits", "8");
            _parity    = GetSetting(settings, "Parity", "None");
            _stopBits  = GetSetting(settings, "StopBits", "One");
            _handshake = GetSetting(settings, "Handshake", "None");
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
    }
}
