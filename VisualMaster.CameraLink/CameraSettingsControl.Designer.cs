using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.CameraLink
{
    partial class CameraSettingsControl
    {
        private IContainer components = null;
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

        private void InitializeComponent()
        {
            components = new Container();
            var lblTriggerMode = new Label();
            var lblTriggerSource = new Label();
            var lblTriggerActivation = new Label();
            var sep1 = new Label();
            var lblExposure = new Label();
            var lblGain = new Label();
            var sep2 = new Label();
            var lblWidth = new Label();
            var lblHeight = new Label();
            var lblOffsetX = new Label();
            var lblOffsetY = new Label();
            var sep3 = new Label();
            var lblFifoCap = new Label();
            _cmbTriggerMode = new ComboBox();
            _cmbTriggerSource = new ComboBox();
            _cmbTriggerActivation = new ComboBox();
            _numExposure = new NumericUpDown();
            _numGain = new NumericUpDown();
            _trkGain = new TrackBar();
            _numWidth = new NumericUpDown();
            _numHeight = new NumericUpDown();
            _numOffsetX = new NumericUpDown();
            _numOffsetY = new NumericUpDown();
            _numFifoCapacity = new NumericUpDown();

            ((System.ComponentModel.ISupportInitialize)(_numExposure)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(_numGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(_trkGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(_numWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(_numHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(_numOffsetX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(_numOffsetY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(_numFifoCapacity)).BeginInit();
            this.SuspendLayout();

            // lblTriggerMode
            lblTriggerMode.AutoSize = true;
            lblTriggerMode.Location = new Point(8, 12);
            lblTriggerMode.Size = new Size(160, 22);
            lblTriggerMode.Text = "触发模式:";
            lblTriggerMode.TextAlign = ContentAlignment.MiddleRight;

            // _cmbTriggerMode
            _cmbTriggerMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTriggerMode.Items.AddRange(new object[] { "连续采集", "软件触发", "硬件触发" });
            _cmbTriggerMode.Location = new Point(180, 10);
            _cmbTriggerMode.Size = new Size(140, 22);

            // lblTriggerSource
            lblTriggerSource.AutoSize = true;
            lblTriggerSource.Location = new Point(8, 42);
            lblTriggerSource.Size = new Size(160, 22);
            lblTriggerSource.Text = "触发源:";
            lblTriggerSource.TextAlign = ContentAlignment.MiddleRight;

            // _cmbTriggerSource
            _cmbTriggerSource.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTriggerSource.Enabled = false;
            _cmbTriggerSource.Items.AddRange(new object[] { "Line0", "Line1", "Line2", "Line3", "Software" });
            _cmbTriggerSource.Location = new Point(180, 40);
            _cmbTriggerSource.Size = new Size(140, 22);

            // lblTriggerActivation
            lblTriggerActivation.AutoSize = true;
            lblTriggerActivation.Location = new Point(8, 72);
            lblTriggerActivation.Size = new Size(160, 22);
            lblTriggerActivation.Text = "触发边沿:";
            lblTriggerActivation.TextAlign = ContentAlignment.MiddleRight;

            // _cmbTriggerActivation
            _cmbTriggerActivation.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTriggerActivation.Enabled = false;
            _cmbTriggerActivation.Items.AddRange(new object[] { "RisingEdge", "FallingEdge", "LevelHigh", "LevelLow" });
            _cmbTriggerActivation.Location = new Point(180, 70);
            _cmbTriggerActivation.Size = new Size(140, 22);

            // sep1
            sep1.BorderStyle = BorderStyle.Fixed3D;
            sep1.Location = new Point(8, 106);
            sep1.Size = new Size(460, 1);

            // lblExposure
            lblExposure.AutoSize = true;
            lblExposure.Location = new Point(8, 118);
            lblExposure.Size = new Size(160, 22);
            lblExposure.Text = "曝光时间 (us):";
            lblExposure.TextAlign = ContentAlignment.MiddleRight;

            // _numExposure
            _numExposure.Increment = new decimal(new int[] { 500, 0, 0, 0 });
            _numExposure.Location = new Point(180, 116);
            _numExposure.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            _numExposure.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            _numExposure.Size = new Size(100, 22);
            _numExposure.Value = new decimal(new int[] { 5000, 0, 0, 0 });

            // lblGain
            lblGain.AutoSize = true;
            lblGain.Location = new Point(8, 148);
            lblGain.Size = new Size(160, 22);
            lblGain.Text = "增益 (dB):";
            lblGain.TextAlign = ContentAlignment.MiddleRight;

            // _numGain
            _numGain.DecimalPlaces = 2;
            _numGain.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            _numGain.Location = new Point(180, 146);
            _numGain.Maximum = new decimal(new int[] { 40, 0, 0, 0 });
            _numGain.Size = new Size(100, 22);

            // _trkGain
            _trkGain.Location = new Point(180, 176);
            _trkGain.Maximum = 40;
            _trkGain.Size = new Size(240, 45);

            // sep2
            sep2.BorderStyle = BorderStyle.Fixed3D;
            sep2.Location = new Point(8, 226);
            sep2.Size = new Size(460, 1);

            // lblWidth
            lblWidth.AutoSize = true;
            lblWidth.Location = new Point(8, 238);
            lblWidth.Size = new Size(160, 22);
            lblWidth.Text = "宽度:";
            lblWidth.TextAlign = ContentAlignment.MiddleRight;

            // _numWidth
            _numWidth.Increment = new decimal(new int[] { 8, 0, 0, 0 });
            _numWidth.Location = new Point(180, 236);
            _numWidth.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            _numWidth.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            _numWidth.Size = new Size(100, 22);

            // lblHeight
            lblHeight.AutoSize = true;
            lblHeight.Location = new Point(8, 268);
            lblHeight.Size = new Size(160, 22);
            lblHeight.Text = "高度:";
            lblHeight.TextAlign = ContentAlignment.MiddleRight;

            // _numHeight
            _numHeight.Increment = new decimal(new int[] { 8, 0, 0, 0 });
            _numHeight.Location = new Point(180, 266);
            _numHeight.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            _numHeight.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            _numHeight.Size = new Size(100, 22);

            // lblOffsetX
            lblOffsetX.AutoSize = true;
            lblOffsetX.Location = new Point(8, 298);
            lblOffsetX.Size = new Size(160, 22);
            lblOffsetX.Text = "偏移 X:";
            lblOffsetX.TextAlign = ContentAlignment.MiddleRight;

            // _numOffsetX
            _numOffsetX.Increment = new decimal(new int[] { 4, 0, 0, 0 });
            _numOffsetX.Location = new Point(180, 296);
            _numOffsetX.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            _numOffsetX.Size = new Size(100, 22);

            // lblOffsetY
            lblOffsetY.AutoSize = true;
            lblOffsetY.Location = new Point(8, 328);
            lblOffsetY.Size = new Size(160, 22);
            lblOffsetY.Text = "偏移 Y:";
            lblOffsetY.TextAlign = ContentAlignment.MiddleRight;

            // _numOffsetY
            _numOffsetY.Increment = new decimal(new int[] { 4, 0, 0, 0 });
            _numOffsetY.Location = new Point(180, 326);
            _numOffsetY.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            _numOffsetY.Size = new Size(100, 22);

            // sep3
            sep3.BorderStyle = BorderStyle.Fixed3D;
            sep3.Location = new Point(8, 362);
            sep3.Size = new Size(460, 1);

            // lblFifoCap
            lblFifoCap.AutoSize = true;
            lblFifoCap.Location = new Point(8, 374);
            lblFifoCap.Size = new Size(160, 22);
            lblFifoCap.Text = "FIFO 容量:";
            lblFifoCap.TextAlign = ContentAlignment.MiddleRight;

            // _numFifoCapacity
            _numFifoCapacity.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            _numFifoCapacity.Location = new Point(180, 372);
            _numFifoCapacity.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            _numFifoCapacity.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _numFifoCapacity.Size = new Size(100, 22);
            _numFifoCapacity.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // CameraSettingsControl
            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(lblFifoCap);
            this.Controls.Add(_numFifoCapacity);
            this.Controls.Add(sep3);
            this.Controls.Add(lblOffsetY);
            this.Controls.Add(_numOffsetY);
            this.Controls.Add(lblOffsetX);
            this.Controls.Add(_numOffsetX);
            this.Controls.Add(lblHeight);
            this.Controls.Add(_numHeight);
            this.Controls.Add(lblWidth);
            this.Controls.Add(_numWidth);
            this.Controls.Add(sep2);
            this.Controls.Add(_trkGain);
            this.Controls.Add(lblGain);
            this.Controls.Add(_numGain);
            this.Controls.Add(lblExposure);
            this.Controls.Add(_numExposure);
            this.Controls.Add(sep1);
            this.Controls.Add(lblTriggerActivation);
            this.Controls.Add(_cmbTriggerActivation);
            this.Controls.Add(lblTriggerSource);
            this.Controls.Add(_cmbTriggerSource);
            this.Controls.Add(lblTriggerMode);
            this.Controls.Add(_cmbTriggerMode);
            this.Font = new Font("Microsoft YaHei", 9F);
            this.Name = "CameraSettingsControl";
            this.ResumeLayout(false);
            this.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(_numExposure)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_numGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_trkGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_numWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_numHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_numOffsetX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_numOffsetY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_numFifoCapacity)).EndInit();
        }
    }
}
