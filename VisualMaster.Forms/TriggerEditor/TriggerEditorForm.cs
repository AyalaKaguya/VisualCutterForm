using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualMaster.WorkFlow;
using VisualMaster.WorkFlow.Triggers;

namespace VisualMaster.Forms.TriggerEditor
{
    public class TriggerEditorForm : Form
    {
        private FlowGraph _graph;
        private FlowExecutor _executor;
        private VisionController _vision;

        private SplitContainer _split;
        private ListView _triggerList;
        private Panel _rightPanel;
        private Label _lblHeader;
        private FlowLayoutPanel _btnPanel;
        private Button _btnAdd;
        private Button _btnDelete;
        private Button _btnFire;

        private TextBox _txtName;
        private ComboBox _cmbSourceType;
        private ComboBox _cmbTargetSubGraph;
        private ComboBox _cmbCameraSlot;
        private NumericUpDown _numMaxConcurrent;
        private NumericUpDown _numTimerInterval;
        private ComboBox _cmbSerialSlot;
        private CheckBox _chkEnabled;

        private Label _lblCameraSlot;
        private Label _lblTimerInterval;
        private Label _lblSerialSlot;
        private Label _lblMaxConcurrent;

        private TriggerEntry _selected;
        private bool _suppressEvents;

        public TriggerEditorForm(FlowGraph graph, FlowExecutor executor, VisionController vision)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _vision = vision;

            Text = "触发器管理器";
            Size = new Size(850, 550);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);
            BackColor = Color.FromArgb(40, 40, 40);

