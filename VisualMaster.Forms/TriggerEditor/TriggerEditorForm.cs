using System;
using System.Drawing;
using System.Windows.Forms;
using VisualMaster.WorkFlow;
using VisualMaster.WorkFlow.Triggers;

namespace VisualMaster.Forms.TriggerEditor
{
    partial class TriggerEditorForm : Form
    {
        private FlowGraph _graph;
        private FlowExecutor _executor;
        private VisionController _vision;

        private TriggerEntry _selected;
        private bool _suppressEvents;

        private System.ComponentModel.IContainer components = null;
        private SplitContainer _split;
        private Panel _leftPanel;
        private Panel _rightPanel;
        private ListView _triggerList;
        private FlowLayoutPanel _btnPanel;
        private Button _btnAdd;
        private Button _btnDelete;
        private Button _btnFire;
        private Label _lblHeader;
        private Panel _propPanel;
        private CheckBox _chkEnabled;
        private TextBox _txtName;
        private ComboBox _cmbSourceType;
        private ComboBox _cmbTargetSubGraph;
        private ComboBox _cmbCameraSlot;
        private NumericUpDown _numMaxConcurrent;
        private NumericUpDown _numTimerInterval;
        private ComboBox _cmbSerialSlot;
        private Label _lblCameraSlot;
        private Label _lblTimerInterval;
        private Label _lblSerialSlot;
        private Label _lblMaxConcurrent;

        public TriggerEditorForm(FlowGraph graph, FlowExecutor executor, VisionController vision)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _vision = vision;

            InitializeComponent();
            WireEvents();
            RefreshTriggerList();
        }

        private void WireEvents()
        {
            _btnAdd.Click += (s, e) => AddTrigger();
            _btnDelete.Click += (s, e) => DeleteSelectedTrigger();
            _btnFire.Click += (s, e) => FireSelected();
            _triggerList.SelectedIndexChanged += OnTriggerSelected;
            _triggerList.ItemChecked += OnTriggerChecked;
            _cmbSourceType.SelectedIndexChanged += OnSourceTypeChanged;
            _txtName.Leave += (s, e) => { if (!_suppressEvents && _selected != null) { _selected.Name = _txtName.Text; RefreshTriggerItem(_selected); } };
            _numMaxConcurrent.ValueChanged += (s, e) => { if (!_suppressEvents && _selected != null) _selected.MaxConcurrent = (int)_numMaxConcurrent.Value; };
            _cmbTargetSubGraph.SelectedIndexChanged += (s, e) =>
            {
                if (!_suppressEvents && _selected != null && _cmbTargetSubGraph.SelectedItem is DisplayItem di)
                { _selected.TargetSubGraphId = idFromTag(di.Tag); RefreshTriggerItem(_selected); }
            };
            _cmbCameraSlot.SelectedIndexChanged += (s, e) =>
            {
                if (!_suppressEvents && _selected != null && _cmbCameraSlot.SelectedItem is DisplayItem di)
                    _selected.CameraSlotId = di.Id ?? "";
            };
            _numTimerInterval.ValueChanged += (s, e) => { if (!_suppressEvents && _selected != null) _selected.TimerIntervalMs = (int)_numTimerInterval.Value; };
            _cmbSerialSlot.SelectedIndexChanged += (s, e) =>
            {
                if (!_suppressEvents && _selected != null && _cmbSerialSlot.SelectedItem is DisplayItem di)
                    _selected.SerialSlotId = di.Id ?? "";
            };
            _chkEnabled.CheckedChanged += (s, e) => { if (!_suppressEvents && _selected != null) { _selected.Enabled = _chkEnabled.Checked; RefreshTriggerItem(_selected); } };
        }

        private void AddTrigger()
        {
            var entry = new TriggerEntry
            {
                Name = "新触发器",
                SourceType = TriggerSourceType.Manual,
                TargetSubGraphId = _graph.SubGraphs.Count > 0 ? _graph.SubGraphs[0].Id : Guid.Empty,
                MaxConcurrent = 1,
            };
            _graph.Triggers.Add(entry);
            RefreshTriggerList();
            SelectTrigger(entry);
        }

        private void DeleteSelectedTrigger()
        {
            if (_selected == null) return;
            _graph.Triggers.Remove(_selected);
            _selected = null;
            RefreshTriggerList();
            ClearProperties();
        }

        private async void FireSelected()
        {
            if (_selected == null) return;
            await _executor.FireManualTrigger(_selected.Id);
        }

        private void RefreshTriggerList()
        {
            _triggerList.Items.Clear();
            foreach (var t in _graph.Triggers)
            {
                var item = new ListViewItem(FormatTriggerName(t)) { Tag = t, Checked = t.Enabled };
                _triggerList.Items.Add(item);
            }
        }

        private void RefreshTriggerItem(TriggerEntry entry)
        {
            foreach (ListViewItem item in _triggerList.Items)
            {
                if (item.Tag == entry)
                {
                    item.Text = FormatTriggerName(entry);
                    item.Checked = entry.Enabled;
                    break;
                }
            }
        }

        private static string FormatTriggerName(TriggerEntry t)
        {
            string type = t.SourceType == TriggerSourceType.CameraFrame ? "[相机帧]"
                : t.SourceType == TriggerSourceType.Timer ? "[定时器]"
                : t.SourceType == TriggerSourceType.SerialMatch ? "[串口]"
                : "[手动]";
            return $"{type} {t.Name}";
        }

