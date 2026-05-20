using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MvCameraControl;

namespace VisualMaster.CameraLink
{
    public class CameraManager : ICameraManager
    {
        // ── 内部运行时状态类，替代混合了配置与状态的 CameraSlot ────────────
        private sealed class DeviceEntry
        {
            public CameraDeviceConfig Config { get; set; }
            public CameraFrameBuffer FrameBuffer { get; set; }
            public ImageFifo Fifo { get; set; }
            public ICamera Camera { get; set; }
            public CameraInfo AssignedCamera { get; set; }
            public string AssignedSerial { get; set; }
            public bool IsConnected { get; set; }
            public bool IsGrabbing { get; set; }

            public CameraDeviceStatus ToStatus() => new CameraDeviceStatus
            {
                DeviceId = Config.DeviceId,
                DisplayName = Config.DisplayName,
                IsConnected = IsConnected,
                IsGrabbing = IsGrabbing,
                AssignedCamera = AssignedCamera,
                AssignedSerial = AssignedSerial,
            };
        }

        private readonly List<CameraInfo> _enumeratedCameras = new List<CameraInfo>();
        private readonly List<DeviceEntry> _devices = new List<DeviceEntry>();
        private volatile bool _disposed;

        public IReadOnlyList<CameraInfo> Cameras => _enumeratedCameras.AsReadOnly();
        public bool IsInitialized { get; private set; }
        public RuntimeDiagnosticsHub Diagnostics { get; set; }

        public IReadOnlyList<CameraDeviceConfig> CameraDevices =>
            _devices.Select(d => d.Config.Clone()).ToList().AsReadOnly();

        // ── 新增事件 ──────────────────────────────────────────────────
        public event EventHandler<CameraDeviceConfig> DeviceOpened;
        public event EventHandler<CameraDeviceConfig> DeviceClosed;

        // ── 已废弃事件（兼容旧代码） ──────────────────────────────────
#pragma warning disable CS0067
        [Obsolete("Use DeviceOpened instead.", false)]
        public event EventHandler<CameraSlot> SlotOpened;

        [Obsolete("Use DeviceClosed instead.", false)]
        public event EventHandler<CameraSlot> SlotClosed;
#pragma warning restore CS0067

        public void Initialize()
        {
            if (IsInitialized) return;
            SDKSystem.Initialize();
            IsInitialized = true;
        }

        public List<CameraInfo> EnumerateCameras()
        {
            _enumeratedCameras.Clear();
            if (!IsInitialized) Initialize();

            var tLayerTypes = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
                | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice
                | DeviceTLayerType.MvGenTLCameraLinkDevice | DeviceTLayerType.MvGenTLXoFDevice;

            int ret = DeviceEnumerator.EnumDevices(tLayerTypes, out List<IDeviceInfo> devInfoList);
            if (ret != MvError.MV_OK) return _enumeratedCameras;

            foreach (var devInfo in devInfoList)
            {
                var info = new CameraInfo
                {
                    ModelName = devInfo.ModelName ?? "",
                    SerialNumber = devInfo.SerialNumber ?? "",
                    UserDefinedName = devInfo.UserDefinedName ?? "",
                    ManufacturerName = devInfo.ManufacturerName ?? "",
                    TransportTypeRaw = (uint)devInfo.TLayerType,
                    TransportTypeName = TransportTypeToString(devInfo.TLayerType),
                    DeviceVersion = devInfo.DeviceVersion ?? "",
                    RawInfo = devInfo,
                };
                if (devInfo is IGigEDeviceInfo gigeInfo)
                    info.IpAddress = gigeInfo.CurrentIp;

                _enumeratedCameras.Add(info);
            }
            return _enumeratedCameras;
        }

        // ── 设备配置 API ──────────────────────────────────────────────

        public CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null)
        {
            var config = new CameraDeviceConfig
            {
                DeviceId = Guid.NewGuid().ToString(),
                DisplayName = displayName ?? $"相机{_devices.Count + 1}",
                Settings = settings ?? new CameraSettings(),
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
            var entry = FindEntry(deviceId);
            if (entry == null) return;
            CloseDevice(deviceId);
            _devices.Remove(entry);
        }

        public void UpdateDeviceSettings(string deviceId, CameraSettings settings)
        {
            if (settings == null) return;
            var entry = FindEntry(deviceId);
            if (entry == null) return;

            entry.Config.Settings = settings.Clone();
            if (entry.FrameBuffer != null)
                entry.FrameBuffer.Capacity = settings.FifoCapacity;
            if (entry.Fifo != null)
                entry.Fifo.Capacity = settings.FifoCapacity;
            if (entry.Camera != null)
                entry.Camera.ApplySettings(settings);
        }

        // ── 运行时状态 API ────────────────────────────────────────────

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

            if (entry.Camera != null)
                CloseDevice(deviceId);

            var camera = new MvsCamera(info);
            camera.Open();
            camera.ApplySettings(entry.Config.Settings);
            camera.ImageAcquired += (s, bmp) =>
            {
                entry.Fifo.Enqueue(bmp, entry.Config.DeviceId);
            };
            camera.Disconnected += (s, e) =>
            {
                entry.IsConnected = false;
            };

            entry.Camera = camera;
            entry.AssignedCamera = info;
            entry.AssignedSerial = info.SerialNumber;
            entry.Config.AssignedSerial = info.SerialNumber;
            entry.IsConnected = true;

            DeviceOpened?.Invoke(this, entry.Config.Clone());
        }

        public void CloseDevice(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry?.Camera == null) return;

