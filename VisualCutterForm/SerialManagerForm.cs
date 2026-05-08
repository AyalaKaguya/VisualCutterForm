using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualCutterForm.Lib;
using VisualMaster.Communication;

namespace VisualCutterForm
{
    public class SerialManagerForm : Form
    {
        private VisionController _vision;
        private RichTextBox _logBox;
        private Button _btnRefresh;
        private Button _btnConnect;
        private Button _btnDisconnect;
        private Button _btnSend;
        private TextBox _txtSend;
        private ComboBox _cmbPorts;
        private Label _lblStatus;

        public SerialManagerForm(VisionController vision)
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "串口管理器";
            Size = new Size(550, 420);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            BuildUI();
            RefreshPortList();

            FormClosing += (s, e) => { };
        }

        private void BuildUI()
        {
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(4, 4, 4, 0),
            };

            var lblPort = new Label
            {
                Text = "串口:",
                Location = new Point(4, 6),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
            };

            _cmbPorts = new ComboBox
            {
                Location = new Point(48, 4),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Microsoft YaHei", 9F),
            };

            _btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(154, 3),
                Size = new Size(50, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnRefresh.FlatAppearance.BorderSize = 1;
            _btnRefresh.Click += (s, e) => RefreshPortList();

            _btnConnect = new Button
            {
                Text = "连接",
                Location = new Point(210, 3),
                Size = new Size(55, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnConnect.FlatAppearance.BorderSize = 1;
            _btnConnect.Click += (s, e) => ConnectPort();

            _btnDisconnect = new Button
            {
                Text = "断开",
                Location = new Point(270, 3),
                Size = new Size(55, 26),
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnDisconnect.FlatAppearance.BorderSize = 1;
            _btnDisconnect.Click += (s, e) => DisconnectPort();

            _lblStatus = new Label
            {
                Text = "",
                Location = new Point(332, 8),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
            };

            topBar.Controls.Add(lblPort);
            topBar.Controls.Add(_cmbPorts);
            topBar.Controls.Add(_btnRefresh);
            topBar.Controls.Add(_btnConnect);
            topBar.Controls.Add(_btnDisconnect);
            topBar.Controls.Add(_lblStatus);

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None,
                WordWrap = false,
            };

            var sendBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                Padding = new Padding(4, 4, 4, 0),
            };
            _txtSend = new TextBox
            {
                Location = new Point(4, 4),
                Size = new Size(300, 22),
                Font = new Font("Microsoft YaHei", 9F),
            };
            _txtSend.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) SendData();
            };
            _btnSend = new Button
            {
                Text = "发送",
                Location = new Point(310, 3),
                Size = new Size(60, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnSend.FlatAppearance.BorderSize = 1;
            _btnSend.Click += (s, e) => SendData();

            sendBar.Controls.Add(_txtSend);
            sendBar.Controls.Add(_btnSend);

            var logPanel = new Panel { Dock = DockStyle.Fill };
            logPanel.Controls.Add(_logBox);
            logPanel.Controls.Add(topBar);
            logPanel.Controls.Add(sendBar);

            Controls.Add(logPanel);
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
            catch (Exception ex)
            {
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 连接失败: {ex.Message}\r\n");
            }
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
            catch (Exception ex)
            {
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 断开失败: {ex.Message}\r\n");
            }
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
            catch (Exception ex)
            {
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 发送失败: {ex.Message}\r\n");
            }
        }
    }
}
