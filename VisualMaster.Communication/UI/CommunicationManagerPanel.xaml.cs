using System;
using System.Windows;
using System.Windows.Controls;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;

namespace VisualMaster.Communication.UI
{
    public partial class CommunicationManagerPanel : UserControl
    {
        private readonly CommunicationManager _manager;
        private readonly CommunicationSystemConfig _config;
        private DeviceManagementControl _devicePage;

        public CommunicationManagerPanel()
            : this(new CommunicationManager(), new CommunicationSystemConfig())
        {
        }

        public CommunicationManagerPanel(CommunicationManager manager, CommunicationSystemConfig config)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            InitializeComponent();
            _manager.LoadConfig(_config);
            ShowDevicePage();
        }

        private void ShowDevicePage()
        {
            if (_devicePage == null)
                _devicePage = new DeviceManagementControl(_manager, _config);
            PageHost.Content = _devicePage;
        }

        private void ShowContentPage(UserControl control)
        {
            PageHost.Content = control;
        }

        private void OnDevicesPage(object sender, RoutedEventArgs e)
        {
            ShowDevicePage();
        }

        private void OnInputPage(object sender, RoutedEventArgs e)
        {
            var control = new InputEventsControl();
            control.LoadConfig(_config);
            ShowContentPage(control);
        }

        private void OnOutputPage(object sender, RoutedEventArgs e)
        {
            var control = new OutputEventsControl();
            control.LoadConfig(_config);
            ShowContentPage(control);
        }

        private void OnHeartbeatPage(object sender, RoutedEventArgs e)
        {
            var control = new HeartbeatControl();
            control.LoadConfig(_config);
            ShowContentPage(control);
        }
    }
}