            BuildLayout();
            RefreshTriggerList();
        }

        private void BuildLayout()
        {
            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300,
                Panel1MinSize = 200,
                BackColor = Color.FromArgb(50, 50, 50),
            };

            BuildLeftPanel();
            BuildRightPanel();

            Controls.Add(_split);
        }

        private void BuildLeftPanel()
        {
            var left = _split.Panel1;
            left.BackColor = Color.FromArgb(45, 45, 45);

            _btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(6, 4, 6, 0),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.FromArgb(50, 50, 50),
            };

            _btnAdd = CreateSmallButton("新建");
            _btnAdd.Click += (s, e) => AddTrigger();
            _btnDelete = CreateSmallButton("删除");
            _btnDelete.Click += (s, e) => DeleteSelectedTrigger();
            _btnDelete.Enabled = false;
            _btnFire = CreateSmallButton("▶ 立即触发");
            _btnFire.Click += (s, e) => FireSelected();
            _btnFire.Enabled = false;

            _btnPanel.Controls.Add(_btnAdd);
            _btnPanel.Controls.Add(_btnDelete);
            _btnPanel.Controls.Add(_btnFire);

            _triggerList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.None,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Microsoft YaHei", 9F),
                CheckBoxes = true,
            };
            _triggerList.Columns.Add("触发器", _triggerList.Width - 4);
            _triggerList.SelectedIndexChanged += OnTriggerSelected;
            _triggerList.ItemChecked += OnTriggerChecked;

            left.Controls.Add(_btnPanel);
            left.Controls.Add(_triggerList);
        }

        private void BuildRightPanel()
        {
            _rightPanel = _split.Panel2;
            _rightPanel.BackColor = Color.FromArgb(48, 48, 48);
            _rightPanel.AutoScroll = true;
            _rightPanel.Padding = new Padding(12);

            _lblHeader = new Label
            {
                Text = "触发器属性",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 152, 219),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 0, 0),
            };

            var propPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(48, 48, 48),
            };

            int y = 8;
            int w = 340;

            _chkEnabled = new CheckBox
            {
                Text = "启用",
                Location = new Point(14, y),
                Size = new Size(60, 26),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(48, 48, 48),
            };
            _chkEnabled.CheckedChanged += (s, e) => { if (!_suppressEvents && _selected != null) { _selected.Enabled = _chkEnabled.Checked; RefreshTriggerItem(_selected); } };
            propPanel.Controls.Add(_chkEnabled);
            y += 34;

            AddLabel(propPanel, "名称", ref y);
            _txtName = new TextBox
            {
                Location = new Point(120, y),
                Size = new Size(w - 130, 26),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _txtName.Leave += (s, e) => { if (!_suppressEvents && _selected != null) { _selected.Name = _txtName.Text; RefreshTriggerItem(_selected); } };
            propPanel.Controls.Add(_txtName);
            y += 34;

            AddLabel(propPanel, "类型", ref y);
            _cmbSourceType = new ComboBox
            {
                Location = new Point(120, y),
                Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };
            _cmbSourceType.Items.AddRange(new[] { "手动触发", "相机帧触发", "定时器触发", "串口匹配触发" });
            _cmbSourceType.SelectedIndexChanged += OnSourceTypeChanged;
            propPanel.Controls.Add(_cmbSourceType);
            y += 34;

            AddLabel(propPanel, "最大并发", ref y);
            _lblMaxConcurrent = propPanel.Controls[propPanel.Controls.Count - 1] as Label;
            _numMaxConcurrent = new NumericUpDown
            {
                Location = new Point(120, y),
                Size = new Size(80, 26),
                Minimum = 1,
                Maximum = 10,
                Value = 1,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };
            _numMaxConcurrent.ValueChanged += (s, e) => { if (!_suppressEvents && _selected != null) _selected.MaxConcurrent = (int)_numMaxConcurrent.Value; };
            propPanel.Controls.Add(_numMaxConcurrent);
            y += 34;

            AddLabel(propPanel, "目标子图", ref y);
            _cmbTargetSubGraph = new ComboBox
            {
                Location = new Point(120, y),
                Size = new Size(w - 130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };
            PopulateSubGraphCombo();
            _cmbTargetSubGraph.SelectedIndexChanged += (s, e) =>
            {
                if (!_suppressEvents && _selected != null && _cmbTargetSubGraph.SelectedItem is DisplayItem di)
                {
                    _selected.TargetSubGraphId = idFromTag(di.Tag);
                    RefreshTriggerItem(_selected);
                }
            };
            propPanel.Controls.Add(_cmbTargetSubGraph);
            y += 38;

            AddLabel(propPanel, "相机槽位", ref y);
            _lblCameraSlot = propPanel.Controls[propPanel.Controls.Count - 1] as Label;
            _cmbCameraSlot = new ComboBox
            {
                Location = new Point(120, y),
                Size = new Size(w - 130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };
            PopulateCameraSlotCombo();
            _cmbCameraSlot.SelectedIndexChanged += (s, e) =>
            {
                if (!_suppressEvents && _selected != null && _cmbCameraSlot.SelectedItem is DisplayItem di)
                    _selected.CameraSlotId = di.Id ?? "";
            };
            propPanel.Controls.Add(_cmbCameraSlot);
            y += 38;

            AddLabel(propPanel, "定时间隔(ms)", ref y);
            _lblTimerInterval = propPanel.Controls[propPanel.Controls.Count - 1] as Label;
            _numTimerInterval = new NumericUpDown
            {
                Location = new Point(120, y),
                Size = new Size(100, 26),
                Minimum = 1,
                Maximum = 60000,
                Value = 100,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };
            _numTimerInterval.ValueChanged += (s, e) => { if (!_suppressEvents && _selected != null) _selected.TimerIntervalMs = (int)_numTimerInterval.Value; };
            propPanel.Controls.Add(_numTimerInterval);
            y += 38;

            AddLabel(propPanel, "串口槽位", ref y);
            _lblSerialSlot = propPanel.Controls[propPanel.Controls.Count - 1] as Label;
            _cmbSerialSlot = new ComboBox
            {
                Location = new Point(120, y),
                Size = new Size(w - 130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };
            PopulateSerialSlotCombo();
            _cmbSerialSlot.SelectedIndexChanged += (s, e) =>
            {
                if (!_suppressEvents && _selected != null && _cmbSerialSlot.SelectedItem is DisplayItem di)
                    _selected.SerialSlotId = di.Id ?? "";
            };
            propPanel.Controls.Add(_cmbSerialSlot);

            _rightPanel.Controls.Add(_lblHeader);
            _rightPanel.Controls.Add(propPanel);
        }

        private void AddLabel(Panel panel, string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text + ":",
                Location = new Point(14, y + 4),
                Size = new Size(100, 22),
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleRight,
            };
            panel.Controls.Add(lbl);
        }

        private Button CreateSmallButton(string text)
        {
            return new Button
            {
                Text = text,
                Size = new Size(text.Contains("▶") ? 90 : 55, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };
        }

        private void AddTrigger()
        {
            var entry = new TriggerEntry
            {
                Name = "新触发器",
                SourceType = TriggerSourceType.Manual,
                TargetSubGraphId = _graph.SubGraphs.FirstOrDefault()?.Id ?? Guid.Empty,
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
            var sg = t.TargetSubGraphId == Guid.Empty ? "?" : "";
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
            _cmbCameraSlot.SelectedIndex = -1;
            _numMaxConcurrent.Value = 1;
            _numTimerInterval.Value = 100;
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
            _numMaxConcurrent.Value = Math.Max(1, Math.Min(10, entry.MaxConcurrent));

            SelectByTag(_cmbTargetSubGraph, entry.TargetSubGraphId);

            PopulateCameraSlotCombo();
            if (!string.IsNullOrEmpty(entry.CameraSlotId))
                SelectById(_cmbCameraSlot, entry.CameraSlotId);

            _numTimerInterval.Value = Math.Max(1, Math.Min(60000, entry.TimerIntervalMs));

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
