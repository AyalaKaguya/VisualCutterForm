namespace VisualMaster.CameraLink.API
{
    /// <summary>
    /// 适配器扫描到的物理相机描述。
    /// 包含足够的信息，用于后续与 <see cref="VisualMaster.Api.CameraDeviceConfig"/> 匹配和打开。
    /// </summary>
    public sealed class DiscoveredCamera
    {
        /// <summary>相机的全局唯一硬件标识（通常为序列号）。</summary>
        public string UniqueId { get; set; }

        public string ModelName { get; set; }
        public string SerialNumber { get; set; }
        public string ManufacturerName { get; set; }
        public string TransportType { get; set; }
        public string DeviceVersion { get; set; }

        /// <summary>GigE 相机的 IP 地址（非 GigE 为 null）。</summary>
        public string IpAddress { get; set; }

        /// <summary>识别到本相机的适配器名称。</summary>
        public string AdapterName { get; set; }

        /// <summary>SDK 原始设备信息对象，供适配器内部使用。</summary>
        public object RawInfo { get; set; }

        public override string ToString()
            => $"[{AdapterName}] {ModelName} | SN: {SerialNumber}";
    }
}
