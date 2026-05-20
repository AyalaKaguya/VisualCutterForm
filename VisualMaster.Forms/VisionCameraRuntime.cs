using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VisualMaster.Api;
using VisualMaster.CameraLink;

namespace VisualMaster.Forms
{
    internal sealed class VisionCameraRuntime
    {
        private readonly CameraManager _cameraManager;

        public VisionCameraRuntime(CameraManager cameraManager)
        {
            _cameraManager = cameraManager;
        }

        public CameraManager CameraManager => _cameraManager;
        public bool IsInitialized => _cameraManager.IsInitialized;

        public void Initialize()
        {
            if (IsInitialized) return;
            _cameraManager.Initialize();
        }

        public int EnumerateCameras()
        {
            Initialize();
            return _cameraManager.EnumerateCameras().Count;
        }

        public IReadOnlyList<CameraInfo> GetDiscoveredCameras()
        {
            return _cameraManager.Cameras.ToList();
        }

        // ── 设备管理 ──────────────────────────────────────────────────

        public CameraDeviceConfig AddCameraDevice(string displayName, CameraSettings settings = null)
        {
            return _cameraManager.AddDevice(displayName, settings);
        }

        public void RemoveCameraDevice(string deviceId)
        {
            _cameraManager.RemoveDevice(deviceId);
        }

        public void OpenSlot(string deviceId, CameraInfo info)
        {
            _cameraManager.OpenDevice(deviceId, info);
        }

        public void CloseSlot(string deviceId)
        {
            _cameraManager.CloseDevice(deviceId);
        }

        public void StartAcquisition(string deviceId)
        {
            _cameraManager.StartGrabbing(deviceId);
        }

        public void StopAcquisition(string deviceId)
        {
            _cameraManager.StopGrabbing(deviceId);
        }

        public void StartAllAcquisitions()
        {
            foreach (var status in _cameraManager.GetDeviceStatuses())
            {
                if (status.IsConnected)
                    _cameraManager.StartGrabbing(status.DeviceId);
            }
        }

        public void StopAllAcquisitions()
        {
            foreach (var status in _cameraManager.GetDeviceStatuses())
                _cameraManager.StopGrabbing(status.DeviceId);
        }

        // ── 帧缓冲 / 快照 ─────────────────────────────────────────────

        public ImageFifo GetFifo(string deviceId)
        {
            return _cameraManager.GetFifo(deviceId);
        }

        public CameraFrameSnapshot PeekLatestFrameSnapshot(string deviceId)
        {
            return GetFifo(deviceId)?.Buffer.PeekLatestSnapshot();
        }

        public CameraFrameSnapshot WaitForNextFrameSnapshot(string deviceId, long afterSequenceNumber, int timeoutMs)
        {
            return GetFifo(deviceId)?.Buffer.WaitForNextSnapshot(afterSequenceNumber, timeoutMs);
        }

        public long GetLatestFrameSequenceNumber(string deviceId)
        {
            return GetFifo(deviceId)?.Buffer.LatestSequenceNumber ?? 0;
        }

        // ── 设备状态查询 ──────────────────────────────────────────────

        public string GetFirstDeviceId()
        {
            return _cameraManager.CameraDevices.Count > 0
                ? _cameraManager.CameraDevices[0].DeviceId : null;
        }

        public string GetDeviceDisplayName(string deviceId)
        {
            return _cameraManager.GetCameraDevice(deviceId)?.DisplayName;
        }

        public void UpdateCameraDeviceDisplayName(string deviceId, string displayName)
        {
            var config = _cameraManager.GetCameraDevice(deviceId);
            if (config == null || string.IsNullOrWhiteSpace(displayName)) return;
            config.DisplayName = displayName.Trim();
        }

        public string GetAssignedSerial(string deviceId)
        {
            return _cameraManager.GetDeviceStatus(deviceId)?.AssignedSerial
                ?? _cameraManager.GetCameraDevice(deviceId)?.AssignedSerial;
        }

        public bool IsCameraConnected(string deviceId)
        {
            return _cameraManager.GetDeviceStatus(deviceId)?.IsConnected == true;
        }

        public void TriggerSoftware(string deviceId)
        {
            _cameraManager.TriggerSoftware(deviceId);
        }

        public bool IsCameraGrabbing(string deviceId)
        {
            return _cameraManager.IsDeviceGrabbing(deviceId);
        }

        public CameraInfo GetAssignedCameraInfo(string deviceId)
        {
            return _cameraManager.GetAssignedCameraInfo(deviceId);
        }

        public bool TryGrabImage(string deviceId, int timeoutMs, out Bitmap bitmap)
        {
            return _cameraManager.TryGrabImage(deviceId, out bitmap, timeoutMs);
        }

