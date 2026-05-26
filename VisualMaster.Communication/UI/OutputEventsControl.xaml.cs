using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisualMaster.Communication.Api;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.UI
{
    public partial class OutputEventsControl : UserControl
    {
        public sealed class OutputEventDisplayItem
        {
            public CommunicationOutputEventConfig Config { get; set; }
            public string Name => Config.Name;
            public string BindingInfo { get; set; }
            public int VariableCount => Config.Variables?.Count ?? 0;
            public int SegmentCount => Config.Segments?.Count ?? 0;
        }

        public sealed class OutputVariableViewModel : NotifyBase
        {
            private readonly CommunicationOutputVariable _variable;

            public OutputVariableViewModel(CommunicationOutputVariable variable)
            {
                _variable = variable ?? throw new ArgumentNullException(nameof(variable));
            }

            public CommunicationOutputVariable Variable => _variable;
            public IReadOnlyList<DataTypeEntry> DataTypeOptions => RuleItemViewModel.DataTypeOptions.Where(o => !o.IsSingleByte).ToList();
            public IReadOnlyList<ByteOrderEntry> ByteOrderOptions => RuleItemViewModel.ByteOrderOptions;

            public string Name
            {
                get => _variable.Name ?? "";
                set { _variable.Name = value; OnPropertyChanged(); Changed?.Invoke(this); }
            }

            public DataTypeEntry SelectedType
            {
                get => DataTypeOptions.FirstOrDefault(o => o.Type == _variable.DataType) ?? DataTypeOptions.First();
                set
                {
                    if (value == null) return;
                    _variable.DataType = value.Type;
                    OnPropertyChanged();
                    Changed?.Invoke(this);
                }
            }

            public ByteOrderEntry SelectedByteOrder
            {
                get => ByteOrderOptions.FirstOrDefault(o => o.Order == _variable.ByteOrder) ?? ByteOrderOptions.First();
                set
                {
                    if (value == null) return;
                    _variable.ByteOrder = value.Order;
                    OnPropertyChanged();
                    Changed?.Invoke(this);
                }
            }

            public event Action<OutputVariableViewModel> Changed;
        }

        public sealed class SegmentKindEntry
        {
            public string Display { get; }
            public CommunicationOutputSegmentKind Kind { get; }

            public SegmentKindEntry(string display, CommunicationOutputSegmentKind kind)
            {
                Display = display;
                Kind = kind;
            }

            public override string ToString() => Display;
        }

        public sealed class CrcMethodEntry
        {
            public string Display { get; }
            public CommunicationCrcMethod Method { get; }

            public CrcMethodEntry(string display, CommunicationCrcMethod method)
            {
                Display = display;
                Method = method;
            }

            public override string ToString() => Display;
        }

        public sealed class OutputSegmentViewModel : NotifyBase
        {
            private readonly CommunicationOutputSegment _segment;
            private readonly Func<IReadOnlyList<string>> _variableNamesProvider;
            private int _index;

            public OutputSegmentViewModel(CommunicationOutputSegment segment, Func<IReadOnlyList<string>> variableNamesProvider)
            {
                _segment = segment ?? throw new ArgumentNullException(nameof(segment));
                _variableNamesProvider = variableNamesProvider ?? (() => new List<string>());
            }

            public CommunicationOutputSegment Segment => _segment;
            public IReadOnlyList<SegmentKindEntry> KindOptions => SegmentKinds;
            public IReadOnlyList<DataTypeEntry> DataTypeOptions => RuleItemViewModel.DataTypeOptions.Where(o => !o.IsSingleByte).ToList();
            public IReadOnlyList<ByteOrderEntry> ByteOrderOptions => RuleItemViewModel.ByteOrderOptions;
            public IReadOnlyList<string> VariableNames => _variableNamesProvider();
            public bool IsVariable => _segment.Kind == CommunicationOutputSegmentKind.Variable;
            public bool IsConstant => _segment.Kind == CommunicationOutputSegmentKind.Constant;

            public int Index
            {
                get => _index;
                set { _index = value; OnPropertyChanged(); }
            }

            public SegmentKindEntry SelectedKind
            {
                get => SegmentKinds.First(k => k.Kind == _segment.Kind);
                set
                {
                    if (value == null) return;
                    _segment.Kind = value.Kind;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsVariable));
                    OnPropertyChanged(nameof(IsConstant));
                    Changed?.Invoke(this);
                }
            }

            public string Value
            {
                get => _segment.Value ?? "";
                set { _segment.Value = value; OnPropertyChanged(); Changed?.Invoke(this); }
            }

            public DataTypeEntry SelectedType
            {
                get => DataTypeOptions.FirstOrDefault(o => o.Type == _segment.DataType) ?? DataTypeOptions.First();
                set
                {
                    if (value == null) return;
                    _segment.DataType = value.Type;
                    OnPropertyChanged();
                    Changed?.Invoke(this);
                }
            }

            public ByteOrderEntry SelectedByteOrder
            {
                get => ByteOrderOptions.FirstOrDefault(o => o.Order == _segment.ByteOrder) ?? ByteOrderOptions.First();
                set
                {
                    if (value == null) return;
                    _segment.ByteOrder = value.Order;
                    OnPropertyChanged();
                    Changed?.Invoke(this);
                }
            }

            public void RefreshVariableNames()
            {
                OnPropertyChanged(nameof(VariableNames));
            }

            public event Action<OutputSegmentViewModel> Changed;
        }

        private static readonly IReadOnlyList<SegmentKindEntry> SegmentKinds = new List<SegmentKindEntry>
        {
            new SegmentKindEntry("常量", CommunicationOutputSegmentKind.Constant),
            new SegmentKindEntry("变量", CommunicationOutputSegmentKind.Variable),
        };

        private static readonly IReadOnlyList<CrcMethodEntry> CrcMethods = new List<CrcMethodEntry>
        {
            new CrcMethodEntry("无", CommunicationCrcMethod.None),
            new CrcMethodEntry("SUM8", CommunicationCrcMethod.Sum8),
            new CrcMethodEntry("XOR8", CommunicationCrcMethod.Xor8),
            new CrcMethodEntry("MODBUS CRC16", CommunicationCrcMethod.ModbusCrc16),
            new CrcMethodEntry("CRC16 CCITT", CommunicationCrcMethod.Crc16Ccitt),
        };

        private CommunicationSystemConfig _config;
        private CommunicationOutputEventConfig _selected;
        private bool _suppress;
        private bool _listSuppress;

        private readonly ObservableCollection<OutputVariableViewModel> _variableViewModels =
            new ObservableCollection<OutputVariableViewModel>();

        private readonly ObservableCollection<OutputSegmentViewModel> _segmentViewModels =
            new ObservableCollection<OutputSegmentViewModel>();

        public ObservableCollection<OutputEventDisplayItem> Events { get; } =
            new ObservableCollection<OutputEventDisplayItem>();

        public OutputEventsControl()
        {
            InitializeComponent();
            EventList.ItemsSource = Events;
            VariableListControl.ItemsSource = _variableViewModels;
            SegmentListControl.ItemsSource = _segmentViewModels;
            CrcMethodCombo.ItemsSource = CrcMethods;
            CrcByteOrderCombo.ItemsSource = RuleItemViewModel.ByteOrderOptions;
        }

        public void LoadConfig(CommunicationSystemConfig config)
        {
            if (_config != null)
            {
                _config.DeviceAdded -= OnConfigDeviceChanged;
                _config.DeviceRemoved -= OnConfigDeviceChanged;
                _config.DeviceUpdated -= OnConfigDeviceUpdated;
            }

            _config = config;
            Events.Clear();
            if (config != null)
            {
                foreach (var item in config.OutputEvents)
                    Events.Add(CreateDisplayItem(item.Clone()));

                _config.DeviceAdded += OnConfigDeviceChanged;
                _config.DeviceRemoved += OnConfigDeviceChanged;
                _config.DeviceUpdated += OnConfigDeviceUpdated;
            }
            EventList.SelectedIndex = Events.Count > 0 ? 0 : -1;
        }

        private void OnConfigDeviceChanged(object sender, object e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshDisplayItems();
                if (_selected != null)
                    RefreshBindingSelectors();
            }));
        }

        private void OnConfigDeviceUpdated(object sender, CommunicationDeviceConfig e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshDisplayItems();
                if (_selected != null)
                    RefreshBindingSelectors();
            }));
        }

        private void RefreshBindingSelectors()
        {
            var wasSuppressing = _suppress;
            _suppress = true;
            try
            {
                RefreshDeviceCombo();
                RefreshBlockCombo();
            }
            finally
            {
                _suppress = wasSuppressing;
            }
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
                ProtocolAssembly = new CommunicationProtocolAssemblyConfig(),
            };
            Events.Add(CreateDisplayItem(item));
            EventList.SelectedIndex = Events.Count - 1;
            Sync();
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (!(EventList.SelectedItem is OutputEventDisplayItem item)) return;
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
            var item = EventList.SelectedItem as OutputEventDisplayItem;
            if (item == null) return;
            var dialog = new TextInputDialog("重命名事件", "事件名称", item.Config.Name)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true) return;
            item.Config.Name = string.IsNullOrWhiteSpace(dialog.Value) ? item.Config.Name : dialog.Value.Trim();
            EditorTitle.Text = item.Config.Name;
            EventList.Items.Refresh();
            Sync();
        }

        private void OnEventSelected(object sender, SelectionChangedEventArgs e)
        {
            _selected = (EventList.SelectedItem as OutputEventDisplayItem)?.Config;
            LoadEditor();
        }

        private void LoadEditor()
        {
            if (_selected == null)
            {
                EditorTitle.Text = "请选择输出事件";
                EditorPlaceholder.Visibility = Visibility.Visible;
                EditorPanel.Visibility = Visibility.Collapsed;
                return;
            }

            if (_selected.ProtocolAssembly == null)
                _selected.ProtocolAssembly = new CommunicationProtocolAssemblyConfig();

            EditorTitle.Text = _selected.Name ?? "";
            EditorPlaceholder.Visibility = Visibility.Collapsed;
            EditorPanel.Visibility = Visibility.Visible;

            _suppress = true;
            try
            {
                RefreshDeviceCombo();
                RefreshBlockCombo();
                HeaderHexBox.Text = _selected.ProtocolAssembly.HeaderHex ?? "";
                CrcToggle.IsChecked = _selected.ProtocolAssembly.CrcEnabled;
                CrcPanel.Visibility = _selected.ProtocolAssembly.CrcEnabled ? Visibility.Visible : Visibility.Collapsed;
                CrcMethodCombo.SelectedItem = CrcMethods.FirstOrDefault(m => m.Method == _selected.ProtocolAssembly.CrcMethod) ?? CrcMethods.First();
                CrcByteOrderCombo.SelectedItem = RuleItemViewModel.ByteOrderOptions.FirstOrDefault(o => o.Order == _selected.ProtocolAssembly.CrcByteOrder)
                    ?? RuleItemViewModel.ByteOrderOptions.First();
                CrcIncludeHeaderToggle.IsChecked = _selected.ProtocolAssembly.CrcIncludesHeader;
                LoadVariables();
                LoadSegments();
            }
            finally { _suppress = false; }
        }

        private void RefreshDeviceCombo()
        {
            var selectedId = _selected?.DeviceId;
            DeviceCombo.Items.Clear();
            if (_config == null) return;
            foreach (var dev in _config.Devices)
            {
                DeviceCombo.Items.Add(new ComboBoxItem { Content = dev.DisplayName, Tag = dev.DeviceId });
                if (dev.DeviceId == selectedId)
                    DeviceCombo.SelectedIndex = DeviceCombo.Items.Count - 1;
            }
        }

        private void RefreshBlockCombo()
        {
            var selectedId = _selected?.BlockId;
            BlockCombo.Items.Clear();
            var dev = _config?.Devices.FirstOrDefault(d => d.DeviceId == _selected?.DeviceId);
            if (dev == null) return;
            foreach (var blk in dev.Blocks)
            {
                BlockCombo.Items.Add(new ComboBoxItem { Content = blk.Name, Tag = blk.BlockId });
                if (blk.BlockId == selectedId)
                    BlockCombo.SelectedIndex = BlockCombo.Items.Count - 1;
            }
        }

        private void OnDeviceComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            var item = DeviceCombo.SelectedItem as ComboBoxItem;
            _selected.DeviceId = item?.Tag as string;
            RefreshBlockCombo();
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

        private void OnHeaderChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            EnsureProtocol();
            _selected.ProtocolAssembly.HeaderHex = HeaderHexBox.Text;
            Sync();
        }

        private void OnCrcToggleChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            EnsureProtocol();
            _selected.ProtocolAssembly.CrcEnabled = CrcToggle.IsChecked == true;
            if (_selected.ProtocolAssembly.CrcEnabled && _selected.ProtocolAssembly.CrcMethod == CommunicationCrcMethod.None)
                _selected.ProtocolAssembly.CrcMethod = CommunicationCrcMethod.ModbusCrc16;
            CrcPanel.Visibility = _selected.ProtocolAssembly.CrcEnabled ? Visibility.Visible : Visibility.Collapsed;
            CrcMethodCombo.SelectedItem = CrcMethods.FirstOrDefault(m => m.Method == _selected.ProtocolAssembly.CrcMethod) ?? CrcMethods.First();
            Sync();
        }

        private void OnCrcMethodChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            EnsureProtocol();
            if (CrcMethodCombo.SelectedItem is CrcMethodEntry entry)
                _selected.ProtocolAssembly.CrcMethod = entry.Method;
            Sync();
        }

        private void OnCrcByteOrderChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            EnsureProtocol();
            if (CrcByteOrderCombo.SelectedItem is ByteOrderEntry entry)
                _selected.ProtocolAssembly.CrcByteOrder = entry.Order;
            Sync();
        }

        private void OnCrcIncludeHeaderChanged(object sender, RoutedEventArgs e)
        {
            if (_suppress || _selected == null) return;
            EnsureProtocol();
            _selected.ProtocolAssembly.CrcIncludesHeader = CrcIncludeHeaderToggle.IsChecked == true;
            Sync();
        }

        private void LoadVariables()
        {
            _listSuppress = true;
            try
            {
                foreach (var vm in _variableViewModels)
                    vm.Changed -= OnVariableChanged;
                _variableViewModels.Clear();

                foreach (var variable in _selected.Variables ?? Enumerable.Empty<CommunicationOutputVariable>())
                {
                    var vm = new OutputVariableViewModel(variable);
                    vm.Changed += OnVariableChanged;
                    _variableViewModels.Add(vm);
                }
            }
            finally { _listSuppress = false; }
        }

        private void LoadSegments()
        {
            _listSuppress = true;
            try
            {
                foreach (var vm in _segmentViewModels)
                    vm.Changed -= OnSegmentChanged;
                _segmentViewModels.Clear();

                foreach (var segment in _selected.Segments ?? Enumerable.Empty<CommunicationOutputSegment>())
                {
                    var vm = new OutputSegmentViewModel(segment, GetVariableNames);
                    vm.Changed += OnSegmentChanged;
                    _segmentViewModels.Add(vm);
                }
                RebuildSegmentIndices();
            }
            finally { _listSuppress = false; }
        }

        private void OnAddVariableClick(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            var variable = new CommunicationOutputVariable
            {
                Name = $"变量{_variableViewModels.Count + 1}",
                DataType = CommunicationBlockDataType.Int16,
                ByteOrder = CommunicationByteOrder.BigEndian,
            };
            var vm = new OutputVariableViewModel(variable);
            vm.Changed += OnVariableChanged;
            _variableViewModels.Add(vm);
            SyncVariables();
        }

        private void OnDeleteVariableClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is OutputVariableViewModel vm)) return;
            vm.Changed -= OnVariableChanged;
            _variableViewModels.Remove(vm);
            SyncVariables();
        }

        private void OnVariableChanged(OutputVariableViewModel vm)
        {
            if (_listSuppress || _selected == null) return;
            SyncVariables();
        }

        private void SyncVariables()
        {
            _selected.Variables = _variableViewModels.Select(v => v.Variable.Clone()).ToList();
            foreach (var segment in _segmentViewModels)
                segment.RefreshVariableNames();
            RefreshDisplayItem();
            Sync();
        }

        private void OnAddSegmentClick(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            var variableName = GetVariableNames().FirstOrDefault();
            var segment = new CommunicationOutputSegment
            {
                Kind = string.IsNullOrEmpty(variableName) ? CommunicationOutputSegmentKind.Constant : CommunicationOutputSegmentKind.Variable,
                Value = variableName ?? "",
                DataType = CommunicationBlockDataType.Bytes,
                ByteOrder = CommunicationByteOrder.BigEndian,
            };
            var vm = new OutputSegmentViewModel(segment, GetVariableNames);
            vm.Changed += OnSegmentChanged;
            _segmentViewModels.Add(vm);
            RebuildSegmentIndices();
            SyncSegments();
        }

        private void OnDeleteSegmentClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is OutputSegmentViewModel vm)) return;
            vm.Changed -= OnSegmentChanged;
            _segmentViewModels.Remove(vm);
            RebuildSegmentIndices();
            SyncSegments();
        }

        private void OnSegmentChanged(OutputSegmentViewModel vm)
        {
            if (_listSuppress || _selected == null) return;
            SyncSegments();
        }

        private void SyncSegments()
        {
            _selected.Segments = _segmentViewModels.Select(s => s.Segment.Clone()).ToList();
            RefreshDisplayItem();
            Sync();
        }

        private void RebuildSegmentIndices()
        {
            for (int i = 0; i < _segmentViewModels.Count; i++)
                _segmentViewModels[i].Index = i + 1;
        }

        private IReadOnlyList<string> GetVariableNames()
        {
            return _variableViewModels.Select(v => v.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        }

        private void RefreshDisplayItems()
        {
            foreach (var item in Events)
            {
                var info = CreateDisplayItem(item.Config);
                item.BindingInfo = info.BindingInfo;
            }
            EventList.Items.Refresh();
        }

        private void RefreshDisplayItem()
        {
            var item = Events.FirstOrDefault(d => d.Config == _selected);
            if (item != null)
            {
                item.BindingInfo = CreateDisplayItem(_selected).BindingInfo;
                EventList.Items.Refresh();
            }
        }

        private OutputEventDisplayItem CreateDisplayItem(CommunicationOutputEventConfig cfg)
        {
            string devName = _config?.Devices.FirstOrDefault(d => d.DeviceId == cfg.DeviceId)?.DisplayName ?? cfg.DeviceId;
            string blkName = null;
            var dev = _config?.Devices.FirstOrDefault(d => d.DeviceId == cfg.DeviceId);
            if (dev != null)
                blkName = dev.Blocks?.FirstOrDefault(b => b.BlockId == cfg.BlockId)?.Name;

            return new OutputEventDisplayItem
            {
                Config = cfg,
                BindingInfo = $"设备: {devName}    块: {blkName ?? cfg.BlockId}",
            };
        }

        private void EnsureProtocol()
        {
            if (_selected.ProtocolAssembly == null)
                _selected.ProtocolAssembly = new CommunicationProtocolAssemblyConfig();
        }

        private void Sync()
        {
            _config?.UpdateOutputEvents(Events.Select(e => e.Config.Clone()));
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
