using System;
using System.Collections.Generic;
using System.Drawing;

namespace VisualMaster.Api
{
    public interface ICameraManager : IDisposable
    {
        bool IsInitialized { get; }
        RuntimeDiagnosticsHub Diagnostics { get; set; }
        void Initialize();

        /// <summary>
        /// 将上层提供的相机系统配置注入管理器。
        /// 必须在 Initialize() 之前或之后立即调用；调用后管理器拥有对该配置对象的引用。
        /// </summary>
        void LoadConfig(CameraSystemConfig config);

        IReadOnlyList<CameraInfo> Cameras { get; }
        List<CameraInfo> EnumerateCameras();

        // ── 设备配置 API ──────────────────────────────────────────────
        IReadOnlyList<CameraDeviceConfig> CameraDevices { get; }
        CameraDeviceConfig GetCameraDevice(string deviceId);

        /// <summary>添加设备定义（不自动连接）。</summary>
        CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null);

        void RemoveDevice(string deviceId);
        void UpdateDeviceSettings(string deviceId, CameraSettings settings);

        // ── 运行时状态 API ────────────────────────────────────────────
        IReadOnlyList<CameraDeviceStatus> GetDeviceStatuses();
        CameraDeviceStatus GetDeviceStatus(string deviceId);

        void OpenDevice(string deviceId, CameraInfo info);
        void CloseDevice(string deviceId);
        void StartGrabbing(string deviceId);
        void StopGrabbing(string deviceId);
        void TriggerSoftware(string deviceId);
        bool IsDeviceOpen(string deviceId);
        bool IsDeviceGrabbing(string deviceId);
        CameraInfo GetAssignedCameraInfo(string deviceId);

        ImageFifo GetFifo(string deviceId);
        bool TryGrabImage(string deviceId, out Bitmap bitmap, int timeoutMs);
        string[] GetAvailablePixelFormats(string deviceId);

        // ── 已废弃的槽位 API（保留向后兼容，将在后续版本删除）────────────
        [Obsolete("Use AddDevice / CameraDevices instead.", false)]
        IReadOnlyList<CameraSlot> Slots { get; }

        [Obsolete("Use AddDevice instead.", false)]
        CameraSlot AddSlot(string name, CameraSettings settings = null);

        [Obsolete("Use RemoveDevice instead.", false)]
        void RemoveSlot(string slotId);

        [Obsolete("Use OpenDevice instead.", false)]
        void OpenSlot(string slotId, CameraInfo info);

        [Obsolete("Use CloseDevice instead.", false)]
        void CloseSlot(string slotId);

        [Obsolete("Use IsDeviceOpen instead.", false)]
        bool IsSlotOpen(string slotId);

        // ── 事件 ───────────────────────────────────────────────────────
        /// <summary>设备已打开（连接到相机）。</summary>
        event EventHandler<CameraDeviceConfig> DeviceOpened;

        /// <summary>设备已关闭。</summary>
        event EventHandler<CameraDeviceConfig> DeviceClosed;

        [Obsolete("Use DeviceOpened instead.", false)]
        event EventHandler<CameraSlot> SlotOpened;

        [Obsolete("Use DeviceClosed instead.", false)]
        event EventHandler<CameraSlot> SlotClosed;
    }
}
