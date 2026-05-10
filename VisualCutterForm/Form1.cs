using VisualMaster.WorkFlow;
using VisualMaster.Api;
using VisualMaster.CameraLink;
using VisualMaster.Communication;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisualMaster.Forms;
using VisualMaster.Forms.Camera;
using VisualMaster.Forms.FlowEditor;
using VisualMaster.WorkFlow.Nodes;

namespace VisualCutterForm
{
    public partial class Form1 : Form
    {
        private MenuStrip _menuStrip;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ImageViewer _previewBox;
        private ToolStripMenuItem _miViewLog;
        private ToolStripMenuItem _miFlowOpen;
        private ToolStripMenuItem _miFlowReload;
        private ToolStripMenuItem _miFlowClose;
        private ToolStripMenuItem _miFlowEditor;
        private ToolStripMenuItem _miLogin;
        private ToolStripMenuItem _miDefaultLogin;
        private ToolStripMenuItem _miAutoStart;

        private VisionController _vision;
        private AppConfig _config;
        private string _selectedCamera;
        private FlowGraph _flowGraph;
        private FlowExecutor _flowExecutor;
        private RichTextBox _logBox;
        private SplitContainer _mainSplit;
        private ComboBox _cameraComboBox;
        private ComboBox _cmbSubGraph;
        private ComboBox _cmbDisplayNode;
        private System.Windows.Forms.Timer _previewTimer;
        private bool _suppressViewSave;
        private object _lastCameraFrameRef;
        private UserRole _currentRole = UserRole.None;

        public Form1()
        {
            InitializeComponent();
            InitializeApp();
        }

        private void InitializeApp()
        {
            Text = "VisualCutter - 视觉裁切系统";

            var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            _config = AppConfig.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);

            _vision = new VisionController();
            _vision.StatusChanged += OnStatusChanged;
            _vision.ErrorOccurred += OnErrorOccurred;

            BuildMenuStrip();
            BuildStatusStrip();
            BuildPreviewArea();

            _vision.Initialize();

            _flowExecutor = new FlowExecutor(_vision);
            _flowExecutor.LogMessage += (s, msg) => AppendLog(msg, Color.FromArgb(180, 180, 180));
            _flowExecutor.ExecutionError += (s, ex) => AppendLog(ex.Message, Color.FromArgb(231, 76, 60));

            UpdateRoleRestrictions();
            UpdateMenuState();

            if (_config.DefaultRole != UserRole.None)
            {
                _currentRole = _config.DefaultRole;
                UpdateRoleRestrictions();
                UpdateMenuState();

                if (!string.IsNullOrEmpty(_config.FlowFilePath) && File.Exists(_config.FlowFilePath))
                {
                    LoadFlowFile(_config.FlowFilePath);
                }

                OnStatusChanged(this, $"默认登录: {RoleDisplayName()}");
            }
            else
            {
                OnStatusChanged(this, "未登录 - 请点击 登录 菜单");
            }
        }

        #region Layout

