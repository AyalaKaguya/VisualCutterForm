using System.Collections.Generic;
using System.Windows.Controls;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.UI;

namespace VisualMaster.Communication.Driver
{
    public sealed class UartDriverFactory : ICommunicationDriverFactory
    {
        public string DriverName => "UART";
        public string DisplayName => "UART 串口";

        public CommunicationDeviceConfig CreateDefaultConfig(string interfaceName)
        {
            var name = string.IsNullOrWhiteSpace(interfaceName) ? "COM1" : interfaceName.Trim();
            return new CommunicationDeviceConfig
            {
                DriverName = DriverName,
                InterfaceName = name,
                DisplayName = $"{DriverName}-{name}",
                DriverSettings = new Dictionary<string, string>
                {
                    ["PortName"] = name,
                    ["BaudRate"] = "9600",
                    ["DataBits"] = "8",
                    ["Parity"] = "None",
                    ["StopBits"] = "One",
                    ["Handshake"] = "None",
                },
            };
        }

        public ICommunicationDriver CreateDriver()
        {
            return new UartDriver();
        }

        public UserControl CreateConfigurationView(CommunicationDeviceConfig config)
        {
            return new UartDriverConfigControl(config);
        }
    }
}
