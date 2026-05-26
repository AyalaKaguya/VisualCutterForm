using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.Config;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public partial class HeartbeatControl : UserControl
    {
        public sealed class HeartbeatDisplayItem
        {
            public CommunicationHeartbeatConfig Config { get; set; }
            public string Name => Config.Name;
            public bool IsEnabled
            {
                get => Config.IsEnabled;
                set => Config.IsEnabled = value;
            }
            public string BindingInfo { get; set; }
            public string OutputInfo { get; set; }
        }

        public sealed class InputEventEntry
        {
            public string EventId { get; }
            public string Display { get; }

            public InputEventEntry(string eventId, string display)
            {
                EventId = eventId;
                Display = display;
            }

            public override string ToString() => Display;
        }

        public sealed class OutputEventEntry
        {
            public string EventId { get; }
            public string Display { get; }

            public OutputEventEntry(string eventId, string display)
            {
                EventId = eventId;
                Display = display;
            }

            public override string ToString() => Display;
        }

        public sealed class VariableValueViewModel : NotifyBase
        {
            private readonly string _name;
            private readonly CommunicationBlockDataType _dataType;
            private readonly CommunicationByteOrder _byteOrder;
            private string _value;

            public VariableValueViewModel(CommunicationOutputVariable variable, string value)
            {
                _name = variable?.Name ?? "";
                _dataType = variable?.DataType ?? CommunicationBlockDataType.Bytes;
                _byteOrder = variable?.ByteOrder ?? CommunicationByteOrder.BigEndian;
                _value = value ?? "";
            }

            public string Name => _name;
            public string TypeDisplay => $"{_dataType} / {_byteOrder}";

            public string Value
            {
                get => _value;
                set
                {
                    _value = value;
                    OnPropertyChanged();
                    Changed?.Invoke(this);
                }
            }

            public event Action<VariableValueViewModel> Changed;
        }

        private CommunicationConfigSection _config;
        private CommunicationHeartbeatConfig _selected;
        private bool _suppress;
        private bool _variableSuppress;
        private bool _ignoreConfigEvents;

        private readonly ObservableCollection<VariableValueViewModel> _variableViewModels =
            new ObservableCollection<VariableValueViewModel>();

        public ObservableCollection<HeartbeatDisplayItem> Heartbeats { get; } =
            new ObservableCollection<HeartbeatDisplayItem>();

        public HeartbeatControl()
        {
            InitializeComponent();
            HeartbeatList.ItemsSource = Heartbeats;
            VariableListControl.ItemsSource = _variableViewModels;
        }

        public void LoadConfig(CommunicationConfigSection config)
        {
            if (_config != null)
                _config.EventsUpdated -= OnConfigEventsUpdated;

            _config = config;
            Heartbeats.Clear();
            if (config != null)
            {
                foreach (var item in config.Heartbeats)
                    Heartbeats.Add(CreateDisplayItem(item.Clone()));

                _config.EventsUpdated += OnConfigEventsUpdated;
            }
            HeartbeatList.SelectedIndex = Heartbeats.Count > 0 ? 0 : -1;
        }

        private void OnConfigEventsUpdated(object sender, EventArgs e)
        {
            if (_ignoreConfigEvents)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshDisplayItems();
                if (_selected != null)
                {
                    RefreshInputRuleCombo();
                    RefreshOutputEventCombo();
                    LoadVariableValues();
                }
            }));
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var firstInput = GetInputEventEntries().FirstOrDefault();
            var firstOutput = _config?.OutputEvents.FirstOrDefault();
            var item = new CommunicationHeartbeatConfig
            {
                Name = $"心跳{Heartbeats.Count + 1}",
                InputEventId = firstInput?.EventId,
                InputRuleId = null,
                OutputEventId = firstOutput?.EventId,
                IsEnabled = true,
                VariableValues = CreateDefaultVariableValues(firstOutput),
            };
            Heartbeats.Add(CreateDisplayItem(item));
            HeartbeatList.SelectedIndex = Heartbeats.Count - 1;
            Sync();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (!(HeartbeatList.SelectedItem is HeartbeatDisplayItem item)) return;
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
            var item = HeartbeatList.SelectedItem as HeartbeatDisplayItem;
            if (item == null) return;
            var dialog = new TextInputDialog("重命名心跳", "心跳名称", item.Config.Name)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true) return;
            item.Config.Name = string.IsNullOrWhiteSpace(dialog.Value) ? item.Config.Name : dialog.Value.Trim();
            EditorTitle.Text = item.Config.Name;
            HeartbeatList.Items.Refresh();
            Sync();
        }

        private void OnHeartbeatSelected(object sender, SelectionChangedEventArgs e)
        {
            _selected = (HeartbeatList.SelectedItem as HeartbeatDisplayItem)?.Config;
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

            if (_selected.VariableValues == null)
                _selected.VariableValues = new Dictionary<string, string>();

            EditorTitle.Text = _selected.Name ?? "";
            EditorPlaceholder.Visibility = Visibility.Collapsed;
            EditorPanel.Visibility = Visibility.Visible;

            _suppress = true;
            try
            {
                RefreshInputRuleCombo();
                RefreshOutputEventCombo();
                LoadVariableValues();
            }
            finally { _suppress = false; }
        }

        private void RefreshInputRuleCombo()
        {
            var wasSuppressing = _suppress;
            _suppress = true;
            try
            {
                InputRuleCombo.ItemsSource = null;
                var entries = GetInputEventEntries();
                InputRuleCombo.ItemsSource = entries;
                InputRuleCombo.SelectedItem = entries.FirstOrDefault(e =>
                    string.Equals(e.EventId, _selected?.InputEventId, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _suppress = wasSuppressing;
            }
        }

        private void RefreshOutputEventCombo()
        {
            var wasSuppressing = _suppress;
            _suppress = true;
            try
            {
                OutputEventCombo.ItemsSource = null;
                var entries = (_config?.OutputEvents ?? new List<CommunicationOutputEventConfig>())
                    .Select(e => new OutputEventEntry(e.EventId, string.IsNullOrWhiteSpace(e.Name) ? e.EventId : e.Name))
                    .ToList();
                OutputEventCombo.ItemsSource = entries;
                OutputEventCombo.SelectedItem = entries.FirstOrDefault(e =>
                    string.Equals(e.EventId, _selected?.OutputEventId, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _suppress = wasSuppressing;
            }
        }

        private List<InputEventEntry> GetInputEventEntries()
        {
            var result = new List<InputEventEntry>();
            if (_config == null) return result;

            foreach (var input in _config.InputEvents)
            {
                var eventName = string.IsNullOrWhiteSpace(input.Name) ? input.EventId : input.Name;
                result.Add(new InputEventEntry(input.EventId, eventName));
            }

            return result;
        }

        private void OnInputRuleChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            var entry = InputRuleCombo.SelectedItem as InputEventEntry;
            _selected.InputEventId = entry?.EventId;
            _selected.InputRuleId = null;
            RefreshDisplayItem();
            Sync();
        }

        private void OnOutputEventChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            var entry = OutputEventCombo.SelectedItem as OutputEventEntry;
            _selected.OutputEventId = entry?.EventId;
            _selected.VariableValues = CreateDefaultVariableValues(GetSelectedOutputEvent());
            LoadVariableValues();
            RefreshDisplayItem();
            Sync();
        }

        private void LoadVariableValues()
        {
            _variableSuppress = true;
            try
            {
                foreach (var vm in _variableViewModels)
                    vm.Changed -= OnVariableValueChanged;
                _variableViewModels.Clear();

                var output = GetSelectedOutputEvent();
                if (output?.Variables == null) return;

                foreach (var variable in output.Variables.Where(v => !string.IsNullOrWhiteSpace(v.Name)))
                {
                    _selected.VariableValues.TryGetValue(variable.Name, out var value);
                    var vm = new VariableValueViewModel(variable, value);
                    vm.Changed += OnVariableValueChanged;
                    _variableViewModels.Add(vm);
                }
            }
            finally { _variableSuppress = false; }
        }

        private void OnVariableValueChanged(VariableValueViewModel vm)
        {
            if (_variableSuppress || _selected == null) return;
            _selected.VariableValues = _variableViewModels
                .Where(v => !string.IsNullOrWhiteSpace(v.Name))
                .ToDictionary(v => v.Name, v => v.Value ?? "");
            Sync();
        }

        private CommunicationOutputEventConfig GetSelectedOutputEvent()
        {
            return _config?.OutputEvents.FirstOrDefault(e => e.EventId == _selected?.OutputEventId);
        }

        private static Dictionary<string, string> CreateDefaultVariableValues(CommunicationOutputEventConfig output)
        {
            var result = new Dictionary<string, string>();
            if (output?.Variables == null) return result;
            foreach (var variable in output.Variables.Where(v => !string.IsNullOrWhiteSpace(v.Name)))
                result[variable.Name] = "";
            return result;
        }

        private void OnListToggleClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToggleButton toggle) || !(toggle.Tag is HeartbeatDisplayItem item)) return;
            item.Config.IsEnabled = toggle.IsChecked == true;
            RefreshDisplayItem(item);
            Sync();
        }

        private HeartbeatDisplayItem CreateDisplayItem(CommunicationHeartbeatConfig cfg)
        {
            var input = _config?.InputEvents.FirstOrDefault(e => e.EventId == cfg.InputEventId);
            var inputName = input?.Name ?? cfg.InputEventId;
            var output = _config?.OutputEvents.FirstOrDefault(e => e.EventId == cfg.OutputEventId);

            return new HeartbeatDisplayItem
            {
                Config = cfg,
                BindingInfo = $"输入: {inputName}",
                OutputInfo = $"输出: {output?.Name ?? cfg.OutputEventId}",
            };
        }

        private void RefreshDisplayItems()
        {
            foreach (var item in Heartbeats)
                RefreshDisplayItem(item);
            HeartbeatList.Items.Refresh();
        }

        private void RefreshDisplayItem()
        {
            var item = Heartbeats.FirstOrDefault(d => d.Config == _selected);
            if (item != null)
                RefreshDisplayItem(item);
        }

        private void RefreshDisplayItem(HeartbeatDisplayItem item)
        {
            var refreshed = CreateDisplayItem(item.Config);
            item.BindingInfo = refreshed.BindingInfo;
            item.OutputInfo = refreshed.OutputInfo;
            HeartbeatList.Items.Refresh();
        }

        private void Sync()
        {
            if (_config == null) return;

            _ignoreConfigEvents = true;
            try
            {
                _config.UpdateHeartbeats(Heartbeats.Select(e => e.Config.Clone()));
            }
            finally
            {
                _ignoreConfigEvents = false;
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
