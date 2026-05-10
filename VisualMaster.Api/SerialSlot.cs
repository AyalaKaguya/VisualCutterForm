namespace VisualMaster.Api
{
    public class SerialSlot
    {
        public string SlotId { get; set; }
        public string SlotName { get; set; }
        public string PortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";
        public ISerialPort Port { get; set; }
        public bool IsConnected { get; set; }
    }
}
