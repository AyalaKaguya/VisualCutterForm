using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationDeviceConfig
    {
        public string DeviceId { get; set; } = Guid.NewGuid().ToString("N");
        public string DisplayName { get; set; }
        public string DriverName { get; set; }
        public string InterfaceName { get; set; }
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, string> DriverSettings { get; set; } = new Dictionary<string, string>();
        public List<CommunicationBlockConfig> Blocks { get; set; } = new List<CommunicationBlockConfig>();

        public CommunicationDeviceConfig Clone()
        {
            return new CommunicationDeviceConfig
            {
                DeviceId = DeviceId,
                DisplayName = DisplayName,
                DriverName = DriverName,
                InterfaceName = InterfaceName,
                IsEnabled = IsEnabled,
                DriverSettings = DriverSettings != null
                    ? new Dictionary<string, string>(DriverSettings)
                    : new Dictionary<string, string>(),
                Blocks = Blocks?.Select(b => b.Clone()).ToList() ?? new List<CommunicationBlockConfig>(),
            };
        }
    }
}
