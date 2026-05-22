using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private void OnListRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (item == null) return;
            item.IsSelected = true;

            var menu = new ContextMenu();
            var rename = new MenuItem { Header = "重命名" };
            rename.Click += OnRenameClick;
            menu.Items.Add(rename);
            var delete = new MenuItem { Header = "删除" };
            delete.Click += (s, args) => OnRemoveClick(s, args);
            menu.Items.Add(delete);
            item.ContextMenu = menu;
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
        {
            var item = HeartbeatList.SelectedItem as CommunicationHeartbeatConfig;
            if (item == null) return;
            var dialog = new TextInputDialog("重命名心跳", "心跳名称", item.Name)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true) return;
            item.Name = string.IsNullOrWhiteSpace(dialog.Value) ? item.Name : dialog.Value.Trim();
            EditorTitle.Text = item.Name;
            HeartbeatList.Items.Refresh();
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
                EditorTitle.Text = "请选择心跳";
                EditorPlaceholder.Visibility = Visibility.Visible;
                EditorPanel.Visibility = Visibility.Collapsed;
                return;
            }
            EditorTitle.Text = _selected.Name ?? "";
            EditorPlaceholder.Visibility = Visibility.Collapsed;
            EditorPanel.Visibility = Visibility.Visible;

            _suppress = true;
            try
            {
                EnabledBox.IsChecked = _selected?.IsEnabled == true;
                InputEventBox.Text = _selected?.InputEventId ?? "";
                OutputEventBox.Text = _selected?.OutputEventId ?? "";
            }
            finally { _suppress = false; }
        }

        private void OnEditorChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.IsEnabled = EnabledBox.IsChecked == true;
            _selected.InputEventId = InputEventBox.Text;
            _selected.OutputEventId = OutputEventBox.Text;
            HeartbeatList.Items.Refresh();
            Sync();
        }

        private void Sync()
        {
            _config?.UpdateHeartbeats(Heartbeats.Select(e => e.Clone()));
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
