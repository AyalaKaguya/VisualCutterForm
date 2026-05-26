using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Api;

namespace VisualCutterForm.Legacy
{
    [Obsolete("Use VisualMaster.CameraLink.Core.CameraManager directly.")]
    public class CameraManager : ICameraManager
    {
        public bool IsInitialized => false;
        public RuntimeDiagnosticsHub Diagnostics { get; set; }
        public IReadOnlyList<CameraInfo> Cameras => new List<CameraInfo>().AsReadOnly();
        public IReadOnlyList<CameraDeviceConfig> CameraDevices => new List<CameraDeviceConfig>().AsReadOnly();

        public CameraManager() { }
        public event EventHandler<CameraDeviceConfig> DeviceOpened { add { } remove { } }
        public event EventHandler<CameraDeviceConfig> DeviceClosed { add { } remove { } }

        public void Initialize() { }
        public Task InitializeRuntimeAsync(CameraSystemConfig config) => Task.CompletedTask;
        public void LoadConfig(CameraSystemConfig config) { }
        public List<CameraInfo> EnumerateCameras() => new List<CameraInfo>();
        public Task<List<CameraInfo>> EnumerateCamerasAsync(CancellationToken ct = default) => Task.FromResult(new List<CameraInfo>());
        public void ApplyConfiguredDevices() { }
        public CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null) => new CameraDeviceConfig { DeviceId = Guid.NewGuid().ToString(), DisplayName = displayName };
        public CameraDeviceConfig GetCameraDevice(string deviceId) => null;
        public void RemoveDevice(string deviceId) { }
        public void UpdateDeviceSettings(string deviceId, CameraSettings settings) { }
        public IReadOnlyList<CameraDeviceStatus> GetDeviceStatuses() => new List<CameraDeviceStatus>().AsReadOnly();
        public CameraDeviceStatus GetDeviceStatus(string deviceId) => null;
        public void OpenDevice(string deviceId, CameraInfo info) { }
        public void CloseDevice(string deviceId) { }
        public void StartGrabbing(string deviceId) { }
        public void StopGrabbing(string deviceId) { }
        public void TriggerSoftware(string deviceId) { }
        public bool IsDeviceOpen(string deviceId) => false;
        public bool IsDeviceGrabbing(string deviceId) => false;
        public CameraInfo GetAssignedCameraInfo(string deviceId) => null;
        public ImageFifo GetFifo(string deviceId) => null;
        public bool TryGrabImage(string deviceId, out Bitmap bitmap, int timeoutMs) { bitmap = null; return false; }
        public string[] GetAvailablePixelFormats(string deviceId) => new string[0];
        public string[] GetAvailableTriggerSources(string deviceId) => new string[0];
        public void AddDeviceWithId(CameraDeviceConfig config) { }
        public void CloseAllDevices() { }
        public void Dispose() { }
    }
}
