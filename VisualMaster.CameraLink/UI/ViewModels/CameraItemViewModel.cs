using VisualMaster.CameraLink.Api;
using System;

namespace VisualMaster.CameraLink.UI.ViewModels
{
    /// <summary>
    /// 单台相机的 ViewModel，关联配置与实时状态。
    /// </summary>
    public sealed class CameraItemViewModel : NotifyBase
    {
        private readonly CameraDeviceConfig _config;
        private CameraDeviceStatus _status;
        private bool _suppressChangeNotification;

        public event EventHandler ConfigChanged;

        public string DeviceId => _config.DeviceId;

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

        public bool IsConnected => _status?.IsConnected == true;
        public bool IsGrabbing  => _status?.IsGrabbing  == true;

        public Action<CameraItemViewModel> IsEnabledChangedCallback { get; set; }

        public bool IsEnabled
        {
            get => _config.IsEnabled;
            set
            {
                if (_config.IsEnabled != value)
                {
                    _config.IsEnabled = value;
                    OnPropertyChanged();
                    IsEnabledChangedCallback?.Invoke(this);
                    NotifyConfigChanged();
                }
            }
        }

        public string StatusText
        {
            get
            {
                if (IsGrabbing)  return "采集中";
                if (IsConnected) return "已连接";
                return "未连接";
            }
        }

        public string AssignedSerial => _status?.AssignedCamera?.SerialNumber
            ?? _config.AssignedSerial ?? "";

        public string ModelName => _status?.AssignedCamera?.ModelName ?? "";

        /// <summary>相机的参数配置（对 UI 直接绑定）。</summary>
        public CameraConfigViewModel ConfigVm { get; }

        public CameraItemViewModel(CameraDeviceConfig config)
        {
            _config   = config ?? throw new ArgumentNullException(nameof(config));
            ConfigVm  = new CameraConfigViewModel(config.Settings ?? new CameraSettings());
            ConfigVm.SettingsChanged += OnSettingsChanged;
        }

        public void RefreshStatus(CameraDeviceStatus status)
        {
            _status = status;
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsGrabbing));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(AssignedSerial));
            OnPropertyChanged(nameof(ModelName));
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            // 将 ViewModel 更改写回 Config.Settings（供 Manager 提取）
            _config.Settings = ConfigVm.ToSettings();
            NotifyConfigChanged();
        }

        public void LoadConfig(CameraDeviceConfig config)
        {
            if (config == null) return;
            _suppressChangeNotification = true;
            try
            {
                _config.DisplayName = config.DisplayName;
                _config.AssignedSerial = config.AssignedSerial;
                _config.IsEnabled = config.IsEnabled;
                _config.Settings = config.Settings?.Clone() ?? new CameraSettings();
                ConfigVm.LoadFrom(_config.Settings);
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(AssignedSerial));
            }
            finally
            {
                _suppressChangeNotification = false;
            }
        }

        public CameraDeviceConfig GetConfig() => _config.Clone();

        private void NotifyConfigChanged()
        {
            if (!_suppressChangeNotification)
                ConfigChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
