using System.Windows.Controls;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI
{
    public interface ICommunicationDriverConfigurationViewFactory
    {
        UserControl CreateConfigurationView(CommunicationDeviceConfig config);
    }
}
