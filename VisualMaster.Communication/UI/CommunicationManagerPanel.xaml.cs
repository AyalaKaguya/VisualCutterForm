using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private Button _activeNavButton;

        private static readonly SolidColorBrush ActiveBg = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
        private static readonly SolidColorBrush ActiveBorder = new SolidColorBrush(Color.FromRgb(0x2E, 0xAA, 0x5F));
        private static readonly SolidColorBrush InactiveBg = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25));
        private static readonly SolidColorBrush InactiveBorder = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30));

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
            _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            _config = viewModel.Config;
            InitializeComponent();
            ShowDevicePage();
        }

        private void SetActivePage(Button button)
        {
            if (_activeNavButton != null)
            {
                _activeNavButton.Background = InactiveBg;
                _activeNavButton.BorderBrush = InactiveBorder;
            }
            _activeNavButton = button;
            button.Background = ActiveBg;
            button.BorderBrush = ActiveBorder;
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
            SetActivePage(BtnDevices);
            ShowDevicePage();
        }

        private void OnInputPage(object sender, RoutedEventArgs e)
        {
            SetActivePage(BtnInput);
            var control = new InputEventsControl();
            control.LoadConfig(_config);
            ShowContentPage(control);
        }

        private void OnOutputPage(object sender, RoutedEventArgs e)
        {
            SetActivePage(BtnOutput);
            var control = new OutputEventsControl();
            control.LoadConfig(_config);
            ShowContentPage(control);
        }

        private void OnHeartbeatPage(object sender, RoutedEventArgs e)
        {
            SetActivePage(BtnHeartbeat);
            var control = new HeartbeatControl();
            control.LoadConfig(_config);
            ShowContentPage(control);
        }
    }
}
