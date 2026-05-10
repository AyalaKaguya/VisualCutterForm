using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace VisualMaster.Forms
{
    public partial class SerialManagerForm : Form
    {
        private VisionController _vision;

        public SerialManagerForm()
        {
            InitializeComponent();
        }

        public SerialManagerForm(VisionController vision) : this()
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "串口管理器";
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            _btnRefreshPorts.Click += (s, e) => RefreshPortList();
            _btnAddSlot.Click += (s, e) => AddSlot();
            _btnRemoveSlot.Click += (s, e) => RemoveSelectedSlot();
            _btnConnect.Click += (s, e) => AddSlot();
            _btnDisconnect.Click += (s, e) => RemoveSelectedSlot();
            _slotListBox.SelectedIndexChanged += OnSlotSelected;
            Closing += (s, e) => { foreach (var kv in _vision.SerialPorts.ToList()) _vision.DisconnectSerial(kv.Key); };

            RefreshPortList();
            RefreshSlotList();
        }

        private void RefreshPortList()
        {
            _portListView.Items.Clear();
            _cmbPortName.Items.Clear();
            try
            {
                var ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    bool open = _vision.IsSerialOpen(port);
                    var item = new ListViewItem(port);
                    item.SubItems.Add(open ? "已打开" : "空闲");
                    _portListView.Items.Add(item);
                }
                _cmbPortName.Items.AddRange(ports);
            }
            catch { }
            if (_cmbPortName.Items.Count > 0) _cmbPortName.SelectedIndex = 0;
        }

        private void RefreshSlotList()
        {
            _slotListBox.Items.Clear();
            foreach (var kv in _vision.SerialPorts)
            {
                var status = kv.Value.IsOpen ? " [已连接]" : " [未连接]";
                _slotListBox.Items.Add(new DisplayItem(kv.Key, $"{kv.Key}{status}"));
            }
        }

        private void AddSlot()
        {
            var port = _cmbPortName.Text?.Trim();
            if (string.IsNullOrEmpty(port))
            {
                MessageBox.Show("请输入或选择一个串口名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_vision.IsSerialOpen(port))
            {
                MessageBox.Show($"串口 {port} 已打开。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var parity = _cmbParity.Text;
                var stopBits = _cmbStopBits.Text;
                var baud = (int)_numBaudRate.Value;
                var dataBits = (int)_numDataBits.Value;
                _vision.ConnectSerial(port, baud, dataBits, parity, stopBits);
                RefreshPortList();
                RefreshSlotList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开串口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveSelectedSlot()
        {
            var item = _slotListBox.SelectedItem as DisplayItem;
            if (item == null) return;
            _vision.DisconnectSerial(item.Id);
            RefreshPortList();
            RefreshSlotList();
            _btnConnect.Enabled = false;
            _btnDisconnect.Enabled = false;
            _btnRemoveSlot.Enabled = false;
        }

        private void OnSlotSelected(object sender, EventArgs e)
        {
            var item = _slotListBox.SelectedItem as DisplayItem;
            bool hasSel = item != null;
            bool isOpen = hasSel && _vision.IsSerialOpen(item.Id);

            _btnRemoveSlot.Enabled = hasSel;
            _btnConnect.Enabled = hasSel && !isOpen;
            _btnDisconnect.Enabled = hasSel && isOpen;
            _btnAddSlot.Enabled = true;

            if (hasSel)
            {
                _txtSlotName.Text = item.Id;
                _cmbPortName.Text = item.Id;
                _lblSlotStatus.Text = isOpen ? $"{item.Id} 已连接" : $"{item.Id} 未连接";
            }
            else
            {
                _txtSlotName.Text = "";
                _lblSlotStatus.Text = "";
            }
        }
    }
}