        private void BuildMenuStrip()
        {
            _menuStrip = new MenuStrip { Font = new Font("Microsoft YaHei", 9F) };

            var miFile = new ToolStripMenuItem("文件(&F)");
            miFile.DropDownItems.Add("加载配置(&L)", null, (s, e) =>
            {
                _config.Reload();
                OnStatusChanged(this, "配置已加载");
            });
            miFile.DropDownItems.Add("保存配置(&S)", null, (s, e) =>
            {
                _config.Save();
                OnStatusChanged(this, "配置已保存");
            });
            miFile.DropDownItems.Add(new ToolStripSeparator());
            miFile.DropDownItems.Add("退出(&X)", null, (s, e) => Close());

            var miFlow = new ToolStripMenuItem("流程(&F)");
            _miFlowOpen = new ToolStripMenuItem("打开流程文件...", null, (s, e) => OpenFlowFile());
            _miFlowReload = new ToolStripMenuItem("重新加载", null, (s, e) =>
            {
                if (!string.IsNullOrEmpty(_config.FlowFilePath) && File.Exists(_config.FlowFilePath))
                    LoadFlowFile(_config.FlowFilePath);
                else
                    OnStatusChanged(this, "无已加载的流程文件");
            });
            _miFlowClose = new ToolStripMenuItem("关闭流程", null, (s, e) =>
            {
                _flowGraph = null;
                _flowExecutor?.Stop();
                _config.FlowFilePath = "";
                _config.Save();
                RebuildViewSelector();
                OnStatusChanged(this, "流程已关闭");
            });
            _miFlowEditor = new ToolStripMenuItem("流程图编辑器...", null, (s, e) =>
            {
                using (var editor = new FlowEditorForm(_flowExecutor, _flowGraph, _vision))
                {
                    if (_flowGraph != null)
                        editor.LoadGraphData(_flowGraph);
                    editor.ShowDialog(this);
                    if (_flowGraph != null)
                    {
                        _flowExecutor.LoadGraph(_flowGraph);
                        _flowExecutor.Start();
                    }
                    RebuildViewSelector();
                }
            });
            var miFlowRunSub = new ToolStripMenuItem("执行子图");
            miFlowRunSub.DropDownOpening += (s, e) => RebuildFlowRunMenu(miFlowRunSub);

            miFlow.DropDownItems.Add(_miFlowOpen);
            miFlow.DropDownItems.Add(_miFlowReload);
            miFlow.DropDownItems.Add(_miFlowClose);
            miFlow.DropDownItems.Add(new ToolStripSeparator());
            miFlow.DropDownItems.Add(_miFlowEditor);
            miFlow.DropDownItems.Add(new ToolStripSeparator());
            miFlow.DropDownItems.Add(miFlowRunSub);

            var miView = new ToolStripMenuItem("视图(&V)");
            _miViewLog = new ToolStripMenuItem("流程日志", null, (s, e) =>
            {
                _miViewLog.Checked = !_miViewLog.Checked;
                _mainSplit.Panel2Collapsed = !_miViewLog.Checked;
            });
            miView.DropDownItems.Add(_miViewLog);

            var miHelp = new ToolStripMenuItem("帮助(&H)");
            miHelp.DropDownItems.Add("诊断信息(&D)", null, (s, e) => ShowDiagnostics());
            miHelp.DropDownItems.Add("关于(&A)", null, (s, e) => ShowAbout());

            var miLogin = new ToolStripMenuItem("登录(&L)");
            miLogin.DropDownItems.Add("登录/切换角色...", null, (s, e) => ShowLogin());
            _miDefaultLogin = new ToolStripMenuItem("设置默认登录...", null, (s, e) => ShowDefaultLogin());
            miLogin.DropDownItems.Add(_miDefaultLogin);
            miLogin.DropDownItems.Add(new ToolStripSeparator());
            _miAutoStart = new ToolStripMenuItem("开机自启动", null, (s, e) => ToggleAutoStart())
            {
                Checked = _config.AutoStart,
            };
            miLogin.DropDownItems.Add(_miAutoStart);

            _miLogin = miLogin;

            _menuStrip.Items.Add(miFile);
            _menuStrip.Items.Add(miFlow);
            _menuStrip.Items.Add(miView);
            _menuStrip.Items.Add(miLogin);
            _menuStrip.Items.Add(miHelp);

            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;
        }

        private void BuildStatusStrip()
        {
            _statusStrip = new StatusStrip { Font = new Font("Microsoft YaHei", 9F) };
            _statusLabel = new ToolStripStatusLabel { Text = "就绪" };
            _statusStrip.Items.Add(_statusLabel);
            Controls.Add(_statusStrip);
        }

        private void BuildPreviewArea()
        {
            var selectorBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(45, 45, 45),
            };

            var lblView = new Label
            {
                Text = "查看:",
                Location = new Point(6, 7),
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Microsoft YaHei", 9F),
                AutoSize = true,
            };

