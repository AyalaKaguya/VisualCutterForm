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
        private Panel _topBar;
        private Label _lblPort;
        private Panel _sendBar;
        private Panel _logPanel;

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
            this._topBar = new System.Windows.Forms.Panel();
            this._lblPort = new System.Windows.Forms.Label();
            this._cmbPorts = new System.Windows.Forms.ComboBox();
            this._btnRefresh = new System.Windows.Forms.Button();
            this._btnConnect = new System.Windows.Forms.Button();
            this._btnDisconnect = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this._logBox = new System.Windows.Forms.RichTextBox();
            this._sendBar = new System.Windows.Forms.Panel();
            this._txtSend = new System.Windows.Forms.TextBox();
            this._btnSend = new System.Windows.Forms.Button();
            this._logPanel = new System.Windows.Forms.Panel();
            this._topBar.SuspendLayout();
            this._sendBar.SuspendLayout();
            this._logPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _topBar
            // 
            this._topBar.Controls.Add(this._lblPort);
            this._topBar.Controls.Add(this._cmbPorts);
            this._topBar.Controls.Add(this._btnRefresh);
            this._topBar.Controls.Add(this._btnConnect);
            this._topBar.Controls.Add(this._btnDisconnect);
            this._topBar.Controls.Add(this._lblStatus);
            this._topBar.Dock = System.Windows.Forms.DockStyle.Top;
            this._topBar.Height = 34;
            this._topBar.Name = "_topBar";
            this._topBar.Padding = new System.Windows.Forms.Padding(4, 4, 4, 0);
            // 
            // _lblPort
            // 
            this._lblPort.AutoSize = true;
            this._lblPort.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblPort.Location = new System.Drawing.Point(4, 6);
            this._lblPort.Name = "_lblPort";
            this._lblPort.Text = "串口:";
            // 
            // _cmbPorts
            // 
            this._cmbPorts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this._cmbPorts.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._cmbPorts.Location = new System.Drawing.Point(48, 4);
            this._cmbPorts.Name = "_cmbPorts";
            this._cmbPorts.Width = 100;
            // 
            // _btnRefresh
            // 
            this._btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRefresh.FlatAppearance.BorderSize = 1;
            this._btnRefresh.Font = new System.Drawing.Font("Microsoft YaHei", 8F);
            this._btnRefresh.Location = new System.Drawing.Point(154, 3);
            this._btnRefresh.Name = "_btnRefresh";
            this._btnRefresh.Size = new System.Drawing.Size(50, 26);
            this._btnRefresh.Text = "刷新";
            // 
            // _btnConnect
            // 
            this._btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnConnect.FlatAppearance.BorderSize = 1;
            this._btnConnect.Font = new System.Drawing.Font("Microsoft YaHei", 8F);
            this._btnConnect.Location = new System.Drawing.Point(210, 3);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Size = new System.Drawing.Size(55, 26);
            this._btnConnect.Text = "连接";
            // 
            // _btnDisconnect
            // 
            this._btnDisconnect.Enabled = false;
            this._btnDisconnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnDisconnect.FlatAppearance.BorderSize = 1;
            this._btnDisconnect.Font = new System.Drawing.Font("Microsoft YaHei", 8F);
            this._btnDisconnect.Location = new System.Drawing.Point(270, 3);
            this._btnDisconnect.Name = "_btnDisconnect";
            this._btnDisconnect.Size = new System.Drawing.Size(55, 26);
            this._btnDisconnect.Text = "断开";
            // 
            // _lblStatus
            // 
            this._lblStatus.AutoSize = true;
            this._lblStatus.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblStatus.Location = new System.Drawing.Point(332, 8);
            this._lblStatus.Name = "_lblStatus";
            // 
            // _logBox
            // 
            this._logBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._logBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._logBox.Font = new System.Drawing.Font("Consolas", 9F);
            this._logBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this._logBox.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this._logBox.Name = "_logBox";
            this._logBox.ReadOnly = true;
            this._logBox.WordWrap = false;
            // 
            // _sendBar
            // 
            this._sendBar.Controls.Add(this._txtSend);
            this._sendBar.Controls.Add(this._btnSend);
            this._sendBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._sendBar.Height = 34;
            this._sendBar.Name = "_sendBar";
            this._sendBar.Padding = new System.Windows.Forms.Padding(4, 4, 4, 0);
            // 
            // _txtSend
            // 
            this._txtSend.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._txtSend.Location = new System.Drawing.Point(4, 4);
            this._txtSend.Name = "_txtSend";
            this._txtSend.Size = new System.Drawing.Size(300, 22);
            // 
            // _btnSend
            // 
            this._btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSend.FlatAppearance.BorderSize = 1;
            this._btnSend.Font = new System.Drawing.Font("Microsoft YaHei", 8F);
            this._btnSend.Location = new System.Drawing.Point(310, 3);
            this._btnSend.Name = "_btnSend";
            this._btnSend.Size = new System.Drawing.Size(60, 26);
            this._btnSend.Text = "发送";
            // 
            // _logPanel
            // 
            this._logPanel.Controls.Add(this._logBox);
            this._logPanel.Controls.Add(this._topBar);
            this._logPanel.Controls.Add(this._sendBar);
            this._logPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._logPanel.Name = "_logPanel";
            // 
            // SerialManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._logPanel);
            this.Name = "SerialManagerForm";
            this._topBar.ResumeLayout(false);
            this._topBar.PerformLayout();
            this._sendBar.ResumeLayout(false);
            this._sendBar.PerformLayout();
            this._logPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
