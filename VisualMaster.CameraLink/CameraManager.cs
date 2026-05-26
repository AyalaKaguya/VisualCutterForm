using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.Api;
using CameraLinkApi = VisualMaster.CameraLink.Api;

namespace VisualMaster.CameraLink
{
    [Obsolete("Use VisualMaster.CameraLink.Core.CameraManager with VisualMaster.CameraLink.Api types instead.")]
    public class CameraManager : ICameraManager
    {
        private readonly Core.CameraManager _inner;

        public CameraManager()
        {
            _inner = new Core.CameraManager();
            _inner.DeviceOpened += (s, e) => DeviceOpened?.Invoke(this, ToApiDeviceConfig(e));
            _inner.DeviceClosed += (s, e) => DeviceClosed?.Invoke(this, ToApiDeviceConfig(e));
        }

        public bool IsInitialized => _inner.IsInitialized;
        public RuntimeDiagnosticsHub Diagnostics { get; set; }
        public IReadOnlyList<CameraInfo> Cameras => _inner.Cameras.Select(ToApiCameraInfo).ToList().AsReadOnly();
        public IReadOnlyList<CameraDeviceConfig> CameraDevices => _inner.CameraDevices.Select(ToApiDeviceConfig).ToList().AsReadOnly();
        public IReadOnlyList<CameraSlot> Slots => new List<CameraSlot>().AsReadOnly();

        public event EventHandler<CameraDeviceConfig> DeviceOpened;
        public event EventHandler<CameraDeviceConfig> DeviceClosed;

        public CameraManager(Core.CameraManager inner) { _inner = inner; }

        public void Initialize() => _inner.Initialize();
        public Task InitializeRuntimeAsync(CameraSystemConfig config) => _inner.InitializeRuntimeAsync(ToLinkSystemConfig(config));
        public void LoadConfig(CameraSystemConfig config) => _inner.LoadConfig(ToLinkSystemConfig(config));
        public List<CameraInfo> EnumerateCameras() => _inner.EnumerateCameras().Select(ToApiCameraInfo).ToList();
        public async Task<List<CameraInfo>> EnumerateCamerasAsync(CancellationToken ct = default) => (await _inner.EnumerateCamerasAsync(ct)).Select(ToApiCameraInfo).ToList();
        public void ApplyConfiguredDevices() => _inner.ApplyConfiguredDevices();
        public CameraDeviceConfig AddDevice(string displayName, CameraSettings settings = null) => ToApiDeviceConfig(_inner.AddDevice(displayName, ToLinkSettings(settings)));
        public CameraDeviceConfig GetCameraDevice(string deviceId) => ToApiDeviceConfig(_inner.GetCameraDevice(deviceId));
        public void RemoveDevice(string deviceId) => _inner.RemoveDevice(deviceId);
        public void UpdateDeviceSettings(string deviceId, CameraSettings settings) => _inner.UpdateDeviceSettings(deviceId, ToLinkSettings(settings));
        public IReadOnlyList<CameraDeviceStatus> GetDeviceStatuses() => _inner.GetDeviceStatuses().Select(ToApiStatus).ToList().AsReadOnly();
        public CameraDeviceStatus GetDeviceStatus(string deviceId) => ToApiStatus(_inner.GetDeviceStatus(deviceId));
        public void OpenDevice(string deviceId, CameraInfo info) => _inner.OpenDevice(deviceId, ToLinkCameraInfo(info));
        public void CloseDevice(string deviceId) => _inner.CloseDevice(deviceId);
        public void StartGrabbing(string deviceId) => _inner.StartGrabbing(deviceId);
        public void StopGrabbing(string deviceId) => _inner.StopGrabbing(deviceId);
        public void TriggerSoftware(string deviceId) => _inner.TriggerSoftware(deviceId);
        public bool IsDeviceOpen(string deviceId) => _inner.IsDeviceOpen(deviceId);
        public bool IsDeviceGrabbing(string deviceId) => _inner.IsDeviceGrabbing(deviceId);
        public CameraInfo GetAssignedCameraInfo(string deviceId) => ToApiCameraInfo(_inner.GetAssignedCameraInfo(deviceId));
        public ImageFifo GetFifo(string deviceId) => null;
        public bool TryGrabImage(string deviceId, out Bitmap bitmap, int timeoutMs) => _inner.TryGrabImage(deviceId, out bitmap, timeoutMs);
        public string[] GetAvailablePixelFormats(string deviceId) => _inner.GetAvailablePixelFormats(deviceId);
        public string[] GetAvailableTriggerSources(string deviceId) => _inner.GetAvailableTriggerSources(deviceId);
        public CameraSlot AddSlot(string name, CameraSettings settings = null) => null;
        public void RemoveSlot(string slotId) { }
        public void OpenSlot(string slotId, CameraInfo info) { }
        public void CloseSlot(string slotId) { }
        public bool IsSlotOpen(string slotId) => false;
        public void AddDeviceWithId(CameraDeviceConfig config) => _inner.AddDeviceWithId(ToLinkDeviceConfig(config));
        public void CloseAllDevices() => _inner.CloseAllDevices();
        public void Dispose() => _inner.Dispose();

