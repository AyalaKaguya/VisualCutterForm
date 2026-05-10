using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using VisualMaster.Communication;

namespace VisualMaster.Forms
{
    public class SerialManagerForm : Form
    {
        private VisionController _vision;
        private TabControl _tabControl;
        private TabPage _tabPortList;
        private TabPage _tabSlotManager;

        private ListView _portListView;
        private Button _btnRefreshPorts;

        private SplitContainer _slotSplit;
        private ListBox _slotListBox;
        private Button _btnAddSlot;
        private Button _btnRemoveSlot;
        private Button _btnConnect;
        private Button _btnDisconnect;
        private Panel _configPanel;
        private ComboBox _cmbPortName;
        private NumericUpDown _numBaudRate;
        private ComboBox _cmbParity;
        private ComboBox _cmbStopBits;
        private NumericUpDown _numDataBits;
        private TextBox _txtSlotName;
        private RichTextBox _logBox;
        private SerialStats _selectedSlot;

        public SerialManagerForm(VisionController vision)
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "串口管理器";
            Size = new Size(850, 520);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            BuildUI();
            RefreshPortList();
            RefreshSerialSlots();
        }

        private void BuildUI()
        {
            _tabControl = new TabControl { Dock = DockStyle.Fill };

            // Tab 1: port list
            _tabPortList = new TabPage("串口列表");
            _portListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("Microsoft YaHei", 9F),
            };
            _portListView.Columns.Add("端口", 120);
            _portListView.Columns.Add("状态", 200);
            _btnRefreshPorts = new Button
            {
                Text = "刷新",
                Dock = DockStyle.Top,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
            };
            _btnRefreshPorts.Click += (s, e) => RefreshPortList();
            _tabPortList.Controls.Add(_portListView);
            _tabPortList.Controls.Add(_btnRefreshPorts);

