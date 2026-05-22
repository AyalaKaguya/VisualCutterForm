using System;
using System.Windows;
using System.Windows.Controls;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public partial class CommunicationManagerPanel : UserControl
    {
        private readonly CommunicationSystemConfig _config;
        private readonly CommunicationManagerViewModel _viewModel;
        private DeviceManagementControl _devicePage;

        public CommunicationManagerPanel()
            : this(new CommunicationManager(), new CommunicationSystemConfig())
        {
        }

        public CommunicationManagerPanel(CommunicationManager manager, CommunicationSystemConfig config)
            : this(new CommunicationManagerViewModel(manager, config))
        {
            _config = config;
        }

        public CommunicationManagerPanel(CommunicationManagerViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _config = viewModel.Config;
            InitializeComponent();
            ShowDevicePage();
        }

        private void ShowDevicePage()
        {
            if (_devicePage == null)
                _devicePage = new DeviceManagementControl(_viewModel);
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
