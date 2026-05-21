using VisualMaster.Api;
using VisualMaster.CameraLink.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace VisualMaster.CameraLink.UI.ViewModels
{
    /// <summary>
    /// 相机管理面板的顶层 ViewModel。
    /// 持有设备列表、发现到的相机列表，以及所有操作命令。
    /// </summary>
    public sealed class CameraManagerViewModel : NotifyBase, IDisposable
    {
        private readonly ICameraManager _manager;
        private readonly CameraSystemConfig _config;

        private CameraItemViewModel _selectedCamera;
        private DiscoveredCameraItemViewModel _selectedDiscovered;
        private bool _isBusy;
        private string _statusMessage;

        public ObservableCollection<CameraItemViewModel> Cameras { get; }
            = new ObservableCollection<CameraItemViewModel>();

        public ObservableCollection<DiscoveredCameraItemViewModel> DiscoveredDevices { get; }
            = new ObservableCollection<DiscoveredCameraItemViewModel>();

        public CameraItemViewModel SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (SetField(ref _selectedCamera, value))
                    RefreshCommands();
            }
        }

        public DiscoveredCameraItemViewModel SelectedDiscovered
        {
            get => _selectedDiscovered;
            set
            {
                if (SetField(ref _selectedDiscovered, value))
                    RefreshCommands();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetField(ref _isBusy, value))
                    RefreshCommands();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetField(ref _statusMessage, value);
        }

        // ── 命令 ──────────────────────────────────────────────────────
        public ICommand ScanCommand           { get; }
        public ICommand AddCameraCommand      { get; }
        public ICommand RemoveCameraCommand   { get; }
        public ICommand ConnectCommand        { get; }
        public ICommand DisconnectCommand     { get; }
        public ICommand StartGrabbingCommand  { get; }
        public ICommand StopGrabbingCommand   { get; }
        public ICommand SaveCommand           { get; }
        public ICommand RevertCommand         { get; }

        public CameraManagerViewModel(ICameraManager manager, CameraSystemConfig config)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _config  = config  ?? throw new ArgumentNullException(nameof(config));

            ScanCommand          = new RelayCommand(ExecuteScan,          () => !IsBusy);
            AddCameraCommand     = new RelayCommand(ExecuteAddCamera,     () => !IsBusy && SelectedDiscovered != null);
            RemoveCameraCommand  = new RelayCommand(ExecuteRemoveCamera,  () => !IsBusy && SelectedCamera != null);
            ConnectCommand       = new RelayCommand(ExecuteConnect,       () => !IsBusy && SelectedCamera != null && !SelectedCamera.IsConnected);
            DisconnectCommand    = new RelayCommand(ExecuteDisconnect,    () => !IsBusy && SelectedCamera != null && SelectedCamera.IsConnected);
            StartGrabbingCommand = new RelayCommand(ExecuteStartGrabbing, () => !IsBusy && SelectedCamera != null && SelectedCamera.IsConnected && !SelectedCamera.IsGrabbing);
            StopGrabbingCommand  = new RelayCommand(ExecuteStopGrabbing,  () => !IsBusy && SelectedCamera != null && SelectedCamera.IsGrabbing);
            SaveCommand          = new RelayCommand(ExecuteSave,          () => !IsBusy);
            RevertCommand        = new RelayCommand(ExecuteRevert,        () => !IsBusy);

            // 从已有配置加载设备列表
            LoadFromConfig();

            // 订阅配置变更事件
            _config.DeviceAdded   += OnConfigDeviceAdded;
            _config.DeviceRemoved += OnConfigDeviceRemoved;
            _config.DeviceUpdated += OnConfigDeviceUpdated;
            _config.Reset += OnConfigReset;
        }

        // ── 初始化 ────────────────────────────────────────────────────

        private void LoadFromConfig()
        {
            foreach (var cam in Cameras)
                cam.ConfigChanged -= OnCameraConfigChanged;
            Cameras.Clear();
            foreach (var dev in _config.Devices)
                Cameras.Add(CreateCameraItem(dev));
        }

        public void RefreshStatuses()
        {
            var statuses = _manager.GetDeviceStatuses();
            foreach (var cam in Cameras)
            {
                var status = statuses.FirstOrDefault(s => s.DeviceId == cam.DeviceId);
                cam.RefreshStatus(status);
            }
        }

        // ── 命令实现 ──────────────────────────────────────────────────

        private async void ExecuteScan()
        {
            IsBusy = true;
            StatusMessage = "正在扫描相机...";
            DiscoveredDevices.Clear();

            try
            {
                var result = await Task.Run(() => _manager.EnumerateCameras());
                foreach (var info in result)
                    DiscoveredDevices.Add(new DiscoveredCameraItemViewModel(info));

                StatusMessage = $"扫描完成，共发现 {DiscoveredDevices.Count} 台相机。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"扫描失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteAddCamera()
        {
            if (SelectedDiscovered == null) return;

            var info = SelectedDiscovered.Info;
            string name = string.IsNullOrEmpty(info.ModelName)
                ? $"相机{Cameras.Count + 1}"
                : info.ModelName;

            var cfg = _config.AddDevice(name);
            cfg.AssignedSerial = info.SerialNumber;
            _config.UpdateDevice(cfg);
        }

        private void ExecuteRemoveCamera()
        {
            if (SelectedCamera == null) return;
            _config.RemoveDevice(SelectedCamera.DeviceId);
        }

        private void ExecuteConnect()
        {
            if (SelectedCamera == null) return;
            try
            {
                var info = DiscoveredDevices
                    .FirstOrDefault(d => d.Info.SerialNumber == SelectedCamera.AssignedSerial)
                    ?.Info;
                if (info == null)
                {
                    StatusMessage = "未找到匹配的物理相机，请先扫描。";
                    return;
                }
                _manager.OpenDevice(SelectedCamera.DeviceId, info);
                SelectedCamera.RefreshStatus(_manager.GetDeviceStatus(SelectedCamera.DeviceId));
                StatusMessage = $"已连接：{SelectedCamera.DisplayName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"连接失败：{ex.Message}";
            }
            RefreshCommands();
        }

        private void ExecuteDisconnect()
        {
            if (SelectedCamera == null) return;
            try
            {
                _manager.CloseDevice(SelectedCamera.DeviceId);
                SelectedCamera.RefreshStatus(_manager.GetDeviceStatus(SelectedCamera.DeviceId));
                StatusMessage = $"已断开：{SelectedCamera.DisplayName}";
            }
            catch (Exception ex) { StatusMessage = $"断开失败：{ex.Message}"; }
            RefreshCommands();
        }

        private void ExecuteStartGrabbing()
        {
            if (SelectedCamera == null) return;
            try
            {
                _manager.UpdateDeviceSettings(SelectedCamera.DeviceId, SelectedCamera.ConfigVm.ToSettings());
                _manager.StartGrabbing(SelectedCamera.DeviceId);
                SelectedCamera.RefreshStatus(_manager.GetDeviceStatus(SelectedCamera.DeviceId));
                StatusMessage = $"已启动采集：{SelectedCamera.DisplayName}";
            }
            catch (Exception ex) { StatusMessage = $"启动采集失败：{ex.Message}"; }
            RefreshCommands();
        }

        private void ExecuteStopGrabbing()
        {
            if (SelectedCamera == null) return;
            try
            {
                _manager.StopGrabbing(SelectedCamera.DeviceId);
                SelectedCamera.RefreshStatus(_manager.GetDeviceStatus(SelectedCamera.DeviceId));
                StatusMessage = $"已停止采集：{SelectedCamera.DisplayName}";
            }
            catch (Exception ex) { StatusMessage = $"停止采集失败：{ex.Message}"; }
            RefreshCommands();
        }

        private void ExecuteSave()
        {
            try
            {
                // 将所有 ViewModel 更改写入 config，再请求保存
                foreach (var cam in Cameras)
                    _config.UpdateDevice(cam.GetConfig());

                _config.RequestSave();
                StatusMessage = "配置已保存。";
            }
            catch (Exception ex) { StatusMessage = $"保存失败：{ex.Message}"; }
        }

        private void ExecuteRevert()
        {
            try
            {
                bool reverted = _config.RevertChanges();
                if (reverted)
                {
                    LoadFromConfig();
                    StatusMessage = "已还原到上次保存的配置。";
                }
                else
                {
                    StatusMessage = "没有可还原的快照。";
                }
            }
            catch (Exception ex) { StatusMessage = $"还原失败：{ex.Message}"; }
        }

        // ── 配置变更事件响应 ──────────────────────────────────────────

        private void OnConfigDeviceAdded(object sender, CameraDeviceConfig cfg)
        {
            InvokeOnUI(() =>
            {
                if (Cameras.All(c => c.DeviceId != cfg.DeviceId))
                    Cameras.Add(CreateCameraItem(cfg));
            });
        }

        private void OnConfigDeviceRemoved(object sender, string deviceId)
        {
            InvokeOnUI(() =>
            {
                var vm = Cameras.FirstOrDefault(c => c.DeviceId == deviceId);
                if (vm != null)
                {
                    vm.ConfigChanged -= OnCameraConfigChanged;
                    Cameras.Remove(vm);
                }
            });
        }

        private void OnConfigDeviceUpdated(object sender, CameraDeviceConfig cfg)
        {
            InvokeOnUI(() =>
            {
                var vm = Cameras.FirstOrDefault(c => c.DeviceId == cfg.DeviceId);
                vm?.LoadConfig(cfg);
            });
        }

        private void OnConfigReset(object sender, EventArgs e)
        {
            InvokeOnUI(LoadFromConfig);
        }

        private CameraItemViewModel CreateCameraItem(CameraDeviceConfig cfg)
        {
            var vm = new CameraItemViewModel(cfg);
            vm.ConfigChanged += OnCameraConfigChanged;
            return vm;
        }

        private void OnCameraConfigChanged(object sender, EventArgs e)
        {
            var vm = sender as CameraItemViewModel;
            if (vm == null || IsBusy) return;
            _config.UpdateDevice(vm.GetConfig());
        }

        private void RefreshCommands()
        {
            (ScanCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (AddCameraCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveCameraCommand  as RelayCommand)?.RaiseCanExecuteChanged();
            (ConnectCommand       as RelayCommand)?.RaiseCanExecuteChanged();
            (DisconnectCommand    as RelayCommand)?.RaiseCanExecuteChanged();
            (StartGrabbingCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopGrabbingCommand  as RelayCommand)?.RaiseCanExecuteChanged();
            (SaveCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (RevertCommand        as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private static void InvokeOnUI(Action action)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
                Application.Current.Dispatcher.BeginInvoke(action);
            else
                action();
        }

        public void Dispose()
        {
            _config.DeviceAdded   -= OnConfigDeviceAdded;
            _config.DeviceRemoved -= OnConfigDeviceRemoved;
            _config.DeviceUpdated -= OnConfigDeviceUpdated;
            _config.Reset -= OnConfigReset;
            foreach (var cam in Cameras)
                cam.ConfigChanged -= OnCameraConfigChanged;
        }
    }

    // ── 发现相机列表项 ─────────────────────────────────────────────────

    public sealed class DiscoveredCameraItemViewModel : NotifyBase
    {
        public CameraInfo Info { get; }

        public string DisplayText =>
            $"{Info.ModelName}  [{Info.TransportTypeName}]  SN: {Info.SerialNumber}";

        public string IpAddress => Info.IpAddress != 0
            ? $"{(Info.IpAddress >> 24) & 0xFF}.{(Info.IpAddress >> 16) & 0xFF}.{(Info.IpAddress >> 8) & 0xFF}.{Info.IpAddress & 0xFF}"
            : "";

        public DiscoveredCameraItemViewModel(CameraInfo info)
        {
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }
    }
}
