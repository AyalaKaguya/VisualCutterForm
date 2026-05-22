using System;
using System.Collections.Generic;
using System.Windows.Controls;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.UI;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.Driver
{
    public sealed class TcpDriverFactory : ICommunicationDriverFactory
    {
        public string DriverName => "TCP";
        public string DisplayName => "TCP 网络";

        public CommunicationDeviceConfig CreateDefaultConfig(IReadOnlyList<ICommunicationDriver> existingDevices)
        {
            return new CommunicationDeviceConfig
            {
                DeviceId = Guid.NewGuid().ToString("N"),
                DriverName = DriverName,
                DisplayName = $"TCP{existingDevices.Count + 1}",
                IsEnabled = false,
                DriverSettings = new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1",
                    ["Port"] = "502",
                },
                Blocks = new List<CommunicationBlockConfig>
                {
                    new CommunicationBlockConfig
                    {
                        BlockId = Guid.NewGuid().ToString("N"),
                        Name = "TCP 数据",
                        BlockName = "TCP 数据",
                        Address = "127.0.0.1:502",
                        DataType = CommunicationBlockDataType.Bytes,
                        PollingEnabled = false,
                    },
                },
            };
        }

        public ICommunicationDriver CreateDriver()
        {
            return new TcpDriver();
        }

        public UserControl CreateConfigurationView(CommunicationDeviceConfig config)
        {
            return new TcpDriverConfigControl(new TcpDriverConfigViewModel(config));
        }
    }
}
