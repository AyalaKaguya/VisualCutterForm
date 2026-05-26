using VisualMaster.Forms;
using VisualMaster.CameraLink;
using VisualMaster.CameraLink.Api;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CameraDeviceConfig = VisualMaster.CameraLink.Api.CameraDeviceConfig;
using CameraSettings = VisualMaster.CameraLink.Api.CameraSettings;
using CameraInfo = VisualMaster.CameraLink.Api.CameraInfo;

namespace VisualMaster.Forms.Camera
{
    public partial class CameraManagerForm : Form
    {
        private VisionController _vision;
        private CameraDeviceConfig _selectedDevice;

        public CameraManagerForm()
        {
            InitializeComponent();
        }

        public CameraManagerForm(VisionController vision) : this()
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "相机资源管理";
            Size = new Size(900, 580);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            WireEvents();

            _vision.EnumerateCameras();
            _discoveryControl.SetCameraManager(_vision.CameraManager);
            _discoveryControl.RefreshList();
            RefreshSlotList();
        }

        private void WireEvents()
        {
            _slotListBox.SelectedIndexChanged += OnSlotSelected;
            _btnAddSlot.Click += (s, e) =>
            {
                _vision.AddCameraDevice($"相机{_vision.GetCameraDeviceConfigs().Count + 1}");
                RefreshSlotList();
            };
            _btnRemoveSlot.Click += (s, e) =>
            {
                if (_selectedDevice == null)
                    return;

                _vision.RemoveCameraDevice(_selectedDevice.DeviceId);
                _selectedDevice = null;
                RefreshSlotList();
            };
            _cmbBindCamera.SelectedIndexChanged += OnBindCameraChanged;
            _btnRefreshCameras.Click += (s, e) => RebuildBindCameraCombo();
            _btnEditSettings.Click += OnEditSettingsClick;
            _btnStartGrab.Click += (s, e) =>
            {
                if (_selectedDevice != null && _vision.IsCameraConnected(_selectedDevice.DeviceId))
                {
                    _vision.StartAcquisition(_selectedDevice.DeviceId);
                    RefreshSlotStatus();
                }
            };
            _btnStopGrab.Click += (s, e) =>
            {
                if (_selectedDevice != null && _vision.IsCameraConnected(_selectedDevice.DeviceId))
                {
                    _vision.StopAcquisition(_selectedDevice.DeviceId);
                    RefreshSlotStatus();
                }
            };
            _btnPreview.Click += (s, e) =>
            {
                if (_selectedDevice == null)
                    return;

                using (var dlg = new CameraPreviewForm(_vision, _selectedDevice.DeviceId))
                    dlg.ShowDialog(this);
            };
            _tabControl.SelectedIndexChanged += OnTabChanged;
            _txtSlotName.TextChanged += OnSlotNameChanged;
            Closing += (s, e) =>
            {
                foreach (var device in _vision.GetCameraDeviceConfigs())
                    _vision.StopAcquisition(device.DeviceId);
            };
        }

        private void OnEditSettingsClick(object sender, EventArgs e)
        {
            if (_selectedDevice == null) return;

            if (_settingsControl.IsReadOnly)
            {
                _settingsControl.IsReadOnly = false;
                _btnEditSettings.Text = "保存设置";
            }
            else
            {
                _settingsControl.CollectToSettings();
                _vision.UpdateCameraSettings(_selectedDevice.DeviceId, _settingsControl.Settings);
                _settingsControl.IsReadOnly = true;
                _btnEditSettings.Text = "编辑设置";
                _lblSlotStatus.Text = $"设置已保存: {_vision.GetCameraDisplayName(_selectedDevice.DeviceId)}";
                _selectedDevice = _vision.GetCameraDeviceConfig(_selectedDevice.DeviceId);
            }
        }

        private void OnTabChanged(object sender, EventArgs e)
        {
            if (_tabControl.SelectedTab == _tabDeviceList)
            {
                _vision.EnumerateCameras();
                _discoveryControl.RefreshList();
            }
        }

