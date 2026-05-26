using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VisualMaster.CameraLink.Api
{
    public interface ICameraManager : IDisposable
    {
        bool IsInitialized { get; }
        RuntimeDiagnosticsHub Diagnostics { get; set; }
        void Initialize();
        Task InitializeRuntimeAsync(CameraSystemConfig config);
        void LoadConfig(CameraSystemConfig config);

        IReadOnlyList<CameraInfo> Cameras { get; }
        List<CameraInfo> EnumerateCameras();
        Task<List<CameraInfo>> EnumerateCamerasAsync(CancellationToken cancellationToken = default(CancellationToken));
        void ApplyConfiguredDevices();

        IReadOnlyList<CameraDeviceConfig> CameraDevices { get; }
        CameraDeviceConfig GetCameraDevice(string deviceId);
        CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null);
        void RemoveDevice(string deviceId);
        void UpdateDeviceSettings(string deviceId, CameraSettings settings);

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
        bool TryGrabImage(string deviceId, out System.Drawing.Bitmap bitmap, int timeoutMs);
        string[] GetAvailablePixelFormats(string deviceId);
        string[] GetAvailableTriggerSources(string deviceId);

        event EventHandler<CameraDeviceConfig> DeviceOpened;
        event EventHandler<CameraDeviceConfig> DeviceClosed;
    }
}
