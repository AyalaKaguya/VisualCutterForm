using VisualMaster.Api;
using VisualMaster.CameraLink;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualCutterForm.Lib;

namespace VisualCutterForm
{
    public class CameraManagerForm : Form
    {
        private VisionController _vision;
        private CameraDiscoveryControl _discoveryControl;
        private CameraSettingsControl _settingsControl;
        private ListBox _slotListBox;
        private Button _btnAssign;
        private Button _btnRemoveSlot;
        private Button _btnAddSlot;
        private Button _btnOpenSettings;
        private Button _btnStartGrab;
        private Button _btnStopGrab;
        private Label _lblSlotStatus;
        private SplitContainer _mainSplit;
        private SplitContainer _rightSplit;
        private CameraSlot _selectedSlot;

        public CameraManagerForm(VisionController vision)
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "相机管理器";
            Size = new Size(900, 550);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            BuildUI();
            _discoveryControl.SetCameraManager(_vision.CameraManager);
            RefreshCameraList();
            RefreshSlotList();

            Closing += (s, e) =>
            {
                foreach (var slot in _vision.CameraManager.Slots)
                    _vision.StopAcquisition(slot.SlotId);
            };
        }

        private void BuildUI()
        {
            // Left: camera discovery
            _discoveryControl = new CameraDiscoveryControl { Dock = DockStyle.Fill };
            var discoveryPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4),
            };
            var lblDiscovery = new Label
            {
                Text = "已发现相机",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
            };
            discoveryPanel.Controls.Add(_discoveryControl);
            discoveryPanel.Controls.Add(lblDiscovery);

            // Right top: slot list
            _slotListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 9F),
                IntegralHeight = false,
            };
            _slotListBox.SelectedIndexChanged += OnSlotSelected;

            var slotBtnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 30,
                Padding = new Padding(4, 3, 4, 0),
            };
            _btnAddSlot = new Button { Text = "添加槽位", Size = new Size(75, 24), FlatStyle = FlatStyle.Flat };
            _btnAddSlot.FlatAppearance.BorderSize = 1;
            _btnAddSlot.Click += (s, e) =>
            {
                _vision.AddSlot($"相机{_vision.CameraManager.Slots.Count + 1}");
                RefreshSlotList();
            };

            _btnRemoveSlot = new Button { Text = "删除槽位", Size = new Size(75, 24), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnRemoveSlot.FlatAppearance.BorderSize = 1;
            _btnRemoveSlot.Click += (s, e) =>
            {
                if (_selectedSlot != null)
                {
                    _vision.RemoveSlot(_selectedSlot.SlotId);
                    _selectedSlot = null;
                    RefreshSlotList();
                }
            };

            _btnAssign = new Button { Text = "分配相机", Size = new Size(75, 24), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnAssign.FlatAppearance.BorderSize = 1;
            _btnAssign.Click += (s, e) =>
            {
                var cam = _discoveryControl.SelectedCamera;
                if (cam == null)
                {
                    _vision.EnumerateCameras();
                    var cameras = _vision.CameraManager.Cameras;
                    if (cameras.Count == 0)
                    {
                        MessageBox.Show("未发现相机设备。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    cam = cameras[0];
                }
                try
                {
                    _vision.OpenSlot(_selectedSlot.SlotId, cam);
                    RefreshSlotList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开相机失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            slotBtnPanel.Controls.Add(_btnAddSlot);
            slotBtnPanel.Controls.Add(_btnRemoveSlot);
            slotBtnPanel.Controls.Add(_btnAssign);

            var slotPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            var lblSlot = new Label
            {
                Text = "相机槽位",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
            };
            slotPanel.Controls.Add(_slotListBox);
            slotPanel.Controls.Add(slotBtnPanel);
            slotPanel.Controls.Add(lblSlot);

            // Right bottom: settings
            _settingsControl = new CameraSettingsControl { Dock = DockStyle.Fill, IsReadOnly = true };
            var settingsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            var lblSettings = new Label
            {
                Text = "设置",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
            };
            var settingsBtnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 36,
                Padding = new Padding(4, 3, 4, 0),
            };
            _btnOpenSettings = new Button { Text = "编辑设置", Size = new Size(75, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnOpenSettings.FlatAppearance.BorderSize = 1;
            _btnOpenSettings.Click += (s, e) =>
            {
                if (_selectedSlot == null) return;
                using (var dlg = new CameraSettingsDialog(_selectedSlot.Settings, _selectedSlot.AssignedCamera, vision: _vision, cameraSerial: _selectedSlot.SlotId))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _vision.UpdateSlotSettings(_selectedSlot.SlotId, dlg.Settings);
                        _settingsControl.Settings = _selectedSlot.Settings;
                    }
                }
            };

            _btnStartGrab = new Button { Text = "开始采集", Size = new Size(75, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnStartGrab.FlatAppearance.BorderSize = 1;
            _btnStartGrab.Click += (s, e) =>
            {
                if (_selectedSlot?.IsConnected == true)
                {
                    _vision.StartAcquisition(_selectedSlot.SlotId);
                    RefreshSlotStatus();
                }
            };

            _btnStopGrab = new Button { Text = "停止采集", Size = new Size(75, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnStopGrab.FlatAppearance.BorderSize = 1;
            _btnStopGrab.Click += (s, e) =>
            {
                if (_selectedSlot?.IsConnected == true)
                {
                    _vision.StopAcquisition(_selectedSlot.SlotId);
                    RefreshSlotStatus();
                }
            };

            _lblSlotStatus = new Label
            {
                Text = "",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
                Padding = new Padding(4, 6, 0, 0),
            };

            settingsBtnPanel.Controls.Add(_btnOpenSettings);
            settingsBtnPanel.Controls.Add(_btnStartGrab);
            settingsBtnPanel.Controls.Add(_btnStopGrab);
            settingsBtnPanel.Controls.Add(_lblSlotStatus);

            settingsPanel.Controls.Add(_settingsControl);
            settingsPanel.Controls.Add(settingsBtnPanel);
            settingsPanel.Controls.Add(lblSettings);

            _rightSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200,
            };
            _rightSplit.Panel1.Controls.Add(slotPanel);
            _rightSplit.Panel2.Controls.Add(settingsPanel);

            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 360,
            };
            _mainSplit.Panel1.Controls.Add(discoveryPanel);
            _mainSplit.Panel2.Controls.Add(_rightSplit);

            Controls.Add(_mainSplit);
        }

        private void RefreshCameraList()
        {
            try
            {
                _vision.EnumerateCameras();
                _discoveryControl.RefreshList();
            }
            catch { }
        }

        private void RefreshSlotList()
        {
            _slotListBox.Items.Clear();
            foreach (var slot in _vision.CameraManager.Slots)
            {
                var status = slot.IsConnected
                    ? (slot.IsGrabbing ? " [采集中]" : " [已连接]")
                    : " [未连接]";
                _slotListBox.Items.Add(new SlotItem { Slot = slot, Display = $"{slot.SlotName}{status}" });
            }

            if (_slotListBox.Items.Count > 0 && _selectedSlot == null)
                _slotListBox.SelectedIndex = 0;
        }

        private void RefreshSlotStatus()
        {
            int idx = _slotListBox.SelectedIndex;
            RefreshSlotList();
            if (idx >= 0 && idx < _slotListBox.Items.Count)
                _slotListBox.SelectedIndex = idx;
        }

        private void OnSlotSelected(object sender, EventArgs e)
        {
            var item = _slotListBox.SelectedItem as SlotItem;
            _selectedSlot = item?.Slot;

            bool hasSlot = _selectedSlot != null;
            bool isConnected = _selectedSlot?.IsConnected == true;
            bool isGrabbing = _selectedSlot?.IsGrabbing == true;

            _btnAssign.Enabled = hasSlot;
            _btnRemoveSlot.Enabled = hasSlot;
            _btnOpenSettings.Enabled = hasSlot;
            _btnStartGrab.Enabled = isConnected && !isGrabbing;
            _btnStopGrab.Enabled = isConnected && isGrabbing;

            if (_selectedSlot != null)
            {
                _settingsControl.Settings = _selectedSlot.Settings ?? new CameraSettings();
                _settingsControl.IsReadOnly = !isConnected;
                _lblSlotStatus.Text = isConnected
                    ? $"状态: {_selectedSlot.AssignedCamera?.ModelName} [{_selectedSlot.AssignedSerial}]{(isGrabbing ? " (采集中)" : "")}"
                    : "状态: 未连接相机";
            }
            else
            {
                _settingsControl.Settings = null;
                _lblSlotStatus.Text = "";
            }
        }

        private class SlotItem
        {
            public CameraSlot Slot;
            public string Display;
            public override string ToString() => Display;
        }
    }
}