            // Tab 2: slot manager
            _tabSlotManager = new TabPage("串口槽位");
            _slotSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 200,
                Panel1MinSize = 150,
            };

            _slotListBox = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Microsoft YaHei", 9F) };
            _slotListBox.SelectedIndexChanged += OnSlotSelected;

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 30,
                Padding = new Padding(4, 3, 4, 0),
            };
            _btnAddSlot = new Button { Text = "添加", Size = new Size(60, 24), FlatStyle = FlatStyle.Flat };
            _btnAddSlot.Click += (s, e) => { AddSlot(); };
            _btnRemoveSlot = new Button { Text = "删除", Size = new Size(60, 24), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnRemoveSlot.Click += (s, e) => { RemoveSelectedSlot(); };
            btnPanel.Controls.Add(_btnAddSlot);
            btnPanel.Controls.Add(_btnRemoveSlot);

            var leftPanel = new Panel { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(_slotListBox);
            leftPanel.Controls.Add(btnPanel);

            // right config panel
            _configPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), AutoScroll = true };
            BuildConfigPanel();

            _slotSplit.Panel1.Controls.Add(leftPanel);
            _slotSplit.Panel2.Controls.Add(_configPanel);
            _tabSlotManager.Controls.Add(_slotSplit);

            _tabControl.TabPages.Add(_tabPortList);
            _tabControl.TabPages.Add(_tabSlotManager);
            Controls.Add(_tabControl);
        }

        private void BuildConfigPanel()
        {
            int y = 8;

            AddLabeledControl("名称:", ref y, out _txtSlotName, new TextBox { Width = 220 });

            var lblPort = new Label { Text = "端口:", Location = new Point(8, y + 4), Size = new Size(80, 22), TextAlign = ContentAlignment.MiddleRight };
            _cmbPortName = new ComboBox { Location = new Point(94, y + 2), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _configPanel.Controls.Add(lblPort);
            _configPanel.Controls.Add(_cmbPortName);
            y += 30;

            AddLabeledNumeric("波特率:", ref y, out _numBaudRate, 1200, 256000, 9600);

            AddLabeledCombo("校验:", ref y, out _cmbParity, new[] { "None", "Odd", "Even", "Mark", "Space" }, 0);
            AddLabeledCombo("停止位:", ref y, out _cmbStopBits, new[] { "One", "Two", "OnePointFive" }, 0);
            AddLabeledNumeric("数据位:", ref y, out _numDataBits, 5, 8, 8);

            _btnConnect = new Button { Text = "连接", Location = new Point(94, y + 4), Size = new Size(70, 26), FlatStyle = FlatStyle.Flat };
            _btnConnect.Click += (s, e) => ConnectSelectedSlot();
            _btnDisconnect = new Button { Text = "断开", Location = new Point(170, y + 4), Size = new Size(70, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnDisconnect.Click += (s, e) => DisconnectSelectedSlot();
            _configPanel.Controls.Add(_btnConnect);
            _configPanel.Controls.Add(_btnDisconnect);
            y += 34;

            var lblLog = new Label { Text = "通信日志:", Location = new Point(8, y + 4), Size = new Size(80, 22) };
            _logBox = new RichTextBox
            {
                Location = new Point(94, y + 2),
                Size = new Size(400, 120),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None,
                WordWrap = false,
            };
            _configPanel.Controls.Add(lblLog);
            _configPanel.Controls.Add(_logBox);
        }

        private void AddLabeledControl(string label, ref int y, out TextBox txt, TextBox control)
        {
            var lbl = new Label { Text = label, Location = new Point(8, y + 4), Size = new Size(80, 22), TextAlign = ContentAlignment.MiddleRight };
            txt = control;
            txt.Location = new Point(94, y + 2);
            _configPanel.Controls.Add(lbl);
            _configPanel.Controls.Add(txt);
            y += 30;
        }

        private void AddLabeledNumeric(string label, ref int y, out NumericUpDown num, int min, int max, int val)
        {
            var lbl = new Label { Text = label, Location = new Point(8, y + 4), Size = new Size(80, 22), TextAlign = ContentAlignment.MiddleRight };
            num = new NumericUpDown { Location = new Point(94, y + 2), Width = 100, Minimum = min, Maximum = max, Value = val };
            _configPanel.Controls.Add(lbl);
            _configPanel.Controls.Add(num);
            y += 30;
        }

        private void AddLabeledCombo(string label, ref int y, out ComboBox cmb, string[] items, int defIdx)
        {
            var lbl = new Label { Text = label, Location = new Point(8, y + 4), Size = new Size(80, 22), TextAlign = ContentAlignment.MiddleRight };
            cmb = new ComboBox { Location = new Point(94, y + 2), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmb.Items.AddRange(items);
            cmb.SelectedIndex = defIdx;
            _configPanel.Controls.Add(lbl);
            _configPanel.Controls.Add(cmb);
            y += 30;
        }

        private void RefreshPortList()
        {
            _portListView.Items.Clear();
            try
            {
                var ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    bool used = _vision.IsSerialOpen(port);
                    var item = new ListViewItem(port);
                    item.SubItems.Add(used ? "已打开" : "空闲");
                    _portListView.Items.Add(item);
                }
                _cmbPortName.Items.Clear();
                _cmbPortName.Items.AddRange(ports);
            }
            catch { }
        }

        private void RefreshSerialSlots()
        {
            _slotListBox.Items.Clear();
            foreach (var kv in _vision.SerialPorts)
            {
                var status = kv.Value.IsOpen ? " [已连接]" : " [未连接]";
                _slotListBox.Items.Add(new SerialStats { PortName = kv.Key, Display = $"{kv.Key}{status}" });
            }
            foreach (var portName in SerialPort.GetPortNames())
            {
                if (!_vision.SerialPorts.ContainsKey(portName))
                    _slotListBox.Items.Add(new SerialStats { PortName = portName, Display = $"{portName} [未连接]" });
            }
        }

        private void AddSlot()
        {
            var port = _cmbPortName.Text;
            if (string.IsNullOrEmpty(port)) return;
            try
            {
                _vision.ConnectSerial(port, (int)_numBaudRate.Value, (int)_numDataBits.Value,
                    _cmbParity.Text, _cmbStopBits.Text);
                RefreshSerialSlots();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开串口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveSelectedSlot()
        {
            if (_selectedSlot == null) return;
            _vision.DisconnectSerial(_selectedSlot.PortName);
            _selectedSlot = null;
            RefreshSerialSlots();
            RefreshPortList();
        }

        private void ConnectSelectedSlot()
        {
            if (_selectedSlot == null) return;
            AddSlot();
        }

        private void DisconnectSelectedSlot()
        {
            RemoveSelectedSlot();
        }

        private void OnSlotSelected(object sender, EventArgs e)
        {
            var item = _slotListBox.SelectedItem as SerialStats;
            _selectedSlot = item;
            bool hasSel = _selectedSlot != null;
            bool isOpen = hasSel && _vision.IsSerialOpen(_selectedSlot.PortName);

            _btnRemoveSlot.Enabled = hasSel;
            _btnConnect.Enabled = hasSel && !isOpen;
            _btnDisconnect.Enabled = hasSel && isOpen;

            if (hasSel)
            {
                _cmbPortName.Text = _selectedSlot.PortName;
                _txtSlotName.Text = _selectedSlot.PortName;
            }
        }

        private class SerialStats
        {
            public string PortName;
            public string Display;
            public override string ToString() => Display;
        }
    }
}
