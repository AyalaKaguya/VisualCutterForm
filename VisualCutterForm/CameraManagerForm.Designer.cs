using System;
using System.Drawing;
using System.Windows.Forms;
using VisualMaster.CameraLink;

namespace VisualCutterForm
{
    partial class CameraManagerForm
    {
        private System.ComponentModel.IContainer components = null;
        private TabControl _tabControl;
        private TabPage _tabDeviceList;
        private TabPage _tabSlotManager;
        private CameraDiscoveryControl _discoveryControl;
        private SplitContainer _tab2Split;
        private ListBox _slotListBox;
        private Button _btnAddSlot;
        private Button _btnRemoveSlot;
        private CameraSettingsControl _settingsControl;
        private ComboBox _cmbBindCamera;
        private Button _btnRefreshCameras;
        private Button _btnStartGrab;
        private Button _btnStopGrab;
        private Button _btnPreview;
        private Button _btnEditSettings;
        private Label _lblSlotStatus;

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
            this.Name = "CameraManagerForm";
            _tabControl = new TabControl { Dock = DockStyle.Fill };

            // ── Tab 1: 相机列表 ──
            _tabDeviceList = new TabPage("相机列表");
            _discoveryControl = new CameraDiscoveryControl { Dock = DockStyle.Fill };
            _tabDeviceList.Controls.Add(_discoveryControl);

            // ── Tab 2: 相机槽位 ──
            _tabSlotManager = new TabPage("相机槽位");

            // left side
            _slotListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 9F),
                IntegralHeight = false,
            };

            var slotBtnPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(4, 2, 4, 0),
            };

            _btnAddSlot = new Button { Text = "添加", Size = new Size(80, 22), FlatStyle = FlatStyle.Flat };
            _btnAddSlot.FlatAppearance.BorderSize = 1;

            _btnRemoveSlot = new Button { Text = "删除", Size = new Size(80, 22), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnRemoveSlot.FlatAppearance.BorderSize = 1;

            slotBtnPanel.Controls.Add(_btnRemoveSlot);
            slotBtnPanel.Controls.Add(_btnAddSlot);

            var slotPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            var lblSlot = new Label
            {
                Text = "槽位",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
            };
            slotPanel.Controls.Add(_slotListBox);
            slotPanel.Controls.Add(slotBtnPanel);
            slotPanel.Controls.Add(lblSlot);

            // right side
            _settingsControl = new CameraSettingsControl { Dock = DockStyle.Fill, IsReadOnly = true };
            var settingsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            var lblSettings = new Label
            {
                Text = "配置",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
            };

            // bind camera row
            var bindPanel = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(4, 4, 4, 0) };
            var lblBind = new Label
            {
                Text = "绑定相机:",
                Location = new Point(4, 6),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
            };
            _cmbBindCamera = new ComboBox
            {
                Location = new Point(80, 4),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9F),
                Enabled = false,
            };

            _btnRefreshCameras = new Button
            {
                Text = "刷新",
                Location = new Point(306, 3),
                Size = new Size(50, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnRefreshCameras.FlatAppearance.BorderSize = 1;

            bindPanel.Controls.Add(lblBind);
            bindPanel.Controls.Add(_cmbBindCamera);
            bindPanel.Controls.Add(_btnRefreshCameras);

            // action bar
            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 36,
                Padding = new Padding(4, 3, 4, 0),
            };

            _btnEditSettings = new Button { Text = "编辑设置", Size = new Size(75, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnEditSettings.FlatAppearance.BorderSize = 1;

            _btnStartGrab = new Button { Text = "开始采集", Size = new Size(75, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnStartGrab.FlatAppearance.BorderSize = 1;

            _btnStopGrab = new Button { Text = "停止采集", Size = new Size(75, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnStopGrab.FlatAppearance.BorderSize = 1;

            _btnPreview = new Button { Text = "预览", Size = new Size(50, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnPreview.FlatAppearance.BorderSize = 1;

            _lblSlotStatus = new Label
            {
                Text = "",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
                Padding = new Padding(8, 5, 0, 0),
            };

            actionBar.Controls.Add(_btnEditSettings);
            actionBar.Controls.Add(_btnStartGrab);
            actionBar.Controls.Add(_btnStopGrab);
            actionBar.Controls.Add(_btnPreview);
            actionBar.Controls.Add(_lblSlotStatus);

            settingsPanel.Controls.Add(_settingsControl);
            settingsPanel.Controls.Add(actionBar);
            settingsPanel.Controls.Add(bindPanel);
            settingsPanel.Controls.Add(lblSettings);

            _tab2Split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 100,
                Panel1MinSize = 80,
            };
            _tab2Split.Panel1.Controls.Add(slotPanel);
            _tab2Split.Panel2.Controls.Add(settingsPanel);

            _tabSlotManager.Controls.Add(_tab2Split);

            _tabControl.TabPages.Add(_tabDeviceList);
            _tabControl.TabPages.Add(_tabSlotManager);

            Controls.Add(_tabControl);
            this.ResumeLayout(false);
        }
    }
}
