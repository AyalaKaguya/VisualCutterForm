using VisualMaster.Forms;
using VisualMaster.Api;
using VisualMaster.CameraLink;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VisualMaster.Forms.Camera
{
    public partial class CameraSettingsDialog : Form
    {
        private VisionController _vision;
        private string _slotId;
        private CameraSettings _settings;
        private CameraInfo _cameraInfo;
        private bool _isReadOnly;

        public CameraSettings Settings => _settings;

        public CameraSettingsDialog()
        {
            InitializeComponent();
        }

        public CameraSettingsDialog(CameraSettings settings, CameraInfo cameraInfo = null,
            bool readOnly = false, VisionController vision = null, string cameraSerial = null) : this()
        {
            _settings = settings?.Clone() as CameraSettings ?? new CameraSettings();
            _cameraInfo = cameraInfo;
            _isReadOnly = readOnly;
            _vision = vision;
            _slotId = cameraSerial;

            Text = _isReadOnly ? "相机设置 (只读)" : "相机设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 520);

            // Wire events
            _treeView.AfterSelect += OnTreeViewAfterSelect;
            _cmbTriggerMode.SelectedIndexChanged += (s, e) =>
            {
                bool isHardware = _cmbTriggerMode.SelectedIndex == 2;
                _cmbTriggerSource.Enabled = isHardware;
                _cmbTriggerActivation.Enabled = isHardware;
            };
            _trkGain.ValueChanged += (s, e) => { _numGain.Value = _trkGain.Value; };
            _numGain.ValueChanged += (s, e) => { _trkGain.Value = (int)_numGain.Value; };
            _previewTimer.Tick += OnPreviewTick;
            _btnDebugSnap.Click += OnSnapClick;
            _btnDebugContinuous.Click += OnContinuousClick;
            _btnDebugSave.Click += OnSaveClick;
            _btnOk.Click += (s, e) => { if (!_isReadOnly) { CollectToSettings(); DialogResult = DialogResult.OK; } };

            FormClosing += (s, e) =>
            {
                StopDebugPreview();
                _previewTimer?.Dispose();
                _previewTimer = null;
            };

            PopulateCameraInfo();
            PopulatePixelFormats();
            PopulateFromSettings();
        }

        private void OnTreeViewAfterSelect(object sender, TreeViewEventArgs e)
        {
            var index = e.Node?.ImageIndex ?? 0;
            _panelInfo.Visible = (index == 0);
            _panelTrigger.Visible = (index == 1);
            _panelExposure.Visible = (index == 2);
            _panelRoi.Visible = (index == 3);
            _panelDebug.Visible = (index == 4);

            if (index == 4)
                StartDebugPreview();
            else
                StopDebugPreview();
        }

        private void OnSnapClick(object sender, EventArgs e)
        {
            if (_vision == null || string.IsNullOrEmpty(_slotId)) return;

            try
            {
                Bitmap grabbed;
                bool ok = _vision.TryGrabImage(_slotId, 2000, out grabbed);
                if (ok && grabbed != null)
                {
                    var old = _previewBox.Image;
                    _previewBox.Image = grabbed;
                    old?.Dispose();

                    var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                    if (lbl.Length > 0) lbl[0].Text = $"单帧触发 · {grabbed.Width}x{grabbed.Height}";
                }
                else
                {
                    var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                    if (lbl.Length > 0) lbl[0].Text = "单帧触发超时";
                }
            }
            catch
            {
                var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                if (lbl.Length > 0) lbl[0].Text = "单帧触发失败";
            }
        }

        private void OnContinuousClick(object sender, EventArgs e)
        {
            if (_vision == null || string.IsNullOrEmpty(_slotId)) return;

            var btn = sender as Button;
            if (btn == null) return;

            if (_vision.IsCameraGrabbing(_slotId))
            {
                _vision.StopAcquisition(_slotId);
                btn.Text = "连续采集";
                btn.BackColor = Color.FromArgb(60, 60, 60);
            }
            else
            {
                _vision.StartAcquisition(_slotId);
                btn.Text = "停止采集";
                btn.BackColor = Color.FromArgb(180, 50, 50);
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            var bmp = _previewBox?.Image as Bitmap;
            if (bmp == null) return;

            using (var dlg = new SaveFileDialog
            {
                Filter = "PNG (*.png)|*.png|BMP (*.bmp)|*.bmp|JPEG (*.jpg)|*.jpg",
                FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                DefaultExt = ".png",
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bmp.Save(dlg.FileName);
                        var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                        if (lbl.Length > 0) lbl[0].Text = $"已保存: {Path.GetFileName(dlg.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                        if (lbl.Length > 0) lbl[0].Text = $"保存失败: {ex.Message}";
                    }
                }
            }
        }

        private void StartDebugPreview() => _previewTimer?.Start();
        private void StopDebugPreview() => _previewTimer?.Stop();

        private void OnPreviewTick(object sender, EventArgs e)
        {
            if (_vision == null || string.IsNullOrEmpty(_slotId))
            {
                var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                if (lbl.Length > 0) lbl[0].Text = "无相机连接";
                return;
            }

            try
            {
                bool grabbing = _vision != null && _vision.IsCameraGrabbing(_slotId);

                var btnContinuous = _panelDebug?.Controls.Find("btnContinuous", true);
                if (btnContinuous.Length > 0 && btnContinuous[0] is Button btn)
                {
                    if (grabbing)
                    {
                        btn.Text = "停止采集";
                        btn.BackColor = Color.FromArgb(180, 50, 50);
                    }
                    else
                    {
                        btn.Text = "连续采集";
                        btn.BackColor = Color.FromArgb(60, 60, 60);
                    }
                }

                var bmp = _vision.PeekLatestFromFifo(_slotId);
                if (bmp != null)
                {
                    var old = _previewBox.Image;
                    _previewBox.Image = bmp;
                    old?.Dispose();
                }

                var lbls = _panelDebug?.Controls.Find("lblDebugStatus", true);
                if (lbls.Length > 0)
                {
                    lbls[0].Text = grabbing
                        ? $"连续采集 · {_previewBox.Image?.Width ?? 0}x{_previewBox.Image?.Height ?? 0}"
                        : "采集已停止";
                }
            }
            catch
            {
            }
        }

        private void PopulateCameraInfo()
        {
            if (_cameraInfo == null)
            {
                _lblModel.Text = "未连接";
                _lblSerial.Text = "-";
                _lblTransport.Text = "-";
                _lblVersion.Text = "-";
                return;
            }

            _lblModel.Text = _cameraInfo.ModelName ?? "-";
            _lblSerial.Text = _cameraInfo.SerialNumber ?? "-";
            _lblTransport.Text = _cameraInfo.TransportTypeName ?? "-";
            _lblVersion.Text = _cameraInfo.DeviceVersion ?? "-";
        }

        private void PopulatePixelFormats()
        {
            try
            {
                if (_vision != null && !string.IsNullOrEmpty(_slotId))
                {
                    var formats = _vision.GetAvailablePixelFormats(_slotId);
                    if (formats != null && formats.Length > 0)
                    {
                        _cmbPixelFormat.Items.Clear();
                        _cmbPixelFormat.Items.AddRange(formats);
                    }
                }
            }
            catch { }
        }

        private void PopulateFromSettings()
        {
            _cmbTriggerMode.SelectedIndex = (int)_settings.TriggerMode;
            _cmbTriggerSource.Text = _settings.TriggerSource ?? "Line0";
            _cmbTriggerActivation.Text = _settings.TriggerActivation ?? "RisingEdge";
            bool isHardware = _settings.TriggerMode == TriggerModeEnum.Hardware;
            _cmbTriggerSource.Enabled = isHardware;
            _cmbTriggerActivation.Enabled = isHardware;

            SetNumSafe(_numExposure, (decimal)_settings.ExposureTimeUs);
            SetNumSafe(_numGain, (decimal)_settings.GainRaw);
            _trkGain.Value = Math.Max(_trkGain.Minimum, Math.Min(_trkGain.Maximum, (int)_settings.GainRaw));

            SetNumSafe(_numWidth, _settings.Width);
            SetNumSafe(_numHeight, _settings.Height);
            SetNumSafe(_numOffsetX, _settings.OffsetX);
            SetNumSafe(_numOffsetY, _settings.OffsetY);
            _cmbPixelFormat.Text = _settings.PixelFormat ?? "";
            SetNumSafe(_numFifoCapacity, _settings.FifoCapacity);

            if (_isReadOnly)
            {
                _cmbTriggerMode.Enabled = false;
                _cmbTriggerSource.Enabled = false;
                _cmbTriggerActivation.Enabled = false;
                _numExposure.Enabled = false;
                _numGain.Enabled = false;
                _trkGain.Enabled = false;
                _numWidth.Enabled = false;
                _numHeight.Enabled = false;
                _numOffsetX.Enabled = false;
                _numOffsetY.Enabled = false;
                _cmbPixelFormat.Enabled = false;
                _numFifoCapacity.Enabled = false;
            }
        }

        public void CollectToSettings()
        {
            _settings.TriggerMode = (TriggerModeEnum)_cmbTriggerMode.SelectedIndex;
            _settings.TriggerSource = _cmbTriggerSource.Text;
            _settings.TriggerActivation = _cmbTriggerActivation.Text;
            _settings.ExposureTimeUs = (double)_numExposure.Value;
            _settings.GainRaw = (double)_numGain.Value;
            _settings.Width = (int)_numWidth.Value;
            _settings.Height = (int)_numHeight.Value;
            _settings.OffsetX = (int)_numOffsetX.Value;
            _settings.OffsetY = (int)_numOffsetY.Value;
            _settings.PixelFormat = _cmbPixelFormat.Text;
            _settings.FifoCapacity = (int)_numFifoCapacity.Value;
        }

        private static void SetNumSafe(NumericUpDown num, decimal value)
        {
            num.Value = value < num.Minimum ? num.Minimum : value > num.Maximum ? num.Maximum : value;
        }
    }
}
