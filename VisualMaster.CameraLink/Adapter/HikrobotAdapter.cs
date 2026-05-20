using VisualMaster.CameraLink.API;
using System;
using System.Collections.Generic;
using MvCameraControl;

namespace VisualMaster.CameraLink.Adapter
{
    /// <summary>
    /// 海康机器人 MVS SDK 适配器。
    /// 负责 SDK 生命周期管理和相机扫描，为每台发现的相机创建 <see cref="HikrobotDevice"/>。
    /// </summary>
    public sealed class HikrobotAdapter : ICameraAdapter
    {
        public string AdapterName => "Hikrobot MVS";

        public bool IsAvailable
        {
            get
            {
                try
                {
                    // 如果 DLL 不存在，访问 SDKSystem 会在首次 JIT 时抛出异常
                    var _ = typeof(SDKSystem);
                    return true;
                }
                catch { return false; }
            }
        }

        public void InitializeSdk()
        {
            SDKSystem.Initialize();
        }

        public void FinalizeSdk()
        {
            SDKSystem.Finalize();
        }

        public IReadOnlyList<DiscoveredCamera> Scan()
        {
            var result = new List<DiscoveredCamera>();

            var tLayerTypes = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
                | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice
                | DeviceTLayerType.MvGenTLCameraLinkDevice | DeviceTLayerType.MvGenTLXoFDevice;

            int ret = DeviceEnumerator.EnumDevices(tLayerTypes, out List<IDeviceInfo> devInfoList);
            if (ret != MvError.MV_OK || devInfoList == null)
                return result;

            foreach (var devInfo in devInfoList)
            {
                var cam = new DiscoveredCamera
                {
                    UniqueId        = devInfo.SerialNumber ?? "",
                    ModelName       = devInfo.ModelName ?? "",
                    SerialNumber    = devInfo.SerialNumber ?? "",
                    ManufacturerName = devInfo.ManufacturerName ?? "",
                    TransportType   = TransportTypeToString(devInfo.TLayerType),
                    DeviceVersion   = devInfo.DeviceVersion ?? "",
                    AdapterName     = AdapterName,
                    RawInfo         = devInfo,
                };

                if (devInfo is IGigEDeviceInfo gigeInfo)
                    cam.IpAddress = UintToIpString(gigeInfo.CurrentIp);

                result.Add(cam);
            }

            return result;
        }

        public ICameraDeviceDriver CreateDevice(DiscoveredCamera discovered)
        {
            if (discovered == null)
                throw new ArgumentNullException(nameof(discovered));
            if (!CanHandle(discovered))
                throw new InvalidOperationException($"此适配器无法处理相机：{discovered}");

            return new HikrobotDevice(discovered);
        }

        public bool CanHandle(DiscoveredCamera discovered)
            => discovered?.AdapterName == AdapterName && discovered.RawInfo is IDeviceInfo;

        // ── 内部辅助 ──────────────────────────────────────────────

        private static string UintToIpString(uint ip)
            => $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";

        private static string TransportTypeToString(DeviceTLayerType t)
        {
            switch (t)
            {
                case DeviceTLayerType.MvGigEDevice:     return "GigE";
                case DeviceTLayerType.MvUsbDevice:      return "USB3";
                case DeviceTLayerType.MvGenTLGigEDevice:    return "GenTL-GigE";
                case DeviceTLayerType.MvGenTLCXPDevice:     return "GenTL-CXP";
                case DeviceTLayerType.MvGenTLCameraLinkDevice: return "CameraLink";
                case DeviceTLayerType.MvGenTLXoFDevice: return "GenTL-XoF";
                default: return t.ToString();
            }
        }
    }
}
