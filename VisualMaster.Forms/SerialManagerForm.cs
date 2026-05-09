using System;
using System.Drawing;
using System.Windows.Forms;
using VisualMaster.Communication;

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
            Size = new Size(550, 420);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            _btnRefresh.Click += (s, e) => RefreshPortList();
            _btnConnect.Click += (s, e) => ConnectPort();
            _btnDisconnect.Click += (s, e) => DisconnectPort();
            _btnSend.Click += (s, e) => SendData();
            _txtSend.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) SendData(); };

            RefreshPortList();
        }

        private void RefreshPortList()
        {
            _cmbPorts.Items.Clear();
            try
            {
                var ports = SerialPortUtility.GetAvailablePorts();
                _cmbPorts.Items.AddRange(ports);
            }
            catch { }
            UpdatePortState();
        }

        private void UpdatePortState()
        {
            var currentPort = _cmbPorts.Text;
            bool isOpen = !string.IsNullOrEmpty(currentPort) && _vision.IsSerialOpen(currentPort);
            _btnConnect.Enabled = !isOpen && !string.IsNullOrEmpty(currentPort);
            _btnDisconnect.Enabled = isOpen;
            _lblStatus.Text = isOpen ? "已连接" : "未连接";
        }

        private void ConnectPort()
        {
            var port = _cmbPorts.Text;
            if (string.IsNullOrEmpty(port)) return;
            try
            {
                _vision.ConnectSerial(port);
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 已连接 {port}\r\n");
                UpdatePortState();
            }
            catch (Exception ex) { _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 连接失败: {ex.Message}\r\n"); }
        }

        private void DisconnectPort()
        {
            var port = _cmbPorts.Text;
            if (string.IsNullOrEmpty(port)) return;
            try
            {
                _vision.DisconnectSerial(port);
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 已断开 {port}\r\n");
                UpdatePortState();
            }
            catch (Exception ex) { _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 断开失败: {ex.Message}\r\n"); }
        }

        private void SendData()
        {
            var text = _txtSend.Text;
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                _vision.OutputResult(_cmbPorts.Text, text);
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] TX: {text}\r\n");
                _txtSend.Text = "";
            }
            catch (Exception ex) { _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 发送失败: {ex.Message}\r\n"); }
        }
    }
}
