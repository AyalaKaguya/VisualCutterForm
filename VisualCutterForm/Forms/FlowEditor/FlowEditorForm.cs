using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using VisualMaster.WorkFlow;
using VisualMaster.WorkFlow.Data;
using VisualMaster.WorkFlow.Triggers;
using VisualMaster.Communication;
using VisualCutterForm.Legacy;
using VisualMaster.Forms;
using VisualMaster.Forms.Camera;
using VisualMaster.Forms.TriggerEditor;

namespace VisualMaster.Forms.FlowEditor
{
    public class FlowEditorForm : System.Windows.Forms.Form
    {
        private MenuStrip _menuStrip;
        private ToolStrip _runToolbar;
        private FlowGraph _graph;
        private FlowExecutor _executor;
        private FlowCanvas _canvas;
        private RichTextBox _logBox;
        private FlowPropertyInspector _inspector;
        private FlowToolbox _toolbox;
        private TabControl _tabSubGraphs;
        private ToolStripButton _btnRunOnce;
        private ToolStripButton _btnRunContinuous;
        private ToolStripButton _btnStop;
        private ToolStripLabel _lblTriggerBindings;
        private bool _runningContinuous;
        private SplitContainer _mainSplit;
        private VisionController _visionController;

        public FlowEditorForm(FlowExecutor executor, FlowGraph graph, VisionController visionController)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _graph = graph ?? new FlowGraph();
            _visionController = visionController;

            InitializeForm();

            if (_graph.Project.SubGraphs.Count == 0)
                _graph.AddSubGraph("子图1");

            _canvas.SubGraph = _graph.Project.SubGraphs[0];
            _tabSubGraphs.TabPages.Add(new TabPage(_graph.Project.SubGraphs[0].Name) { Tag = _graph.Project.SubGraphs[0].Id });
            _tabSubGraphs.SelectedTab = _tabSubGraphs.TabPages[0];
            UpdateCurrentSubGraphBindingSummary();
        }

        public void LoadGraphData(FlowGraph graph)
        {
            if (graph == null) return;
            _graph = graph;
            _executor.LoadGraph(_graph);

            _tabSubGraphs.TabPages.Clear();
            foreach (var sg in _graph.Project.SubGraphs)
            {
                _tabSubGraphs.TabPages.Add(new TabPage(sg.Name) { Tag = sg.Id });
            }
            _canvas.SubGraph = _graph.Project.SubGraphs.FirstOrDefault();
            UpdateCurrentSubGraphBindingSummary();
        }

        private void InitializeForm()
        {
            Text = "流程图编辑器";
            Size = new Size(1200, 750);
            WindowState = FormWindowState.Maximized;

            BuildMenu();
            BuildLayout();
        }