            _cmbSubGraph = new ComboBox
            {
                Location = new Point(48, 4),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9F),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };

            _cmbDisplayNode = new ComboBox
            {
                Location = new Point(204, 4),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9F),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };

            _cmbSubGraph.SelectedIndexChanged += OnSourceSelectionChanged;
            _cmbDisplayNode.SelectedIndexChanged += OnDisplayNodeChanged;

            selectorBar.Controls.Add(lblView);
            selectorBar.Controls.Add(_cmbSubGraph);
            selectorBar.Controls.Add(_cmbDisplayNode);

            _cameraComboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9F),
                Height = 26,
                Visible = false,
            };
            _cameraComboBox.SelectedIndexChanged += (s, e) =>
            {
                _selectedCamera = _cameraComboBox.SelectedItem as string;
                UpdateMenuState();
            };

            _previewBox = new ImageViewer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
            };

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None,
                WordWrap = false,
            };

            var logHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.FromArgb(45, 45, 45),
            };
            var lblLog = new Label
            {
                Text = "流程日志",
                Location = new Point(8, 3),
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Microsoft YaHei", 8F),
                AutoSize = true,
            };
            var btnClearLog = new Button
            {
                Text = "清除",
                Location = new Point(100, 2),
                Size = new Size(50, 20),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei", 8F),
            };
            btnClearLog.FlatAppearance.BorderSize = 0;
            btnClearLog.Click += (s, e) => _logBox.Clear();
            logHeader.Controls.Add(lblLog);
            logHeader.Controls.Add(btnClearLog);

            var logPanel = new Panel { Dock = DockStyle.Fill };
            logPanel.Controls.Add(_logBox);
            logPanel.Controls.Add(logHeader);

            var previewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4),
            };
            previewContainer.Controls.Add(_previewBox);
            previewContainer.Controls.Add(selectorBar);
            previewContainer.Controls.Add(_cameraComboBox);

            _mainSplit = new SplitContainer
            {
                Orientation = Orientation.Horizontal,
                SplitterDistance = 650,
            };
            _mainSplit.Panel1.Controls.Add(previewContainer);
            _mainSplit.Panel2.Controls.Add(logPanel);
            _mainSplit.Panel2Collapsed = true;

            Controls.Add(_mainSplit);

            _previewTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _previewTimer.Tick += OnPreviewTimerTick;
            _previewTimer.Start();

            RebuildViewSelector();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _menuStrip.Location = new Point(0, 0);
            _menuStrip.Width = ClientSize.Width;
            PositionMainSplit();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_menuStrip != null)
                _menuStrip.Width = ClientSize.Width;
            PositionMainSplit();
        }

        private void PositionMainSplit()
        {
            if (_mainSplit == null) return;
            int menuBottom = _menuStrip?.Bottom ?? 0;
            int statusHeight = _statusStrip?.Height ?? 0;
            _mainSplit.Left = 0;
            _mainSplit.Top = menuBottom;
            _mainSplit.Width = ClientSize.Width;
            _mainSplit.Height = ClientSize.Height - menuBottom - statusHeight;
        }

        #endregion

        #region Menu Handlers

        private void OpenOrAssignCamera(int index)
        {
            try
            {
                var info = _vision.CameraManager.Cameras[index];
                var slot = _vision.CameraManager.Slots.Count > 0 ? _vision.CameraManager.Slots[0] : null;
                if (slot == null)
                {
                    slot = _vision.AddSlot($"相机1");
                }

                _vision.OpenSlot(slot.SlotId, info);
                RefreshCameraComboBox();
                UpdateMenuState();
                OnStatusChanged(this, $"相机已连接: {info}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开相机失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshCameraComboBox()
        {
            _cameraComboBox.Items.Clear();
            foreach (var slot in _vision.CameraManager.Slots)
            {
                _cameraComboBox.Items.Add(slot.SlotId);
            }
            if (_cameraComboBox.Items.Count > 0)
                _cameraComboBox.SelectedIndex = 0;
        }

        private void RebuildFlowRunMenu(ToolStripMenuItem miRun)
        {
            miRun.DropDownItems.Clear();

            if (_flowGraph == null || _flowGraph.SubGraphs.Count == 0)
            {
                miRun.DropDownItems.Add(new ToolStripMenuItem("(未加载流程)") { Enabled = false });
                return;
            }

            var canRun = _currentRole >= UserRole.Operator;

            if (_flowGraph.SubGraphs.Count > 1)
            {
                var miAll = new ToolStripMenuItem("\u25b6 执行全部 (手动触发)", null, async (s, e) =>
                {
                    _flowExecutor.LoadGraph(_flowGraph);
                    _flowExecutor.Start();
                    OnStatusChanged(this, "流程已启动");
                    foreach (var sg in _flowGraph.SubGraphs.Where(sg2 => sg2.Trigger == SubGraphTrigger.SoftManualTrigger))
                    {
                        await _flowExecutor.TriggerSubGraph(sg.Id);
                    }
                    OnStatusChanged(this, "流程执行完毕");
                });
                miAll.Enabled = canRun;
                miRun.DropDownItems.Add(miAll);
                miRun.DropDownItems.Add(new ToolStripSeparator());
            }

            foreach (var sg in _flowGraph.SubGraphs)
            {
                var sgCaptured = sg;
                var label = $"{sgCaptured.Name} ({sgCaptured.Trigger.ToDisplayName()})";
                var miItem = new ToolStripMenuItem(label, null, async (s, e) =>
                {
                    _flowExecutor.LoadGraph(_flowGraph);
                    _flowExecutor.Start();
                    OnStatusChanged(this, $"执行子图: {sgCaptured.Name}");
                    await _flowExecutor.TriggerSubGraph(sgCaptured.Id);
                    OnStatusChanged(this, $"子图 [{sgCaptured.Name}] 执行完毕");
                });
                miItem.Enabled = canRun;
                miRun.DropDownItems.Add(miItem);
            }
        }

        #endregion

        #region Preview

        private void RebuildViewSelector()
        {
            _suppressViewSave = true;

            var prevSubGraph = _cmbSubGraph.SelectedItem?.ToString();
            var prevNode = _cmbDisplayNode.SelectedItem?.ToString();

            _cmbSubGraph.Items.Clear();
            _cmbSubGraph.Items.Add("相机实时");
            if (_flowGraph != null)
            {
                foreach (var sg in _flowGraph.SubGraphs)
                    _cmbSubGraph.Items.Add(sg.Name);
            }

            if (prevSubGraph != null && _cmbSubGraph.Items.Contains(prevSubGraph))
                _cmbSubGraph.SelectedItem = prevSubGraph;
            else if (!string.IsNullOrEmpty(_config.ViewSource) && _cmbSubGraph.Items.Contains(_config.ViewSource))
                _cmbSubGraph.SelectedItem = _config.ViewSource;
            else
                _cmbSubGraph.SelectedIndex = 0;

            RebuildDisplayNodeList();

            if (prevNode != null && _cmbDisplayNode.Items.Contains(prevNode))
                _cmbDisplayNode.SelectedItem = prevNode;
            else if (!string.IsNullOrEmpty(_config.ViewNode) && _cmbDisplayNode.Items.Contains(_config.ViewNode))
                _cmbDisplayNode.SelectedItem = _config.ViewNode;

            _suppressViewSave = false;
        }

        private void RebuildDisplayNodeList()
        {
            _cmbDisplayNode.Items.Clear();
            var subGraphName = _cmbSubGraph.SelectedItem?.ToString();
            if (subGraphName == null || subGraphName == "相机实时" || _flowGraph == null) return;

            var sg = _flowGraph.FindSubGraphByName(subGraphName);
            if (sg == null) return;

            foreach (var node in sg.Nodes)
            {
                if (node is ImageDisplayNode)
                    _cmbDisplayNode.Items.Add(node.Name);
            }

            if (_cmbDisplayNode.Items.Count > 0)
                _cmbDisplayNode.SelectedIndex = 0;
        }

        private void OnSourceSelectionChanged(object sender, EventArgs e)
        {
            RebuildDisplayNodeList();
            _lastCameraFrameRef = null;

            var source = _cmbSubGraph.SelectedItem?.ToString();
            if (source == "相机实时" && !string.IsNullOrEmpty(_selectedCamera))
            {
                var raw = _vision?.PeekLatestNoClone(_selectedCamera);
                if (raw != null)
                {
                    _lastCameraFrameRef = raw;
                    ShowPreview(new Bitmap(raw));
                }
            }

            if (_suppressViewSave) return;
            _config.ViewSource = source ?? "";
            _config.ViewNode = "";
            _config.SaveDebounced();
        }

        private ImageDisplayNode GetSelectedDisplayNode()
        {
            var subGraphName = _cmbSubGraph.SelectedItem?.ToString();
            var nodeName = _cmbDisplayNode.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(subGraphName) || string.IsNullOrEmpty(nodeName) || _flowGraph == null)
                return null;

            var sg = _flowGraph.FindSubGraphByName(subGraphName);
            if (sg == null) return null;

            foreach (var node in sg.Nodes)
            {
                if (node is ImageDisplayNode dn && dn.Name == nodeName)
                    return dn;
            }
            return null;
        }

        private void OnDisplayNodeChanged(object sender, EventArgs e)
        {
            var dn = GetSelectedDisplayNode();
            if (dn != null)
            {
                ShowPreview(dn.GetPreviewBitmap());
            }

            if (_suppressViewSave) return;
            _config.ViewNode = _cmbDisplayNode.SelectedItem?.ToString() ?? "";
            _config.SaveDebounced();
        }

        private void ShowPreview(Bitmap bmp)
        {
            _previewBox.Image = bmp;
        }

        private void OnPreviewTimerTick(object sender, EventArgs e)
        {
            var source = _cmbSubGraph.SelectedItem?.ToString();

            if (source == "相机实时")
            {
                if (!string.IsNullOrEmpty(_selectedCamera))
                {
                    var raw = _vision?.PeekLatestNoClone(_selectedCamera);
                    if (raw != null && raw != _lastCameraFrameRef)
                    {
                        _lastCameraFrameRef = raw;
                        ShowPreview(new Bitmap(raw));
                        return;
                    }
                }
            }
            else
            {
                var dn = GetSelectedDisplayNode();
                if (dn != null && dn.IsModified)
                {
                    var bmp = dn.GetPreviewBitmap();
                    if (bmp != null)
                    {
                        ShowPreview(bmp);
                        dn.MarkViewed();
                        return;
                    }
                }
            }
        }

        #endregion

        #region Flow

        private void OpenFlowFile()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "流程图文件 (*.flow)|*.flow|JSON文件 (*.json)|*.json",
                Title = "打开流程图",
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadFlowFile(dlg.FileName);
                }
            }
        }

        private void LoadFlowFile(string path)
        {
            try
            {
                var warnings = new List<string>();
                _flowGraph = FlowSerializer.DeserializeFromFile(path, warnings);
                _vision?.SyncFromGraph(_flowGraph);
                _flowExecutor.LoadGraph(_flowGraph);
                _config.FlowFilePath = path;
                _config.SaveDebounced();
                RebuildViewSelector();
                OnStatusChanged(this, $"流程已加载: {System.IO.Path.GetFileName(path)}");
                if (warnings.Count > 0)
                    AppendLog($"反序列化警告 ({warnings.Count}):\n{string.Join("\n", warnings.Take(5))}{(warnings.Count > 5 ? $"\n... 等 {warnings.Count} 条" : "")}", Color.FromArgb(241, 196, 15));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载流程失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AppendLog(string text, Color color)
        {
            LogRingBuffer.Append(_logBox, text, color);
        }

        #endregion

        #region State

        private void UpdateMenuState()
        {
            var anySerial = _vision.SerialPorts.Count > 0
                && _vision.SerialPorts.Values.Any(sp => sp.IsOpen);
        }

        private void OnStatusChanged(object sender, string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => OnStatusChanged(sender, message)));
                return;
            }

            _statusLabel.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        private void OnErrorOccurred(object sender, Exception ex)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => OnErrorOccurred(sender, ex)));
                return;
            }

            MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Role & Login

        private void ShowLogin()
        {
            using (var dlg = new LoginForm(_config))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _currentRole = dlg.SelectedRole;
                    UpdateRoleRestrictions();
                    UpdateMenuState();

                    string roleName;
                    switch (_currentRole)
                    {
                        case UserRole.Admin: roleName = "管理员"; break;
                        case UserRole.Engineer: roleName = "工程师"; break;
                        case UserRole.Operator: roleName = "用户"; break;
                        default: roleName = "未登录"; break;
                    }
                    OnStatusChanged(this, $"登录成功: {roleName}");

                    if (_currentRole >= UserRole.Admin
                        && !string.IsNullOrEmpty(_config.FlowFilePath)
                        && File.Exists(_config.FlowFilePath))
                    {
                        LoadFlowFile(_config.FlowFilePath);
                    }
                }
            }
        }

        private void ShowDefaultLogin()
        {
            using (var dlg = new DefaultLoginForm(_config))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _config.DefaultRole = dlg.SelectedRole;
                    _config.Save();

                    string name;
                    switch (dlg.SelectedRole)
                    {
                        case UserRole.Admin: name = "管理员"; break;
                        case UserRole.Engineer: name = "工程师"; break;
                        case UserRole.Operator: name = "用户"; break;
                        default: name = "不自动登录"; break;
                    }
                    OnStatusChanged(this, $"默认登录已设为: {name}");
                }
            }
        }

        private void ToggleAutoStart()
        {
            _config.AutoStart = !_config.AutoStart;
            _config.Save();
            _miAutoStart.Checked = _config.AutoStart;
            SetWindowsAutoStart(_config.AutoStart);
            OnStatusChanged(this, _config.AutoStart ? "已启用开机自启动" : "已关闭开机自启动");
        }

        private void SetWindowsAutoStart(bool enable)
        {
            const string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            var appName = "VisualCutter";
            var appPath = Application.ExecutablePath;

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    if (enable)
                        key?.SetValue(appName, $"\"{appPath}\"");
                    else
                        key?.DeleteValue(appName, false);
                }
            }
            catch
            {
                // registry write may fail without admin rights
            }
        }

        private void UpdateRoleRestrictions()
        {
            // 文件: 加载/保存配置 → 管理员
            var miFile = (ToolStripMenuItem)_menuStrip.Items[0];
            miFile.DropDownItems[0].Enabled = _currentRole >= UserRole.Admin;  // 加载配置
            miFile.DropDownItems[1].Enabled = _currentRole >= UserRole.Admin;  // 保存配置

            // 流程: 打开/重载/关闭/编辑器 → 管理员
            _miFlowOpen.Enabled = _currentRole >= UserRole.Admin;
            _miFlowReload.Enabled = _currentRole >= UserRole.Admin;
            _miFlowClose.Enabled = _currentRole >= UserRole.Admin;
            _miFlowEditor.Enabled = _currentRole >= UserRole.Admin;

            // 视图: 流程日志 → 始终可用
            _miViewLog.Enabled = true;

            // 登录菜单 → 始终可用; 设置默认登录 → 管理员
            _miLogin.Enabled = true;
            _miDefaultLogin.Enabled = _currentRole >= UserRole.Admin;
            _miAutoStart.Enabled = _currentRole >= UserRole.Admin;
            _miAutoStart.Checked = _config.AutoStart;

            // 帮助 → 始终可用
            _menuStrip.Items[4].Enabled = true;
        }

        #endregion

        #region Help / Diagnostics

        private void ShowDiagnostics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"版本: {Application.ProductVersion}");
            sb.AppendLine($"运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
            sb.AppendLine($"配置文件: {_config?.FilePath ?? "无"}");
            sb.AppendLine($"登录角色: {RoleDisplayName()}");
            sb.AppendLine($"加载流程: {_config?.FlowFilePath ?? "无"}");
            sb.AppendLine($"注册相机数: {_vision?.CameraManager?.Cameras?.Count ?? 0}");
            sb.AppendLine($"相机槽位: {(_vision?.CameraManager?.Slots?.Count > 0 ? string.Join(", ", _vision.CameraManager.Slots.Select(s => s.SlotName + (s.IsConnected ? $" ({s.AssignedSerial})" : " (未连接)"))) : "无")}");
            var openPorts = _vision?.SerialPorts?.Where(kv => kv.Value.IsOpen).Select(kv => kv.Key).ToList();
            sb.AppendLine($"串口状态: {(openPorts != null && openPorts.Count > 0 ? $"已连接 ({string.Join(", ", openPorts)})" : "未连接")}");
            sb.AppendLine($"日期: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            MessageBox.Show(sb.ToString(), "诊断信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                $"VisualCutter 视觉裁切系统\n版本 {Application.ProductVersion}\n\n© 2026",
                "关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private string RoleDisplayName()
        {
            switch (_currentRole)
            {
                case UserRole.Admin: return "管理员";
                case UserRole.Engineer: return "工程师";
                case UserRole.Operator: return "用户";
                default: return "未登录";
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _previewTimer?.Stop();
            _flowExecutor?.Stop();
            _flowExecutor?.Dispose();
            _vision?.StopAllAcquisitions();
            _config?.Save();
            _vision?.Dispose();
            base.OnFormClosing(e);
        }
    }
}