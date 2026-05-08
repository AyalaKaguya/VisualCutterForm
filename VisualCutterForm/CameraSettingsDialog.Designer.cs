using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualCutterForm
{
    partial class CameraSettingsDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TreeView _treeView;
        private Panel _contentPanel;
        private SplitContainer _splitContainer;
        private Panel _panelInfo;
        private Panel _panelTrigger;
        private Panel _panelExposure;
        private Panel _panelRoi;
        private Panel _panelDebug;
        private PictureBox _previewBox;
        private Timer _previewTimer;
        private Label _lblModel;
        private Label _lblSerial;
        private Label _lblTransport;
        private Label _lblVersion;
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
        private Button _btnOk;
        private Button _btnDebugSnap;
        private Button _btnDebugContinuous;
        private Button _btnDebugSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private Panel CreatePanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                AutoScroll = true,
            };
        }

        private static void AddLabelValue(Panel panel, string labelText, ref int y,
            out Label valueLabel, int valueWidth)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(12, y + 2),
                Size = new Size(70, 20),
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
            };
            valueLabel = new Label
            {
                Location = new Point(88, y + 2),
                Size = new Size(valueWidth, 20),
                Font = new Font("Microsoft YaHei", 9F),
                AutoSize = false,
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(valueLabel);
            y += 28;
        }

        private static void AddNumeric(Panel panel, string labelText, ref int y,
            out NumericUpDown numeric, int min, int max, int value, int increment,
            int decimalPlaces = 0)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(12, y + 4),
                Size = new Size(160, 22),
                Font = new Font("Microsoft YaHei", 9F),
                TextAlign = ContentAlignment.MiddleRight,
            };
            numeric = new NumericUpDown
            {
                Location = new Point(184, y + 2),
                Size = new Size(100, 22),
                Minimum = min,
                Maximum = max,
                Value = Clamp(value, min, max),
                Increment = increment,
                DecimalPlaces = decimalPlaces,
                Font = new Font("Microsoft YaHei", 9F),
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(numeric);
            y += 30;
        }

        private static void AddCombo(Panel panel, string labelText, ref int y,
            out ComboBox comboBox, string[] items, int defaultIndex)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(12, y + 4),
                Size = new Size(160, 22),
                Font = new Font("Microsoft YaHei", 9F),
                TextAlign = ContentAlignment.MiddleRight,
            };
            comboBox = new ComboBox
            {
                Location = new Point(184, y + 2),
                Size = new Size(140, 22),
                Font = new Font("Microsoft YaHei", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
            };
            comboBox.Items.AddRange(items);
            if (defaultIndex >= 0 && defaultIndex < items.Length)
                comboBox.SelectedIndex = defaultIndex;

            panel.Controls.Add(lbl);
            panel.Controls.Add(comboBox);
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
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "CameraSettingsDialog";
            _splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 160,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
            };

            _treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                Font = new Font("Microsoft YaHei", 10F),
            };

            _treeView.Nodes.Add(new TreeNode("相机信息", 0, 0));
            _treeView.Nodes.Add(new TreeNode("触发器", 1, 1));
            _treeView.Nodes.Add(new TreeNode("曝光", 2, 2));
            _treeView.Nodes.Add(new TreeNode("图像尺寸/ROI", 3, 3));
            _treeView.Nodes.Add(new TreeNode("调试画面", 4, 4));

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                AutoScroll = true,
            };

            _splitContainer.Panel1.Controls.Add(_treeView);
            _splitContainer.Panel2.Controls.Add(_contentPanel);
            Controls.Add(_splitContainer);

            // Build info panel
            _panelInfo = CreatePanel();
            var y = 12;
            AddLabelValue(_panelInfo, "型号:", ref y, out _lblModel, 260);
            AddLabelValue(_panelInfo, "序列号:", ref y, out _lblSerial, 260);
            AddLabelValue(_panelInfo, "传输类型:", ref y, out _lblTransport, 260);
            AddLabelValue(_panelInfo, "固件版本:", ref y, out _lblVersion, 260);

            // Build trigger panel
            _panelTrigger = CreatePanel();
            y = 12;
            _cmbTriggerMode = new ComboBox
            {
                Location = new Point(184, y + 2),
                Size = new Size(140, 22),
                Font = new Font("Microsoft YaHei", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _cmbTriggerMode.Items.AddRange(new[] { "连续采集", "软件触发", "硬件触发" });
            _panelTrigger.Controls.Add(new Label
            {
                Text = "触发模式:",
                Location = new Point(12, y + 4),
                Size = new Size(160, 22),
                Font = new Font("Microsoft YaHei", 9F),
                TextAlign = ContentAlignment.MiddleRight,
            });
            _panelTrigger.Controls.Add(_cmbTriggerMode);
            y += 36;

            AddCombo(_panelTrigger, "触发源:", ref y, out _cmbTriggerSource,
                new[] { "Line0", "Line1", "Line2", "Line3", "Software" }, 0);
            AddCombo(_panelTrigger, "触发边沿:", ref y, out _cmbTriggerActivation,
                new[] { "RisingEdge", "FallingEdge", "LevelHigh", "LevelLow" }, 0);

            // Build exposure panel
            _panelExposure = CreatePanel();
            y = 12;
            AddNumeric(_panelExposure, "曝光时间 (us):", ref y, out _numExposure, 100, 10000000, 5000, 500);
            AddNumeric(_panelExposure, "增益 (dB):", ref y, out _numGain, 0, 40, 0, 1, 2);

            _trkGain = new TrackBar
            {
                Location = new Point(184, y),
                Size = new Size(220, 45),
                Minimum = 0,
                Maximum = 40,
                Value = 0,
            };
            _panelExposure.Controls.Add(_trkGain);
            y += 50;

            // Build ROI panel
            _panelRoi = CreatePanel();
            y = 12;
            AddNumeric(_panelRoi, "宽度:", ref y, out _numWidth, 64, 32768, 0, 8);
            AddNumeric(_panelRoi, "高度:", ref y, out _numHeight, 64, 32768, 0, 8);
            AddNumeric(_panelRoi, "偏移 X:", ref y, out _numOffsetX, 0, 32768, 0, 4);
            AddNumeric(_panelRoi, "偏移 Y:", ref y, out _numOffsetY, 0, 32768, 0, 4);
            y += 12;
            AddNumeric(_panelRoi, "FIFO 容量:", ref y, out _numFifoCapacity, 1, 100, 10, 1);

            // Build debug panel
            _panelDebug = CreatePanel();
            _panelDebug.AutoScroll = false;

            var toolbar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(600, 34),
                BackColor = Color.FromArgb(40, 40, 40),
            };

            _btnDebugSnap = new Button
            {
                Text = "单帧触发",
                Location = new Point(4, 4),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            _btnDebugSnap.FlatAppearance.BorderSize = 0;

            _btnDebugContinuous = new Button
            {
                Text = "连续采集",
                Location = new Point(88, 4),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei", 8.5F),
                Name = "btnContinuous",
            };
            _btnDebugContinuous.FlatAppearance.BorderSize = 0;

            _btnDebugSave = new Button
            {
                Text = "保存图片",
                Location = new Point(172, 4),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            _btnDebugSave.FlatAppearance.BorderSize = 0;

            toolbar.Controls.Add(_btnDebugSnap);
            toolbar.Controls.Add(_btnDebugContinuous);
            toolbar.Controls.Add(_btnDebugSave);

            _previewBox = new PictureBox
            {
                Location = new Point(0, 34),
                Size = new Size(600, 360),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(30, 30, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };

            var lblStatus = new Label
            {
                Text = "无相机连接",
                Location = new Point(0, 398),
                Size = new Size(600, 22),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.FromArgb(35, 35, 35),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 8.5F),
                Name = "lblDebugStatus",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };

            _panelDebug.Controls.Add(lblStatus);
            _panelDebug.Controls.Add(_previewBox);
            _panelDebug.Controls.Add(toolbar);

            _panelDebug.Resize += (s, ev) =>
            {
                toolbar.Width = _panelDebug.ClientSize.Width;
                lblStatus.Width = _panelDebug.ClientSize.Width;
                lblStatus.Top = _panelDebug.ClientSize.Height - 22;
                _previewBox.Size = new Size(_panelDebug.ClientSize.Width, _panelDebug.ClientSize.Height - 34 - 22);
            };

            _previewTimer = new Timer { Interval = 50 };

            _contentPanel.Controls.Add(_panelInfo);
            _contentPanel.Controls.Add(_panelTrigger);
            _contentPanel.Controls.Add(_panelExposure);
            _contentPanel.Controls.Add(_panelRoi);
            _contentPanel.Controls.Add(_panelDebug);

            _treeView.SelectedNode = _treeView.Nodes[0];

            _btnOk = new Button { Text = "确定", DialogResult = DialogResult.OK, Size = new Size(80, 30) };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Size = new Size(80, 30) };

            var flowLayout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(0, 6, 12, 6),
            };
            flowLayout.Controls.Add(btnCancel);
            flowLayout.Controls.Add(_btnOk);
            Controls.Add(flowLayout);
            this.ResumeLayout(false);
        }
    }
}
