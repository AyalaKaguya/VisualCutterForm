using System;
using System.Collections.Generic;
using System.Threading;
using MvCameraControl;

namespace VisualCutterForm.Lib
{
    public class CameraManager : IDisposable
    {
        private readonly List<CameraInfo> _cameras = new List<CameraInfo>();
        private volatile bool _disposed;

        public IReadOnlyList<CameraInfo> Cameras => _cameras.AsReadOnly();
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized) return;
            SDKSystem.Initialize();
            IsInitialized = true;
        }

        public int EnumerateDevices()
        {
            _cameras.Clear();

            if (!IsInitialized)
                Initialize();

            var tLayerTypes = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
                | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice
                | DeviceTLayerType.MvGenTLCameraLinkDevice | DeviceTLayerType.MvGenTLXoFDevice;

            int ret = DeviceEnumerator.EnumDevices(tLayerTypes, out List<IDeviceInfo> devInfoList);
            if (ret != MvError.MV_OK) return 0;

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

                _cameras.Add(info);
            }

            return _cameras.Count;
        }

        public ICamera OpenCamera(CameraInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var camera = new MvsCamera(info);
            camera.Open();
            return camera;
        }

        public ICamera OpenCamera(int index)
        {
            if (index < 0 || index >= _cameras.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return OpenCamera(_cameras[index]);
        }

        public ICamera OpenFirstCamera()
        {
            if (_cameras.Count == 0)
                EnumerateDevices();
            if (_cameras.Count == 0)
                throw new InvalidOperationException("No cameras found.");
            return OpenCamera(_cameras[0]);
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

            if (IsInitialized)
            {
                SDKSystem.Finalize();
                IsInitialized = false;
            }
        }
    }
}
