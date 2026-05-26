using System;
using System.Collections.Generic;
using System.Linq;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI.ViewModels
{
    public sealed class CommunicationDeviceItemViewModel : NotifyBase
    {
        private readonly CommunicationDeviceConfig _config;
        private bool _suppressChangeNotification;

        public event EventHandler ConfigChanged;
        public bool IsUpdatingFromStatus { get; private set; }

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
        public CommunicationDeviceRuntimeState RuntimeState { get; private set; }
        public string LastError { get; private set; }

        public string StatusText
        {
            get
            {
                switch (RuntimeState)
                {
                    case CommunicationDeviceRuntimeState.Connected:
                        return "已连接";
                    case CommunicationDeviceRuntimeState.Connecting:
                        return "连接中";
                    case CommunicationDeviceRuntimeState.Disconnecting:
                        return "断开中";
                    case CommunicationDeviceRuntimeState.Faulted:
                        return string.IsNullOrWhiteSpace(LastError) ? "故障" : $"故障：{LastError}";
                    case CommunicationDeviceRuntimeState.Disabled:
                        return "已关闭";
                    default:
                        return "未连接";
                }
            }
        }

        public UartDriverConfigViewModel DriverConfig { get; private set; }
        public TcpDriverConfigViewModel TcpDriverConfig { get; private set; }

        public CommunicationDeviceItemViewModel(CommunicationDeviceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            RuntimeState = _config.IsEnabled
                ? CommunicationDeviceRuntimeState.Disconnected
                : CommunicationDeviceRuntimeState.Disabled;
            if (_config.DriverName == "UART")
                DriverConfig = new UartDriverConfigViewModel(_config);
            else if (_config.DriverName == "TCP")
                TcpDriverConfig = new TcpDriverConfigViewModel(_config);
        }

        public void EnsureDriverConfig()
        {
            if (_config.DriverName == "UART" && DriverConfig == null)
                DriverConfig = new UartDriverConfigViewModel(_config);
            else if (_config.DriverName == "TCP" && TcpDriverConfig == null)
                TcpDriverConfig = new TcpDriverConfigViewModel(_config);
        }

        public void RefreshStatus(bool isConnected)
        {
            IsConnected = isConnected;
            RuntimeState = isConnected
                ? CommunicationDeviceRuntimeState.Connected
                : IsEnabled
                    ? CommunicationDeviceRuntimeState.Disconnected
                    : CommunicationDeviceRuntimeState.Disabled;
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(RuntimeState));
            OnPropertyChanged(nameof(StatusText));
        }

        public void ApplyStatus(CommunicationDeviceStatus status)
        {
            if (status == null || status.DeviceId != DeviceId) return;
            _suppressChangeNotification = true;
            IsUpdatingFromStatus = true;
            try
            {
                _config.IsEnabled = status.IsEnabled;
                IsConnected = status.IsConnected;
                RuntimeState = status.State;
                LastError = status.LastError;
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(RuntimeState));
                OnPropertyChanged(nameof(LastError));
                OnPropertyChanged(nameof(StatusText));
            }
            finally
            {
                IsUpdatingFromStatus = false;
                _suppressChangeNotification = false;
            }
        }

        public void LoadConfig(CommunicationDeviceConfig config)
        {
            if (config == null) return;
            _suppressChangeNotification = true;
            try
            {
                _config.DisplayName = config.DisplayName;
                _config.IsEnabled = config.IsEnabled;
                _config.DriverName = config.DriverName;
                if (config.DriverSettings != null)
                    _config.DriverSettings = new Dictionary<string, string>(config.DriverSettings);
                if (config.Blocks != null && config.Blocks.Count > 0)
                    _config.Blocks = config.Blocks.Select(b => b.Clone()).ToList();
                DriverConfig?.LoadFrom(_config);
                TcpDriverConfig?.LoadFrom(_config);
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DriverName));
                OnPropertyChanged(nameof(IsEnabled));
                RuntimeState = _config.IsEnabled
                    ? CommunicationDeviceRuntimeState.Disconnected
                    : CommunicationDeviceRuntimeState.Disabled;
                OnPropertyChanged(nameof(RuntimeState));
                OnPropertyChanged(nameof(StatusText));
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
