using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.Forms.Camera
{
    partial class CameraManagerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabDeviceList;
        private System.Windows.Forms.TabPage _tabSlotManager;
        private CameraDiscoveryControl _discoveryControl;
        private System.Windows.Forms.SplitContainer _tab2Split;
        private System.Windows.Forms.ListBox _slotListBox;
        private System.Windows.Forms.Button _btnAddSlot;
        private System.Windows.Forms.Button _btnRemoveSlot;
        private CameraSettingsControl _settingsControl;
        private System.Windows.Forms.ComboBox _cmbBindCamera;
        private System.Windows.Forms.Button _btnRefreshCameras;
        private System.Windows.Forms.Button _btnStartGrab;
        private System.Windows.Forms.Button _btnStopGrab;
        private System.Windows.Forms.Button _btnPreview;
        private System.Windows.Forms.Button _btnEditSettings;
        private System.Windows.Forms.Label _lblSlotStatus;
        private System.Windows.Forms.Panel _slotPanel;
        private System.Windows.Forms.FlowLayoutPanel _slotBtnPanel;
        private System.Windows.Forms.Label _lblSlotHeader;
        private System.Windows.Forms.Panel _settingsPanel;
        private System.Windows.Forms.Label _lblSettingsHeader;
        private System.Windows.Forms.Panel _bindPanel;
        private System.Windows.Forms.Label _lblBind;
        private System.Windows.Forms.Label _lblSlotName;
        private System.Windows.Forms.TextBox _txtSlotName;
        private System.Windows.Forms.FlowLayoutPanel _actionBar;
        private System.Windows.Forms.Panel _namePanel;

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
            VisualMaster.Api.CameraSettings cameraSettings1 = new VisualMaster.Api.CameraSettings();
            this._tabControl = new System.Windows.Forms.TabControl();
            this._tabDeviceList = new System.Windows.Forms.TabPage();
            this._discoveryControl = new CameraDiscoveryControl();
            this._tabSlotManager = new System.Windows.Forms.TabPage();
            this._tab2Split = new System.Windows.Forms.SplitContainer();
            this._slotPanel = new System.Windows.Forms.Panel();
            this._slotListBox = new System.Windows.Forms.ListBox();
            this._slotBtnPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnRemoveSlot = new System.Windows.Forms.Button();
            this._btnAddSlot = new System.Windows.Forms.Button();
            this._lblSlotHeader = new System.Windows.Forms.Label();
            this._settingsPanel = new System.Windows.Forms.Panel();
            this._settingsControl = new CameraSettingsControl();
            this._namePanel = new System.Windows.Forms.Panel();
            this._lblSlotName = new System.Windows.Forms.Label();
            this._txtSlotName = new System.Windows.Forms.TextBox();
            this._actionBar = new System.Windows.Forms.FlowLayoutPanel();
            this._btnEditSettings = new System.Windows.Forms.Button();
            this._btnStartGrab = new System.Windows.Forms.Button();
            this._btnStopGrab = new System.Windows.Forms.Button();
            this._btnPreview = new System.Windows.Forms.Button();
            this._lblSlotStatus = new System.Windows.Forms.Label();
            this._bindPanel = new System.Windows.Forms.Panel();
            this._lblBind = new System.Windows.Forms.Label();
            this._cmbBindCamera = new System.Windows.Forms.ComboBox();
            this._btnRefreshCameras = new System.Windows.Forms.Button();
            this._lblSettingsHeader = new System.Windows.Forms.Label();
            this._tabControl.SuspendLayout();
            this._tabDeviceList.SuspendLayout();
            this._tabSlotManager.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._tab2Split)).BeginInit();
            this._tab2Split.Panel1.SuspendLayout();
            this._tab2Split.Panel2.SuspendLayout();
            this._tab2Split.SuspendLayout();
            this._slotPanel.SuspendLayout();
            this._slotBtnPanel.SuspendLayout();
            this._settingsPanel.SuspendLayout();
            this._actionBar.SuspendLayout();
            this._bindPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _tabControl
            // 
            this._tabControl.Controls.Add(this._tabDeviceList);
            this._tabControl.Controls.Add(this._tabSlotManager);
            this._tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabControl.Location = new System.Drawing.Point(0, 0);
            this._tabControl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._tabControl.Name = "_tabControl";
            this._tabControl.SelectedIndex = 0;
            this._tabControl.Size = new System.Drawing.Size(1317, 786);
            this._tabControl.TabIndex = 0;
            // 
            // _tabDeviceList
            // 
            this._tabDeviceList.Controls.Add(this._discoveryControl);
            this._tabDeviceList.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabDeviceList.Location = new System.Drawing.Point(4, 28);
            this._tabDeviceList.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._tabDeviceList.Name = "_tabDeviceList";
            this._tabDeviceList.Size = new System.Drawing.Size(1309, 754);
            this._tabDeviceList.TabIndex = 0;
            this._tabDeviceList.Text = "相机列表";
            // 
            // _discoveryControl
            // 
            this._discoveryControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._discoveryControl.Location = new System.Drawing.Point(0, 0);
            this._discoveryControl.Margin = new System.Windows.Forms.Padding(4);
            this._discoveryControl.Name = "_discoveryControl";
            this._discoveryControl.Size = new System.Drawing.Size(1309, 754);
            this._discoveryControl.TabIndex = 0;
            // 
            // _tabSlotManager
            // 
            this._tabSlotManager.Controls.Add(this._tab2Split);
            this._tabSlotManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabSlotManager.Location = new System.Drawing.Point(4, 28);
            this._tabSlotManager.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._tabSlotManager.Name = "_tabSlotManager";
            this._tabSlotManager.Size = new System.Drawing.Size(1309, 754);
            this._tabSlotManager.TabIndex = 1;
            this._tabSlotManager.Text = "相机设备";
            // 
            // _tab2Split
            // 
            this._tab2Split.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tab2Split.Location = new System.Drawing.Point(0, 0);
            this._tab2Split.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._tab2Split.Name = "_tab2Split";
            // 
            // _tab2Split.Panel1
            // 
            this._tab2Split.Panel1.Controls.Add(this._slotPanel);
            this._tab2Split.Panel1MinSize = 80;
            // 
            // _tab2Split.Panel2
            // 
            this._tab2Split.Panel2.Controls.Add(this._settingsPanel);
            this._tab2Split.Size = new System.Drawing.Size(1309, 754);
            this._tab2Split.SplitterDistance = 293;
            this._tab2Split.SplitterWidth = 6;
            this._tab2Split.TabIndex = 0;
            // 
            // _slotPanel
            // 
            this._slotPanel.Controls.Add(this._slotListBox);
            this._slotPanel.Controls.Add(this._slotBtnPanel);
            this._slotPanel.Controls.Add(this._lblSlotHeader);
            this._slotPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._slotPanel.Location = new System.Drawing.Point(0, 0);
            this._slotPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._slotPanel.Name = "_slotPanel";
            this._slotPanel.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._slotPanel.Size = new System.Drawing.Size(293, 754);
            this._slotPanel.TabIndex = 0;
            // 
            // _slotListBox
            // 
            this._slotListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._slotListBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._slotListBox.IntegralHeight = false;
            this._slotListBox.ItemHeight = 24;
            this._slotListBox.Location = new System.Drawing.Point(6, 117);
            this._slotListBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._slotListBox.Name = "_slotListBox";
            this._slotListBox.Size = new System.Drawing.Size(281, 631);
            this._slotListBox.TabIndex = 0;
            // 
            // _slotBtnPanel
            // 
            this._slotBtnPanel.Controls.Add(this._btnAddSlot);
            this._slotBtnPanel.Controls.Add(this._btnRemoveSlot);
            this._slotBtnPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._slotBtnPanel.Location = new System.Drawing.Point(6, 42);
            this._slotBtnPanel.Name = "_slotBtnPanel";
            this._slotBtnPanel.Padding = new System.Windows.Forms.Padding(6, 3, 6, 0);
            this._slotBtnPanel.Size = new System.Drawing.Size(281, 39);
            this._slotBtnPanel.TabIndex = 1;
            this._slotBtnPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            // 
            // _btnRemoveSlot
            // 
            this._btnRemoveSlot.Enabled = false;
            this._btnRemoveSlot.Location = new System.Drawing.Point(6, 3);
            this._btnRemoveSlot.Name = "_btnRemoveSlot";
            this._btnRemoveSlot.Size = new System.Drawing.Size(75, 33);
            this._btnRemoveSlot.TabIndex = 1;
            this._btnRemoveSlot.Text = "删除";
            // 
            // _btnAddSlot
            // 
            this._btnAddSlot.Location = new System.Drawing.Point(87, 3);
            this._btnAddSlot.Name = "_btnAddSlot";
            this._btnAddSlot.Size = new System.Drawing.Size(75, 33);
            this._btnAddSlot.TabIndex = 0;
            this._btnAddSlot.Text = "添加";
            // 
            // _lblSlotHeader
            // 
            this._lblSlotHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblSlotHeader.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this._lblSlotHeader.Location = new System.Drawing.Point(6, 6);
            this._lblSlotHeader.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._lblSlotHeader.Name = "_lblSlotHeader";
            this._lblSlotHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this._lblSlotHeader.Size = new System.Drawing.Size(281, 36);
            this._lblSlotHeader.TabIndex = 2;
            this._lblSlotHeader.Text = "相机资源";
            this._lblSlotHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _settingsPanel
            // 
            this._settingsPanel.Controls.Add(this._settingsControl);
            this._settingsPanel.Controls.Add(this._actionBar);
            this._settingsPanel.Controls.Add(this._bindPanel);
            this._settingsPanel.Controls.Add(this._namePanel);
            this._settingsPanel.Controls.Add(this._lblSettingsHeader);
            this._settingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._settingsPanel.Location = new System.Drawing.Point(0, 0);
            this._settingsPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._settingsPanel.Name = "_settingsPanel";
            this._settingsPanel.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._settingsPanel.Size = new System.Drawing.Size(1010, 754);
            this._settingsPanel.TabIndex = 0;
            // 
            // _settingsControl
            // 
            this._settingsControl.AutoScroll = true;
            this._settingsControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._settingsControl.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._settingsControl.IsReadOnly = true;
            this._settingsControl.Location = new System.Drawing.Point(6, 90);
            this._settingsControl.Name = "_settingsControl";
            cameraSettings1.ExposureTimeUs = 5000D;
            cameraSettings1.FifoCapacity = 10;
            cameraSettings1.GainRaw = 0D;
            cameraSettings1.Height = 64;
            cameraSettings1.OffsetX = 0;
            cameraSettings1.OffsetY = 0;
            cameraSettings1.PixelFormat = "";
            cameraSettings1.TriggerActivation = "";
            cameraSettings1.TriggerSource = "";
            cameraSettings1.Width = 64;
            this._settingsControl.Settings = cameraSettings1;
            this._settingsControl.Size = new System.Drawing.Size(998, 604);
            this._settingsControl.TabIndex = 0;
            // 
            // _actionBar
            // 
            this._actionBar.Controls.Add(this._btnEditSettings);
            this._actionBar.Controls.Add(this._btnStartGrab);
            this._actionBar.Controls.Add(this._btnStopGrab);
            this._actionBar.Controls.Add(this._btnPreview);
            this._actionBar.Controls.Add(this._lblSlotStatus);
            this._actionBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._actionBar.Location = new System.Drawing.Point(6, 694);
            this._actionBar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._actionBar.Name = "_actionBar";
            this._actionBar.Padding = new System.Windows.Forms.Padding(6, 4, 6, 0);
            this._actionBar.Size = new System.Drawing.Size(998, 54);
            this._actionBar.TabIndex = 1;
            // 
            // _btnEditSettings
            // 
            this._btnEditSettings.Enabled = false;
            this._btnEditSettings.Enabled = false;
            this._btnEditSettings.Location = new System.Drawing.Point(10, 8);
            this._btnEditSettings.Name = "_btnEditSettings";
            this._btnEditSettings.Size = new System.Drawing.Size(112, 39);
            this._btnEditSettings.TabIndex = 0;
            this._btnEditSettings.Text = "编辑设置";
            // 
            // _btnStartGrab
            // 
            this._btnStartGrab.Enabled = false;
            this._btnStartGrab.Location = new System.Drawing.Point(130, 8);
            this._btnStartGrab.Name = "_btnStartGrab";
            this._btnStartGrab.Size = new System.Drawing.Size(112, 39);
            this._btnStartGrab.TabIndex = 1;
            this._btnStartGrab.Text = "开始采集";
            // 
            // _btnStopGrab
            // 
            this._btnStopGrab.Enabled = false;
            this._btnStopGrab.Location = new System.Drawing.Point(250, 8);
            this._btnStopGrab.Name = "_btnStopGrab";
            this._btnStopGrab.Size = new System.Drawing.Size(112, 39);
            this._btnStopGrab.TabIndex = 2;
            this._btnStopGrab.Text = "停止采集";
            // 
            // _btnPreview
            // 
            this._btnPreview.Enabled = false;
            this._btnPreview.Location = new System.Drawing.Point(370, 8);
            this._btnPreview.Name = "_btnPreview";
            this._btnPreview.Size = new System.Drawing.Size(75, 39);
            this._btnPreview.TabIndex = 3;
            this._btnPreview.Text = "预览";
            // 
            // _lblSlotStatus
            // 
            this._lblSlotStatus.AutoSize = true;
            this._lblSlotStatus.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._lblSlotStatus.Location = new System.Drawing.Point(453, 4);
            this._lblSlotStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._lblSlotStatus.Name = "_lblSlotStatus";
            this._lblSlotStatus.Padding = new System.Windows.Forms.Padding(12, 8, 0, 0);
            this._lblSlotStatus.Size = new System.Drawing.Size(12, 32);
            this._lblSlotStatus.TabIndex = 4;
            // 
            // _bindPanel
            // 
            this._bindPanel.Controls.Add(this._lblBind);
            this._bindPanel.Controls.Add(this._cmbBindCamera);
            this._bindPanel.Controls.Add(this._btnRefreshCameras);
            this._bindPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._bindPanel.Location = new System.Drawing.Point(6, 42);
            this._bindPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._bindPanel.Name = "_bindPanel";
            this._bindPanel.Padding = new System.Windows.Forms.Padding(6, 6, 6, 0);
            this._bindPanel.Size = new System.Drawing.Size(998, 48);
            this._bindPanel.TabIndex = 2;
            // 
            // _namePanel
            // 
            this._namePanel.Controls.Add(this._lblSlotName);
            this._namePanel.Controls.Add(this._txtSlotName);
            this._namePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._namePanel.Location = new System.Drawing.Point(6, 78);
            this._namePanel.Name = "_namePanel";
            this._namePanel.Size = new System.Drawing.Size(998, 36);
            this._namePanel.TabIndex = 4;
            // 
            // _lblSlotName
            // 
            this._lblSlotName.AutoSize = true;
            this._lblSlotName.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._lblSlotName.Location = new System.Drawing.Point(6, 6);
            this._lblSlotName.Name = "_lblSlotName";
            this._lblSlotName.Size = new System.Drawing.Size(50, 24);
            this._lblSlotName.TabIndex = 0;
            this._lblSlotName.Text = "名称:";
            // 
            // _txtSlotName
            // 
            this._txtSlotName.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._txtSlotName.Location = new System.Drawing.Point(120, 4);
            this._txtSlotName.Name = "_txtSlotName";
            this._txtSlotName.Size = new System.Drawing.Size(328, 31);
            this._txtSlotName.TabIndex = 1;
            // 
            // _lblBind
            // 
            this._lblBind.AutoSize = true;
            this._lblBind.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._lblBind.Location = new System.Drawing.Point(6, 9);
            this._lblBind.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._lblBind.Name = "_lblBind";
            this._lblBind.Size = new System.Drawing.Size(86, 24);
            this._lblBind.TabIndex = 0;
            this._lblBind.Text = "绑定相机:";
            // 
            // _cmbBindCamera
            // 
            this._cmbBindCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbBindCamera.Enabled = false;
            this._cmbBindCamera.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._cmbBindCamera.Location = new System.Drawing.Point(120, 6);
            this._cmbBindCamera.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._cmbBindCamera.Name = "_cmbBindCamera";
            this._cmbBindCamera.Size = new System.Drawing.Size(328, 32);
            this._cmbBindCamera.TabIndex = 1;
            // 
            // _btnRefreshCameras
            // 
            this._btnRefreshCameras.Location = new System.Drawing.Point(459, 4);
            this._btnRefreshCameras.Name = "_btnRefreshCameras";
            this._btnRefreshCameras.Size = new System.Drawing.Size(75, 39);
            this._btnRefreshCameras.TabIndex = 2;
            this._btnRefreshCameras.Text = "刷新";
            // 
            // _lblSettingsHeader
            // 
            this._lblSettingsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblSettingsHeader.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this._lblSettingsHeader.Location = new System.Drawing.Point(6, 6);
            this._lblSettingsHeader.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._lblSettingsHeader.Name = "_lblSettingsHeader";
            this._lblSettingsHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this._lblSettingsHeader.Size = new System.Drawing.Size(998, 36);
            this._lblSettingsHeader.TabIndex = 3;
            this._lblSettingsHeader.Text = "配置";
            this._lblSettingsHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CameraManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1317, 786);
            this.Controls.Add(this._tabControl);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "CameraManagerForm";
            this._tabControl.ResumeLayout(false);
            this._tabDeviceList.ResumeLayout(false);
            this._tabSlotManager.ResumeLayout(false);
            this._tab2Split.Panel1.ResumeLayout(false);
            this._tab2Split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._tab2Split)).EndInit();
            this._tab2Split.ResumeLayout(false);
            this._slotPanel.ResumeLayout(false);
            this._slotBtnPanel.ResumeLayout(false);
            this._settingsPanel.ResumeLayout(false);
            this._actionBar.ResumeLayout(false);
            this._actionBar.PerformLayout();
            this._bindPanel.ResumeLayout(false);
            this._bindPanel.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
