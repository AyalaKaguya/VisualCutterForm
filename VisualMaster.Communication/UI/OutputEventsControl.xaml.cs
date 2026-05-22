using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI
{
    public partial class OutputEventsControl : UserControl
    {
        private CommunicationSystemConfig _config;
        private CommunicationOutputEventConfig _selected;
        private bool _suppress;

        public ObservableCollection<CommunicationOutputEventConfig> Events { get; } =
            new ObservableCollection<CommunicationOutputEventConfig>();

        public OutputEventsControl()
        {
            InitializeComponent();
            EventList.ItemsSource = Events;
        }

        public void LoadConfig(CommunicationSystemConfig config)
        {
            _config = config;
            Events.Clear();
            if (config != null)
            {
                foreach (var item in config.OutputEvents)
                    Events.Add(item.Clone());
            }
            EventList.SelectedIndex = Events.Count > 0 ? 0 : -1;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var firstDevice = _config?.Devices.FirstOrDefault();
            var firstBlock = firstDevice?.Blocks.FirstOrDefault();
            var item = new CommunicationOutputEventConfig
            {
                Name = $"输出事件{Events.Count + 1}",
                DeviceId = firstDevice?.DeviceId,
                BlockId = firstBlock?.BlockId,
            };
            Events.Add(item);
            EventList.SelectedItem = item;
            Sync();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (!(EventList.SelectedItem is CommunicationOutputEventConfig item)) return;
            Events.Remove(item);
            EventList.SelectedIndex = Events.Count > 0 ? 0 : -1;
            Sync();
        }

        private void OnEventSelected(object sender, SelectionChangedEventArgs e)
        {
            _selected = EventList.SelectedItem as CommunicationOutputEventConfig;
            LoadEditor();
        }

        private void LoadEditor()
        {
            if (_selected == null)
            {
                EditorPanel.Visibility = Visibility.Collapsed;
                return;
            }
            EditorPanel.Visibility = Visibility.Visible;

            _suppress = true;
            try
            {
                NameBox.Text = _selected?.Name ?? "";
                DeviceBox.Text = _selected?.DeviceId ?? "";
                BlockBox.Text = _selected?.BlockId ?? "";
                VariableGrid.DataContext = _selected;
                SegmentGrid.DataContext = _selected;
            }
            finally { _suppress = false; }
        }

        private void OnEditorChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.Name = NameBox.Text;
            _selected.DeviceId = DeviceBox.Text;
            _selected.BlockId = BlockBox.Text;
            EventList.Items.Refresh();
            Sync();
        }

        private void Sync()
        {
            _config?.UpdateOutputEvents(Events.Select(e => e.Clone()));
        }
    }
}
