using System;
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

        public CommunicationDeviceConfig CreateDefaultConfig(IReadOnlyList<ICommunicationDriver> existingDevices)
        {
            return new CommunicationDeviceConfig
            {
                DeviceId = Guid.NewGuid().ToString("N"),
                DriverName = DriverName,
                DisplayName = $"UART{existingDevices.Count + 1}",
                IsEnabled = true,
                DriverSettings = new Dictionary<string, string>
                {
                    ["PortName"] = "COM1",
                    ["BaudRate"] = "9600",
                    ["DataBits"] = "8",
                    ["Parity"] = "None",
                    ["StopBits"] = "One",
                    ["Handshake"] = "None",
                },
                Blocks = new List<CommunicationBlockConfig>
                {
                    new CommunicationBlockConfig
                    {
                        BlockId = Guid.NewGuid().ToString("N"),
                        Name = "串口数据",
                        BlockName = "串口数据",
                        Address = "COM1",
                        DataType = CommunicationBlockDataType.Bytes,
                        PollingEnabled = true,
                        PollingIntervalMs = 500,
                        PollingTimeoutMs = 1000,
                    },
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
