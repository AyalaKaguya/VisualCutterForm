using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Api;
using VisualMaster.CameraLink.Core;

namespace VisualMaster.CameraLink
{
    [Obsolete("Use VisualMaster.CameraLink.Core.CameraManager instead.")]
    public class CameraManager : ICameraManager
    {
        private readonly Core.CameraManager _inner;

        public CameraManager()
        {
            _inner = new Core.CameraManager();
        }

        public bool IsInitialized => _inner.IsInitialized;
        public RuntimeDiagnosticsHub Diagnostics { get => _inner.Diagnostics; set => _inner.Diagnostics = value; }
        public IReadOnlyList<CameraInfo> Cameras => _inner.Cameras;
        public IReadOnlyList<CameraDeviceConfig> CameraDevices => _inner.CameraDevices;

        public event EventHandler<CameraDeviceConfig> DeviceOpened { add => _inner.DeviceOpened += value; remove => _inner.DeviceOpened -= value; }
        public event EventHandler<CameraDeviceConfig> DeviceClosed { add => _inner.DeviceClosed += value; remove => _inner.DeviceClosed -= value; }

        public void Initialize() => _inner.Initialize();
        public Task InitializeRuntimeAsync(CameraSystemConfig config) => _inner.InitializeRuntimeAsync(config);
        public void LoadConfig(CameraSystemConfig config) => _inner.LoadConfig(config);
        public List<CameraInfo> EnumerateCameras() => _inner.EnumerateCameras();
        public Task<List<CameraInfo>> EnumerateCamerasAsync(CancellationToken ct = default) => _inner.EnumerateCamerasAsync(ct);
        public void ApplyConfiguredDevices() => _inner.ApplyConfiguredDevices();
        public CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null) => _inner.AddDevice(displayName, settings);
        public CameraDeviceConfig GetCameraDevice(string deviceId) => _inner.GetCameraDevice(deviceId);
        public void RemoveDevice(string deviceId) => _inner.RemoveDevice(deviceId);
        public void UpdateDeviceSettings(string deviceId, CameraSettings settings) => _inner.UpdateDeviceSettings(deviceId, settings);
        public IReadOnlyList<CameraDeviceStatus> GetDeviceStatuses() => _inner.GetDeviceStatuses();
        public CameraDeviceStatus GetDeviceStatus(string deviceId) => _inner.GetDeviceStatus(deviceId);
        public void OpenDevice(string deviceId, CameraInfo info) => _inner.OpenDevice(deviceId, info);
        public void CloseDevice(string deviceId) => _inner.CloseDevice(deviceId);
        public void StartGrabbing(string deviceId) => _inner.StartGrabbing(deviceId);
        public void StopGrabbing(string deviceId) => _inner.StopGrabbing(deviceId);
        public void TriggerSoftware(string deviceId) => _inner.TriggerSoftware(deviceId);
        public bool IsDeviceOpen(string deviceId) => _inner.IsDeviceOpen(deviceId);
        public bool IsDeviceGrabbing(string deviceId) => _inner.IsDeviceGrabbing(deviceId);
        public CameraInfo GetAssignedCameraInfo(string deviceId) => _inner.GetAssignedCameraInfo(deviceId);
        public ImageFifo GetFifo(string deviceId) => _inner.GetFifo(deviceId);
        public bool TryGrabImage(string deviceId, out Bitmap bitmap, int timeoutMs) => _inner.TryGrabImage(deviceId, out bitmap, timeoutMs);
        public string[] GetAvailablePixelFormats(string deviceId) => _inner.GetAvailablePixelFormats(deviceId);
        public string[] GetAvailableTriggerSources(string deviceId) => _inner.GetAvailableTriggerSources(deviceId);
        public void AddDeviceWithId(CameraDeviceConfig config) => _inner.AddDeviceWithId(config);
        public void CloseAllDevices() => _inner.CloseAllDevices();
        public void Dispose() => _inner.Dispose();
    }
}