        private void BuildMenu()
        {
            _menuStrip = new MenuStrip { Font = new Font("Microsoft YaHei", 9F) };

            var miFile = new ToolStripMenuItem("文件(&F)");
            miFile.DropDownItems.Add("新建流程(&N)", null, (s, e) => NewGraph());
            miFile.DropDownItems.Add("打开流程(&O)...", null, (s, e) => OpenGraph());
            miFile.DropDownItems.Add("保存流程(&S)", null, (s, e) => SaveGraph());
            miFile.DropDownItems.Add("另存为(&A)...", null, (s, e) => SaveGraphAs());
            miFile.DropDownItems.Add(new ToolStripSeparator());
            miFile.DropDownItems.Add("退出(&X)", null, (s, e) => Close());

            var miEdit = new ToolStripMenuItem("编辑(&E)");
            miEdit.DropDownItems.Add("删除选中节点(&D)", null, (s, e) =>
            {
                var sel = _canvas.NodeViews.Find(v => v.IsSelected);
                if (sel != null) _canvas.RemoveNode(sel);
            });
            miEdit.DropDownItems.Add(new ToolStripSeparator());
            miEdit.DropDownItems.Add("添加子图", null, (s, e) =>
            {
                var sg = _graph.AddSubGraph($"子图{_graph.Project.SubGraphs.Count + 1}");
                AddSubGraphTab(sg);
            });
            miEdit.DropDownItems.Add("删除当前子图", null, (s, e) => DeleteCurrentSubGraph());
            miEdit.DropDownItems.Add("重命名当前子图", null, (s, e) => RenameCurrentSubGraph());

            var miView = new ToolStripMenuItem("视图(&V)");
            miView.DropDownItems.Add("放大", null, (s, e) => _canvas.ZoomIn());
            miView.DropDownItems.Add("缩小", null, (s, e) => _canvas.ZoomOut());
            miView.DropDownItems.Add("重置视图", null, (s, e) => _canvas.ResetView());

            var miRun = new ToolStripMenuItem("运行(&R)");
            miRun.DropDownItems.Add("执行当前子图", null, (s, e) => RunCurrentSubGraph());
            miRun.DropDownItems.Add("停止所有", null, (s, e) => _executor.Stop());

            var miIO = new ToolStripMenuItem("输入输出(&I)");
            miIO.DropDownItems.Add("相机管理器...", null, (s, e) =>
            {
                using (var dlg = new CameraManagerForm(_visionController))
                    dlg.ShowDialog(this);
            });
            miIO.DropDownItems.Add("串口管理器...", null, (s, e) =>
            {
                using (var dlg = new SerialManagerForm(_visionController))
                    dlg.ShowDialog(this);
            });
            miIO.DropDownItems.Add("触发器编辑器...", null, (s, e) =>
            {
                using (var dlg = new TriggerEditorForm(_graph, _executor, _visionController))
                    dlg.ShowDialog(this);

                UpdateCurrentSubGraphBindingSummary();
            });

            _menuStrip.Items.Add(miFile);
            _menuStrip.Items.Add(miEdit);
            _menuStrip.Items.Add(miView);
            _menuStrip.Items.Add(miRun);
            _menuStrip.Items.Add(miIO);
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;
        }

        private void BuildLayout()
        {
            _toolbox = new FlowToolbox
            {
                Dock = DockStyle.Fill,
            };
            _toolbox.NodeTypeSelected += (type) =>
            {
                _canvas.AddNode(type, new Point(100, 100));
            };

            _inspector = new FlowPropertyInspector
            {
                Dock = DockStyle.Fill,
                GetActiveCameras = () => _visionController?.GetCameraDeviceConfigs()?
                    .Select(s => new DisplayItem(s.DeviceId, $"{s.DisplayName} ({ShortId(s.DeviceId)})"))
                    .ToList() ?? new List<DisplayItem>(),
                GetActiveSerialSlots = () => _visionController?.GetSerialDeviceConfigs()?
                    .Select(s => new DisplayItem(s.DeviceId, $"{s.DisplayName} ({s.PortName})"))
                    .ToList() ?? new List<DisplayItem>(),
            };
            _inspector.PropertyChanged += (node, prop, val) =>
            {
                _canvas.Invalidate();
            };

            _runToolbar = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(50, 50, 50),
                Renderer = new DarkToolStripRenderer(),
            };

