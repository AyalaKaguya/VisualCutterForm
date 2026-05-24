using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using VisualMaster.Api;
using VisualMaster.CameraLink.Adapter;
using VisualMaster.CameraLink.API;
using VisualMaster.CameraLink.Core;

namespace VisualMaster.CameraLink
{
    public class CameraManager : ICameraManager
    {
        private readonly List<ICameraAdapter> _adapters = new List<ICameraAdapter>();
        private readonly List<ManagedCamera> _devices = new List<ManagedCamera>();
        private readonly List<CameraInfo> _enumeratedCameras = new List<CameraInfo>();
        private readonly Dictionary<string, DiscoveredCamera> _discoveries =
            new Dictionary<string, DiscoveredCamera>(StringComparer.OrdinalIgnoreCase);

        private CameraSystemConfig _systemConfig;
        private volatile bool _disposed;

        public IReadOnlyList<CameraInfo> Cameras => _enumeratedCameras.AsReadOnly();
        public IReadOnlyList<CameraDeviceConfig> CameraDevices =>
            _devices.Select(d => d.Config.Clone()).ToList().AsReadOnly();

        public bool IsInitialized { get; private set; }
        public RuntimeDiagnosticsHub Diagnostics { get; set; }

        public event EventHandler<CameraDeviceConfig> DeviceOpened;
        public event EventHandler<CameraDeviceConfig> DeviceClosed;

#pragma warning disable CS0067
        [Obsolete("Use DeviceOpened instead.", false)]
        public event EventHandler<CameraSlot> SlotOpened;

        [Obsolete("Use DeviceClosed instead.", false)]
        public event EventHandler<CameraSlot> SlotClosed;
#pragma warning restore CS0067

        public CameraManager()
        {
            _adapters.Add(new HikrobotAdapter());
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            foreach (var adapter in _adapters.Where(a => a.IsAvailable))
            {
                try
                {
                    adapter.InitializeSdk();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"无法初始化相机适配器 {adapter.AdapterName}，请确认驱动已正确安装。\n{ex.Message}", ex);
                }
            }

            IsInitialized = true;
        }

        public async Task InitializeRuntimeAsync(CameraSystemConfig config)
        {
            LoadConfig(config);
            await Task.Run(() => EnumerateCameras()).ConfigureAwait(false);
            ApplyConfiguredDevices();
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

        public List<CameraInfo> EnumerateCameras()
        {
            if (!IsInitialized) Initialize();

            _enumeratedCameras.Clear();
            _discoveries.Clear();

            var adapters = _adapters.Where(a => a.IsAvailable).ToList();
            var tasks = adapters.Select(adapter => Task.Run(() => ScanAdapter(adapter))).ToArray();
            Task.WaitAll(tasks);

            foreach (var task in tasks)
            {
                foreach (var discovered in task.Result)
                {
                    _discoveries[MakeDiscoveryKey(discovered.AdapterName, discovered.SerialNumber)] = discovered;
                    _enumeratedCameras.Add(ToCameraInfo(discovered));
                }
            }

            return _enumeratedCameras.ToList();
        }

        public void ApplyConfiguredDevices()
        {
            if (_enumeratedCameras.Count == 0)
                EnumerateCameras();

            foreach (var entry in _devices.ToList())
            {
                var serial = entry.Config.AssignedSerial;
                if (string.IsNullOrWhiteSpace(serial) || entry.IsConnected)
                    continue;

                var info = _enumeratedCameras.FirstOrDefault(c =>
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
            return _devices.Select(d => d.ToStatus()).ToList().AsReadOnly();
        }

        public CameraDeviceStatus GetDeviceStatus(string deviceId)
        {
            return FindEntry(deviceId)?.ToStatus();
        }

        public void OpenDevice(string deviceId, CameraInfo info)
        {
            var entry = FindEntry(deviceId);
            if (entry == null)
                throw new InvalidOperationException($"Device {deviceId} not found.");

            if (entry.IsConnected)
                CloseDevice(deviceId);

            var discovery = ResolveDiscovery(info);
            var adapter = _adapters.FirstOrDefault(a => a.IsAvailable && a.CanHandle(discovery));
            if (adapter == null)
                throw new InvalidOperationException($"No camera adapter can open {info}.");

            var driver = adapter.CreateDevice(discovery);
            entry.Attach(driver, discovery);
            entry.Diagnostics = Diagnostics;

            if (_systemConfig?.GetDevice(deviceId) != null)
            {
                var cfg = _systemConfig.GetDevice(deviceId);
                cfg.AssignedSerial = driver.UniqueHardwareId;
                cfg.Settings = entry.Config.Settings.Clone();
                _systemConfig.UpdateDevice(cfg);
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

        [Obsolete("Use CameraDevices instead.", false)]
        public IReadOnlyList<CameraSlot> Slots =>
            _devices.Select(ToLegacySlot).ToList().AsReadOnly();

        [Obsolete("Use AddDevice instead.", false)]
        public CameraSlot AddSlot(string name, CameraSettings settings = null)
        {
            var config = AddDevice(name, settings);
            return ToLegacySlot(FindEntry(config.DeviceId));
        }

        [Obsolete("Use RemoveDevice instead.", false)]
        public void RemoveSlot(string slotId) => RemoveDevice(slotId);

        [Obsolete("Use OpenDevice instead.", false)]
        public void OpenSlot(string slotId, CameraInfo info) => OpenDevice(slotId, info);

        [Obsolete("Use CloseDevice instead.", false)]
        public void CloseSlot(string slotId) => CloseDevice(slotId);

        [Obsolete("Use IsDeviceOpen instead.", false)]
        public bool IsSlotOpen(string slotId) => IsDeviceOpen(slotId);

        public void AddDeviceWithId(CameraDeviceConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.DeviceId)) return;
            if (FindEntry(config.DeviceId) != null) return;
            AddDeviceEntry(config.Clone());
        }

        public void CloseAllDevices()
        {
            foreach (var entry in _devices.ToList())
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
            foreach (var entry in _devices.ToList())
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
            return _devices.Find(d => d.DeviceId == deviceId);
        }

        private void AddDeviceEntry(CameraDeviceConfig config)
        {
            var entry = new ManagedCamera(config) { Diagnostics = Diagnostics };
            _devices.Add(entry);
        }

        private void RemoveDeviceEntry(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry == null) return;
            entry.Dispose();
            _devices.Remove(entry);
        }

        private void ApplyDeviceConfig(CameraDeviceConfig config)
        {
            if (config == null) return;
            var entry = FindEntry(config.DeviceId);
            if (entry == null) return;

            entry.Config.DisplayName = config.DisplayName;
            entry.Config.AssignedSerial = config.AssignedSerial;
            entry.ApplySettings(config.Settings ?? new CameraSettings());
        }

        private IReadOnlyList<DiscoveredCamera> ScanAdapter(ICameraAdapter adapter)
        {
            try { return adapter.Scan() ?? new List<DiscoveredCamera>(); }
            catch { return new List<DiscoveredCamera>(); }
        }

        private DiscoveredCamera ResolveDiscovery(CameraInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var key = MakeDiscoveryKey(info.AdapterName, info.SerialNumber);
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

        private static CameraSlot ToLegacySlot(ManagedCamera entry)
        {
            if (entry == null) return null;
            return new CameraSlot
            {
                SlotId = entry.Config.DeviceId,
                SlotName = entry.Config.DisplayName,
                Settings = entry.Config.Settings?.Clone() ?? new CameraSettings(),
                AssignedSerial = entry.Config.AssignedSerial,
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
