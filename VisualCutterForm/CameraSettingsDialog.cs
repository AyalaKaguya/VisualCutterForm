using VisualMaster.Api;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using VisualCutterForm.Lib;

namespace VisualCutterForm
{
    public class CameraSettingsDialog : Form
    {
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
        private VisionController _vision;
        private string _cameraSerial;

        private Label _lblModel;
        private Label _lblSerial;
        private Label _lblTransport;
        private Label _lblVersion;

        private CheckBox _chkTriggerEnabled;
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

        private CameraSettings _settings;
        private CameraInfo _cameraInfo;
        private bool _isReadOnly;

        public CameraSettings Settings => _settings;

        public CameraSettingsDialog(CameraSettings settings, CameraInfo cameraInfo = null,
            bool readOnly = false, VisionController vision = null, string cameraSerial = null)
        {
            _settings = settings?.Clone() as CameraSettings ?? new CameraSettings();
            _cameraInfo = cameraInfo;
            _isReadOnly = readOnly;
            _vision = vision;
            _cameraSerial = cameraSerial;

            InitializeForm();
            PopulateCameraInfo();
            PopulateFromSettings();
        }

        private void InitializeForm()
        {
            Text = _isReadOnly ? "相机设置 (只读)" : "相机设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 520);

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
            _treeView.AfterSelect += OnTreeViewAfterSelect;

            _treeView.Nodes.Add(CreateTreeNode("相机信息", 0));
            _treeView.Nodes.Add(CreateTreeNode("触发器", 1));
            _treeView.Nodes.Add(CreateTreeNode("曝光", 2));
            _treeView.Nodes.Add(CreateTreeNode("图像尺寸/ROI", 3));
            _treeView.Nodes.Add(CreateTreeNode("调试画面", 4));

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                AutoScroll = true,
            };

            _splitContainer.Panel1.Controls.Add(_treeView);
            _splitContainer.Panel2.Controls.Add(_contentPanel);
            Controls.Add(_splitContainer);

            BuildInfoPanel();
            _contentPanel.Controls.Add(_panelInfo);
            BuildTriggerPanel();
            _contentPanel.Controls.Add(_panelTrigger);
            BuildExposurePanel();
            _contentPanel.Controls.Add(_panelExposure);
            BuildRoiPanel();
            _contentPanel.Controls.Add(_panelRoi);
            BuildDebugPanel();
            _contentPanel.Controls.Add(_panelDebug);

            _treeView.SelectedNode = _treeView.Nodes[0];

            var btnOk = new Button { Text = "确定", DialogResult = DialogResult.OK, Size = new Size(80, 30) };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Size = new Size(80, 30) };

            btnOk.Click += (s, e) =>
            {
                if (!_isReadOnly)
                {
                    CollectToSettings();
                    DialogResult = DialogResult.OK;
                }
            };

