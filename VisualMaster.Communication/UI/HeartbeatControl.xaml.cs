using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI
{
    public partial class HeartbeatControl : UserControl
    {
        private CommunicationSystemConfig _config;
        private CommunicationHeartbeatConfig _selected;
        private bool _suppress;

        public ObservableCollection<CommunicationHeartbeatConfig> Heartbeats { get; } =
            new ObservableCollection<CommunicationHeartbeatConfig>();

        public HeartbeatControl()
        {
            InitializeComponent();
            HeartbeatList.ItemsSource = Heartbeats;
        }

        public void LoadConfig(CommunicationSystemConfig config)
        {
            _config = config;
            Heartbeats.Clear();
            if (config != null)
            {
                foreach (var item in config.Heartbeats)
                    Heartbeats.Add(item.Clone());
            }
            HeartbeatList.SelectedIndex = Heartbeats.Count > 0 ? 0 : -1;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var item = new CommunicationHeartbeatConfig
            {
                Name = $"心跳{Heartbeats.Count + 1}",
                InputEventId = _config?.InputEvents.FirstOrDefault()?.EventId,
                OutputEventId = _config?.OutputEvents.FirstOrDefault()?.EventId,
                IsEnabled = true,
            };
            Heartbeats.Add(item);
            HeartbeatList.SelectedItem = item;
            Sync();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (!(HeartbeatList.SelectedItem is CommunicationHeartbeatConfig item)) return;
            Heartbeats.Remove(item);
            HeartbeatList.SelectedIndex = Heartbeats.Count > 0 ? 0 : -1;
            Sync();
        }

        private void OnHeartbeatSelected(object sender, SelectionChangedEventArgs e)
        {
            _selected = HeartbeatList.SelectedItem as CommunicationHeartbeatConfig;
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
                EnabledBox.IsChecked = _selected?.IsEnabled == true;
                NameBox.Text = _selected?.Name ?? "";
                InputEventBox.Text = _selected?.InputEventId ?? "";
                OutputEventBox.Text = _selected?.OutputEventId ?? "";
            }
            finally { _suppress = false; }
        }

        private void OnEditorChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.IsEnabled = EnabledBox.IsChecked == true;
            _selected.Name = NameBox.Text;
            _selected.InputEventId = InputEventBox.Text;
            _selected.OutputEventId = OutputEventBox.Text;
            HeartbeatList.Items.Refresh();
            Sync();
        }

        private void Sync()
        {
            _config?.UpdateHeartbeats(Heartbeats.Select(e => e.Clone()));
        }
    }
}
