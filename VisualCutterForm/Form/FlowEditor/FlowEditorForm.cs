using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using VisualCutterForm.Lib;
using VisualCutterForm.Lib.Flow;

namespace VisualCutterForm.FlowEditor
{
    public class FlowEditorForm : System.Windows.Forms.Form
    {
        private MenuStrip _menuStrip;
        private ToolStrip _runToolbar;
        private FlowGraph _graph;
        private FlowExecutor _executor;
        private VisionController _visionController;
        private FlowCanvas _canvas;
        private RichTextBox _logBox;
        private FlowPropertyInspector _inspector;
        private FlowToolbox _toolbox;
        private TabControl _tabSubGraphs;
        private ToolStripButton _btnRunOnce;
        private ToolStripButton _btnRunContinuous;
        private ToolStripButton _btnStop;
        private ToolStripComboBox _cmbTriggerMode;
        private bool _syncingTriggerCombo;
        private bool _runningContinuous;
        private SplitContainer _mainSplit;

        public FlowEditorForm(VisionController visionController)
        {
            _visionController = visionController ?? throw new ArgumentNullException(nameof(visionController));
            _executor = new FlowExecutor(visionController);
            _graph = new FlowGraph();

            InitializeForm();

            _graph.AddSubGraph("子图1", SubGraphTrigger.SoftManualTrigger);
            _canvas.SubGraph = _graph.SubGraphs[0];
            _tabSubGraphs.TabPages.Add(new TabPage(_graph.SubGraphs[0].Name) { Tag = _graph.SubGraphs[0].Id });
            _tabSubGraphs.SelectedTab = _tabSubGraphs.TabPages[0];
        }

        public void LoadGraphData(FlowGraph graph)
        {
            if (graph == null) return;
            _graph = graph;
            _executor.LoadGraph(_graph);

            _tabSubGraphs.TabPages.Clear();
            foreach (var sg in _graph.SubGraphs)
            {
                _tabSubGraphs.TabPages.Add(new TabPage(sg.Name) { Tag = sg.Id });
            }
            _canvas.SubGraph = _graph.SubGraphs.FirstOrDefault();
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
                var sg = _graph.AddSubGraph($"子图{_graph.SubGraphs.Count + 1}");
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

            _menuStrip.Items.Add(miFile);
            _menuStrip.Items.Add(miEdit);
            _menuStrip.Items.Add(miView);
            _menuStrip.Items.Add(miRun);
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
                GetActiveCameras = () => _visionController?.Slots?.Keys.ToList() ?? new List<string>(),
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

            _cmbTriggerMode = new ToolStripComboBox("触发模式")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                Width = 130,
            };
            _cmbTriggerMode.Items.AddRange(new[]
            {
                "手动触发",
                "相机触发",
                "通讯触发",
                "持续运行",
            });
            _cmbTriggerMode.SelectedIndexChanged += OnTriggerModeChanged;
            _runToolbar.Items.Add(new ToolStripLabel("触发:") { ForeColor = Color.FromArgb(180, 180, 180) });
            _runToolbar.Items.Add(_cmbTriggerMode);

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
                    await _executor.TriggerSubGraph(sg.Id);
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
        }

        private void OnSubGraphTabChanged(object sender, EventArgs e)
        {
            if (_tabSubGraphs.SelectedTab?.Tag is Guid id)
            {
                var sg = _graph.FindSubGraph(id);
                if (sg != null)
                {
                    _canvas.SubGraph = sg;
                    SyncTriggerCombo(sg.Trigger);
                    _canvas.ClearSelection();
                    _inspector.ShowSubGraph(sg);
                }
            }
        }

        private void OnTriggerModeChanged(object sender, EventArgs e)
        {
            if (_syncingTriggerCombo) return;
            if (_cmbTriggerMode.SelectedIndex < 0) return;
            var sg = _canvas.SubGraph;
            if (sg == null) return;

            sg.Trigger = (SubGraphTrigger)_cmbTriggerMode.SelectedIndex;
        }

        private void SyncTriggerCombo(SubGraphTrigger trigger)
        {
            _syncingTriggerCombo = true;
            var idx = (int)trigger;
            if (_cmbTriggerMode.SelectedIndex != idx)
                _cmbTriggerMode.SelectedIndex = idx;
            _syncingTriggerCombo = false;
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

            if (_graph.SubGraphs.Count == 0)
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
        }

        private void RenameCurrentSubGraph()
        {
            if (!(_tabSubGraphs.SelectedTab?.Tag is Guid id))
                return;

            var sg = _graph.FindSubGraph(id);
            if (sg == null) return;

            using (var form = new Form
            {
                Width = 300, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent, Text = "重命名子图",
                MaximizeBox = false, MinimizeBox = false
            })
            {
                var lbl = new Label { Text = "新名称:", Left = 10, Top = 20, Width = 60 };
                var txt = new TextBox { Left = 80, Top = 18, Width = 180, Text = sg.Name };
                var btnOk = new Button { Text = "确定", Left = 80, Width = 80, Top = 50, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Left = 170, Width = 80, Top = 50, DialogResult = DialogResult.Cancel };
                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                {
                    sg.Name = txt.Text.Trim();
                    _tabSubGraphs.SelectedTab.Text = sg.Name;
                }
            }
        }

        private void NewGraph()
        {
            _graph = new FlowGraph();
            _graph.AddSubGraph("子图1");
            _tabSubGraphs.TabPages.Clear();
            _tabSubGraphs.TabPages.Add(new TabPage("子图1") { Tag = _graph.SubGraphs[0].Id });
            _canvas.SubGraph = _graph.SubGraphs[0];
            SyncTriggerCombo(SubGraphTrigger.SoftManualTrigger);
            _executor.LoadGraph(_graph);
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
                        _graph = FlowSerializer.DeserializeFromFile(dlg.FileName);
                        _executor.LoadGraph(_graph);

                        _tabSubGraphs.TabPages.Clear();
                        foreach (var sg in _graph.SubGraphs)
                        {
                            _tabSubGraphs.TabPages.Add(new TabPage(sg.Name) { Tag = sg.Id });
                        }
                        _canvas.SubGraph = _graph.SubGraphs.FirstOrDefault();
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
                await _executor.TriggerSubGraph(sg.Id);
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _executor?.Dispose();
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