            var flowLayout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(0, 6, 12, 6),
            };
            flowLayout.Controls.Add(btnCancel);
            flowLayout.Controls.Add(btnOk);
            Controls.Add(flowLayout);

            FormClosing += (s, e) =>
            {
                StopDebugPreview();
                _previewTimer?.Dispose();
                _previewTimer = null;
            };
        }

        private static TreeNode CreateTreeNode(string text, int imageIndex)
        {
            return new TreeNode(text, imageIndex, imageIndex);
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

        private void BuildInfoPanel()
        {
            _panelInfo = CreatePanel();
            var y = 12;

            AddLabelValue(_panelInfo, "型号:", ref y, out _lblModel, 260);
            AddLabelValue(_panelInfo, "序列号:", ref y, out _lblSerial, 260);
            AddLabelValue(_panelInfo, "传输类型:", ref y, out _lblTransport, 260);
            AddLabelValue(_panelInfo, "固件版本:", ref y, out _lblVersion, 260);
        }

        private void BuildTriggerPanel()
        {
            _panelTrigger = CreatePanel();
            var y = 12;

            _chkTriggerEnabled = new CheckBox
            {
                Text = "启用外部触发",
                Location = new Point(12, y),
                Size = new Size(200, 24),
                Font = new Font("Microsoft YaHei", 9.5F),
                AutoSize = true,
            };
            _chkTriggerEnabled.CheckedChanged += (s, e) =>
            {
                _cmbTriggerSource.Enabled = _chkTriggerEnabled.Checked;
                _cmbTriggerActivation.Enabled = _chkTriggerEnabled.Checked;
            };
            _panelTrigger.Controls.Add(_chkTriggerEnabled);
            y += 36;

            AddCombo(_panelTrigger, "触发源:", ref y, out _cmbTriggerSource,
                new[] { "Line0", "Line1", "Line2", "Line3", "Software" }, 4);
            AddCombo(_panelTrigger, "触发边沿:", ref y, out _cmbTriggerActivation,
                new[] { "RisingEdge", "FallingEdge", "LevelHigh", "LevelLow" }, 1);
        }

        private void BuildExposurePanel()
        {
            _panelExposure = CreatePanel();
            var y = 12;

            AddNumeric(_panelExposure, "曝光时间 (us):", ref y, out _numExposure,
                100, 10000000, 5000, 500);
            AddNumeric(_panelExposure, "增益 (dB):", ref y, out _numGain,
                0, 40, 0, 1, 2);

            _trkGain = new TrackBar
            {
                Location = new Point(184, y),
                Size = new Size(220, 45),
                Minimum = 0,
                Maximum = 40,
                Value = 0,
            };
            _trkGain.ValueChanged += (s, e) =>
            {
                _numGain.Value = _trkGain.Value;
            };
            _numGain.ValueChanged += (s, e) =>
            {
                _trkGain.Value = (int)_numGain.Value;
            };
            _panelExposure.Controls.Add(_trkGain);
            y += 50;
        }

        private void BuildRoiPanel()
        {
            _panelRoi = CreatePanel();
            var y = 12;

            AddNumeric(_panelRoi, "宽度:", ref y, out _numWidth,
                64, 32768, 0, 8);
            AddNumeric(_panelRoi, "高度:", ref y, out _numHeight,
                64, 32768, 0, 8);
            AddNumeric(_panelRoi, "偏移 X:", ref y, out _numOffsetX,
                0, 32768, 0, 4);
            AddNumeric(_panelRoi, "偏移 Y:", ref y, out _numOffsetY,
                0, 32768, 0, 4);
            y += 12;
            AddNumeric(_panelRoi, "FIFO 容量:", ref y, out _numFifoCapacity,
                1, 100, 10, 1);
        }

        private void BuildDebugPanel()
        {
            _panelDebug = CreatePanel();
            _panelDebug.AutoScroll = false;

            var toolbar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(600, 34),
                BackColor = Color.FromArgb(40, 40, 40),
            };

            var btnSnap = new Button
            {
                Text = "单帧触发",
                Location = new Point(4, 4),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            btnSnap.FlatAppearance.BorderSize = 0;
            btnSnap.Click += OnSnapClick;

            var btnContinuous = new Button
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
            btnContinuous.FlatAppearance.BorderSize = 0;
            btnContinuous.Click += OnContinuousClick;

            var btnSave = new Button
            {
                Text = "保存图片",
                Location = new Point(172, 4),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += OnSaveClick;

            toolbar.Controls.Add(btnSnap);
            toolbar.Controls.Add(btnContinuous);
            toolbar.Controls.Add(btnSave);

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
                _previewBox.Size = new Size(
                    _panelDebug.ClientSize.Width,
                    _panelDebug.ClientSize.Height - 34 - 22);
            };

            _previewTimer = new Timer { Interval = 50 };
            _previewTimer.Tick += OnPreviewTick;
        }

        private void OnSnapClick(object sender, EventArgs e)
        {
            if (_vision == null || string.IsNullOrEmpty(_cameraSerial)) return;

            try
            {
                if (_vision.Slots.TryGetValue(_cameraSerial, out var slot))
                {
                    var bmp = slot.Camera.TryGrabImage(out var grabbed, 2000)
                        ? grabbed : null;

                    if (bmp != null)
                    {
                        var old = _previewBox.Image;
                        _previewBox.Image = bmp;
                        old?.Dispose();

                        var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                        if (lbl.Length > 0) lbl[0].Text = $"单帧触发 · {bmp.Width}x{bmp.Height}";
                    }
                    else
                    {
                        var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                        if (lbl.Length > 0) lbl[0].Text = "单帧触发超时";
                    }
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
            if (_vision == null || string.IsNullOrEmpty(_cameraSerial)) return;

            var btn = sender as Button;
            if (btn == null) return;

            if (!_vision.Slots.TryGetValue(_cameraSerial, out var slot)) return;

            if (slot.IsGrabbing)
            {
                _vision.StopAcquisition(_cameraSerial);
                btn.Text = "连续采集";
                btn.BackColor = Color.FromArgb(60, 60, 60);
            }
            else
            {
                _vision.StartAcquisition(_cameraSerial);
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
                        if (lbl.Length > 0) lbl[0].Text = $"已保存: {System.IO.Path.GetFileName(dlg.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                        if (lbl.Length > 0) lbl[0].Text = $"保存失败: {ex.Message}";
                    }
                }
            }
        }

        private void StartDebugPreview()
        {
            if (_previewTimer == null) return;

            var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
            if (lbl.Length > 0) lbl[0].Text = "正在连接...";

            _previewTimer.Start();
        }

        private void StopDebugPreview()
        {
            _previewTimer?.Stop();
        }

        private void OnPreviewTick(object sender, EventArgs e)
        {
            if (_vision == null || string.IsNullOrEmpty(_cameraSerial))
            {
                var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                if (lbl.Length > 0) lbl[0].Text = "无相机连接";
                return;
            }

            try
            {
                bool grabbing = _vision.Slots.TryGetValue(_cameraSerial, out var slot) && slot.IsGrabbing;

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

                var bmp = _vision.PeekLatestFromFifo(_cameraSerial);
                if (bmp != null)
                {
                    var old = _previewBox.Image;
                    _previewBox.Image = bmp;
                    old?.Dispose();
                }

                var lbl = _panelDebug?.Controls.Find("lblDebugStatus", true);
                if (lbl.Length > 0)
                {
                    lbl[0].Text = grabbing
                        ? $"连续采集 · {_previewBox.Image?.Width ?? 0}x{_previewBox.Image?.Height ?? 0}"
                        : "采集已停止";
                }
            }
            catch
            {
                // ignore preview errors
            }
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

        private void PopulateFromSettings()
        {
            _chkTriggerEnabled.Checked = _settings.TriggerEnabled;
            _cmbTriggerSource.Text = _settings.TriggerSource ?? "Line0";
            _cmbTriggerActivation.Text = _settings.TriggerActivation ?? "RisingEdge";
            _cmbTriggerSource.Enabled = _settings.TriggerEnabled;
            _cmbTriggerActivation.Enabled = _settings.TriggerEnabled;

            SetNumSafe(_numExposure, (decimal)_settings.ExposureTimeUs);
            SetNumSafe(_numGain, (decimal)_settings.Gain);
            _trkGain.Value = Math.Max(_trkGain.Minimum, Math.Min(_trkGain.Maximum, (int)_settings.Gain));

            SetNumSafe(_numWidth, _settings.Width);
            SetNumSafe(_numHeight, _settings.Height);
            SetNumSafe(_numOffsetX, _settings.OffsetX);
            SetNumSafe(_numOffsetY, _settings.OffsetY);
            SetNumSafe(_numFifoCapacity, _settings.FifiCapacity);

            if (_isReadOnly)
            {
                _chkTriggerEnabled.Enabled = false;
                _cmbTriggerSource.Enabled = false;
                _cmbTriggerActivation.Enabled = false;
                _numExposure.Enabled = false;
                _numGain.Enabled = false;
                _trkGain.Enabled = false;
                _numWidth.Enabled = false;
                _numHeight.Enabled = false;
                _numOffsetX.Enabled = false;
                _numOffsetY.Enabled = false;
                _numFifoCapacity.Enabled = false;
            }
        }

        private void CollectToSettings()
        {
            _settings.TriggerEnabled = _chkTriggerEnabled.Checked;
            _settings.TriggerSource = _cmbTriggerSource.Text;
            _settings.TriggerActivation = _cmbTriggerActivation.Text;
            _settings.ExposureTimeUs = (float)_numExposure.Value;
            _settings.Gain = (float)_numGain.Value;
            _settings.Width = (int)_numWidth.Value;
            _settings.Height = (int)_numHeight.Value;
            _settings.OffsetX = (int)_numOffsetX.Value;
            _settings.OffsetY = (int)_numOffsetY.Value;
            _settings.FifiCapacity = (int)_numFifoCapacity.Value;
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

        private static void SetNumSafe(NumericUpDown num, decimal value)
        {
            num.Value = Clamp(value, num.Minimum, num.Maximum);
        }
    }
}
