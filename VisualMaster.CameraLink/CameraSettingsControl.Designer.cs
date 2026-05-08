using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.CameraLink
{
    partial class CameraSettingsControl
    {
        private System.ComponentModel.IContainer components = null;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
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

        private void InitializeComponent()
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
    }
}
