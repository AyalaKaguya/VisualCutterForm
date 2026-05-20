namespace VisualMaster.Api
{
    public class CameraDeviceConfig
    {
        public string DeviceId { get; set; }
        public string DisplayName { get; set; }
        public string AssignedSerial { get; set; }
        public CameraSettings Settings { get; set; } = new CameraSettings();

        public CameraDeviceConfig Clone()
        {
            return new CameraDeviceConfig
            {
                DeviceId = DeviceId,
                DisplayName = DisplayName,
                AssignedSerial = AssignedSerial,
                Settings = Settings?.Clone() ?? new CameraSettings(),
            };
        }
    }
}