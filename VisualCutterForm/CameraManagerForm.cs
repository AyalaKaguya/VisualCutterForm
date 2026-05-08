using VisualMaster.Api;
using VisualMaster.CameraLink;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualCutterForm.Lib;

namespace VisualCutterForm
{
    public class CameraManagerForm : Form
    {
        private VisionController _vision;
        private TabControl _tabControl;
        private TabPage _tabDeviceList;
        private TabPage _tabSlotManager;

        // Tab 1: device list
        private CameraDiscoveryControl _discoveryControl;

        // Tab 2
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
        private CameraSlot _selectedSlot;

        public CameraManagerForm(VisionController vision)
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "相机管理器";
            Size = new Size(900, 580);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            BuildUI();

            _vision.EnumerateCameras();
            _discoveryControl.SetCameraManager(_vision.CameraManager);
            _discoveryControl.RefreshList();
            RefreshSlotList();

            Closing += (s, e) =>
            {
                foreach (var slot in _vision.CameraManager.Slots)
                    _vision.StopAcquisition(slot.SlotId);
            };
        }

        private void BuildUI()
        {
            _tabControl = new TabControl { Dock = DockStyle.Fill };

            // ── Tab 1: 相机列表 ──
            _tabDeviceList = new TabPage("相机列表");
            _discoveryControl = new CameraDiscoveryControl { Dock = DockStyle.Fill };
            _tabDeviceList.Controls.Add(_discoveryControl);

            // ── Tab 2: 相机槽位 ──
            _tabSlotManager = new TabPage("相机槽位");

            // left: slot list
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

            slotBtnPanel.Controls.Add(_btnAddSlot);
            slotBtnPanel.Controls.Add(_btnRemoveSlot);

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

            // right: settings
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
            _cmbBindCamera.SelectedIndexChanged += OnBindCameraChanged;

            _btnRefreshCameras = new Button
            {
                Text = "刷新",
                Location = new Point(306, 3),
                Size = new Size(50, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnRefreshCameras.FlatAppearance.BorderSize = 1;
            _btnRefreshCameras.Click += (s, e) => RebuildBindCameraCombo();

            bindPanel.Controls.Add(lblBind);
            bindPanel.Controls.Add(_cmbBindCamera);
            bindPanel.Controls.Add(_btnRefreshCameras);

            // action buttons bar
            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 36,
                Padding = new Padding(4, 3, 4, 0),
            };

            _btnEditSettings = new Button { Text = "编辑设置", Size = new Size(75, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnEditSettings.FlatAppearance.BorderSize = 1;
            _btnEditSettings.Click += (s, e) =>
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

            _btnPreview = new Button { Text = "预览", Size = new Size(50, 26), FlatStyle = FlatStyle.Flat, Enabled = false };
            _btnPreview.FlatAppearance.BorderSize = 1;
            _btnPreview.Click += (s, e) =>
            {
                if (_selectedSlot == null) return;
                using (var dlg = new CameraPreviewForm(_vision, _selectedSlot.SlotId))
                    dlg.ShowDialog(this);
            };

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
                SplitterDistance = 90,
            };
            _tab2Split.Panel1.Controls.Add(slotPanel);
            _tab2Split.Panel2.Controls.Add(settingsPanel);

            _tabSlotManager.Controls.Add(_tab2Split);

            _tabControl.TabPages.Add(_tabDeviceList);
            _tabControl.TabPages.Add(_tabSlotManager);

            _tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (_tabControl.SelectedTab == _tabDeviceList)
                {
                    _vision.EnumerateCameras();
                    _discoveryControl.RefreshList();
                }
            };

            Controls.Add(_tabControl);
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

            _btnRemoveSlot.Enabled = hasSlot;
            _btnEditSettings.Enabled = hasSlot;
            _btnStartGrab.Enabled = isConnected && !isGrabbing;
            _btnStopGrab.Enabled = isConnected && isGrabbing;
            _btnPreview.Enabled = isConnected;
            _cmbBindCamera.Enabled = hasSlot;

            if (_selectedSlot != null)
            {
                RebuildBindCameraCombo();
                _settingsControl.Settings = _selectedSlot.Settings ?? new CameraSettings();
                _settingsControl.IsReadOnly = false;
                _lblSlotStatus.Text = isConnected
                    ? $"{_selectedSlot.AssignedCamera?.ModelName} [{_selectedSlot.AssignedSerial}]{(isGrabbing ? " (采集中)" : "")}"
                    : "未绑定相机";
            }
            else
            {
                _cmbBindCamera.Items.Clear();
                _cmbBindCamera.Enabled = false;
                _settingsControl.Settings = null;
                _lblSlotStatus.Text = "";
            }
        }

        private void RebuildBindCameraCombo()
        {
            _cmbBindCamera.Items.Clear();

            var cameras = _vision.CameraManager.Cameras;
            if (cameras.Count == 0)
            {
                _vision.EnumerateCameras();
                cameras = _vision.CameraManager.Cameras;
            }

            _cmbBindCamera.Items.Add(new ComboItem { Info = null, Display = "（未绑定）" });

            foreach (var cam in cameras)
            {
                bool alreadyBound = false;
                foreach (var slot in _vision.CameraManager.Slots)
                {
                    if (slot.SlotId != (_selectedSlot?.SlotId)
                        && slot.IsConnected
                        && slot.AssignedSerial == cam.SerialNumber)
                    {
                        alreadyBound = true;
                        break;
                    }
                }

                if (alreadyBound) continue;

                _cmbBindCamera.Items.Add(new ComboItem
                {
                    Info = cam,
                    Display = $"{cam.ModelName} [{cam.SerialNumber}] ({cam.TransportTypeName})",
                });
            }

            // select current
            if (_selectedSlot?.IsConnected == true && _selectedSlot.AssignedSerial != null)
            {
                for (int i = 0; i < _cmbBindCamera.Items.Count; i++)
                {
                    var item = _cmbBindCamera.Items[i] as ComboItem;
                    if (item?.Info != null && item.Info.SerialNumber == _selectedSlot.AssignedSerial)
                    {
                        _cmbBindCamera.SelectedIndex = i;
                        return;
                    }
                }
            }

            _cmbBindCamera.SelectedIndex = 0;
        }

        private void OnBindCameraChanged(object sender, EventArgs e)
        {
            if (_selectedSlot == null) return;

            var item = _cmbBindCamera.SelectedItem as ComboItem;
            if (item == null) return;

            if (item.Info == null)
            {
                if (_selectedSlot.IsConnected)
                {
                    _vision.CloseSlot(_selectedSlot.SlotId);
                    RefreshSlotList();
                    OnSlotSelected(null, EventArgs.Empty);
                }
                return;
            }

            // safety check for duplicate
            foreach (var slot in _vision.CameraManager.Slots)
            {
                if (slot.SlotId != _selectedSlot.SlotId
                    && slot.IsConnected
                    && slot.AssignedSerial == item.Info.SerialNumber)
                {
                    MessageBox.Show($"相机 {item.Info.SerialNumber} 已绑定到其他槽位。", "无法重复绑定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    RebuildBindCameraCombo();
                    return;
                }
            }

            try
            {
                if (_selectedSlot.IsConnected)
                    _vision.CloseSlot(_selectedSlot.SlotId);

                _vision.OpenSlot(_selectedSlot.SlotId, item.Info);
                _settingsControl.IsReadOnly = false;
                RefreshSlotList();

                int idx = -1;
                for (int i = 0; i < _slotListBox.Items.Count; i++)
                {
                    if ((_slotListBox.Items[i] as SlotItem)?.Slot?.SlotId == _selectedSlot.SlotId)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0) _slotListBox.SelectedIndex = idx;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"绑定相机失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class SlotItem
        {
            public CameraSlot Slot;
            public string Display;
            public override string ToString() => Display;
        }

        private class ComboItem
        {
            public CameraInfo Info;
            public string Display;
            public override string ToString() => Display;
        }
    }
}
