namespace VisualMaster.Api
{
    /// <summary>
    /// 相机设备的运行时状态快照（只读）。
    /// 配置信息请使用 <see cref="CameraDeviceConfig"/>。
    /// </summary>
    public sealed class CameraDeviceStatus
    {
        public string DeviceId { get; set; }
        public string DisplayName { get; set; }
        public bool IsConnected { get; set; }
        public bool IsGrabbing { get; set; }
        public CameraInfo AssignedCamera { get; set; }
        public string AssignedSerial { get; set; }
    }
}
