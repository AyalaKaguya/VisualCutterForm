using System.Windows.Controls;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public sealed class UartDriverConfigurationViewFactory : ICommunicationDriverConfigurationViewFactory
    {
        public string DriverName => "UART";

        public UserControl CreateConfigurationView(CommunicationDeviceConfig config)
        {
            return new UartDriverConfigControl(new UartDriverConfigViewModel(config));
        }
    }
}
