using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI
{
    public partial class CommunicationBlockListControl : UserControl
    {
        private CommunicationDeviceConfig _device;

        public CommunicationBlockListControl()
        {
            InitializeComponent();
            Grid.ItemsSource = Blocks;
        }

        public ObservableCollection<CommunicationBlockConfig> Blocks { get; } =
            new ObservableCollection<CommunicationBlockConfig>();

        public event EventHandler BlocksChanged;
        public event EventHandler<CommunicationBlockConfig> MonitorRequested;

        public void LoadDevice(CommunicationDeviceConfig device)
        {
            _device = device;
            Blocks.Clear();
            if (device?.Blocks == null) return;
            foreach (var block in device.Blocks)
                Blocks.Add(block);
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            if (_device == null) return;
            var address = $"{_device.DriverName}-{_device.InterfaceName}-Block{Blocks.Count + 1}";
            var block = new CommunicationBlockConfig
            {
                Name = $"Block{Blocks.Count + 1}",
                BlockName = $"Block{Blocks.Count + 1}",
                Address = address,
            };
            Blocks.Add(block);
            Sync();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is CommunicationBlockConfig block)
            {
                Blocks.Remove(block);
                Sync();
            }
        }

        private void OnMonitorClick(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is CommunicationBlockConfig block)
                MonitorRequested?.Invoke(this, block);
        }

        private void Sync()
        {
            if (_device != null)
                _device.Blocks = Blocks.Select(b => b.Clone()).ToList();
            BlocksChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
