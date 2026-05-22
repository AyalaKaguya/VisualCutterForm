using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI.ViewModels
{
    public sealed class TcpDriverConfigViewModel : NotifyBase
    {
        private string _ipAddress;
        private string _port;
        private string _blockName;

        public event EventHandler ConfigChanged;

        public string IpAddress
        {
            get => _ipAddress;
            set { if (SetField(ref _ipAddress, value)) Notify(); }
        }

        public string Port
        {
            get => _port;
            set { if (SetField(ref _port, value)) Notify(); }
        }

        public string BlockName
        {
            get => _blockName;
            set { if (SetField(ref _blockName, value)) Notify(); }
        }

        public ICommand RefreshCommand { get; }

        public TcpDriverConfigViewModel(CommunicationDeviceConfig config)
        {
            RefreshCommand = new RelayCommand(() => { });
            LoadFrom(config);
        }

        public void LoadFrom(CommunicationDeviceConfig config)
        {
            if (config == null) return;

            var settings = config.DriverSettings ?? new Dictionary<string, string>();
            _ipAddress = GetSetting(settings, "IpAddress", "127.0.0.1");
            _port      = GetSetting(settings, "Port", "502");
            _blockName = config.Blocks?.FirstOrDefault()?.Name ?? "TCP 数据";

            OnPropertyChanged(null);
        }

        public void ToDeviceConfig(CommunicationDeviceConfig config)
        {
            if (config == null) return;
            if (config.DriverSettings == null)
                config.DriverSettings = new Dictionary<string, string>();

            config.DriverSettings["IpAddress"] = _ipAddress ?? "127.0.0.1";
            config.DriverSettings["Port"]      = _port ?? "502";

            if (config.Blocks?.Count > 0)
            {
                config.Blocks[0].Name = _blockName ?? "TCP 数据";
                config.Blocks[0].Address = $"{_ipAddress}:{_port}";
            }
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
