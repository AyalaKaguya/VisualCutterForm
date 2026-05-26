using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.CameraLink.Api;
using VisualMaster.CameraLink.Adapter;
using VisualMaster.CameraLink.API;

namespace VisualMaster.CameraLink.Core
{
    public class CameraManager : ICameraManager
    {
        private readonly List<ICameraAdapter> _adapters = new List<ICameraAdapter>();
        private readonly List<ManagedCamera> _devices = new List<ManagedCamera>();
        private readonly Dictionary<string, ManagedCamera> _deviceIndex =
            new Dictionary<string, ManagedCamera>(StringComparer.OrdinalIgnoreCase);
        private readonly List<CameraInfo> _enumeratedCameras = new List<CameraInfo>();
        private readonly Dictionary<string, DiscoveredCamera> _discoveries =
            new Dictionary<string, DiscoveredCamera>(StringComparer.OrdinalIgnoreCase);

        // 所有集合访问统一由此锁保护
        private readonly object _lock = new object();
        private CameraSystemConfig _systemConfig;
        private volatile bool _disposed;

        public IReadOnlyList<CameraInfo> Cameras
        {
            get { lock (_lock) return _enumeratedCameras.ToList().AsReadOnly(); }
        }
        public IReadOnlyList<CameraDeviceConfig> CameraDevices
        {
            get { lock (_lock) return _devices.Select(d => d.Config.Clone()).ToList().AsReadOnly(); }
        }

        public bool IsInitialized { get; private set; }
        public RuntimeDiagnosticsHub Diagnostics { get; set; }

        public event EventHandler<CameraDeviceConfig> DeviceOpened;
        public event EventHandler<CameraDeviceConfig> DeviceClosed;

        public CameraManager()
        {
            _adapters.Add(new HikrobotAdapter());
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            var failures = new List<string>();
            bool anySuccess = false;

            foreach (var adapter in _adapters.Where(a => a.IsAvailable))
            {
                try
                {
                    adapter.InitializeSdk();
                    anySuccess = true;
                }
                catch (Exception ex)
                {
                    failures.Add($"{adapter.AdapterName}: {ex.Message}");
                    Diagnostics?.Record(new RuntimeDiagnosticEvent
                    {
                        EventType = RuntimeDiagnosticEventType.FlowFailed,
                        Message   = $"相机适配器初始化失败 ({adapter.AdapterName})：{ex.Message}",
                    });
                }
            }

            // 所有已安装的适配器全部失败才抛出异常
            if (!anySuccess && failures.Count > 0)
                throw new InvalidOperationException(
                    $"无法初始化任何相机适配器，请确认驱动已正确安装。\n{string.Join("\n", failures)}");

            IsInitialized = true;
        }

        public async Task InitializeRuntimeAsync(CameraSystemConfig config)
        {
            LoadConfig(config);
            await EnumerateCamerasAsync().ConfigureAwait(false);
            await AutoConnectEnabledDevicesAsync().ConfigureAwait(false);
        }

        private async Task AutoConnectEnabledDevicesAsync()
        {
            List<ManagedCamera> snapshot;
            lock (_lock) snapshot = _devices.ToList();

            foreach (var entry in snapshot)
            {
                if (!entry.Config.IsEnabled || entry.IsConnected)
                    continue;

                var serial = entry.Config.AssignedSerial;
                if (string.IsNullOrWhiteSpace(serial))
                    continue;

                CameraInfo info;
                lock (_lock)
                    info = _enumeratedCameras.FirstOrDefault(c =>
                        string.Equals(c.SerialNumber, serial, StringComparison.OrdinalIgnoreCase));
                if (info == null)
                    continue;

                try
                {
                    OpenDevice(entry.DeviceId, info);
                }
                catch (Exception ex)
                {
                    entry.Config.IsEnabled = false;
                    Diagnostics?.Record(new RuntimeDiagnosticEvent
                    {
                        EventType = RuntimeDiagnosticEventType.FlowFailed,
                        DeviceId  = entry.DeviceId,
                        Message   = $"相机自动连接失败，已禁用: {entry.Config.DisplayName} (SN: {serial}) \u2014 {ex.Message}",
                    });
                }
            }
        }

