using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.UI
{
    public partial class InputEventsControl : UserControl
    {
        private CommunicationSystemConfig _config;
        private CommunicationInputEventConfig _selected;
        private bool _suppress;

        public ObservableCollection<CommunicationInputEventConfig> Events { get; } =
            new ObservableCollection<CommunicationInputEventConfig>();

        public InputEventsControl()
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
                foreach (var item in config.InputEvents)
                    Events.Add(item.Clone());
            }
            EventList.SelectedIndex = Events.Count > 0 ? 0 : -1;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var firstDevice = _config?.Devices.FirstOrDefault();
            var firstBlock = firstDevice?.Blocks.FirstOrDefault();
            var item = new CommunicationInputEventConfig
            {
                Name = $"输入事件{Events.Count + 1}",
                DeviceId = firstDevice?.DeviceId,
                BlockId = firstBlock?.BlockId,
                MinimumLength = 1,
            };
            Events.Add(item);
            EventList.SelectedItem = item;
            Sync();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (!(EventList.SelectedItem is CommunicationInputEventConfig item)) return;
            Events.Remove(item);
            EventList.SelectedIndex = Events.Count > 0 ? 0 : -1;
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
            var item = EventList.SelectedItem as CommunicationInputEventConfig;
            if (item == null) return;
            var dialog = new TextInputDialog("重命名事件", "事件名称", item.Name)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true) return;
            item.Name = string.IsNullOrWhiteSpace(dialog.Value) ? item.Name : dialog.Value.Trim();
            EditorTitle.Text = item.Name;
            EventList.Items.Refresh();
            Sync();
        }

        private void OnEventSelected(object sender, SelectionChangedEventArgs e)
        {
            _selected = EventList.SelectedItem as CommunicationInputEventConfig;
            LoadEditor();
        }

        private void LoadEditor()
        {
            if (_selected == null)
            {
                EditorTitle.Text = "请选择输入事件";
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
                DeviceBox.Text = _selected?.DeviceId ?? "";
                BlockBox.Text = _selected?.BlockId ?? "";
                AsciiBox.IsChecked = _selected?.TreatAsAscii == true;
                MinLengthBox.Text = (_selected?.MinimumLength ?? 0).ToString();
                RuleGrid.DataContext = _selected;
            }
            finally { _suppress = false; }
        }

        private void OnEditorChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.DeviceId = DeviceBox.Text;
            _selected.BlockId = BlockBox.Text;
            _selected.TreatAsAscii = AsciiBox.IsChecked == true;
            if (int.TryParse(MinLengthBox.Text, out var len))
                _selected.MinimumLength = len;
            EventList.Items.Refresh();
            Sync();
        }

        private void Sync()
        {
            _config?.UpdateInputEvents(Events.Select(e => e.Clone()));
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
