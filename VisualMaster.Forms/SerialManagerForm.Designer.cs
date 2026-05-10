namespace VisualMaster.Forms
{
    partial class SerialManagerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabPortList;
        private System.Windows.Forms.TabPage _tabSlotManager;
        private System.Windows.Forms.ListView _portListView;
        private System.Windows.Forms.Button _btnRefreshPorts;
        private System.Windows.Forms.SplitContainer _slotSplit;
        private System.Windows.Forms.Panel _slotPanel;
        private System.Windows.Forms.ListBox _slotListBox;
        private System.Windows.Forms.FlowLayoutPanel _slotBtnPanel;
        private System.Windows.Forms.Button _btnAddSlot;
        private System.Windows.Forms.Button _btnRemoveSlot;
        private System.Windows.Forms.Label _lblSlotHeader;
        private System.Windows.Forms.Panel _settingsPanel;
        private System.Windows.Forms.Label _lblSettingsHeader;
        private System.Windows.Forms.Label _lblPortName;
        private System.Windows.Forms.ComboBox _cmbPortName;
        private System.Windows.Forms.Label _lblBaudRate;
        private System.Windows.Forms.NumericUpDown _numBaudRate;
        private System.Windows.Forms.Label _lblParity;
        private System.Windows.Forms.ComboBox _cmbParity;
        private System.Windows.Forms.Label _lblStopBits;
        private System.Windows.Forms.ComboBox _cmbStopBits;
        private System.Windows.Forms.Label _lblDataBits;
        private System.Windows.Forms.NumericUpDown _numDataBits;
        private System.Windows.Forms.Panel _configPanel;
        private System.Windows.Forms.FlowLayoutPanel _actionBar;
        private System.Windows.Forms.Button _btnConnect;
        private System.Windows.Forms.Button _btnDisconnect;
        private System.Windows.Forms.Label _lblSlotStatus;
        private System.Windows.Forms.RichTextBox _logBox;

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
            this._tabControl = new System.Windows.Forms.TabControl();
            this._tabPortList = new System.Windows.Forms.TabPage();
            this._portListView = new System.Windows.Forms.ListView();
            this._btnRefreshPorts = new System.Windows.Forms.Button();
            this._tabSlotManager = new System.Windows.Forms.TabPage();
            this._slotSplit = new System.Windows.Forms.SplitContainer();
            this._slotPanel = new System.Windows.Forms.Panel();
            this._slotListBox = new System.Windows.Forms.ListBox();
            this._slotBtnPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnRemoveSlot = new System.Windows.Forms.Button();
            this._btnAddSlot = new System.Windows.Forms.Button();
            this._lblSlotHeader = new System.Windows.Forms.Label();
            this._settingsPanel = new System.Windows.Forms.Panel();
            this._lblSettingsHeader = new System.Windows.Forms.Label();
            this._configPanel = new System.Windows.Forms.Panel();
            this._lblPortName = new System.Windows.Forms.Label();
            this._cmbPortName = new System.Windows.Forms.ComboBox();
            this._lblBaudRate = new System.Windows.Forms.Label();
            this._numBaudRate = new System.Windows.Forms.NumericUpDown();
            this._lblParity = new System.Windows.Forms.Label();
            this._cmbParity = new System.Windows.Forms.ComboBox();
            this._lblStopBits = new System.Windows.Forms.Label();
            this._cmbStopBits = new System.Windows.Forms.ComboBox();
            this._lblDataBits = new System.Windows.Forms.Label();
            this._numDataBits = new System.Windows.Forms.NumericUpDown();
            this._actionBar = new System.Windows.Forms.FlowLayoutPanel();
            this._btnConnect = new System.Windows.Forms.Button();
            this._btnDisconnect = new System.Windows.Forms.Button();
            this._lblSlotStatus = new System.Windows.Forms.Label();
            this._logBox = new System.Windows.Forms.RichTextBox();
            this._tabControl.SuspendLayout();
            this._tabPortList.SuspendLayout();
            this._tabSlotManager.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._slotSplit)).BeginInit();
            this._slotSplit.Panel1.SuspendLayout();
            this._slotSplit.Panel2.SuspendLayout();
            this._slotSplit.SuspendLayout();
            this._slotPanel.SuspendLayout();
            this._slotBtnPanel.SuspendLayout();
            this._settingsPanel.SuspendLayout();
            this._configPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numBaudRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numDataBits)).BeginInit();
            this._actionBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // _tabControl
            // 
            this._tabControl.Controls.Add(this._tabPortList);
            this._tabControl.Controls.Add(this._tabSlotManager);
            this._tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabControl.Location = new System.Drawing.Point(0, 0);
            this._tabControl.Name = "_tabControl";
            this._tabControl.SelectedIndex = 0;
            this._tabControl.Size = new System.Drawing.Size(834, 492);
            this._tabControl.TabIndex = 0;
            // 
            // _tabPortList
            // 
            this._tabPortList.Controls.Add(this._portListView);
            this._tabPortList.Controls.Add(this._btnRefreshPorts);
            this._tabPortList.Location = new System.Drawing.Point(4, 28);
            this._tabPortList.Name = "_tabPortList";
            this._tabPortList.Size = new System.Drawing.Size(826, 460);
            this._tabPortList.TabIndex = 0;
            this._tabPortList.Text = "串口列表";
            // 
            // _portListView
            // 
            this._portListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._portListView.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._portListView.FullRowSelect = true;
            this._portListView.GridLines = true;
            this._portListView.MultiSelect = false;
            this._portListView.Name = "_portListView";
            this._portListView.View = System.Windows.Forms.View.Details;
            this._portListView.Columns.Add("端口", 120);
            this._portListView.Columns.Add("状态", 200);
            // 
            // _btnRefreshPorts
            // 
            this._btnRefreshPorts.Dock = System.Windows.Forms.DockStyle.Top;
            this._btnRefreshPorts.Height = 30;
            this._btnRefreshPorts.Name = "_btnRefreshPorts";
            this._btnRefreshPorts.Text = "刷新";
            // 
            // _tabSlotManager
            // 
            this._tabSlotManager.Controls.Add(this._slotSplit);
            this._tabSlotManager.Location = new System.Drawing.Point(4, 28);
            this._tabSlotManager.Name = "_tabSlotManager";
            this._tabSlotManager.Size = new System.Drawing.Size(826, 460);
            this._tabSlotManager.TabIndex = 1;
            this._tabSlotManager.Text = "串口槽位";
            // 
            // _slotSplit
            // 
            this._slotSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this._slotSplit.Location = new System.Drawing.Point(0, 0);
            this._slotSplit.Name = "_slotSplit";
            this._slotSplit.Orientation = System.Windows.Forms.Orientation.Vertical;
            this._slotSplit.Panel1MinSize = 100;
            this._slotSplit.Panel1.Controls.Add(this._slotPanel);
            this._slotSplit.Panel2.Controls.Add(this._settingsPanel);
            this._slotSplit.Size = new System.Drawing.Size(826, 460);
            this._slotSplit.SplitterDistance = 160;
            this._slotSplit.TabIndex = 0;
            // 
            // _slotPanel
            // 
            this._slotPanel.Controls.Add(this._slotListBox);
            this._slotPanel.Controls.Add(this._slotBtnPanel);
            this._slotPanel.Controls.Add(this._lblSlotHeader);
            this._slotPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._slotPanel.Name = "_slotPanel";
            this._slotPanel.Padding = new System.Windows.Forms.Padding(4);
            // 
            // _slotListBox
            // 
            this._slotListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._slotListBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._slotListBox.IntegralHeight = false;
            this._slotListBox.Name = "_slotListBox";
            // 
            // _slotBtnPanel
            // 
            this._slotBtnPanel.Controls.Add(this._btnRemoveSlot);
            this._slotBtnPanel.Controls.Add(this._btnAddSlot);
            this._slotBtnPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._slotBtnPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this._slotBtnPanel.Height = 30;
            this._slotBtnPanel.Name = "_slotBtnPanel";
            this._slotBtnPanel.Padding = new System.Windows.Forms.Padding(4, 3, 4, 0);
            // 
            // _btnRemoveSlot
            // 
            this._btnRemoveSlot.Enabled = false;
            this._btnRemoveSlot.Name = "_btnRemoveSlot";
            this._btnRemoveSlot.Size = new System.Drawing.Size(50, 24);
            this._btnRemoveSlot.Text = "删除";
            // 
            // _btnAddSlot
            // 
            this._btnAddSlot.Name = "_btnAddSlot";
            this._btnAddSlot.Size = new System.Drawing.Size(50, 24);
            this._btnAddSlot.Text = "添加";
            // 
            // _lblSlotHeader
            // 
            this._lblSlotHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblSlotHeader.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
            this._lblSlotHeader.Height = 24;
            this._lblSlotHeader.Name = "_lblSlotHeader";
            this._lblSlotHeader.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this._lblSlotHeader.Text = "槽位";
            this._lblSlotHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _settingsPanel
            // 
            this._settingsPanel.AutoScroll = true;
            this._settingsPanel.Controls.Add(this._lblSettingsHeader);
            this._settingsPanel.Controls.Add(this._configPanel);
            this._settingsPanel.Controls.Add(this._actionBar);
            this._settingsPanel.Controls.Add(this._logBox);
            this._settingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._settingsPanel.Name = "_settingsPanel";
            this._settingsPanel.Padding = new System.Windows.Forms.Padding(8);
            // 
            // _configPanel
            // 
            this._configPanel.Controls.Add(this._lblPortName);
            this._configPanel.Controls.Add(this._cmbPortName);
            this._configPanel.Controls.Add(this._lblBaudRate);
            this._configPanel.Controls.Add(this._numBaudRate);
            this._configPanel.Controls.Add(this._lblParity);
            this._configPanel.Controls.Add(this._cmbParity);
            this._configPanel.Controls.Add(this._lblStopBits);
            this._configPanel.Controls.Add(this._cmbStopBits);
            this._configPanel.Controls.Add(this._lblDataBits);
            this._configPanel.Controls.Add(this._numDataBits);
            this._configPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._configPanel.Height = 210;
            this._configPanel.Name = "_configPanel";
            // 
            // _lblSettingsHeader
            // 
            this._lblSettingsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblSettingsHeader.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
            this._lblSettingsHeader.Height = 24;
            this._lblSettingsHeader.Name = "_lblSettingsHeader";
            this._lblSettingsHeader.Text = "配置";
            this._lblSettingsHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblPortName
            // 
            this._lblPortName.AutoSize = true;
            this._lblPortName.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblPortName.Location = new System.Drawing.Point(12, 32);
            this._lblPortName.Name = "_lblPortName";
            this._lblPortName.Size = new System.Drawing.Size(56, 24);
            this._lblPortName.Text = "端口:";
            this._lblPortName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _cmbPortName
            // 
            this._cmbPortName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbPortName.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._cmbPortName.Location = new System.Drawing.Point(80, 30);
            this._cmbPortName.Name = "_cmbPortName";
            this._cmbPortName.Size = new System.Drawing.Size(120, 30);
            // 
            // _lblBaudRate
            // 
            this._lblBaudRate.AutoSize = true;
            this._lblBaudRate.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblBaudRate.Location = new System.Drawing.Point(12, 68);
            this._lblBaudRate.Name = "_lblBaudRate";
            this._lblBaudRate.Size = new System.Drawing.Size(68, 24);
            this._lblBaudRate.Text = "波特率:";
            this._lblBaudRate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _numBaudRate
            // 
            this._numBaudRate.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._numBaudRate.Increment = new decimal(new int[] { 1200, 0, 0, 0 });
            this._numBaudRate.Location = new System.Drawing.Point(80, 66);
            this._numBaudRate.Maximum = new decimal(new int[] { 256000, 0, 0, 0 });
            this._numBaudRate.Minimum = new decimal(new int[] { 1200, 0, 0, 0 });
            this._numBaudRate.Name = "_numBaudRate";
            this._numBaudRate.Size = new System.Drawing.Size(100, 27);
            this._numBaudRate.Value = new decimal(new int[] { 9600, 0, 0, 0 });
            // 
            // _lblParity
            // 
            this._lblParity.AutoSize = true;
            this._lblParity.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblParity.Location = new System.Drawing.Point(12, 104);
            this._lblParity.Name = "_lblParity";
            this._lblParity.Size = new System.Drawing.Size(50, 24);
            this._lblParity.Text = "校验:";
            this._lblParity.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _cmbParity
            // 
            this._cmbParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbParity.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._cmbParity.Items.AddRange(new object[] { "None", "Odd", "Even", "Mark", "Space" });
            this._cmbParity.Location = new System.Drawing.Point(80, 102);
            this._cmbParity.Name = "_cmbParity";
            this._cmbParity.SelectedIndex = 0;
            this._cmbParity.Size = new System.Drawing.Size(100, 30);
            // 
            // _lblStopBits
            // 
            this._lblStopBits.AutoSize = true;
            this._lblStopBits.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblStopBits.Location = new System.Drawing.Point(12, 140);
            this._lblStopBits.Name = "_lblStopBits";
            this._lblStopBits.Size = new System.Drawing.Size(68, 24);
            this._lblStopBits.Text = "停止位:";
            this._lblStopBits.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _cmbStopBits
            // 
            this._cmbStopBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbStopBits.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._cmbStopBits.Items.AddRange(new object[] { "One", "Two", "OnePointFive" });
            this._cmbStopBits.Location = new System.Drawing.Point(80, 138);
            this._cmbStopBits.Name = "_cmbStopBits";
            this._cmbStopBits.SelectedIndex = 0;
            this._cmbStopBits.Size = new System.Drawing.Size(100, 30);
            // 
            // _lblDataBits
            // 
            this._lblDataBits.AutoSize = true;
            this._lblDataBits.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblDataBits.Location = new System.Drawing.Point(12, 176);
            this._lblDataBits.Name = "_lblDataBits";
            this._lblDataBits.Size = new System.Drawing.Size(68, 24);
            this._lblDataBits.Text = "数据位:";
            this._lblDataBits.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _numDataBits
            // 
            this._numDataBits.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._numDataBits.Location = new System.Drawing.Point(80, 174);
            this._numDataBits.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            this._numDataBits.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            this._numDataBits.Name = "_numDataBits";
            this._numDataBits.Size = new System.Drawing.Size(80, 27);
            this._numDataBits.Value = new decimal(new int[] { 8, 0, 0, 0 });
            // 
            // _actionBar
            // 
            this._actionBar.Controls.Add(this._btnConnect);
            this._actionBar.Controls.Add(this._btnDisconnect);
            this._actionBar.Controls.Add(this._lblSlotStatus);
            this._actionBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._actionBar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this._actionBar.Height = 36;
            this._actionBar.Name = "_actionBar";
            this._actionBar.Padding = new System.Windows.Forms.Padding(4, 3, 4, 0);
            // 
            // _btnConnect
            // 
            this._btnConnect.Enabled = false;
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Size = new System.Drawing.Size(60, 26);
            this._btnConnect.Text = "连接";
            // 
            // _btnDisconnect
            // 
            this._btnDisconnect.Enabled = false;
            this._btnDisconnect.Name = "_btnDisconnect";
            this._btnDisconnect.Size = new System.Drawing.Size(60, 26);
            this._btnDisconnect.Text = "断开";
            // 
            // _lblSlotStatus
            // 
            this._lblSlotStatus.AutoSize = true;
            this._lblSlotStatus.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lblSlotStatus.Name = "_lblSlotStatus";
            this._lblSlotStatus.Padding = new System.Windows.Forms.Padding(8, 4, 0, 0);
            // 
            // _logBox
            // 
            this._logBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this._logBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._logBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._logBox.Font = new System.Drawing.Font("Consolas", 9F);
            this._logBox.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this._logBox.Name = "_logBox";
            this._logBox.ReadOnly = true;
            this._logBox.WordWrap = false;
            // 
            // SerialManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._tabControl);
            this.Name = "SerialManagerForm";
            this.Size = new System.Drawing.Size(850, 520);
            this._tabControl.ResumeLayout(false);
            this._tabPortList.ResumeLayout(false);
            this._tabSlotManager.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._slotSplit)).EndInit();
            this._slotSplit.Panel1.ResumeLayout(false);
            this._slotSplit.Panel2.ResumeLayout(false);
            this._slotSplit.ResumeLayout(false);
            this._slotPanel.ResumeLayout(false);
            this._slotBtnPanel.ResumeLayout(false);
            this._settingsPanel.ResumeLayout(false);
            this._settingsPanel.PerformLayout();
            this._configPanel.ResumeLayout(false);
            this._configPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numBaudRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numDataBits)).EndInit();
            this._actionBar.ResumeLayout(false);
            this._actionBar.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
