using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.CameraLink
{
    partial class CameraSettingsControl
    {
        private IContainer components = null;
        private Label lblTriggerMode;
        private Label lblTriggerSource;
        private Label lblTriggerActivation;
        private Label sep1;
        private Label lblExposure;
        private Label lblGain;
        private Label sep2;
        private Label lblWidth;
        private Label lblHeight;
        private Label lblOffsetX;
        private Label lblOffsetY;
        private Label sep3;
        private Label lblFifoCap;
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
            this.lblTriggerMode = new System.Windows.Forms.Label();
            this.lblTriggerSource = new System.Windows.Forms.Label();
            this.lblTriggerActivation = new System.Windows.Forms.Label();
            this.sep1 = new System.Windows.Forms.Label();
            this.lblExposure = new System.Windows.Forms.Label();
            this.lblGain = new System.Windows.Forms.Label();
            this.sep2 = new System.Windows.Forms.Label();
            this.lblWidth = new System.Windows.Forms.Label();
            this.lblHeight = new System.Windows.Forms.Label();
            this.lblOffsetX = new System.Windows.Forms.Label();
            this.lblOffsetY = new System.Windows.Forms.Label();
            this.sep3 = new System.Windows.Forms.Label();
            this.lblFifoCap = new System.Windows.Forms.Label();
            this._cmbTriggerMode = new System.Windows.Forms.ComboBox();
            this._cmbTriggerSource = new System.Windows.Forms.ComboBox();
            this._cmbTriggerActivation = new System.Windows.Forms.ComboBox();
            this._numExposure = new System.Windows.Forms.NumericUpDown();
            this._numGain = new System.Windows.Forms.NumericUpDown();
            this._trkGain = new System.Windows.Forms.TrackBar();
            this._numWidth = new System.Windows.Forms.NumericUpDown();
            this._numHeight = new System.Windows.Forms.NumericUpDown();
            this._numOffsetX = new System.Windows.Forms.NumericUpDown();
            this._numOffsetY = new System.Windows.Forms.NumericUpDown();
            this._numFifoCapacity = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this._numExposure)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._trkGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numOffsetX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numOffsetY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numFifoCapacity)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTriggerMode
            // 
            this.lblTriggerMode.AutoSize = true;
            this.lblTriggerMode.Location = new System.Drawing.Point(8, 12);
            this.lblTriggerMode.Name = "lblTriggerMode";
            this.lblTriggerMode.Size = new System.Drawing.Size(86, 24);
            this.lblTriggerMode.TabIndex = 22;
            this.lblTriggerMode.Text = "触发模式:";
            this.lblTriggerMode.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTriggerSource
            // 
            this.lblTriggerSource.AutoSize = true;
            this.lblTriggerSource.Location = new System.Drawing.Point(8, 42);
            this.lblTriggerSource.Name = "lblTriggerSource";
            this.lblTriggerSource.Size = new System.Drawing.Size(68, 24);
            this.lblTriggerSource.TabIndex = 20;
            this.lblTriggerSource.Text = "触发源:";
            this.lblTriggerSource.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTriggerActivation
            // 
            this.lblTriggerActivation.AutoSize = true;
            this.lblTriggerActivation.Location = new System.Drawing.Point(8, 72);
            this.lblTriggerActivation.Name = "lblTriggerActivation";
            this.lblTriggerActivation.Size = new System.Drawing.Size(86, 24);
            this.lblTriggerActivation.TabIndex = 18;
            this.lblTriggerActivation.Text = "触发边沿:";
            this.lblTriggerActivation.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sep1
            // 
            this.sep1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.sep1.Location = new System.Drawing.Point(8, 106);
            this.sep1.Name = "sep1";
            this.sep1.Size = new System.Drawing.Size(460, 1);
            this.sep1.TabIndex = 17;
            // 
            // lblExposure
            // 
            this.lblExposure.AutoSize = true;
            this.lblExposure.Location = new System.Drawing.Point(8, 118);
            this.lblExposure.Name = "lblExposure";
            this.lblExposure.Size = new System.Drawing.Size(122, 24);
            this.lblExposure.TabIndex = 15;
            this.lblExposure.Text = "曝光时间 (us):";
            this.lblExposure.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblGain
            // 
            this.lblGain.AutoSize = true;
            this.lblGain.Location = new System.Drawing.Point(8, 148);
            this.lblGain.Name = "lblGain";
            this.lblGain.Size = new System.Drawing.Size(90, 24);
            this.lblGain.TabIndex = 13;
            this.lblGain.Text = "增益 (dB):";
            this.lblGain.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sep2
            // 
            this.sep2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.sep2.Location = new System.Drawing.Point(8, 226);
            this.sep2.Name = "sep2";
            this.sep2.Size = new System.Drawing.Size(460, 1);
            this.sep2.TabIndex = 11;
            // 
            // lblWidth
            // 
            this.lblWidth.AutoSize = true;
            this.lblWidth.Location = new System.Drawing.Point(8, 238);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(50, 24);
            this.lblWidth.TabIndex = 9;
            this.lblWidth.Text = "宽度:";
            this.lblWidth.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHeight
            // 
            this.lblHeight.AutoSize = true;
            this.lblHeight.Location = new System.Drawing.Point(8, 268);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(50, 24);
            this.lblHeight.TabIndex = 7;
            this.lblHeight.Text = "高度:";
            this.lblHeight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblOffsetX
            // 
            this.lblOffsetX.AutoSize = true;
            this.lblOffsetX.Location = new System.Drawing.Point(8, 298);
            this.lblOffsetX.Name = "lblOffsetX";
            this.lblOffsetX.Size = new System.Drawing.Size(67, 24);
            this.lblOffsetX.TabIndex = 5;
            this.lblOffsetX.Text = "偏移 X:";
            this.lblOffsetX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblOffsetY
            // 
            this.lblOffsetY.AutoSize = true;
            this.lblOffsetY.Location = new System.Drawing.Point(8, 328);
            this.lblOffsetY.Name = "lblOffsetY";
            this.lblOffsetY.Size = new System.Drawing.Size(66, 24);
            this.lblOffsetY.TabIndex = 3;
            this.lblOffsetY.Text = "偏移 Y:";
            this.lblOffsetY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sep3
            // 
            this.sep3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.sep3.Location = new System.Drawing.Point(8, 362);
            this.sep3.Name = "sep3";
            this.sep3.Size = new System.Drawing.Size(460, 1);
            this.sep3.TabIndex = 2;
            // 
            // lblFifoCap
            // 
            this.lblFifoCap.AutoSize = true;
            this.lblFifoCap.Location = new System.Drawing.Point(8, 374);
            this.lblFifoCap.Name = "lblFifoCap";
            this.lblFifoCap.Size = new System.Drawing.Size(95, 24);
            this.lblFifoCap.TabIndex = 0;
            this.lblFifoCap.Text = "FIFO 容量:";
            this.lblFifoCap.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _cmbTriggerMode
            // 
            this._cmbTriggerMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbTriggerMode.Items.AddRange(new object[] {
            "连续采集",
            "软件触发",
            "硬件触发"});
            this._cmbTriggerMode.Location = new System.Drawing.Point(180, 10);
            this._cmbTriggerMode.Name = "_cmbTriggerMode";
            this._cmbTriggerMode.Size = new System.Drawing.Size(140, 32);
            this._cmbTriggerMode.TabIndex = 23;
            // 
            // _cmbTriggerSource
            // 
            this._cmbTriggerSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbTriggerSource.Enabled = false;
            this._cmbTriggerSource.Items.AddRange(new object[] {
            "Line0",
            "Line1",
            "Line2",
            "Line3",
            "Software"});
            this._cmbTriggerSource.Location = new System.Drawing.Point(180, 40);
            this._cmbTriggerSource.Name = "_cmbTriggerSource";
            this._cmbTriggerSource.Size = new System.Drawing.Size(140, 32);
            this._cmbTriggerSource.TabIndex = 21;
            // 
            // _cmbTriggerActivation
            // 
            this._cmbTriggerActivation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbTriggerActivation.Enabled = false;
            this._cmbTriggerActivation.Items.AddRange(new object[] {
            "RisingEdge",
            "FallingEdge",
            "LevelHigh",
            "LevelLow"});
            this._cmbTriggerActivation.Location = new System.Drawing.Point(180, 70);
            this._cmbTriggerActivation.Name = "_cmbTriggerActivation";
            this._cmbTriggerActivation.Size = new System.Drawing.Size(140, 32);
            this._cmbTriggerActivation.TabIndex = 19;
            // 
            // _numExposure
            // 
            this._numExposure.Increment = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._numExposure.Location = new System.Drawing.Point(180, 116);
            this._numExposure.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this._numExposure.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this._numExposure.Name = "_numExposure";
            this._numExposure.Size = new System.Drawing.Size(100, 31);
            this._numExposure.TabIndex = 16;
            this._numExposure.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            // 
            // _numGain
            // 
            this._numGain.DecimalPlaces = 2;
            this._numGain.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this._numGain.Location = new System.Drawing.Point(180, 146);
            this._numGain.Maximum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this._numGain.Name = "_numGain";
            this._numGain.Size = new System.Drawing.Size(100, 31);
            this._numGain.TabIndex = 14;
            // 
            // _trkGain
            // 
            this._trkGain.Location = new System.Drawing.Point(180, 176);
            this._trkGain.Maximum = 40;
            this._trkGain.Name = "_trkGain";
            this._trkGain.Size = new System.Drawing.Size(240, 69);
            this._trkGain.TabIndex = 12;
            // 
            // _numWidth
            // 
            this._numWidth.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this._numWidth.Location = new System.Drawing.Point(180, 236);
            this._numWidth.Maximum = new decimal(new int[] {
            32768,
            0,
            0,
            0});
            this._numWidth.Minimum = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this._numWidth.Name = "_numWidth";
            this._numWidth.Size = new System.Drawing.Size(100, 31);
            this._numWidth.TabIndex = 10;
            this._numWidth.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // _numHeight
            // 
            this._numHeight.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this._numHeight.Location = new System.Drawing.Point(180, 266);
            this._numHeight.Maximum = new decimal(new int[] {
            32768,
            0,
            0,
            0});
            this._numHeight.Minimum = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this._numHeight.Name = "_numHeight";
            this._numHeight.Size = new System.Drawing.Size(100, 31);
            this._numHeight.TabIndex = 8;
            this._numHeight.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // _numOffsetX
            // 
            this._numOffsetX.Increment = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this._numOffsetX.Location = new System.Drawing.Point(180, 296);
            this._numOffsetX.Maximum = new decimal(new int[] {
            32768,
            0,
            0,
            0});
            this._numOffsetX.Name = "_numOffsetX";
            this._numOffsetX.Size = new System.Drawing.Size(100, 31);
            this._numOffsetX.TabIndex = 6;
            // 
            // _numOffsetY
            // 
            this._numOffsetY.Increment = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this._numOffsetY.Location = new System.Drawing.Point(180, 326);
            this._numOffsetY.Maximum = new decimal(new int[] {
            32768,
            0,
            0,
            0});
            this._numOffsetY.Name = "_numOffsetY";
            this._numOffsetY.Size = new System.Drawing.Size(100, 31);
            this._numOffsetY.TabIndex = 4;
            // 
            // _numFifoCapacity
            // 
            this._numFifoCapacity.Location = new System.Drawing.Point(180, 372);
            this._numFifoCapacity.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._numFifoCapacity.Name = "_numFifoCapacity";
            this._numFifoCapacity.Size = new System.Drawing.Size(100, 31);
            this._numFifoCapacity.TabIndex = 1;
            this._numFifoCapacity.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // CameraSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblFifoCap);
            this.Controls.Add(this._numFifoCapacity);
            this.Controls.Add(this.sep3);
            this.Controls.Add(this.lblOffsetY);
            this.Controls.Add(this._numOffsetY);
            this.Controls.Add(this.lblOffsetX);
            this.Controls.Add(this._numOffsetX);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this._numHeight);
            this.Controls.Add(this.lblWidth);
            this.Controls.Add(this._numWidth);
            this.Controls.Add(this.sep2);
            this.Controls.Add(this._trkGain);
            this.Controls.Add(this.lblGain);
            this.Controls.Add(this._numGain);
            this.Controls.Add(this.lblExposure);
            this.Controls.Add(this._numExposure);
            this.Controls.Add(this.sep1);
            this.Controls.Add(this.lblTriggerActivation);
            this.Controls.Add(this._cmbTriggerActivation);
            this.Controls.Add(this.lblTriggerSource);
            this.Controls.Add(this._cmbTriggerSource);
            this.Controls.Add(this.lblTriggerMode);
            this.Controls.Add(this._cmbTriggerMode);
            this.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.Name = "CameraSettingsControl";
            this.Size = new System.Drawing.Size(532, 458);
            ((System.ComponentModel.ISupportInitialize)(this._numExposure)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._trkGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numOffsetX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numOffsetY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numFifoCapacity)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

            }
        }
