namespace VisualMaster.CameraLink.Api
{
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