        public void LoadConfig(CameraSystemConfig config)
        {
            if (_systemConfig != null)
            {
                _systemConfig.DeviceAdded -= OnConfigDeviceAdded;
                _systemConfig.DeviceRemoved -= OnConfigDeviceRemoved;
                _systemConfig.DeviceUpdated -= OnConfigDeviceUpdated;
                _systemConfig.Reset -= OnConfigReset;
            }

            _systemConfig = config;

            if (_systemConfig == null) return;

            SyncDevicesFromConfig(_systemConfig);
            _systemConfig.DeviceAdded += OnConfigDeviceAdded;
            _systemConfig.DeviceRemoved += OnConfigDeviceRemoved;
            _systemConfig.DeviceUpdated += OnConfigDeviceUpdated;
            _systemConfig.Reset += OnConfigReset;
        }

        public async Task<List<CameraInfo>> EnumerateCamerasAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsInitialized) Initialize();

            cancellationToken.ThrowIfCancellationRequested();

            List<ICameraAdapter> adapters;
            lock (_lock) adapters = _adapters.Where(a => a.IsAvailable).ToList();

            var tasks = adapters.Select(adapter =>
                Task.Run(() => ScanAdapter(adapter, cancellationToken), cancellationToken)).ToArray();

            // 增加 30 秒安全超时，防止扫描卡死让调用者永久等待
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // 是超时而非调用方取消，继续处理已完成的部分结果
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            lock (_lock)
            {
                _enumeratedCameras.Clear();
                _discoveries.Clear();

                foreach (var task in tasks.Where(t => t.Status == TaskStatus.RanToCompletion))
                {
                    foreach (var discovered in task.Result)
                    {
                        _discoveries[MakeDiscoveryKey(discovered.AdapterName, discovered.SerialNumber)] = discovered;
                        _enumeratedCameras.Add(ToCameraInfo(discovered));
                    }
                }

                return _enumeratedCameras.ToList();
            }
        }

        public List<CameraInfo> EnumerateCameras()
        {
            return EnumerateCamerasAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public void ApplyConfiguredDevices()
        {
            List<CameraInfo> cameras;
            List<ManagedCamera> snapshot;
            lock (_lock)
            {
                cameras  = _enumeratedCameras.ToList();
                snapshot = _devices.ToList();
            }

            if (cameras.Count == 0)
            {
                EnumerateCameras();
                lock (_lock)
                {
                    cameras  = _enumeratedCameras.ToList();
                    snapshot = _devices.ToList();
                }
            }

            foreach (var entry in snapshot)
            {
                var serial = entry.Config.AssignedSerial;
                if (string.IsNullOrWhiteSpace(serial) || entry.IsConnected)
                    continue;

                var info = cameras.FirstOrDefault(c =>
                    string.Equals(c.SerialNumber, serial, StringComparison.OrdinalIgnoreCase));
                if (info == null)
                    continue;

                OpenDevice(entry.DeviceId, info);
            }
        }

        public CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null)
        {
            if (_systemConfig != null)
                return _systemConfig.AddDevice(displayName, settings).Clone();

            var config = new CameraDeviceConfig
            {
                DeviceId = Guid.NewGuid().ToString(),
                DisplayName = displayName ?? $"相机{_devices.Count + 1}",
                Settings = settings?.Clone() ?? new CameraSettings(),
            };
            AddDeviceEntry(config);
            return config.Clone();
        }

        public CameraDeviceConfig GetCameraDevice(string deviceId)
        {
            return FindEntry(deviceId)?.Config.Clone();
        }

        public void RemoveDevice(string deviceId)
        {
            if (_systemConfig?.GetDevice(deviceId) != null)
            {
                _systemConfig.RemoveDevice(deviceId);
                return;
            }

            RemoveDeviceEntry(deviceId);
        }

        public void UpdateDeviceSettings(string deviceId, CameraSettings settings)
        {
            if (settings == null) return;

            if (_systemConfig?.GetDevice(deviceId) != null)
            {
                var config = _systemConfig.GetDevice(deviceId);
                config.Settings = settings.Clone();
                _systemConfig.UpdateDevice(config);
                return;
            }

            ApplyDeviceConfig(new CameraDeviceConfig
            {
                DeviceId = deviceId,
                DisplayName = FindEntry(deviceId)?.Config.DisplayName,
                Settings = settings.Clone(),
            });
        }

        public IReadOnlyList<CameraDeviceStatus> GetDeviceStatuses()
        {
            lock (_lock) return _devices.Select(d => d.ToStatus()).ToList().AsReadOnly();
        }

        public CameraDeviceStatus GetDeviceStatus(string deviceId)
        {
            return FindEntry(deviceId)?.ToStatus();
        }

        public void OpenDevice(string deviceId, CameraInfo info)
        {
            ManagedCamera entry = FindEntry(deviceId);
            if (entry == null)
                throw new InvalidOperationException($"Device {deviceId} not found.");

            if (entry.IsConnected)
                CloseDevice(deviceId);

            DiscoveredCamera discovery;
            lock (_lock) discovery = ResolveDiscovery(info);

            ICameraAdapter adapter;
            lock (_lock)
                adapter = _adapters.FirstOrDefault(a => a.IsAvailable && a.CanHandle(discovery));
            if (adapter == null)
                throw new InvalidOperationException($"No camera adapter can open {info}.");

            var driver = adapter.CreateDevice(discovery);
            entry.Attach(driver, discovery);
            entry.Diagnostics = Diagnostics;

            lock (_lock)
            {
                if (_systemConfig?.GetDevice(deviceId) != null)
                {
                var cfg = _systemConfig.GetDevice(deviceId);
                    cfg.AssignedSerial = driver.UniqueHardwareId;
                    cfg.Settings = entry.Config.Settings.Clone();
                    _systemConfig.UpdateDevice(cfg);
                }
            }

            DeviceOpened?.Invoke(this, entry.Config.Clone());
        }

        public void CloseDevice(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry == null || !entry.IsConnected) return;

            entry.Detach();
            DeviceClosed?.Invoke(this, entry.Config.Clone());
        }

        public void StartGrabbing(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry == null) return;
            entry.StartGrabbing();
        }

        public void StopGrabbing(string deviceId)
        {
            FindEntry(deviceId)?.StopGrabbing();
        }

        public void TriggerSoftware(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry == null || !entry.IsConnected)
                throw new InvalidOperationException($"Device {deviceId} is not connected.");

            entry.TriggerSoftware();
        }

        public bool IsDeviceOpen(string deviceId)
        {
            return FindEntry(deviceId)?.IsConnected == true;
        }

        public bool IsDeviceGrabbing(string deviceId)
        {
            return FindEntry(deviceId)?.IsGrabbing == true;
        }

        public CameraInfo GetAssignedCameraInfo(string deviceId)
        {
            return FindEntry(deviceId)?.LastKnownInfo;
        }

        public ImageFifo GetFifo(string deviceId)
        {
            return FindEntry(deviceId)?.Fifo;
        }

        public bool TryGrabImage(string deviceId, out Bitmap bitmap, int timeoutMs)
        {
            bitmap = null;
            var entry = FindEntry(deviceId);
            return entry != null && entry.TryGrabImage(out bitmap, timeoutMs);
        }

        public string[] GetAvailablePixelFormats(string deviceId)
        {
            return FindEntry(deviceId)?.GetAvailablePixelFormats() ?? new string[0];
        }

        public string[] GetAvailableTriggerSources(string deviceId)
        {
            return FindEntry(deviceId)?.GetAvailableTriggerSources() ?? new string[0];
        }

        public void AddDeviceWithId(CameraDeviceConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.DeviceId)) return;
            if (FindEntry(config.DeviceId) != null) return;
            AddDeviceEntry(config.Clone());
        }

        public void CloseAllDevices()
        {
            List<ManagedCamera> snapshot;
            lock (_lock) snapshot = _devices.ToList();
            foreach (var entry in snapshot)
                CloseDevice(entry.DeviceId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            CloseAllDevices();
            foreach (var entry in _devices)
                entry.Dispose();
            _devices.Clear();

            if (IsInitialized)
            {
                foreach (var adapter in _adapters.Where(a => a.IsAvailable))
                {
                    try { adapter.FinalizeSdk(); }
                    catch { /* 进程退出时 SDK 资源由 OS 回收 */ }
                }
                IsInitialized = false;
            }
        }

        private void SyncDevicesFromConfig(CameraSystemConfig config)
        {
            var configIds = new HashSet<string>(config.Devices.Select(d => d.DeviceId));
            List<ManagedCamera> snapshot;
            lock (_lock) snapshot = _devices.ToList();

            foreach (var entry in snapshot)
            {
                if (!configIds.Contains(entry.DeviceId))
                    RemoveDeviceEntry(entry.DeviceId);
            }

            foreach (var device in config.Devices)
            {
                var entry = FindEntry(device.DeviceId);
                if (entry == null)
                    AddDeviceEntry(device.Clone());
                else
                    ApplyDeviceConfig(device);
            }
        }

        private void OnConfigDeviceAdded(object sender, CameraDeviceConfig cfg)
        {
            if (FindEntry(cfg.DeviceId) == null)
                AddDeviceEntry(cfg.Clone());
        }

        private void OnConfigDeviceRemoved(object sender, string deviceId)
        {
            RemoveDeviceEntry(deviceId);
        }

        private void OnConfigDeviceUpdated(object sender, CameraDeviceConfig cfg)
        {
            ApplyDeviceConfig(cfg);
        }

        private void OnConfigReset(object sender, EventArgs e)
        {
            if (_systemConfig != null)
                SyncDevicesFromConfig(_systemConfig);
        }

        private ManagedCamera FindEntry(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;
            lock (_lock)
                return _deviceIndex.TryGetValue(deviceId, out var entry) ? entry : null;
        }

        private void AddDeviceEntry(CameraDeviceConfig config)
        {
            var entry = new ManagedCamera(config) { Diagnostics = Diagnostics };
            lock (_lock)
            {
                _devices.Add(entry);
                _deviceIndex[config.DeviceId] = entry;
            }
        }

        private void RemoveDeviceEntry(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry == null) return;
            entry.Dispose();
            lock (_lock)
            {
                _devices.Remove(entry);
                _deviceIndex.Remove(deviceId);
            }
        }

        private void ApplyDeviceConfig(CameraDeviceConfig config)
        {
            if (config == null) return;
            var entry = FindEntry(config.DeviceId);
            if (entry == null) return;

            entry.Config.DisplayName = config.DisplayName;
            entry.Config.AssignedSerial = config.AssignedSerial;
            entry.Config.IsEnabled = config.IsEnabled;
            entry.Config.Settings = config.Settings?.Clone() ?? new CameraSettings();
        }

        private IReadOnlyList<DiscoveredCamera> ScanAdapter(ICameraAdapter adapter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return adapter.Scan(cancellationToken) ?? new List<DiscoveredCamera>(); }
            catch (OperationCanceledException) { throw; }
            catch { return new List<DiscoveredCamera>(); }
        }

        private DiscoveredCamera ResolveDiscovery(CameraInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var key = MakeDiscoveryKey(info.AdapterName, info.SerialNumber);
            // 调用方负责在锁内调用此方法
            if (_discoveries.TryGetValue(key, out var discovered))
                return discovered;

            discovered = new DiscoveredCamera
            {
                UniqueId = info.SerialNumber ?? "",
                ModelName = info.ModelName ?? "",
                SerialNumber = info.SerialNumber ?? "",
                ManufacturerName = info.ManufacturerName ?? "",
                TransportType = info.TransportTypeName ?? "",
                DeviceVersion = info.DeviceVersion ?? "",
                AdapterName = string.IsNullOrEmpty(info.AdapterName) ? "Hikrobot MVS" : info.AdapterName,
                IpAddress = UintToIpString(info.IpAddress),
                RawInfo = info.RawInfo,
            };
            _discoveries[MakeDiscoveryKey(discovered.AdapterName, discovered.SerialNumber)] = discovered;
            return discovered;
        }

        private static CameraInfo ToCameraInfo(DiscoveredCamera discovered)
        {
            return new CameraInfo
            {
                ModelName = discovered.ModelName ?? "",
                SerialNumber = discovered.SerialNumber ?? "",
                ManufacturerName = discovered.ManufacturerName ?? "",
                TransportTypeName = discovered.TransportType ?? "",
                AdapterName = discovered.AdapterName ?? "",
                DeviceVersion = discovered.DeviceVersion ?? "",
                IpAddress = IpStringToUint(discovered.IpAddress),
                RawInfo = discovered.RawInfo,
            };
        }

        private static string MakeDiscoveryKey(string adapterName, string serial)
        {
            return $"{adapterName ?? ""}|{serial ?? ""}";
        }

        private static uint IpStringToUint(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return 0;
            try
            {
                var bytes = System.Net.IPAddress.Parse(ip).GetAddressBytes();
                return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16)
                     | ((uint)bytes[2] << 8) | bytes[3];
            }
            catch { return 0; }
        }

        private static string UintToIpString(uint ip)
        {
            if (ip == 0) return "";
            return $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
        }
    }
}
