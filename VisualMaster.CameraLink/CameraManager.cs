using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.Threading;
using MvCameraControl;

namespace VisualMaster.CameraLink
{
    public class CameraManager : ICameraManager
    {
        private readonly List<CameraInfo> _enumeratedCameras = new List<CameraInfo>();
        private readonly List<CameraSlot> _slots = new List<CameraSlot>();
        private volatile bool _disposed;

        public IReadOnlyList<CameraInfo> Cameras => _enumeratedCameras.AsReadOnly();
        public bool IsInitialized { get; private set; }
        public IReadOnlyList<CameraSlot> Slots => _slots.AsReadOnly();

        public event EventHandler<CameraSlot> SlotOpened;
        public event EventHandler<CameraSlot> SlotClosed;

        public void Initialize()
        {
            if (IsInitialized) return;
            SDKSystem.Initialize();
            IsInitialized = true;
        }

        public List<CameraInfo> EnumerateCameras()
        {
            _enumeratedCameras.Clear();

            if (!IsInitialized)
                Initialize();

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
                {
                    info.IpAddress = gigeInfo.CurrentIp;
                }

                _enumeratedCameras.Add(info);
            }

            return _enumeratedCameras;
        }

        public CameraSlot AddSlot(string name, CameraSettings settings = null)
        {
            var slot = new CameraSlot
            {
                SlotId = Guid.NewGuid().ToString(),
                SlotName = name ?? $"相机{_slots.Count + 1}",
                Settings = settings ?? new CameraSettings(),
                Fifo = new ImageFifo(settings?.FifoCapacity ?? 10),
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

        public void OpenSlot(string slotId, CameraInfo info)
        {
            var slot = _slots.Find(s => s.SlotId == slotId);
            if (slot == null)
                throw new InvalidOperationException($"Slot {slotId} not found.");

            if (slot.Camera != null)
                CloseSlot(slotId);

            var camera = new MvsCamera(info);
            camera.Open();
            camera.ApplySettings(slot.Settings);
            camera.ImageAcquired += (s, bmp) =>
            {
                slot.Fifo.Enqueue(bmp);
            };
            camera.Disconnected += (s, e) =>
            {
                slot.IsConnected = false;
            };

            slot.Camera = camera;
            slot.AssignedCamera = info;
            slot.AssignedSerial = info.SerialNumber;
            slot.IsConnected = true;

            SlotOpened?.Invoke(this, slot);
        }

        public void CloseSlot(string slotId)
        {
            var slot = _slots.Find(s => s.SlotId == slotId);
            if (slot == null || slot.Camera == null) return;

            slot.Camera.StopGrabbing();
            slot.Camera.Dispose();
            slot.Camera = null;
            slot.IsConnected = false;
            slot.IsGrabbing = false;

            SlotClosed?.Invoke(this, slot);
        }

        public void StartGrabbing(string slotId)
        {
            var slot = _slots.Find(s => s.SlotId == slotId);
            if (slot?.Camera == null) return;

            slot.Camera.ApplySettings(slot.Settings);
            slot.Camera.StartGrabbing();
            slot.IsGrabbing = true;
        }

        public void StopGrabbing(string slotId)
        {
            var slot = _slots.Find(s => s.SlotId == slotId);
            if (slot?.Camera == null) return;

            slot.Camera.StopGrabbing();
            slot.IsGrabbing = false;
        }

        public void TriggerSoftware(string slotId)
        {
            var slot = _slots.Find(s => s.SlotId == slotId);
            if (slot?.Camera == null)
                throw new InvalidOperationException($"Slot {slotId} is not connected.");

            slot.Camera.TriggerSoftware();
        }

        public bool IsSlotOpen(string slotId)
        {
            var slot = _slots.Find(s => s.SlotId == slotId);
            return slot?.Camera != null;
        }

        public CameraSlot FindSlotBySerial(string serial)
        {
            return _slots.Find(s => s.AssignedSerial == serial);
        }

        public void CloseAllSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot.Camera != null)
                    CloseSlot(slot.SlotId);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            CloseAllSlots();

            if (IsInitialized)
            {
                SDKSystem.Finalize();
                IsInitialized = false;
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
    }
}
