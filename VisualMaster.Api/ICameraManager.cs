using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace VisualMaster.Api
{
    public interface ICameraManager : IDisposable
    {
        bool IsInitialized { get; }
        RuntimeDiagnosticsHub Diagnostics { get; set; }
        void Initialize();
        Task InitializeRuntimeAsync(CameraSystemConfig config);

        /// <summary>
        /// 将上层提供的相机系统配置注入管理器。
        /// 必须在 Initialize() 之前或之后立即调用；调用后管理器拥有对该配置对象的引用。
        /// </summary>
        void LoadConfig(CameraSystemConfig config);

        IReadOnlyList<CameraInfo> Cameras { get; }
        List<CameraInfo> EnumerateCameras();
        Task<List<CameraInfo>> EnumerateCamerasAsync(CancellationToken cancellationToken = default(CancellationToken));
        void ApplyConfiguredDevices();

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
        string[] GetAvailableTriggerSources(string deviceId);

        // ── 事件 ───────────────────────────────────────────────────────
        /// <summary>设备已打开（连接到相机）。</summary>
        event EventHandler<CameraDeviceConfig> DeviceOpened;

        /// <summary>设备已关闭。</summary>
        event EventHandler<CameraDeviceConfig> DeviceClosed;
    }
}
