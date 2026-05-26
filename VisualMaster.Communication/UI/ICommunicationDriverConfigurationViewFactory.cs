using System.Windows.Controls;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI
{
    public interface ICommunicationDriverConfigurationViewFactory
    {
        string DriverName { get; }
        UserControl CreateConfigurationView(CommunicationDeviceConfig config);
    }
}
