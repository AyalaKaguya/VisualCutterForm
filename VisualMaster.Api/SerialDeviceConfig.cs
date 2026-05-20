namespace VisualMaster.Api
{
    public class SerialDeviceConfig
    {
        public string DeviceId { get; set; }
        public string DisplayName { get; set; }
        public string PortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";

        public SerialDeviceConfig Clone()
        {
            return new SerialDeviceConfig
            {
                DeviceId = DeviceId,
                DisplayName = DisplayName,
                PortName = PortName,
                BaudRate = BaudRate,
                DataBits = DataBits,
                Parity = Parity,
                StopBits = StopBits,
            };
        }
    }
}