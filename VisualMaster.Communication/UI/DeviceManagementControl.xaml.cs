using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public partial class DeviceManagementControl : UserControl
    {
        private readonly CommunicationManagerViewModel _viewModel;
        private bool _suppressDeviceToggle;

        public DeviceManagementControl(CommunicationManagerViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            InitializeComponent();
            BlockList.BlocksChanged += OnBlocksChanged;
            BlockList.MonitorRequested += OnMonitorRequested;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            LoadSelectedDevice();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CommunicationManagerViewModel.SelectedDevice))
                LoadSelectedDevice();
        }

        private void LoadSelectedDevice()
        {
            var deviceVm = _viewModel.SelectedDevice;
            if (deviceVm == null)
            {
                DriverConfigHost.Content = new TextBlock
                {
                    Text = "点击设备列表右上角 + 选择驱动后开始配置。",
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(12),
                };
                BlockList.Visibility = Visibility.Collapsed;
                return;
            }

            deviceVm.EnsureDriverConfig();

            if (deviceVm.DriverName == "UART")
            {
                deviceVm.DriverConfig.LoadFrom(_viewModel.GetDeviceConfig(deviceVm.DeviceId));
                var control = new UartDriverConfigControl(deviceVm.DriverConfig);
                control.RealtimeRequested += OnUartRealtimeRequested;
                control.ConfigChanged += OnDriverConfigChanged;
                DriverConfigHost.Content = control;
                BlockList.Visibility = Visibility.Collapsed;
            }
            else
            {
                var driver = _viewModel.GetDriver(deviceVm.DeviceId);
                DriverConfigHost.Content = driver?.CreateConfigurationView();
                BlockList.Visibility = Visibility.Visible;
            }
        }

        private void OnAddDeviceClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel.AddUartDeviceCommand.CanExecute(null))
                _viewModel.AddUartDeviceCommand.Execute(null);
        }

        private void OnRemoveDeviceClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel.RemoveDeviceCommand.CanExecute(null))
                _viewModel.RemoveDeviceCommand.Execute(null);
        }

        private async void OnDeviceEnabledToggled(object sender, RoutedEventArgs e)
        {
            if (_suppressDeviceToggle) return;
            if (!(sender is System.Windows.Controls.Primitives.ToggleButton toggle)) return;
            var deviceVm = toggle.DataContext as CommunicationDeviceItemViewModel;
            if (deviceVm == null) return;

            _suppressDeviceToggle = true;
            try
            {
                _viewModel.SelectedDevice = deviceVm;
                if (_viewModel.ToggleDeviceCommand.CanExecute(null))
                    _viewModel.ToggleDeviceCommand.Execute(null);
            }
            finally
            {
                _suppressDeviceToggle = false;
                LoadSelectedDevice();
            }
        }

        private void OnDeviceListRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (item == null) return;
            item.IsSelected = true;

            var menu = new ContextMenu();
            var rename = new MenuItem { Header = "重命名" };
            rename.Click += OnRenameDeviceClick;
            menu.Items.Add(rename);
            item.ContextMenu = menu;
        }

        private void OnRenameDeviceClick(object sender, RoutedEventArgs e)
        {
            var deviceVm = DeviceList.SelectedItem as CommunicationDeviceItemViewModel;
            if (deviceVm == null) return;
            var dialog = new TextInputDialog("重命名设备", "设备标题", deviceVm.DisplayName)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true) return;
            deviceVm.DisplayName = string.IsNullOrWhiteSpace(dialog.Value) ? deviceVm.DisplayName : dialog.Value.Trim();
        }

        private void OnBlocksChanged(object sender, EventArgs e)
        {
            ApplySelectedDeviceChanges();
        }

        private void OnMonitorRequested(object sender, CommunicationBlockConfig e)
        {
            if (_viewModel.SelectedDevice == null || e == null) return;
            var block = _viewModel.FindBlock(_viewModel.SelectedDevice.DeviceId, e.BlockId);
            if (block == null) return;
            new RawBytesMonitorWindow(block)
            {
                Owner = Window.GetWindow(this),
            }.Show();
        }

        private void OnUartRealtimeRequested(object sender, CommunicationBlockConfig e)
        {
            if (_viewModel.SelectedDevice == null || e == null) return;
            ApplySelectedDeviceChanges();
            var block = _viewModel.FindBlock(_viewModel.SelectedDevice.DeviceId, e.BlockId);
            if (block == null) return;
            new RawBytesMonitorWindow(block)
            {
                Owner = Window.GetWindow(this),
                Title = $"实时监视 - {e.Name}",
            }.Show();
        }

        private void OnDriverConfigChanged(object sender, EventArgs e)
        {
            ApplySelectedDeviceChanges();
        }

        private void ApplySelectedDeviceChanges()
        {
            if (_viewModel.SelectedDevice?.DriverConfig != null)
            {
                var config = _viewModel.GetDeviceConfig(_viewModel.SelectedDevice.DeviceId);
                if (config != null)
                {
                    _viewModel.SelectedDevice.DriverConfig.ToDeviceConfig(config);
                    _viewModel.UpdateDeviceConfig(config);
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T typed) return typed;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
