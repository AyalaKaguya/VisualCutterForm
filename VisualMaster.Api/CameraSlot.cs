namespace VisualMaster.Api
{
    public class CameraSlot
    {
        public string SlotId { get; set; }
        public string SlotName { get; set; }
        public CameraSettings Settings { get; set; }
        public CameraInfo AssignedCamera { get; set; }
        public string AssignedSerial { get; set; }
        public ImageFifo Fifo { get; set; }
        public ICamera Camera { get; set; }
        public bool IsConnected { get; set; }
        public bool IsGrabbing { get; set; }
    }
}