        private void OnSlotNameChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null) return;

            _vision.UpdateCameraDeviceDisplayName(_selectedDevice.DeviceId, _txtSlotName.Text);
            _selectedDevice = _vision.GetCameraDeviceConfig(_selectedDevice.DeviceId);
            RefreshSlotList();
        }

        private void RefreshSlotList()
        {
            _slotListBox.Items.Clear();
            foreach (var device in _vision.GetCameraDeviceConfigs())
            {
                var isConnected = _vision.IsCameraConnected(device.DeviceId);
                var isGrabbing = _vision.IsCameraGrabbing(device.DeviceId);
                var status = isConnected ? (isGrabbing ? " [采集中]" : " [已连接]") : " [未连接]";
                _slotListBox.Items.Add(new DisplayItem(device.DeviceId, $"{device.DisplayName}{status}") { Tag = device });
            }

            if (_selectedDevice != null)
            {
                for (int i = 0; i < _slotListBox.Items.Count; i++)
                {
                    var item = _slotListBox.Items[i] as DisplayItem;
                    if (item?.Id == _selectedDevice.DeviceId)
                    {
                        _slotListBox.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (_slotListBox.Items.Count > 0 && _selectedDevice == null)
                _slotListBox.SelectedIndex = 0;
        }

        private void RefreshSlotStatus()
        {
            int idx = _slotListBox.SelectedIndex;
            RefreshSlotList();
            if (idx >= 0 && idx < _slotListBox.Items.Count) _slotListBox.SelectedIndex = idx;
        }

        private void OnSlotSelected(object sender, EventArgs e)
        {
            var item = _slotListBox.SelectedItem as DisplayItem;
            _selectedDevice = item != null ? item.Tag as CameraDeviceConfig : null;

            bool hasSlot = _selectedDevice != null;
            bool isConnected = _selectedDevice != null && _vision.IsCameraConnected(_selectedDevice.DeviceId);
            bool isGrabbing = _selectedDevice != null && _vision.IsCameraGrabbing(_selectedDevice.DeviceId);

            _btnRemoveSlot.Enabled = hasSlot;
            _btnEditSettings.Enabled = hasSlot;
            _btnStartGrab.Enabled = isConnected && !isGrabbing;
            _btnStopGrab.Enabled = isConnected && isGrabbing;
            _btnPreview.Enabled = isConnected;
            _cmbBindCamera.Enabled = hasSlot;

            if (_selectedDevice != null)
            {
                _txtSlotName.Enabled = true;
                _txtSlotName.Text = _selectedDevice.DisplayName ?? "";
                RebuildBindCameraCombo();
                _settingsControl.Settings = _vision.GetCameraSettings(_selectedDevice.DeviceId) ?? new CameraSettings();
                _settingsControl.IsReadOnly = true;
                _btnEditSettings.Text = "编辑设置";
                var assignedCamera = _vision.GetAssignedCameraInfo(_selectedDevice.DeviceId);
                _lblSlotStatus.Text = isConnected
                    ? $"{assignedCamera?.ModelName} [{_vision.GetCameraAssignedSerial(_selectedDevice.DeviceId)}]{(isGrabbing ? " (采集中)" : "")}" 
                    : "未绑定相机";
            }
            else
            {
                _txtSlotName.Enabled = false;
                _txtSlotName.Text = "";
                _cmbBindCamera.Items.Clear();
                _cmbBindCamera.Enabled = false;
                _settingsControl.Settings = null;
                _lblSlotStatus.Text = string.Empty;
            }
        }

        private bool IsSerialBoundToOtherSlot(string serial, string excludeSlotId)
        {
            foreach (var device in _vision.GetCameraDeviceConfigs())
                if (device.DeviceId != excludeSlotId && _vision.IsCameraConnected(device.DeviceId) && device.AssignedSerial == serial)
                    return true;
            return false;
        }

        private void RebuildBindCameraCombo()
        {
            _cmbBindCamera.Items.Clear();
            var cameras = _vision.GetDiscoveredCameras();
            if (cameras.Count == 0) { _vision.EnumerateCameras(); cameras = _vision.GetDiscoveredCameras(); }

            _cmbBindCamera.Items.Add(new DisplayItem(null, "（未绑定）"));
            foreach (var cam in cameras)
            {
                if (IsSerialBoundToOtherSlot(cam.SerialNumber, _selectedDevice?.DeviceId)) continue;
                _cmbBindCamera.Items.Add(new DisplayItem(cam.SerialNumber, $"{cam.ModelName} [{cam.SerialNumber}] ({cam.TransportTypeName})") { Tag = cam });
            }
            if (_selectedDevice != null && _vision.IsCameraConnected(_selectedDevice.DeviceId) && _selectedDevice.AssignedSerial != null)
                for (int i = 0; i < _cmbBindCamera.Items.Count; i++)
                {
                    var ci = _cmbBindCamera.Items[i] as DisplayItem;
                    if (ci?.Tag is CameraInfo info && info.SerialNumber == _selectedDevice.AssignedSerial)
                    { _cmbBindCamera.SelectedIndex = i; return; }
                }
            _cmbBindCamera.SelectedIndex = 0;
        }
        private void OnBindCameraChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null) return;
            var item = _cmbBindCamera.SelectedItem as DisplayItem;
            if (item == null) return;
            var info = item.Tag as CameraInfo;
            if (info == null)
            {
                if (_vision.IsCameraConnected(_selectedDevice.DeviceId))
                {
                    _vision.CloseSlot(_selectedDevice.DeviceId);
                    _selectedDevice = _vision.GetCameraDeviceConfig(_selectedDevice.DeviceId);
                    RefreshSlotList();
                    OnSlotSelected(null, EventArgs.Empty);
                }
                return;
            }
            if (IsSerialBoundToOtherSlot(info.SerialNumber, _selectedDevice.DeviceId))
            {
                MessageBox.Show($"相机 {info.SerialNumber} 已绑定到其他设备。", "无法重复绑定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RebuildBindCameraCombo();
                return;
            }
            try
            {
                if (_vision.IsCameraConnected(_selectedDevice.DeviceId))
                    _vision.CloseSlot(_selectedDevice.DeviceId);

                _vision.OpenSlot(_selectedDevice.DeviceId, info);
                _settingsControl.IsReadOnly = true;
                _btnEditSettings.Text = "编辑设置";
                _selectedDevice = _vision.GetCameraDeviceConfig(_selectedDevice.DeviceId);
                RefreshSlotList();
                int idx = -1;
                for (int i = 0; i < _slotListBox.Items.Count; i++)
                    if ((_slotListBox.Items[i] as DisplayItem)?.Id == _selectedDevice.DeviceId) { idx = i; break; }
                if (idx >= 0) _slotListBox.SelectedIndex = idx;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"绑定相机失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}