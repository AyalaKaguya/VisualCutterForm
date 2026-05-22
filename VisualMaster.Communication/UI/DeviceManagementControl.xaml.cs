using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Core;

namespace VisualMaster.Communication.UI
{
    public partial class DeviceManagementControl : UserControl
    {
        private readonly CommunicationManager _manager;
        private readonly CommunicationSystemConfig _config;
        private CommunicationDeviceConfig _selectedDevice;
        private bool _suppressDeviceToggle;

        public DeviceManagementControl(CommunicationManager manager, CommunicationSystemConfig config)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            InitializeComponent();
            BlockList.BlocksChanged += OnBlocksChanged;
            BlockList.MonitorRequested += OnMonitorRequested;
            RefreshDevices();
            if (DeviceList.Items.Count > 0)
                DeviceList.SelectedIndex = 0;
            else
                LoadSelectedDevice();
        }

        private void RefreshDevices()
        {
            var selectedId = _selectedDevice?.DeviceId;
            _suppressDeviceToggle = true;
            DeviceList.ItemsSource = null;
            DeviceList.ItemsSource = _config.Devices.Select(d => d.Clone()).ToList();
            try
            {
                if (string.IsNullOrEmpty(selectedId)) return;

                foreach (var item in DeviceList.Items.OfType<CommunicationDeviceConfig>())
                {
                    if (item.DeviceId == selectedId)
                    {
                        DeviceList.SelectedItem = item;
                        break;
                    }
                }
            }
            finally
            {
                _suppressDeviceToggle = false;
            }
        }

        private void OnAddDeviceClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var menu = new ContextMenu();
            foreach (var factory in _manager.DriverFactories)
            {
                var item = new MenuItem { Header = factory.DisplayName, Tag = factory.DriverName };
                item.Click += OnAddDriverMenuItemClick;
                menu.Items.Add(item);
            }
            button.ContextMenu = menu;
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }

        private async void OnAddDriverMenuItemClick(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            var driverName = item?.Tag as string;
            if (string.IsNullOrEmpty(driverName)) return;
            var device = _manager.AddDevice(driverName);
            RefreshDevices();
            DeviceList.SelectedItem = DeviceList.Items.OfType<CommunicationDeviceConfig>()
                .FirstOrDefault(d => d.DeviceId == device.DeviceId);

            try
            {
                await _manager.StartDeviceAsync(device.DeviceId);
            }
            catch (Exception ex)
            {
                try { await _manager.StopDeviceAsync(device.DeviceId); } catch { }
                device.IsEnabled = false;
                _config.UpdateDevice(device);
                _manager.LoadConfig(_config);
                RefreshDevices();
                MessageBox.Show(Window.GetWindow(this), $"设备已创建，但连接失败：{ex.Message}\n\n请检查配置后重新启用。",
                    "连接失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedDevice = DeviceList.SelectedItem as CommunicationDeviceConfig;
            LoadSelectedDevice();
        }

        private void LoadSelectedDevice()
        {
            if (_selectedDevice == null)
            {
                DeviceTitleText.Text = "请选择或添加一个通信设备";
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

            DeviceTitleText.Text = _selectedDevice.DisplayName;
            var factory = _manager.DriverFactories.FirstOrDefault(f => f.DriverName == _selectedDevice.DriverName);
            if (_selectedDevice.DriverName == "UART")
            {
                var control = new UartDriverConfigControl(_selectedDevice);
                control.RealtimeRequested += OnUartRealtimeRequested;
                control.ConfigChanged += OnDriverConfigChanged;
                DriverConfigHost.Content = control;
                BlockList.Visibility = Visibility.Collapsed;
            }
            else
            {
                DriverConfigHost.Content = factory?.CreateConfigurationView(_selectedDevice);
                BlockList.Visibility = Visibility.Visible;
                BlockList.LoadDevice(_selectedDevice);
            }
        }

        private async void OnDeviceEnabledToggled(object sender, RoutedEventArgs e)
        {
            if (_suppressDeviceToggle) return;
            if (!(sender is System.Windows.Controls.Primitives.ToggleButton toggle)) return;
            var deviceId = toggle.Tag as string;
            var device = _config.GetDevice(deviceId);
            if (device == null) return;
            device.IsEnabled = toggle.IsChecked == true;
            _config.UpdateDevice(device);
            _manager.LoadConfig(_config);
            try
            {
                if (device.IsEnabled)
                {
                    await _manager.StartDeviceAsync(device.DeviceId);
                }
                else
                {
                    await _manager.StopDeviceAsync(device.DeviceId);
                }

                if (_selectedDevice?.DeviceId == device.DeviceId)
                    _selectedDevice = device;
                DeviceList.Items.Refresh();
            }
            catch (Exception ex)
            {
                try { await _manager.StopDeviceAsync(device.DeviceId); } catch { }
                device.IsEnabled = false;
                _config.UpdateDevice(device);
                _manager.LoadConfig(_config);
                if (_selectedDevice?.DeviceId == device.DeviceId)
                    _selectedDevice = device;
                RefreshDevices();
                MessageBox.Show(Window.GetWindow(this), $"设备无法启用，已切换为关闭：{ex.Message}", "通信设备不可用",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var device = DeviceList.SelectedItem as CommunicationDeviceConfig;
            if (device == null) return;
            var dialog = new TextInputDialog("重命名设备", "设备标题", device.DisplayName)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true) return;
            device.DisplayName = string.IsNullOrWhiteSpace(dialog.Value) ? device.DisplayName : dialog.Value.Trim();
            _config.UpdateDevice(device);
            _manager.LoadConfig(_config);
            _selectedDevice = device;
            RefreshDevices();
        }

        private void OnBlocksChanged(object sender, EventArgs e)
        {
            ApplySelectedDeviceChanges(true);
        }

        private void OnMonitorRequested(object sender, CommunicationBlockConfig e)
        {
            if (_selectedDevice == null || e == null) return;
            new RawBytesMonitorWindow(_manager, _selectedDevice.DeviceId, e.BlockId)
            {
                Owner = Window.GetWindow(this),
            }.Show();
        }

        private void OnUartRealtimeRequested(object sender, CommunicationBlockConfig e)
        {
            if (_selectedDevice == null || e == null) return;
            ApplySelectedDeviceChanges(false);
            new RawBytesMonitorWindow(_manager, _selectedDevice.DeviceId, e.BlockId)
            {
                Owner = Window.GetWindow(this),
                Title = $"实时监视 - {e.Name}",
            }.Show();
        }

        private void OnDriverConfigChanged(object sender, EventArgs e)
        {
            ApplySelectedDeviceChanges(false);
        }

        private void ApplySelectedDeviceChanges(bool refreshList)
        {
            if (_selectedDevice == null) return;
            _config.UpdateDevice(_selectedDevice);
            _manager.LoadConfig(_config);
            DeviceTitleText.Text = _selectedDevice.DisplayName;
            if (refreshList)
                RefreshDevices();
            else
                DeviceList.Items.Refresh();
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

        private async void OnRemoveDeviceClick(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null) return;

            var deviceId = _selectedDevice.DeviceId;
            var name = _selectedDevice.DisplayName;

            try { await _manager.StopDeviceAsync(deviceId); } catch { }
            _config.RemoveDevice(deviceId);
            _manager.LoadConfig(_config);
            _selectedDevice = null;
            RefreshDevices();
            LoadSelectedDevice();
        }
    }
}
