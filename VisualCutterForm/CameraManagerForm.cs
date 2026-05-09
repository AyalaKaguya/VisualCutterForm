using VisualMaster.Api;
using VisualMaster.CameraLink;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualCutterForm.Lib;

namespace VisualCutterForm
{
    public partial class CameraManagerForm : Form
    {
        private VisionController _vision;
        private CameraSlot _selectedSlot;

        public CameraManagerForm()
        {
            InitializeComponent();
        }

        public CameraManagerForm(VisionController vision) : this()
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            Text = "相机管理器";
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
            _btnAddSlot.Click += (s, e) => { _vision.AddSlot($"相机{_vision.CameraManager.Slots.Count + 1}"); RefreshSlotList(); };
            _btnRemoveSlot.Click += (s, e) => { if (_selectedSlot != null) { _vision.RemoveSlot(_selectedSlot.SlotId); _selectedSlot = null; RefreshSlotList(); } };
            _cmbBindCamera.SelectedIndexChanged += OnBindCameraChanged;
            _btnRefreshCameras.Click += (s, e) => RebuildBindCameraCombo();
            _btnEditSettings.Click += OnEditSettingsClick;
            _btnStartGrab.Click += (s, e) => { if (_selectedSlot != null && _selectedSlot.IsConnected) { _vision.StartAcquisition(_selectedSlot.SlotId); RefreshSlotStatus(); } };
            _btnStopGrab.Click += (s, e) => { if (_selectedSlot != null && _selectedSlot.IsConnected) { _vision.StopAcquisition(_selectedSlot.SlotId); RefreshSlotStatus(); } };
            _btnPreview.Click += (s, e) => { if (_selectedSlot == null) return; using (var dlg = new CameraPreviewForm(_vision, _selectedSlot.SlotId)) dlg.ShowDialog(this); };
            _tabControl.SelectedIndexChanged += OnTabChanged;
            Closing += (s, e) => { foreach (var slot in _vision.CameraManager.Slots) _vision.StopAcquisition(slot.SlotId); };
        }

        private void OnEditSettingsClick(object sender, EventArgs e)
        {
            if (_selectedSlot == null) return;

            if (_settingsControl.IsReadOnly)
            {
                _settingsControl.IsReadOnly = false;
                _btnEditSettings.Text = "保存设置";
            }
            else
            {
                _settingsControl.CollectToSettings();
                _vision.UpdateSlotSettings(_selectedSlot.SlotId, _settingsControl.Settings);
                _settingsControl.IsReadOnly = true;
                _btnEditSettings.Text = "编辑设置";
                _lblSlotStatus.Text = $"设置已保存: {_selectedSlot.SlotName}";
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

        private void RefreshSlotList()
        {
            _slotListBox.Items.Clear();
            foreach (var slot in _vision.CameraManager.Slots)
            {
                var status = slot.IsConnected ? (slot.IsGrabbing ? " [采集中]" : " [已连接]") : " [未连接]";
                _slotListBox.Items.Add(new SlotItem { Slot = slot, Display = $"{slot.SlotName}{status}" });
            }
            if (_slotListBox.Items.Count > 0 && _selectedSlot == null)
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
            var item = _slotListBox.SelectedItem as SlotItem;
            _selectedSlot = item != null ? item.Slot : null;

            bool hasSlot = _selectedSlot != null;
            bool isConnected = _selectedSlot != null && _selectedSlot.IsConnected;
            bool isGrabbing = _selectedSlot != null && _selectedSlot.IsGrabbing;

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
                _settingsControl.IsReadOnly = true;
                _btnEditSettings.Text = "编辑设置";
                _lblSlotStatus.Text = isConnected
                    ? $"{_selectedSlot.AssignedCamera?.ModelName} [{_selectedSlot.AssignedSerial}]{(isGrabbing ? " (采集中)" : "")}"
                    : "未绑定相机";
            }
            else
            {
                _cmbBindCamera.Items.Clear();
                _cmbBindCamera.Enabled = false;
                _settingsControl.Settings = null;
                _lblSlotStatus.Text = string.Empty;
            }
        }

        private bool IsSerialBoundToOtherSlot(string serial, string excludeSlotId)
        {
            foreach (var slot in _vision.CameraManager.Slots)
                if (slot.SlotId != excludeSlotId && slot.IsConnected && slot.AssignedSerial == serial)
                    return true;
            return false;
        }

        private void RebuildBindCameraCombo()
        {
            _cmbBindCamera.Items.Clear();
            var cameras = _vision.CameraManager.Cameras;
            if (cameras.Count == 0) { _vision.EnumerateCameras(); cameras = _vision.CameraManager.Cameras; }

            _cmbBindCamera.Items.Add(new ComboItem { Info = null, Display = "（未绑定）" });

            foreach (var cam in cameras)
            {
                if (IsSerialBoundToOtherSlot(cam.SerialNumber, _selectedSlot?.SlotId)) continue;
                _cmbBindCamera.Items.Add(new ComboItem { Info = cam, Display = $"{cam.ModelName} [{cam.SerialNumber}] ({cam.TransportTypeName})" });
            }

            if (_selectedSlot != null && _selectedSlot.IsConnected && _selectedSlot.AssignedSerial != null)
                for (int i = 0; i < _cmbBindCamera.Items.Count; i++)
                {
                    var ci = _cmbBindCamera.Items[i] as ComboItem;
                    if (ci != null && ci.Info != null && ci.Info.SerialNumber == _selectedSlot.AssignedSerial)
                    { _cmbBindCamera.SelectedIndex = i; return; }
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
                if (_selectedSlot.IsConnected) { _vision.CloseSlot(_selectedSlot.SlotId); RefreshSlotList(); OnSlotSelected(null, EventArgs.Empty); }
                return;
            }

            if (IsSerialBoundToOtherSlot(item.Info.SerialNumber, _selectedSlot.SlotId))
            {
                MessageBox.Show($"相机 {item.Info.SerialNumber} 已绑定到其他槽位。", "无法重复绑定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RebuildBindCameraCombo();
                return;
            }

            try
            {
                if (_selectedSlot.IsConnected) _vision.CloseSlot(_selectedSlot.SlotId);
                _vision.OpenSlot(_selectedSlot.SlotId, item.Info);
                _settingsControl.IsReadOnly = true;
                _btnEditSettings.Text = "编辑设置";
                RefreshSlotList();
                int idx = -1;
                for (int i = 0; i < _slotListBox.Items.Count; i++)
                    if ((_slotListBox.Items[i] as SlotItem)?.Slot?.SlotId == _selectedSlot.SlotId) { idx = i; break; }
                if (idx >= 0) _slotListBox.SelectedIndex = idx;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"绑定相机失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class SlotItem { public CameraSlot Slot; public string Display; public override string ToString() => Display; }
        private class ComboItem { public CameraInfo Info; public string Display; public override string ToString() => Display; }
    }
}