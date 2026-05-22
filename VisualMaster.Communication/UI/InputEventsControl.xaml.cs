using System;
using System.Collections.Generic;
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
        public sealed class InputEventDisplayItem
        {
            public CommunicationInputEventConfig Config { get; set; }
            public string Name => Config.Name;
            public string BindingInfo { get; set; }
            public List<CommunicationInputMatchRule> Rules => Config.Rules;
            public object EventId => Config.EventId;
        }

        private CommunicationSystemConfig _config;
        private CommunicationInputEventConfig _selected;
        private bool _suppress;

        public ObservableCollection<InputEventDisplayItem> Events { get; } =
            new ObservableCollection<InputEventDisplayItem>();

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
                    Events.Add(CreateDisplayItem(item));
            }
            EventList.SelectedIndex = Events.Count > 0 ? 0 : -1;
        }

        private InputEventDisplayItem CreateDisplayItem(CommunicationInputEventConfig cfg)
        {
            string devName = _config?.Devices.FirstOrDefault(d => d.DeviceId == cfg.DeviceId)?.DisplayName ?? cfg.DeviceId;
            string blkName = null;
            var dev = _config?.Devices.FirstOrDefault(d => d.DeviceId == cfg.DeviceId);
            if (dev != null)
                blkName = dev.Blocks?.FirstOrDefault(b => b.BlockId == cfg.BlockId)?.Name;
            return new InputEventDisplayItem
            {
                Config = cfg,
                BindingInfo = $"设备: {devName}    块: {blkName ?? cfg.BlockId}",
            };
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
            Events.Add(CreateDisplayItem(item));
            EventList.SelectedIndex = Events.Count - 1;
            Sync();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (!(EventList.SelectedItem is InputEventDisplayItem displayItem)) return;
            Events.Remove(displayItem);
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
            var displayItem = EventList.SelectedItem as InputEventDisplayItem;
            if (displayItem == null) return;
            var dialog = new TextInputDialog("重命名事件", "事件名称", displayItem.Config.Name)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true) return;
            displayItem.Config.Name = string.IsNullOrWhiteSpace(dialog.Value) ? displayItem.Config.Name : dialog.Value.Trim();
            EditorTitle.Text = displayItem.Config.Name;
            EventList.Items.Refresh();
            Sync();
        }

        private void OnEventSelected(object sender, SelectionChangedEventArgs e)
        {
            var displayItem = EventList.SelectedItem as InputEventDisplayItem;
            _selected = displayItem?.Config;
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
                PopulateDeviceCombo();
                PopulateBlockCombo();
                LengthCheckToggle.IsChecked = _selected?.LengthCheckEnabled == true;
                LengthCheckPanel.Visibility = _selected?.LengthCheckEnabled == true ? Visibility.Visible : Visibility.Collapsed;
                MinLengthToggle.IsChecked = _selected?.MinLengthEnabled == true;
                MinLengthBox.Text = (_selected?.MinimumLength ?? 0).ToString();
                ExactLengthToggle.IsChecked = _selected?.ExactLengthEnabled == true;
                ExactLengthBox.Text = (_selected?.ExactLength ?? 0).ToString();
                AsciiToggle.IsChecked = _selected?.TreatAsAscii == true;
                RuleGrid.DataContext = _selected;
            }
            finally { _suppress = false; }
        }

        private void PopulateDeviceCombo()
        {
            DeviceCombo.Items.Clear();
            if (_config == null) return;
            foreach (var dev in _config.Devices)
            {
                DeviceCombo.Items.Add(new ComboBoxItem { Content = dev.DisplayName, Tag = dev.DeviceId });
                if (dev.DeviceId == _selected.DeviceId)
                    DeviceCombo.SelectedIndex = DeviceCombo.Items.Count - 1;
            }
        }

        private void PopulateBlockCombo()
        {
            BlockCombo.Items.Clear();
            var dev = _config?.Devices.FirstOrDefault(d => d.DeviceId == _selected.DeviceId);
            if (dev == null) return;
            foreach (var blk in dev.Blocks)
            {
                BlockCombo.Items.Add(new ComboBoxItem { Content = blk.Name, Tag = blk.BlockId });
                if (blk.BlockId == _selected.BlockId)
                    BlockCombo.SelectedIndex = BlockCombo.Items.Count - 1;
            }
        }

        private void OnDeviceComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            var item = DeviceCombo.SelectedItem as ComboBoxItem;
            _selected.DeviceId = item?.Tag as string;
            PopulateBlockCombo();
            RefreshDisplayItem();
            Sync();
        }

        private void OnBlockComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            var item = BlockCombo.SelectedItem as ComboBoxItem;
            _selected.BlockId = item?.Tag as string;
            RefreshDisplayItem();
            Sync();
        }

        private void OnLengthCheckChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.LengthCheckEnabled = LengthCheckToggle.IsChecked == true;
            LengthCheckPanel.Visibility = _selected.LengthCheckEnabled ? Visibility.Visible : Visibility.Collapsed;
            Sync();
        }

        private void OnMinLengthToggleChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.MinLengthEnabled = MinLengthToggle.IsChecked == true;
            Sync();
        }

        private void OnMinLengthChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            if (int.TryParse(MinLengthBox.Text, out var len))
                _selected.MinimumLength = len;
            Sync();
        }

        private void OnMinLengthUp(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            if (int.TryParse(MinLengthBox.Text, out var v))
            {
                v++;
                MinLengthBox.Text = v.ToString();
                _selected.MinimumLength = v;
                Sync();
            }
        }

        private void OnMinLengthDown(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            if (int.TryParse(MinLengthBox.Text, out var v) && v > 0)
            {
                v--;
                MinLengthBox.Text = v.ToString();
                _selected.MinimumLength = v;
                Sync();
            }
        }

        private void OnExactLengthChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            if (int.TryParse(ExactLengthBox.Text, out var len))
                _selected.ExactLength = len;
            Sync();
        }

        private void OnExactLengthUp(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            if (int.TryParse(ExactLengthBox.Text, out var v))
            {
                v++;
                ExactLengthBox.Text = v.ToString();
                _selected.ExactLength = v;
                Sync();
            }
        }

        private void OnExactLengthDown(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            if (int.TryParse(ExactLengthBox.Text, out var v) && v > 0)
            {
                v--;
                ExactLengthBox.Text = v.ToString();
                _selected.ExactLength = v;
                Sync();
            }
        }

        private void OnNumericPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void OnExactLengthToggleChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.ExactLengthEnabled = ExactLengthToggle.IsChecked == true;
            Sync();
        }

        private void OnAsciiToggleChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            _selected.TreatAsAscii = AsciiToggle.IsChecked == true;
            Sync();
        }

        private void RefreshDisplayItem()
        {
            var displayItem = Events.FirstOrDefault(d => d.Config == _selected);
            if (displayItem != null)
            {
                displayItem.BindingInfo = CreateDisplayItem(_selected).BindingInfo;
                EventList.Items.Refresh();
            }
        }

        private void Sync()
        {
            _config?.UpdateInputEvents(Events.Select(d => d.Config.Clone()));
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

        private static void SelectText(ComboBox combo, string text)
        {
            foreach (var item in combo.Items)
            {
                if ((item as ComboBoxItem)?.Content?.ToString() == text)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            combo.Text = text;
        }
    }
}
