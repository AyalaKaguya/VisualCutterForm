using System.Windows.Controls;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public sealed class TcpDriverConfigurationViewFactory : ICommunicationDriverConfigurationViewFactory
    {
        public string DriverName => "TCP";

        public UserControl CreateConfigurationView(CommunicationDeviceConfig config)
        {
            return new TcpDriverConfigControl(new TcpDriverConfigViewModel(config));
        }
    }
}
