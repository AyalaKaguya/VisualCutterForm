using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Config;
using VisualMaster.Communication.Core;
using VisualMaster.Communication.UI;

namespace VisualMaster.Communication.UI.ViewModels
{
    public sealed class CommunicationManagerViewModel : NotifyBase, IDisposable
    {
        private readonly CommunicationManager _manager;
        private readonly CommunicationConfigSection _config;
        private readonly Dictionary<string, ICommunicationDriverConfigurationViewFactory> _configurationViewFactories =
            new Dictionary<string, ICommunicationDriverConfigurationViewFactory>(StringComparer.OrdinalIgnoreCase);

        private CommunicationDeviceItemViewModel _selectedDevice;
        private bool _isBusy;
        private string _statusMessage;

        public ObservableCollection<CommunicationDeviceItemViewModel> Devices { get; }
            = new ObservableCollection<CommunicationDeviceItemViewModel>();

        public CommunicationDeviceItemViewModel SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetField(ref _selectedDevice, value))
                    RefreshCommands();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetField(ref _isBusy, value))
                    RefreshCommands();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetField(ref _statusMessage, value);
        }

        public CommunicationConfigSection Config => _config;

        public IReadOnlyList<ICommunicationDriverFactory> DriverFactories => _manager.DriverFactories;

        public ICommand AddUartDeviceCommand   { get; }
        public ICommand RemoveDeviceCommand    { get; }
        public ICommand ToggleDeviceCommand    { get; }

        public CommunicationManagerViewModel(CommunicationManager manager, CommunicationConfigSection config)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _config  = config  ?? throw new ArgumentNullException(nameof(config));

            RegisterConfigurationViewFactory(new UartDriverConfigurationViewFactory());
            RegisterConfigurationViewFactory(new TcpDriverConfigurationViewFactory());

            _manager.LoadConfig(_config);

            AddUartDeviceCommand = new RelayCommand(ExecuteAddUartDevice, () => !IsBusy);
            RemoveDeviceCommand  = new RelayCommand(ExecuteRemoveDevice,  () => !IsBusy && SelectedDevice != null);
            ToggleDeviceCommand  = new RelayCommand(ExecuteToggleDevice,  () => !IsBusy && SelectedDevice != null);

            LoadFromConfig();

            _config.DeviceAdded   += OnConfigDeviceAdded;
            _config.DeviceRemoved += OnConfigDeviceRemoved;
            _config.DeviceUpdated += OnConfigDeviceUpdated;
            _config.Reset         += OnConfigReset;
            _manager.DeviceStatusChanged += OnDeviceStatusChanged;

            AutoConnectEnabledDevices();
        }

        private async void AutoConnectEnabledDevices()
        {
            foreach (var devVm in Devices)
            {
                if (!devVm.IsEnabled) continue;
                try
                {
                    await _manager.StartDeviceAsync(devVm.DeviceId);
                }
                catch
                {
                    devVm.ApplyStatus(_manager.GetDeviceStatus(devVm.DeviceId));
                }
            }
            RefreshStatuses();
        }

        private void LoadFromConfig()
        {
            Devices.Clear();
            foreach (var dev in _config.Devices)
                Devices.Add(new CommunicationDeviceItemViewModel(dev));
        }

        public void RefreshStatuses()
        {
            foreach (var devVm in Devices)
            {
                var status = _manager.GetDeviceStatus(devVm.DeviceId);
                if (status != null)
                    devVm.ApplyStatus(status);
                else
                {
                    var driver = _manager.Drivers.FirstOrDefault(d => d.DeviceId == devVm.DeviceId);
                    devVm.RefreshStatus(driver?.IsConnected == true);
                }
            }
        }

        public async void AddDevice(string driverName)
        {
            IsBusy = true;
            StatusMessage = "正在添加通信设备...";
            try
            {
                var device = _manager.AddDevice(driverName);
                StatusMessage = $"已添加：{device.DisplayName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"添加失败：{ex.Message}";
                MessageBox.Show(ex.Message, "添加设备失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void ExecuteAddUartDevice()
        {
            AddDevice("UART");
        }

        private async void ExecuteRemoveDevice()
        {
            if (SelectedDevice == null) return;
            IsBusy = true;
            string deviceId = SelectedDevice.DeviceId;
            string displayName = SelectedDevice.DisplayName;
            try
            {
                try { await _manager.StopDeviceAsync(deviceId); } catch { }
                _config.RemoveDevice(deviceId);
                _manager.LoadConfig(_config);
                StatusMessage = $"已移除：{displayName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"移除失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void ExecuteToggleDevice()
        {
            if (SelectedDevice == null) return;
            var device = _config.GetDevice(SelectedDevice.DeviceId);
            if (device == null) return;

            device.IsEnabled = SelectedDevice.IsEnabled;
            _config.UpdateDevice(device);
            _manager.LoadConfig(_config);

            if (device.IsEnabled)
            {
                try
                {
                    await _manager.StartDeviceAsync(device.DeviceId);
                    StatusMessage = $"已连接：{device.DisplayName}";
                }
                catch (Exception ex)
                {
                    SelectedDevice.ApplyStatus(_manager.GetDeviceStatus(device.DeviceId));
                    StatusMessage = $"连接失败：{ex.Message}";
                    MessageBox.Show(Application.Current?.MainWindow, $"设备无法启用，已切换为关闭：{ex.Message}", "通信设备不可用",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                await _manager.StopDeviceAsync(device.DeviceId);
                StatusMessage = $"已断开：{device.DisplayName}";
            }

            RefreshStatuses();
            RefreshCommands();
        }

        private void OnConfigDeviceAdded(object sender, CommunicationDeviceConfig cfg)
        {
            InvokeOnUI(() =>
            {
                if (Devices.All(d => d.DeviceId != cfg.DeviceId))
                    Devices.Add(new CommunicationDeviceItemViewModel(cfg));
            });
        }

        private void OnConfigDeviceRemoved(object sender, string deviceId)
        {
            InvokeOnUI(() =>
            {
                var vm = Devices.FirstOrDefault(d => d.DeviceId == deviceId);
                if (vm != null)
                    Devices.Remove(vm);
            });
        }

        private void OnConfigDeviceUpdated(object sender, CommunicationDeviceConfig cfg)
        {
            InvokeOnUI(() =>
            {
                var vm = Devices.FirstOrDefault(d => d.DeviceId == cfg.DeviceId);
                vm?.LoadConfig(cfg);
                var status = _manager.GetDeviceStatus(cfg.DeviceId);
                if (status != null)
                    vm?.ApplyStatus(status);
            });
        }

        private void OnConfigReset(object sender, EventArgs e)
        {
            InvokeOnUI(() =>
            {
                LoadFromConfig();
                RefreshStatuses();
            });
        }

        private void OnDeviceStatusChanged(object sender, CommunicationDeviceStatusChangedEventArgs e)
        {
            InvokeOnUI(() =>
            {
                var vm = Devices.FirstOrDefault(d => d.DeviceId == e.Status.DeviceId);
                vm?.ApplyStatus(e.Status);
                if (SelectedDevice?.DeviceId == e.Status.DeviceId && !string.IsNullOrWhiteSpace(e.Status.LastError))
                    StatusMessage = e.Status.LastError;
            });
        }

        private void RefreshCommands()
        {
            (AddUartDeviceCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveDeviceCommand  as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleDeviceCommand  as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private static void InvokeOnUI(Action action)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
                Application.Current.Dispatcher.BeginInvoke(action);
            else
                action();
        }

        public CommunicationDeviceConfig GetDeviceConfig(string deviceId)
        {
            return _config?.GetDevice(deviceId);
        }

        public ICommunicationDriver GetDriver(string deviceId)
        {
            return _manager.Drivers.FirstOrDefault(d => d.DeviceId == deviceId);
        }

        public UserControl CreateConfigurationView(string deviceId)
        {
            var config = _config?.GetDevice(deviceId);
            if (config == null) return null;
            return _configurationViewFactories.TryGetValue(config.DriverName, out var factory)
                ? factory.CreateConfigurationView(config)
                : null;
        }

        public void RegisterConfigurationViewFactory(ICommunicationDriverConfigurationViewFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _configurationViewFactories[factory.DriverName] = factory;
        }

        public ICommunicationBlock FindBlock(string deviceId, string blockId)
        {
            return _manager.FindBlock(deviceId, blockId);
        }

        public void UpdateDeviceConfig(CommunicationDeviceConfig config)
        {
            if (config == null) return;
            _config.UpdateDevice(config);
            _manager.LoadConfig(_config);
        }

        public void Dispose()
        {
            _config.DeviceAdded   -= OnConfigDeviceAdded;
            _config.DeviceRemoved -= OnConfigDeviceRemoved;
            _config.DeviceUpdated -= OnConfigDeviceUpdated;
            _config.Reset         -= OnConfigReset;
            _manager.DeviceStatusChanged -= OnDeviceStatusChanged;
        }
    }
}
