using VisualMaster.Api;
using VisualMaster.CameraLink.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private CancellationTokenSource _scanCts;

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
        public ICommand RemoveCameraCommand   { get; }
        public ICommand OpenPreviewCommand    { get; }
        public ICommand ReloadConfigCommand   { get; }

        public CameraManagerViewModel(ICameraManager manager, CameraSystemConfig config)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _config  = config  ?? throw new ArgumentNullException(nameof(config));

            ScanCommand          = new RelayCommand(ExecuteScan,          () => !IsBusy);
            RemoveCameraCommand  = new RelayCommand(ExecuteRemoveCamera,  () => !IsBusy && SelectedCamera != null);
            OpenPreviewCommand   = new RelayCommand(ExecuteOpenPreview,   () => !IsBusy && SelectedCamera != null && SelectedCamera.IsConnected);
            ReloadConfigCommand  = new RelayCommand(ExecuteReloadConfig,  () => !IsBusy && SelectedCamera != null);

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
            CancelCurrentScan();
            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            IsBusy = true;
            StatusMessage = "正在扫描相机...";
            DiscoveredDevices.Clear();

            try
            {
                var result = await _manager.EnumerateCamerasAsync(token);
                foreach (var info in result)
                    DiscoveredDevices.Add(new DiscoveredCameraItemViewModel(info, OnAddDiscoveredCamera));

                StatusMessage = $"扫描完成，共发现 {DiscoveredDevices.Count} 台相机。";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "扫描已取消。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"扫描失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
                if (_scanCts != null)
                {
                    _scanCts.Dispose();
                    _scanCts = null;
                }
            }
        }

        private void CancelCurrentScan()
        {
            if (_scanCts != null)
            {
                try { _scanCts.Cancel(); } catch { }
                try { _scanCts.Dispose(); } catch { }
                _scanCts = null;
            }
        }

        private void OnAddDiscoveredCamera(DiscoveredCameraItemViewModel discovered)
        {
            if (discovered == null || IsBusy) return;
            SelectedDiscovered = discovered;
            ExecuteAddCamera();
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
            if (SelectedCamera.IsConnected)
            {
                try { _manager.StopGrabbing(SelectedCamera.DeviceId); } catch { }
                try { _manager.CloseDevice(SelectedCamera.DeviceId); } catch { }
            }
            _config.RemoveDevice(SelectedCamera.DeviceId);
        }

        public void RenameSelectedCamera(string newName)
        {
            if (SelectedCamera == null || string.IsNullOrWhiteSpace(newName)) return;
            SelectedCamera.DisplayName = newName;
        }

        private void OnIsEnabledChanged(CameraItemViewModel vm)
        {
            if (IsBusy || vm == null) return;

            if (vm.IsEnabled)
            {
                var info = DiscoveredDevices.FirstOrDefault(d =>
                    d.Info.SerialNumber == vm.AssignedSerial)?.Info;
                if (info == null || !vm.IsConnected)
                {
                    try
                    {
                        if (info != null)
                        {
                            _manager.OpenDevice(vm.DeviceId, info);
                            RefreshCameraOptions(vm.DeviceId);
                            _manager.UpdateDeviceSettings(vm.DeviceId, vm.ConfigVm.ToSettings());
                            _manager.StartGrabbing(vm.DeviceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        vm.IsEnabled = false;
                        System.Diagnostics.Debug.WriteLine($"[Camera] 自动连接失败: {vm.DisplayName}\n{ex.Message}");
                    }
                }
            }
            else
            {
                try
                {
                    if (vm.IsGrabbing) _manager.StopGrabbing(vm.DeviceId);
                    if (vm.IsConnected) _manager.CloseDevice(vm.DeviceId);
                }
                catch { }
            }

            vm.RefreshStatus(_manager.GetDeviceStatus(vm.DeviceId));
            RefreshCommands();
        }

        private async void ExecuteReloadConfig()
        {
            if (SelectedCamera == null) return;
            IsBusy = true;
            StatusMessage = "正在重载配置...";

            try
            {
                var wasEnabled = SelectedCamera.IsEnabled;
                var info = DiscoveredDevices.FirstOrDefault(d =>
                    d.Info.SerialNumber == SelectedCamera.AssignedSerial)?.Info;

                if (info == null)
                {
                    StatusMessage = "未找到匹配相机，请先扫描。";
                    return;
                }

                _manager.CloseDevice(SelectedCamera.DeviceId);

                _config.UpdateDevice(SelectedCamera.GetConfig());

                _manager.OpenDevice(SelectedCamera.DeviceId, info);
                RefreshCameraOptions(SelectedCamera.DeviceId);
                _manager.UpdateDeviceSettings(SelectedCamera.DeviceId, SelectedCamera.ConfigVm.ToSettings());

                if (wasEnabled)
                {
                    _manager.StartGrabbing(SelectedCamera.DeviceId);
                }

                SelectedCamera.RefreshStatus(_manager.GetDeviceStatus(SelectedCamera.DeviceId));
                StatusMessage = "配置已重载。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"重载失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private void ExecuteOpenPreview()
        {
            if (SelectedCamera == null) return;
            var window = new CameraPreviewWindow(_manager, SelectedCamera.DeviceId)
            {
                Title = $"相机预览 - {SelectedCamera.DisplayName}",
                Owner = Application.Current?.MainWindow,
            };
            window.Show();
        }

        private void RefreshCameraOptions(string deviceId)
        {
            if (SelectedCamera == null) return;
            try
            {
                var sources = _manager.GetAvailableTriggerSources(deviceId);
                if (sources != null && sources.Length > 0)
                    SelectedCamera.ConfigVm.TriggerSources = sources;

                var formats = _manager.GetAvailablePixelFormats(deviceId);
                if (formats != null && formats.Length > 0)
                    SelectedCamera.ConfigVm.PixelFormats = formats;
            }
            catch { }
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
            vm.IsEnabledChangedCallback = OnIsEnabledChanged;
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
            (RemoveCameraCommand  as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenPreviewCommand   as RelayCommand)?.RaiseCanExecuteChanged();
            (ReloadConfigCommand  as RelayCommand)?.RaiseCanExecuteChanged();
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
            CancelCurrentScan();
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
        public ICommand AddCommand { get; }

        public string ModelName => Info.ModelName ?? "";
        public string SerialNumber => Info.SerialNumber ?? "";
        public string Manufacturer => Info.ManufacturerName ?? "";
        public string TransportType => Info.TransportTypeName ?? "";
        public string DeviceVersion => Info.DeviceVersion ?? "";

        public string IpAddress => Info.IpAddress != 0
            ? $"{(Info.IpAddress >> 24) & 0xFF}.{(Info.IpAddress >> 16) & 0xFF}.{(Info.IpAddress >> 8) & 0xFF}.{Info.IpAddress & 0xFF}"
            : "";

        public string DisplayText =>
            $"{Info.ModelName}  [{Info.TransportTypeName}]  SN: {Info.SerialNumber}";

        public DiscoveredCameraItemViewModel(CameraInfo info, Action<DiscoveredCameraItemViewModel> onAdd = null)
        {
            Info = info ?? throw new ArgumentNullException(nameof(info));
            AddCommand = new RelayCommand(() => onAdd?.Invoke(this));
        }
    }
}