            _btnRunOnce = new ToolStripButton("执行一次", null, OnRunOnceClick)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.White,
            };
            _btnRunContinuous = new ToolStripButton("连续执行", null, OnRunContinuousClick)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.White,
            };
            _btnStop = new ToolStripButton("停止", null, OnRunStopClick)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.White,
                Enabled = false,
            };

            _runToolbar.Items.Add(new ToolStripLabel("运行:") { ForeColor = Color.FromArgb(180, 180, 180) });
            _runToolbar.Items.Add(_btnRunOnce);
            _runToolbar.Items.Add(_btnRunContinuous);
            _runToolbar.Items.Add(_btnStop);
            _runToolbar.Items.Add(new ToolStripSeparator());
            _runToolbar.Items.Add(new ToolStripLabel("当前流程绑定:") { ForeColor = Color.FromArgb(180, 180, 180) });
            _lblTriggerBindings = new ToolStripLabel("无")
            {
                ForeColor = Color.FromArgb(220, 220, 220),
                AutoToolTip = true,
            };
            _runToolbar.Items.Add(_lblTriggerBindings);

            var btnManualTrigger = new ToolStripDropDownButton("▶ 手动触发")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.White,
            };
            btnManualTrigger.DropDownOpening += (s, e) =>
            {
                btnManualTrigger.DropDownItems.Clear();
                var manualTriggers = _graph?.Project?.Routing?.Triggers?.Where(t => t.Enabled && t.SourceType == VisualMaster.WorkFlow.Triggers.TriggerSourceType.Manual).ToList();
                if (manualTriggers != null && manualTriggers.Count > 0)
                {
                    foreach (var t in manualTriggers)
                    {
                        var targetNames = t.GetTargetSubGraphIds()
                            .Select(id => _graph.FindSubGraph(id)?.Name)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToList();
                        var label = targetNames.Count > 0
                            ? $"{t.Name} → {string.Join(", ", targetNames)}"
                            : t.Name;
                        btnManualTrigger.DropDownItems.Add(label, null, async (s2, e2) =>
                        {
                            await _executor.FireManualTrigger(t.Id);
                        });
                    }
                    btnManualTrigger.DropDownItems.Add(new ToolStripSeparator());
                }
                btnManualTrigger.DropDownItems.Add("触发器编辑器...", null, (s2, e2) =>
                {
                    using (var dlg = new VisualMaster.Forms.TriggerEditor.TriggerEditorForm(_graph, _executor, _visionController))
                        dlg.ShowDialog(this);

                    UpdateCurrentSubGraphBindingSummary();
                });
            };
            _runToolbar.Items.Add(btnManualTrigger);

            var btnToggleLog = new ToolStripButton("日志", null, (s, e) =>
            {
                if (_mainSplit != null)
                {
                    _mainSplit.Panel2Collapsed = !_mainSplit.Panel2Collapsed;
                    ((ToolStripButton)s).Checked = !_mainSplit.Panel2Collapsed;
                }
            })
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ForeColor = Color.White,
                Checked = true,
                CheckOnClick = true,
                Alignment = ToolStripItemAlignment.Right,
            };
            _runToolbar.Items.Add(btnToggleLog);

            _tabSubGraphs = new TabControl
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Microsoft YaHei", 9F),
            };
            _tabSubGraphs.SelectedIndexChanged += OnSubGraphTabChanged;

            _canvas = new FlowCanvas
            {
                Dock = DockStyle.Fill,
            };
            _canvas.NodeSelected += (node) =>
            {
                _inspector.ShowNode(node);
                if (node == null)
                    _inspector.ShowSubGraph(_canvas.SubGraph);
            };

            _canvas.DeselectAll += () =>
            {
                _inspector.ShowSubGraph(_canvas.SubGraph);
            };

            var canvasPanel = new Panel { Dock = DockStyle.Fill };
            canvasPanel.Controls.Add(_canvas);
            canvasPanel.Controls.Add(_tabSubGraphs);

            var rightPanel = new Panel { Dock = DockStyle.Fill };
            rightPanel.Controls.Add(canvasPanel);
            rightPanel.Controls.Add(_runToolbar);

            var splitRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 700,
            };
            splitRight.Panel1.Controls.Add(rightPanel);
            splitRight.Panel2.Controls.Add(_inspector);

            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 190,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
            };
            mainSplit.Panel1.Controls.Add(_toolbox);
            mainSplit.Panel2.Controls.Add(splitRight);

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
                Text = "执行日志",
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

            var mainVSplit = _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 550,
            };
            mainVSplit.Panel1.Controls.Add(mainSplit);
            mainVSplit.Panel2.Controls.Add(logPanel);

            Controls.Add(mainVSplit);

            _executor.LogMessage += OnExecLog;
            _executor.ExecutionError += OnExecError;
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "";

            return id.Length <= 8 ? id : id.Substring(0, 8);
        }

        private void OnExecLog(object sender, string msg)
        {
            AppendLog(msg, Color.FromArgb(180, 180, 180));
        }

        private void OnExecError(object sender, Exception ex)
        {
            AppendLog(ex.Message, Color.FromArgb(231, 76, 60));
        }

        private void AppendLog(string text, Color color)
        {
            LogRingBuffer.Append(_logBox, text, color);
        }

        private void OnRunOnceClick(object sender, EventArgs e)
        {
            RunCurrentSubGraph();
        }

        private async void OnRunContinuousClick(object sender, EventArgs e)
        {
            var sg = _canvas.SubGraph;
            if (sg == null) return;

            _executor.LoadGraph(_graph);
            _executor.Start();
            _runningContinuous = true;
            _btnRunOnce.Enabled = false;
            _btnRunContinuous.Enabled = false;
            _btnStop.Enabled = true;

            try
            {
                while (_runningContinuous)
                {
                    using (var ctx = new FlowTriggerContext { SourceType = TriggerSourceType.Manual, TriggerName = "手动连续" })
                        await _executor.TriggerSubGraph(sg.Id, ctx);
                    _canvas.Invalidate();
                    await Task.Delay(10);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.ToString()); }

            _btnRunOnce.Enabled = true;
            _btnRunContinuous.Enabled = true;
            _btnStop.Enabled = false;
        }

        private void OnRunStopClick(object sender, EventArgs e)
        {
            _runningContinuous = false;
            _executor.Stop();
            _canvas.Invalidate();
        }

        private void AddSubGraphTab(FlowSubGraph sg)
        {
            var tab = new TabPage(sg.Name) { Tag = sg.Id };
            _tabSubGraphs.TabPages.Add(tab);
            _tabSubGraphs.SelectedTab = tab;
            UpdateCurrentSubGraphBindingSummary();
        }

        private void OnSubGraphTabChanged(object sender, EventArgs e)
        {
            if (_tabSubGraphs.SelectedTab?.Tag is Guid id)
            {
                var sg = _graph.FindSubGraph(id);
                if (sg != null)
                {
                    _canvas.SubGraph = sg;
                    _canvas.ClearSelection();
                    _inspector.ShowSubGraph(sg);
                    UpdateCurrentSubGraphBindingSummary();
                }
            }
        }

        private void DeleteCurrentSubGraph()
        {
            if (!(_tabSubGraphs.SelectedTab?.Tag is Guid id))
                return;

            var sg = _graph.FindSubGraph(id);
            if (sg == null) return;

            var result = MessageBox.Show(
                $"确定要删除子图 \"{sg.Name}\" 吗？\n此操作不可撤销。",
                "删除子图", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result != DialogResult.OK) return;

            _executor.Stop();
            _graph.RemoveSubGraph(id);

            var tab = _tabSubGraphs.SelectedTab;
            _tabSubGraphs.TabPages.Remove(tab);

            if (_graph.Project.SubGraphs.Count == 0)
            {
                var newSg = _graph.AddSubGraph("子图1");
                _tabSubGraphs.TabPages.Add(new TabPage("子图1") { Tag = newSg.Id });
                _canvas.SubGraph = newSg;
            }
            else
            {
                var firstTab = _tabSubGraphs.TabPages[0];
                _tabSubGraphs.SelectedTab = firstTab;
                _canvas.SubGraph = _graph.FindSubGraph((Guid)firstTab.Tag);
            }

            UpdateCurrentSubGraphBindingSummary();
        }

        private void RenameCurrentSubGraph()
        {
            if (!(_tabSubGraphs.SelectedTab?.Tag is Guid id))
                return;

            var sg = _graph.FindSubGraph(id);
            if (sg == null) return;

            var newName = ShowInputDialog("重命名子图", "新名称:", sg.Name);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                sg.Name = newName;
                _tabSubGraphs.SelectedTab.Text = sg.Name;
                UpdateCurrentSubGraphBindingSummary();
            }
        }

        private static string ShowInputDialog(string title, string prompt, string initialValue)
        {
            using (var dlg = new InputDialog(title, prompt, initialValue))
            {
                return dlg.InputText;
            }
        }

        private void NewGraph()
        {
            _graph = new FlowGraph();
            _graph.AddSubGraph("子图1");
            _tabSubGraphs.TabPages.Clear();
            _tabSubGraphs.TabPages.Add(new TabPage("子图1") { Tag = _graph.Project.SubGraphs[0].Id });
            _canvas.SubGraph = _graph.Project.SubGraphs[0];
            _executor.LoadGraph(_graph);
            UpdateCurrentSubGraphBindingSummary();
        }

        private void OpenGraph()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "流程图文件 (*.flow)|*.flow|JSON文件 (*.json)|*.json",
                Title = "打开流程图",
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var warnings = new List<string>();
                        _graph = FlowSerializer.DeserializeFromFile(dlg.FileName, warnings);
                        _visionController?.SyncFromGraph(_graph);
                        _executor.LoadGraph(_graph);

                        if (warnings.Count > 0)
                        {
                            var msg = $"加载完成，但有 {warnings.Count} 个警告:\n{string.Join("\n", warnings.Take(5))}";
                            MessageBox.Show(msg, "加载警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        _tabSubGraphs.TabPages.Clear();
                        foreach (var sg in _graph.Project.SubGraphs)
                        {
                            _tabSubGraphs.TabPages.Add(new TabPage(sg.Name) { Tag = sg.Id });
                        }
                        _canvas.SubGraph = _graph.Project.SubGraphs.FirstOrDefault();
                        UpdateCurrentSubGraphBindingSummary();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveGraph()
        {
            if (_currentFilePath == null)
            {
                SaveGraphAs();
                return;
            }

            try
            {
                _visionController?.SyncToGraph(_graph);
                FlowSerializer.SerializeToFile(_graph, _currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string _currentFilePath;

        private void SaveGraphAs()
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = "流程图文件 (*.flow)|*.flow",
                Title = "保存流程图",
                DefaultExt = ".flow",
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _currentFilePath = dlg.FileName;
                    _visionController?.SyncToGraph(_graph);
                    FlowSerializer.SerializeToFile(_graph, _currentFilePath);
                }
            }
        }

        private async void RunCurrentSubGraph()
        {
            var sg = _canvas.SubGraph;
            if (sg == null)
            {
                MessageBox.Show("没有可执行的子图。", "提示");
                return;
            }

            _executor.LoadGraph(_graph);
            _executor.Start();

            try
            {
                using (var ctx = new FlowTriggerContext { SourceType = TriggerSourceType.Manual, TriggerName = "手动单次" })
                    await _executor.TriggerSubGraph(sg.Id, ctx);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _canvas.Invalidate();
            }
        }

        private void UpdateCurrentSubGraphBindingSummary()
        {
            if (_lblTriggerBindings == null)
                return;

            var subGraph = _canvas?.SubGraph;
            if (subGraph == null)
            {
                _lblTriggerBindings.Text = "无";
                _lblTriggerBindings.ToolTipText = "当前没有选中的流程。";
                return;
            }

            var boundTriggers = _graph?.Project?.Routing?.Triggers?
                .Where(t => t.GetTargetSubGraphIds().Contains(subGraph.Id))
                .Select(t => t.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList() ?? new List<string>();

            if (boundTriggers.Count == 0)
            {
                _lblTriggerBindings.Text = "无";
                _lblTriggerBindings.ToolTipText = $"流程 {subGraph.Name} 当前没有触发器绑定。";
                return;
            }

            var summary = string.Join("，", boundTriggers.Take(3));
            if (boundTriggers.Count > 3)
                summary += $" 等 {boundTriggers.Count} 个";

            _lblTriggerBindings.Text = summary;
            _lblTriggerBindings.ToolTipText = string.Join(Environment.NewLine, boundTriggers);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_executor != null)
            {
                _executor.LogMessage -= OnExecLog;
                _executor.ExecutionError -= OnExecError;
            }
            base.OnFormClosing(e);
        }

        private class DarkToolStripRenderer : ToolStripProfessionalRenderer
        {
            public DarkToolStripRenderer() : base(new DarkColorTable()) { }

            private class DarkColorTable : ProfessionalColorTable
            {
                public override Color ToolStripGradientBegin => Color.FromArgb(50, 50, 50);
                public override Color ToolStripGradientMiddle => Color.FromArgb(50, 50, 50);
                public override Color ToolStripGradientEnd => Color.FromArgb(50, 50, 50);
                public override Color ToolStripBorder => Color.FromArgb(60, 60, 60);
                public override Color ButtonSelectedBorder => Color.FromArgb(52, 152, 219);
                public override Color ButtonSelectedGradientBegin => Color.FromArgb(60, 60, 60);
                public override Color ButtonSelectedGradientEnd => Color.FromArgb(60, 60, 60);
                public override Color ButtonCheckedGradientBegin => Color.FromArgb(70, 70, 70);
            }
        }
    }
}