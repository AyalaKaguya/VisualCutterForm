using System;

namespace VisualMaster.Api
{
    /// <summary>
    /// 已废弃的串口槽位类。配置请使用 <see cref="SerialDeviceConfig"/>。
    /// </summary>
    [Obsolete("Use SerialDeviceConfig for configuration.", false)]
    public class SerialSlot
    {
        public string SlotId { get; set; }
        public string SlotName { get; set; }
        public string PortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";
    }
}