        public string[] GetAvailablePixelFormats(string deviceId)
        {
            return _cameraManager.GetAvailablePixelFormats(deviceId);
        }

        public IReadOnlyList<CameraDeviceStatus> GetDeviceStatuses()
        {
            return _cameraManager.GetDeviceStatuses();
        }

        public Bitmap TryDequeueFromFifo(string deviceId, int timeoutMs = -1)
        {
            return GetFifo(deviceId)?.TryDequeue(timeoutMs);
        }

        public Bitmap PeekLatestFromFifo(string deviceId)
        {
            return GetFifo(deviceId)?.PeekLatest();
        }

        public Bitmap PeekLatestNoClone(string deviceId)
        {
            return GetFifo(deviceId)?.PeekLatestNoClone();
        }

        public CameraSettings GetCameraSettings(string deviceId)
        {
            return _cameraManager.GetCameraDevice(deviceId)?.Settings;
        }

        public void UpdateCameraSettings(string deviceId, CameraSettings settings)
        {
            _cameraManager.UpdateDeviceSettings(deviceId, settings);
        }

        public IReadOnlyList<CameraDeviceConfig> GetCameraDeviceConfigs()
        {
            return _cameraManager.CameraDevices.ToList();
        }

        public CameraDeviceConfig GetCameraDeviceConfig(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;
            return _cameraManager.GetCameraDevice(deviceId);
        }

        public void LoadCameraDevices(IEnumerable<CameraDeviceConfig> cameraConfigs)
        {
            foreach (var status in _cameraManager.GetDeviceStatuses().ToList())
                _cameraManager.RemoveDevice(status.DeviceId);

            if (cameraConfigs == null) return;

            foreach (var config in cameraConfigs)
            {
                // 用保存的 DeviceId 和显示名重建设备
                var newConfig = new CameraDeviceConfig
                {
                    DeviceId = config.DeviceId,
                    DisplayName = config.DisplayName ?? "相机",
                    Settings = config.Settings?.Clone() ?? new CameraSettings(),
                    AssignedSerial = config.AssignedSerial,
                };
                // 直接向 CameraManager 注入（通过内部 AddDevice + ID 覆盖）
                // 注意：AddDevice 生成新 GUID，需要用配置中的 ID
                _cameraManager.AddDeviceWithId(newConfig);

                if (string.IsNullOrEmpty(config.AssignedSerial)) continue;

                var cameras = _cameraManager.Cameras;
                if (cameras.Count == 0)
                    _cameraManager.EnumerateCameras();

                var info = _cameraManager.Cameras.FirstOrDefault(c => c.SerialNumber == config.AssignedSerial);
                if (info != null)
                {
                    try { _cameraManager.OpenDevice(config.DeviceId, info); }
                    catch { }
                }
            }
        }

        // ── 已废弃的兼容方法 ──────────────────────────────────────────

        [System.Obsolete("Use AddCameraDevice instead.", false)]
        public CameraSlot AddSlot(string name, CameraSettings settings = null)
        {
            var config = _cameraManager.AddDevice(name, settings);
            return config != null ? new CameraSlot
            {
                SlotId = config.DeviceId,
                SlotName = config.DisplayName,
                Settings = config.Settings,
                AssignedSerial = config.AssignedSerial,
            } : null;
        }

        [System.Obsolete("Use RemoveCameraDevice instead.", false)]
        public void RemoveSlot(string slotId) => RemoveCameraDevice(slotId);

        [System.Obsolete("Use GetDeviceStatuses instead.", false)]
        public IReadOnlyList<CameraSlot> GetSlots()
        {
            return _cameraManager.GetDeviceStatuses()
                .Select(s => new CameraSlot
                {
                    SlotId = s.DeviceId,
                    SlotName = s.DisplayName,
                    Settings = _cameraManager.GetCameraDevice(s.DeviceId)?.Settings,
                    AssignedSerial = s.AssignedSerial,
                }).ToList().AsReadOnly();
        }

        [System.Obsolete("Use GetDeviceStatuses instead.", false)]
        public CameraSlot GetSlotById(string slotId)
        {
            var status = _cameraManager.GetDeviceStatus(slotId);
            if (status == null) return null;
            return new CameraSlot
            {
                SlotId = status.DeviceId,
                SlotName = status.DisplayName,
                Settings = _cameraManager.GetCameraDevice(slotId)?.Settings,
                AssignedSerial = status.AssignedSerial,
            };
        }

        [System.Obsolete("Use GetFirstDeviceId instead.", false)]
        public CameraSlot GetFirstSlot()
        {
            var id = GetFirstDeviceId();
            return id != null ? GetSlotById(id) : null;
        }
    }
}
