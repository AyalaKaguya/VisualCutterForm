using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualCutterForm
{
    partial class SerialManagerForm
    {
        private System.ComponentModel.IContainer components = null;
        private RichTextBox _logBox;
        private Button _btnRefresh;
        private Button _btnConnect;
        private Button _btnDisconnect;
        private Button _btnSend;
        private TextBox _txtSend;
        private ComboBox _cmbPorts;
        private Label _lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "SerialManagerForm";
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

            _btnConnect = new Button
            {
                Text = "连接",
                Location = new Point(210, 3),
                Size = new Size(55, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnConnect.FlatAppearance.BorderSize = 1;

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
            _btnSend = new Button
            {
                Text = "发送",
                Location = new Point(310, 3),
                Size = new Size(60, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnSend.FlatAppearance.BorderSize = 1;

            sendBar.Controls.Add(_txtSend);
            sendBar.Controls.Add(_btnSend);

            var logPanel = new Panel { Dock = DockStyle.Fill };
            logPanel.Controls.Add(_logBox);
            logPanel.Controls.Add(topBar);
            logPanel.Controls.Add(sendBar);

            Controls.Add(logPanel);
            this.ResumeLayout(false);
        }
    }
}