        private void SelectTrigger(TriggerEntry entry)
        {
            foreach (ListViewItem item in _triggerList.Items)
            {
                if (item.Tag == entry)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        private void ClearProperties()
        {
            _suppressEvents = true;
            _txtName.Text = "";
            _cmbSourceType.SelectedIndex = 0;
            _cmbTargetSubGraph.SelectedIndex = -1;
            PopulateCameraSlotCombo();
            _cmbCameraSlot.SelectedIndex = -1;
            _numMaxConcurrent.Value = 1;
            _numTimerInterval.Value = 100;
            PopulateSerialSlotCombo();
            _cmbSerialSlot.SelectedIndex = -1;
            _chkEnabled.Checked = true;
            _btnDelete.Enabled = false;
            _btnFire.Enabled = false;
            _suppressEvents = false;
            UpdateVisibility(TriggerSourceType.Manual);
        }

        private void OnTriggerSelected(object sender, EventArgs e)
        {
            _selected = null;
            if (_triggerList.SelectedItems.Count == 0)
            {
                ClearProperties();
                return;
            }

            var item = _triggerList.SelectedItems[0];
            _selected = item.Tag as TriggerEntry;
            if (_selected == null) return;

            PopulateProperties(_selected);
            _btnDelete.Enabled = true;
            _btnFire.Enabled = true;
        }

        private void OnTriggerChecked(object sender, ItemCheckedEventArgs e)
        {
            var entry = e.Item.Tag as TriggerEntry;
            if (entry != null) entry.Enabled = e.Item.Checked;
        }

        private void OnSourceTypeChanged(object sender, EventArgs e)
        {
            if (_suppressEvents || _selected == null) return;
            var idx = _cmbSourceType.SelectedIndex;
            var type = idx == 0 ? TriggerSourceType.Manual
                : idx == 1 ? TriggerSourceType.CameraFrame
                : idx == 2 ? TriggerSourceType.Timer
                : TriggerSourceType.SerialMatch;
            _selected.SourceType = type;
            UpdateVisibility(type);
            RefreshTriggerItem(_selected);
        }

        private void UpdateVisibility(TriggerSourceType type)
        {
            _lblCameraSlot.Visible = _cmbCameraSlot.Visible = (type == TriggerSourceType.CameraFrame);
            _lblTimerInterval.Visible = _numTimerInterval.Visible = (type == TriggerSourceType.Timer);
            _lblSerialSlot.Visible = _cmbSerialSlot.Visible = (type == TriggerSourceType.SerialMatch);
            _lblMaxConcurrent.Visible = _numMaxConcurrent.Visible = (type != TriggerSourceType.Manual);
        }

        private void PopulateProperties(TriggerEntry entry)
        {
            _suppressEvents = true;

            _chkEnabled.Checked = entry.Enabled;
            _txtName.Text = entry.Name ?? "";
            _cmbSourceType.SelectedIndex = (int)entry.SourceType;
            _numMaxConcurrent.Value = entry.MaxConcurrent < 1 ? 1 : entry.MaxConcurrent > 10 ? 10 : entry.MaxConcurrent;

            PopulateSubGraphCombo();
            if (entry.TargetSubGraphId != Guid.Empty)
                SelectByTag(_cmbTargetSubGraph, entry.TargetSubGraphId);

            PopulateCameraSlotCombo();
            if (!string.IsNullOrEmpty(entry.CameraSlotId))
                SelectById(_cmbCameraSlot, entry.CameraSlotId);

            _numTimerInterval.Value = entry.TimerIntervalMs < 1 ? 1 : entry.TimerIntervalMs > 60000 ? 60000 : entry.TimerIntervalMs;

            PopulateSerialSlotCombo();
            if (!string.IsNullOrEmpty(entry.SerialSlotId))
                SelectById(_cmbSerialSlot, entry.SerialSlotId);

            UpdateVisibility(entry.SourceType);

            _suppressEvents = false;
        }

        private void PopulateSubGraphCombo()
        {
            _cmbTargetSubGraph.Items.Clear();
            foreach (var sg in _graph.SubGraphs)
                _cmbTargetSubGraph.Items.Add(new DisplayItem("", sg.Name) { Tag = sg.Id });
            if (_cmbTargetSubGraph.Items.Count > 0) _cmbTargetSubGraph.SelectedIndex = 0;
        }

        private void PopulateCameraSlotCombo()
        {
            _cmbCameraSlot.Items.Clear();
            if (_vision?.CameraManager?.Slots != null)
            {
                foreach (var s in _vision.CameraManager.Slots)
                    _cmbCameraSlot.Items.Add(new DisplayItem(s.SlotId, $"{s.SlotName} ({s.SlotId.Substring(0, 8)})"));
            }
            if (_cmbCameraSlot.Items.Count > 0) _cmbCameraSlot.SelectedIndex = 0;
        }

        private void PopulateSerialSlotCombo()
        {
            _cmbSerialSlot.Items.Clear();
            var serialSlots = _vision?.GetSerialSlots();
            if (serialSlots != null)
            {
                foreach (var s in serialSlots)
                    _cmbSerialSlot.Items.Add(new DisplayItem(s.SlotId, $"{s.SlotName} ({s.PortName})"));
            }
            if (_cmbSerialSlot.Items.Count > 0) _cmbSerialSlot.SelectedIndex = 0;
        }

        private void SelectByTag(ComboBox cmb, Guid tag)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is DisplayItem di && di.Tag is Guid g && g == tag)
                { cmb.SelectedIndex = i; return; }
            }
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }

        private void SelectById(ComboBox cmb, string id)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is DisplayItem di && di.Id == id)
                { cmb.SelectedIndex = i; return; }
            }
        }

        private static Guid idFromTag(object tag)
        {
            return tag is Guid g ? g : Guid.Empty;
        }
    }
}
