using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using MvCameraControl;

namespace VisualMaster.CameraLink
{
    public class CameraManager : ICameraManager
    {
        private readonly List<CameraSlot> _slots = new List<CameraSlot>();
        private readonly List<CameraInfo> _discoveredCameras = new List<CameraInfo>();
        private volatile bool _disposed;
        private bool _sdkInitialized;

        public List<CameraSlot> Slots => _slots;
        public IReadOnlyList<CameraInfo> DiscoveredCameras => _discoveredCameras.AsReadOnly();

        public event EventHandler<CameraSlot> SlotOpened;
        public event EventHandler<CameraSlot> SlotClosed;

        public List<CameraInfo> EnumerateCameras()
        {
            _discoveredCameras.Clear();

            if (!_sdkInitialized)
            {
                SDKSystem.Initialize();
                _sdkInitialized = true;
            }

            var tLayerTypes = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
                | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice
                | DeviceTLayerType.MvGenTLCameraLinkDevice | DeviceTLayerType.MvGenTLXoFDevice;

            int ret = DeviceEnumerator.EnumDevices(tLayerTypes, out List<IDeviceInfo> devInfoList);
            if (ret != MvError.MV_OK) return _discoveredCameras;

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
                {
                    info.IpAddress = gigeInfo.CurrentIp;
                }

                _discoveredCameras.Add(info);
            }

            return _discoveredCameras;
        }

        public CameraSlot AddSlot(string name, CameraSettings settings = null)
        {
            var slot = new CameraSlot
            {
                SlotId = Guid.NewGuid().ToString("N").Substring(0, 8),
                SlotName = name ?? $"相机{_slots.Count + 1}",
                Settings = settings?.Clone() as CameraSettings ?? new CameraSettings(),
            };
            _slots.Add(slot);
            return slot;
        }

        public void RemoveSlot(string slotId)
        {
            var slot = _slots.Find(s => s.SlotId == slotId);
            if (slot == null) return;

            CloseSlot(slotId);
            _slots.Remove(slot);
        }

        public ICamera OpenSlot(string slotId, CameraInfo info)
        {
            var slot = GetSlot(slotId);
            if (slot == null)
                throw new InvalidOperationException($"Slot {slotId} not found.");

            if (slot.Camera != null)
                CloseSlot(slotId);

            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var camera = new MvsCamera(info);
            camera.Open();

            camera.ImageAcquired += (s, bmp) =>
            {
                if (bmp != null && slot.Fifo != null)
                    slot.Fifo.Enqueue(bmp);
            };

            camera.Disconnected += (s, e) =>
            {
                slot.IsConnected = false;
                slot.IsGrabbing = false;
            };

            var fifo = new ImageFifo(slot.Settings.FifoCapacity);

            slot.Camera = camera;
            slot.Fifo = fifo;
            slot.AssignedCamera = info;
            slot.AssignedSerial = info.SerialNumber ?? "";
            slot.AssignedModel = info.ModelName ?? "";
            slot.IsConnected = true;

            SlotOpened?.Invoke(this, slot);
            return camera;
        }

        public void CloseSlot(string slotId)
        {
            var slot = GetSlot(slotId);
            if (slot == null || slot.Camera == null) return;

            slot.Camera.StopGrabbing();
            slot.IsGrabbing = false;
            slot.Camera.Dispose();
            slot.Camera = null;
            slot.Fifo?.Dispose();
            slot.Fifo = null;
            slot.IsConnected = false;

            SlotClosed?.Invoke(this, slot);
        }

        public void TriggerSoftware(string slotId)
        {
            var slot = GetSlot(slotId);
            if (slot?.Camera == null)
                throw new InvalidOperationException($"Camera slot '{slotId}' is not open.");

            slot.Camera.TriggerSoftware();
        }

        public bool IsSlotOpen(string slotId)
        {
            var slot = GetSlot(slotId);
            return slot != null && slot.Camera != null && slot.Camera.IsOpen;
        }

        public CameraSlot GetSlot(string slotId)
        {
            return _slots.Find(s => s.SlotId == slotId);
        }

        public void StartGrabbing(string slotId)
        {
            var slot = GetSlot(slotId);
            if (slot?.Camera == null) return;

            slot.Camera.ApplySettings(slot.Settings);
            slot.Camera.StartGrabbing();
            slot.IsGrabbing = true;
        }

        public void StopGrabbing(string slotId)
        {
            var slot = GetSlot(slotId);
            if (slot?.Camera == null) return;

            slot.Camera.StopGrabbing();
            slot.IsGrabbing = false;
        }

        public void CloseAllSlots()
        {
            foreach (var slot in new List<CameraSlot>(_slots))
            {
                CloseSlot(slot.SlotId);
                _slots.Remove(slot);
            }
        }

        public void TryReconnectSlot(string slotId)
        {
            var slot = GetSlot(slotId);
            if (slot == null || slot.IsConnected || string.IsNullOrEmpty(slot.AssignedSerial))
                return;

            var cameras = EnumerateCameras();
            var info = cameras.Find(c => c.SerialNumber == slot.AssignedSerial);
            if (info != null)
            {
                try
                {
                    OpenSlot(slotId, info);
                }
                catch
                {
                }
            }
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            CloseAllSlots();

            if (_sdkInitialized)
            {
                SDKSystem.Finalize();
                _sdkInitialized = false;
            }
        }
    }
}
