using VisualMaster.Api;
using System;
using System.Windows.Forms;

namespace VisualMaster.Forms.Camera
{
    public partial class CameraSettingsControl : System.Windows.Forms.UserControl
    {
        private CameraSettings _settings;
        private bool _isReadOnly;

        public CameraSettings Settings
        {
            get
            {
                CollectToSettings();
                return _settings;
            }
            set
            {
                _settings = value?.Clone() as CameraSettings ?? new CameraSettings();
                PopulateFromSettings();
            }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set
            {
                _isReadOnly = value;
                SetReadOnlyMode();
            }
        }

        private bool _syncingGain;

        public CameraSettingsControl()
        {
            Dock = System.Windows.Forms.DockStyle.Fill;
            AutoScroll = true;
            InitializeComponent();
            _cmbTriggerMode.SelectedIndexChanged += (s, e) =>
            {
                bool isHardware = _cmbTriggerMode.SelectedIndex == 2;
                _cmbTriggerSource.Enabled = isHardware && !_isReadOnly;
                _cmbTriggerActivation.Enabled = isHardware && !_isReadOnly;
            };
            _trkGain.ValueChanged += (s, e) =>
            {
                if (_syncingGain) return;
                _syncingGain = true;
                _numGain.Value = _trkGain.Value;
                _syncingGain = false;
            };
            _numGain.ValueChanged += (s, e) =>
            {
                if (_syncingGain) return;
                _syncingGain = true;
                int clamped = Math.Max(_trkGain.Minimum, Math.Min(_trkGain.Maximum, (int)_numGain.Value));
                _trkGain.Value = clamped;
                _syncingGain = false;
            };
        }

        public void CollectToSettings()
        {
            if (_settings == null) _settings = new CameraSettings();

            _settings.TriggerMode = (TriggerModeEnum)_cmbTriggerMode.SelectedIndex;
            _settings.TriggerSource = _cmbTriggerSource.Text;
            _settings.TriggerActivation = _cmbTriggerActivation.Text;
            _settings.ExposureTimeUs = (double)_numExposure.Value;
            _settings.GainRaw = (double)_numGain.Value;
            _settings.Width = (int)_numWidth.Value;
            _settings.Height = (int)_numHeight.Value;
            _settings.OffsetX = (int)_numOffsetX.Value;
            _settings.OffsetY = (int)_numOffsetY.Value;
            _settings.FifoCapacity = (int)_numFifoCapacity.Value;
        }

        private void PopulateFromSettings()
        {
            if (_settings == null) return;

            _cmbTriggerMode.SelectedIndex = (int)_settings.TriggerMode;
            _cmbTriggerSource.Text = _settings.TriggerSource ?? "Line0";
            _cmbTriggerActivation.Text = _settings.TriggerActivation ?? "RisingEdge";
            bool isHardware = _settings.TriggerMode == TriggerModeEnum.Hardware;
            _cmbTriggerSource.Enabled = isHardware && !_isReadOnly;
            _cmbTriggerActivation.Enabled = isHardware && !_isReadOnly;

            SetNumSafe(_numExposure, (decimal)_settings.ExposureTimeUs);
            SetNumSafe(_numGain, (decimal)_settings.GainRaw);
            _trkGain.Value = Math.Max(_trkGain.Minimum, Math.Min(_trkGain.Maximum, (int)_settings.GainRaw));

            SetNumSafe(_numWidth, _settings.Width);
            SetNumSafe(_numHeight, _settings.Height);
            SetNumSafe(_numOffsetX, _settings.OffsetX);
            SetNumSafe(_numOffsetY, _settings.OffsetY);
            SetNumSafe(_numFifoCapacity, _settings.FifoCapacity);
        }

        private void SetReadOnlyMode()
        {
            _cmbTriggerMode.Enabled = !_isReadOnly;
            bool isHardware = _settings?.TriggerMode == TriggerModeEnum.Hardware;
            _cmbTriggerSource.Enabled = !_isReadOnly && isHardware;
            _cmbTriggerActivation.Enabled = !_isReadOnly && isHardware;
            _numExposure.Enabled = !_isReadOnly;
            _numGain.Enabled = !_isReadOnly;
            _trkGain.Enabled = !_isReadOnly;
            _numWidth.Enabled = !_isReadOnly;
            _numHeight.Enabled = !_isReadOnly;
            _numOffsetX.Enabled = !_isReadOnly;
            _numOffsetY.Enabled = !_isReadOnly;
            _numFifoCapacity.Enabled = !_isReadOnly;
        }

        private static void SetNumSafe(System.Windows.Forms.NumericUpDown num, decimal value)
        {
            num.Value = value < num.Minimum ? num.Minimum : value > num.Maximum ? num.Maximum : value;
        }
    }
}
