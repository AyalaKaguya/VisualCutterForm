using VisualMaster.Api;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.CameraLink
{
    public class CameraSettingsControl : UserControl
    {
        private CameraSettings _settings;
        private bool _isReadOnly;

        private ComboBox _cmbTriggerMode;
        private ComboBox _cmbTriggerSource;
        private ComboBox _cmbTriggerActivation;
        private NumericUpDown _numExposure;
        private NumericUpDown _numGain;
        private TrackBar _trkGain;
        private NumericUpDown _numWidth;
        private NumericUpDown _numHeight;
        private NumericUpDown _numOffsetX;
        private NumericUpDown _numOffsetY;
        private NumericUpDown _numFifoCapacity;

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

        public CameraSettingsControl()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            Font = new Font("Microsoft YaHei", 9F);
            var y = 8;

            _cmbTriggerMode = new ComboBox
            {
                Location = new Point(180, y + 2),
                Size = new Size(140, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _cmbTriggerMode.Items.AddRange(new[] { "连续采集", "软件触发", "硬件触发" });
            _cmbTriggerMode.SelectedIndexChanged += (s, e) =>
            {
                bool isHardware = _cmbTriggerMode.SelectedIndex == 2;
                _cmbTriggerSource.Enabled = isHardware && !_isReadOnly;
                _cmbTriggerActivation.Enabled = isHardware && !_isReadOnly;
            };
            Controls.Add(new Label
            {
                Text = "触发模式:",
                Location = new Point(8, y + 4),
                Size = new Size(160, 22),
                TextAlign = ContentAlignment.MiddleRight,
            });
            Controls.Add(_cmbTriggerMode);
            y += 30;

            AddCombo("触发源:", ref y, out _cmbTriggerSource,
                new[] { "Line0", "Line1", "Line2", "Line3", "Software" }, 0);
            AddCombo("触发边沿:", ref y, out _cmbTriggerActivation,
                new[] { "RisingEdge", "FallingEdge", "LevelHigh", "LevelLow" }, 0);
            y += 4;

            var sep1 = new Label { Location = new Point(8, y), Size = new Size(460, 1), BorderStyle = BorderStyle.Fixed3D };
            Controls.Add(sep1);
            y += 8;

            AddNumeric("曝光时间 (us):", ref y, out _numExposure, 100, 10000000, 5000, 500);
            AddNumeric("增益 (dB):", ref y, out _numGain, 0, 40, 0, 1, 2);

            _trkGain = new TrackBar
            {
                Location = new Point(180, y),
                Size = new Size(240, 45),
                Minimum = 0,
                Maximum = 40,
                Value = 0,
            };
            _trkGain.ValueChanged += (s, e) => { _numGain.Value = _trkGain.Value; };
            _numGain.ValueChanged += (s, e) => { _trkGain.Value = (int)_numGain.Value; };
            Controls.Add(_trkGain);
            y += 50;

            var sep2 = new Label { Location = new Point(8, y), Size = new Size(460, 1), BorderStyle = BorderStyle.Fixed3D };
            Controls.Add(sep2);
            y += 8;

            AddNumeric("宽度:", ref y, out _numWidth, 64, 32768, 0, 8);
            AddNumeric("高度:", ref y, out _numHeight, 64, 32768, 0, 8);
            AddNumeric("偏移 X:", ref y, out _numOffsetX, 0, 32768, 0, 4);
            AddNumeric("偏移 Y:", ref y, out _numOffsetY, 0, 32768, 0, 4);

            y += 4;
            var sep3 = new Label { Location = new Point(8, y), Size = new Size(460, 1), BorderStyle = BorderStyle.Fixed3D };
            Controls.Add(sep3);
            y += 8;

            AddNumeric("FIFO 容量:", ref y, out _numFifoCapacity, 1, 100, 10, 1);
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

        private void AddNumeric(string labelText, ref int y, out NumericUpDown numeric,
            int min, int max, int value, int increment, int decimalPlaces = 0)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(8, y + 4),
                Size = new Size(160, 22),
                TextAlign = ContentAlignment.MiddleRight,
            };
            numeric = new NumericUpDown
            {
                Location = new Point(180, y + 2),
                Size = new Size(100, 22),
                Minimum = min,
                Maximum = max,
                Value = Clamp(value, min, max),
                Increment = increment,
                DecimalPlaces = decimalPlaces,
            };
            Controls.Add(lbl);
            Controls.Add(numeric);
            y += 30;
        }

        private void AddCombo(string labelText, ref int y, out ComboBox comboBox,
            string[] items, int defaultIndex)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(8, y + 4),
                Size = new Size(160, 22),
                TextAlign = ContentAlignment.MiddleRight,
            };
            comboBox = new ComboBox
            {
                Location = new Point(180, y + 2),
                Size = new Size(140, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
            };
            comboBox.Items.AddRange(items);
            if (defaultIndex >= 0 && defaultIndex < items.Length)
                comboBox.SelectedIndex = defaultIndex;

            Controls.Add(lbl);
            Controls.Add(comboBox);
            y += 30;
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static void SetNumSafe(NumericUpDown num, decimal value)
        {
            num.Value = Clamp(value, num.Minimum, num.Maximum);
        }
    }
}