            entry.Camera.StopGrabbing();
            entry.Camera.Dispose();
            entry.Camera = null;
            entry.IsConnected = false;
            entry.IsGrabbing = false;

            DeviceClosed?.Invoke(this, entry.Config.Clone());
        }

        public void StartGrabbing(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry?.Camera == null) return;

            entry.Camera.ApplySettings(entry.Config.Settings);
            entry.Camera.StartGrabbing();
            entry.IsGrabbing = true;
        }

        public void StopGrabbing(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry?.Camera == null) return;

            entry.Camera.StopGrabbing();
            entry.IsGrabbing = false;
        }

        public void TriggerSoftware(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry?.Camera == null)
                throw new InvalidOperationException($"Device {deviceId} is not connected.");

            entry.Camera.TriggerSoftware();
        }

        public bool IsDeviceOpen(string deviceId)
        {
            return FindEntry(deviceId)?.Camera != null;
        }

        public bool IsDeviceGrabbing(string deviceId)
        {
            return FindEntry(deviceId)?.IsGrabbing == true;
        }

        public CameraInfo GetAssignedCameraInfo(string deviceId)
        {
            return FindEntry(deviceId)?.AssignedCamera;
        }

        public ImageFifo GetFifo(string deviceId)
        {
            return FindEntry(deviceId)?.Fifo;
        }

        public bool TryGrabImage(string deviceId, out Bitmap bitmap, int timeoutMs)
        {
            bitmap = null;
            var entry = FindEntry(deviceId);
            if (entry?.Camera == null || !entry.IsConnected) return false;
            return entry.Camera.TryGrabImage(out bitmap, timeoutMs);
        }

        public string[] GetAvailablePixelFormats(string deviceId)
        {
            var entry = FindEntry(deviceId);
            if (entry?.Camera == null) return new string[0];
            try { return entry.Camera.GetAvailablePixelFormats() ?? new string[0]; }
            catch { return new string[0]; }
        }

        // ── 已废弃的槽位兼容 API ─────────────────────────────────────

        [Obsolete("Use CameraDevices instead.", false)]
        public IReadOnlyList<CameraSlot> Slots =>
            _devices.Select(d => ToLegacySlot(d)).ToList().AsReadOnly();

        [Obsolete("Use AddDevice instead.", false)]
        public CameraSlot AddSlot(string name, CameraSettings settings = null)
        {
            var config = new CameraDeviceConfig
            {
                DeviceId = Guid.NewGuid().ToString(),
                DisplayName = name ?? $"相机{_devices.Count + 1}",
                Settings = settings ?? new CameraSettings(),
            };
            AddDeviceEntry(config);
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

        public void CloseAllDevices()
        {
            foreach (var entry in _devices.ToList())
            {
                if (entry.Camera != null)
                    CloseDevice(entry.Config.DeviceId);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CloseAllDevices();
            if (IsInitialized)
            {
                SDKSystem.Finalize();
                IsInitialized = false;
            }
        }

        // ── 私有帮助方法 ──────────────────────────────────────────────

        private DeviceEntry FindEntry(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;
            return _devices.Find(d => d.Config.DeviceId == deviceId);
        }

        private void AddDeviceEntry(CameraDeviceConfig config)
        {
            var frameBuffer = new CameraFrameBuffer(config.Settings?.FifoCapacity ?? 10);
            var capturedConfig = config;
            frameBuffer.SnapshotPublished += (s, snapshot) =>
            {
                Diagnostics?.Record(new RuntimeDiagnosticEvent
                {
                    EventType = RuntimeDiagnosticEventType.SnapshotPublished,
                    CorrelationId = snapshot.CorrelationId,
                    DeviceId = capturedConfig.DeviceId,
                    SnapshotId = snapshot.SnapshotId,
                    SnapshotSequence = snapshot.SequenceNumber,
                    Message = $"快照已发布: {capturedConfig.DisplayName}",
                });
            };

            _devices.Add(new DeviceEntry
            {
                Config = config,
                FrameBuffer = frameBuffer,
                Fifo = new ImageFifo(frameBuffer),
            });
        }

        /// <summary>
        /// 使用配置中的现有 DeviceId 添加设备（用于反序列化恢复）。
        /// </summary>
        public void AddDeviceWithId(CameraDeviceConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.DeviceId)) return;
            // 如果已存在则跳过
            if (FindEntry(config.DeviceId) != null) return;
            AddDeviceEntry(config);
        }

        private static CameraSlot ToLegacySlot(DeviceEntry entry)
        {
            if (entry == null) return null;
            return new CameraSlot
            {
                SlotId = entry.Config.DeviceId,
                SlotName = entry.Config.DisplayName,
                Settings = entry.Config.Settings?.Clone() ?? new CameraSettings(),
                AssignedSerial = entry.AssignedSerial ?? entry.Config.AssignedSerial,
            };
        }

        private static string TransportTypeToString(DeviceTLayerType type)
        {
            switch (type)
            {
                case DeviceTLayerType.MvGigEDevice: return "GigE";
                case DeviceTLayerType.MvUsbDevice: return "USB3";
                case DeviceTLayerType.MvGenTLGigEDevice: return "GenTL/GigE";
                case DeviceTLayerType.MvGenTLCameraLinkDevice: return "CameraLink";
                case DeviceTLayerType.MvGenTLCXPDevice: return "CoaXPress";
                case DeviceTLayerType.MvGenTLXoFDevice: return "XoF";
                default: return "Unknown";
            }
        }
    }
}

