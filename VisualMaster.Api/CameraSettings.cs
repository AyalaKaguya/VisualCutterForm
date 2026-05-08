namespace VisualMaster.Api
{
    public class CameraSettings
    {
        public double ExposureTimeUs { get; set; } = 5000;
        public double GainRaw { get; set; } = 0;
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;
        public string PixelFormat { get; set; } = "";
        public TriggerModeEnum TriggerMode { get; set; } = TriggerModeEnum.Continuous;
        public string TriggerSource { get; set; } = "Software";
        public string TriggerActivation { get; set; } = "RisingEdge";
        public int FifoCapacity { get; set; } = 10;

        public CameraSettings Clone()
        {
            return (CameraSettings)MemberwiseClone();
        }
    }
}
