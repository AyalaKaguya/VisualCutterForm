using System;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI.ViewModels
{
    public sealed class CommunicationDeviceItemViewModel : NotifyBase
    {
        private readonly CommunicationDeviceConfig _config;
        private bool _suppressChangeNotification;

        public event EventHandler ConfigChanged;

        public string DeviceId => _config.DeviceId;
        public string DriverName => _config.DriverName;

        public string DisplayName
        {
            get => _config.DisplayName;
            set
            {
                if (_config.DisplayName != value)
                {
                    _config.DisplayName = value;
                    OnPropertyChanged();
                    NotifyConfigChanged();
                }
            }
        }

        public bool IsEnabled
        {
            get => _config.IsEnabled;
            set
            {
                if (_config.IsEnabled != value)
                {
                    _config.IsEnabled = value;
                    OnPropertyChanged();
                    NotifyConfigChanged();
                }
            }
        }

        public bool IsConnected { get; private set; }

        public string StatusText => IsConnected ? "已连接" : "未连接";

        public UartDriverConfigViewModel DriverConfig { get; private set; }

        public CommunicationDeviceItemViewModel(CommunicationDeviceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (_config.DriverName == "UART")
                DriverConfig = new UartDriverConfigViewModel(_config);
        }

        public void EnsureDriverConfig()
        {
            if (DriverConfig == null && _config.DriverName == "UART")
                DriverConfig = new UartDriverConfigViewModel(_config);
        }

        public void RefreshStatus(bool isConnected)
        {
            IsConnected = isConnected;
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(StatusText));
        }

        public void LoadConfig(CommunicationDeviceConfig config)
        {
            if (config == null) return;
            _suppressChangeNotification = true;
            try
            {
                _config.DisplayName = config.DisplayName;
                _config.IsEnabled = config.IsEnabled;
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(IsEnabled));
            }
            finally
            {
                _suppressChangeNotification = false;
            }
        }

        public CommunicationDeviceConfig GetConfig() => _config.Clone();

        private void NotifyConfigChanged()
        {
            if (!_suppressChangeNotification)
                ConfigChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