        private static CameraLinkApi.CameraDeviceConfig ToLinkDeviceConfig(CameraDeviceConfig c) => c == null ? null : new CameraLinkApi.CameraDeviceConfig { DeviceId = c.DeviceId, DisplayName = c.DisplayName, AssignedSerial = c.AssignedSerial, IsEnabled = c.IsEnabled, Settings = ToLinkSettings(c.Settings) };
        private static CameraDeviceConfig ToApiDeviceConfig(CameraLinkApi.CameraDeviceConfig c) => c == null ? null : new CameraDeviceConfig { DeviceId = c.DeviceId, DisplayName = c.DisplayName, AssignedSerial = c.AssignedSerial, IsEnabled = c.IsEnabled, Settings = ToApiSettings(c.Settings) };
        private static CameraLinkApi.CameraSettings ToLinkSettings(CameraSettings s) => s == null ? new CameraLinkApi.CameraSettings() : new CameraLinkApi.CameraSettings { ExposureTimeUs = s.ExposureTimeUs, GainRaw = s.GainRaw, Width = s.Width, Height = s.Height, OffsetX = s.OffsetX, OffsetY = s.OffsetY, PixelFormat = s.PixelFormat, TriggerMode = (CameraLinkApi.TriggerModeEnum)(int)s.TriggerMode, TriggerSource = s.TriggerSource, TriggerActivation = s.TriggerActivation, FifoCapacity = s.FifoCapacity, MonochromeOutput = s.MonochromeOutput };
        private static CameraSettings ToApiSettings(CameraLinkApi.CameraSettings s) => s == null ? new CameraSettings() : new CameraSettings { ExposureTimeUs = s.ExposureTimeUs, GainRaw = s.GainRaw, Width = s.Width, Height = s.Height, OffsetX = s.OffsetX, OffsetY = s.OffsetY, PixelFormat = s.PixelFormat, TriggerMode = (TriggerModeEnum)(int)s.TriggerMode, TriggerSource = s.TriggerSource, TriggerActivation = s.TriggerActivation, FifoCapacity = s.FifoCapacity, MonochromeOutput = s.MonochromeOutput };
        private static CameraLinkApi.CameraSystemConfig ToLinkSystemConfig(CameraSystemConfig c)
        {
            if (c == null) return null;
            var link = new CameraLinkApi.CameraSystemConfig();
            link.LoadFrom(c.Devices.Select(ToLinkDeviceConfig));
            link.SaveRequested += (s, e) => c.RequestSave();
            return link;
        }
        private static CameraLinkApi.CameraInfo ToLinkCameraInfo(CameraInfo i) => i == null ? null : new CameraLinkApi.CameraInfo { ModelName = i.ModelName, SerialNumber = i.SerialNumber, UserDefinedName = i.UserDefinedName, ManufacturerName = i.ManufacturerName, TransportTypeName = i.TransportTypeName, AdapterName = i.AdapterName, TransportTypeRaw = i.TransportTypeRaw, IpAddress = i.IpAddress, DeviceVersion = i.DeviceVersion, RawInfo = i.RawInfo };
        private static CameraInfo ToApiCameraInfo(CameraLinkApi.CameraInfo i) => i == null ? null : new CameraInfo { ModelName = i.ModelName, SerialNumber = i.SerialNumber, UserDefinedName = i.UserDefinedName, ManufacturerName = i.ManufacturerName, TransportTypeName = i.TransportTypeName, AdapterName = i.AdapterName, TransportTypeRaw = i.TransportTypeRaw, IpAddress = i.IpAddress, DeviceVersion = i.DeviceVersion, RawInfo = i.RawInfo };
        private static CameraDeviceStatus ToApiStatus(CameraLinkApi.CameraDeviceStatus s) => s == null ? null : new CameraDeviceStatus { DeviceId = s.DeviceId, DisplayName = s.DisplayName, IsConnected = s.IsConnected, IsGrabbing = s.IsGrabbing, AssignedCamera = ToApiCameraInfo(s.AssignedCamera), AssignedSerial = s.AssignedSerial };
    }
}
