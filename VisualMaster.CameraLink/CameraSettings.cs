namespace VisualMaster.CameraLink
{
    public class CameraSettings
    {
        public string SerialNumber { get; set; } = "";
        public string ModelName { get; set; } = "";

        public bool TriggerEnabled { get; set; }
        public string TriggerSource { get; set; } = "Software";
        public string TriggerActivation { get; set; } = "RisingEdge";

        public float ExposureTimeUs { get; set; } = 5000f;
        public float Gain { get; set; } = 0f;

        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;

        public int FifiCapacity { get; set; } = 10;

        public CameraSettings Clone()
        {
            return (CameraSettings)MemberwiseClone();
        }
    }
}
