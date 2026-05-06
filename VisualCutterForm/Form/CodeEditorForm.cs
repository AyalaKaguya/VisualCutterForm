using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using VisualCutterForm.Lib.Flow;

namespace VisualCutterForm
{
    public class CodeEditorForm : Form
    {
        private FastColoredTextBox _editor;
        private TreeView _pinTree;
        private TreeView _snippetTree;
        private RichTextBox _logBox;
        private ToolStripMenuItem _miDebug;
        private ToolStripMenuItem _miRelease;
        private MenuStrip _mainMenu;

        private readonly string _originalCode;
        private readonly FlowNode _node;
        private List<string> _dllRefs;
        private List<string> _nugetPkgs;

        public string SourceCode => _editor.Text;
        public string ExtraReferences => string.Join(";", _dllRefs);
        public string NuGetPackages => string.Join(",", _nugetPkgs);
        public bool IsDebug => _miDebug.Checked;
        public FlowNode EditedNode => _node;

        private static readonly string DefaultTemplate =
@"/*
 * VisualCutter 计算节点模板
 * ───────────────────────────────────────────
 * 机制:
 *  1. 系统根据「同名 Pin」自动填充 public 字段
 *  2. 执行 Execute() 后, 系统从 public 字段读取输出值
 *  3. Context 字段自动注入, 用于输出日志
 *
 * 可用 API:
 *  Context.Log(""msg"")        — 输出信息日志
 *  Context.LogWarning(""msg"") — 输出警告日志
 *  Context.LogError(""msg"")   — 输出错误日志
 *
 * OpenCV 常用操作:
 *  mat.Clone()           — 深拷贝
 *  mat.CvtColor(code)    — 颜色转换 (ColorConversionCodes.BGR2GRAY)
 *  mat.Threshold(thresh, maxVal, type) — 二值化
 *  mat.Resize(new Size(w, h))  — 缩放
 *  mat.Crop(rect)        — 裁剪
 *  Cv2.FindContours(...) — 找轮廓
 *  Cv2.BitwiseAnd(...)   — 位运算
 * ───────────────────────────────────────────
 */

public class UserCode
{
    // === 输入字段 (系统自动填充) ===";

        public CodeEditorForm(FlowNode node)
        {
            _node = node;
            _originalCode = node.GetNodeProperties().FirstOrDefault(p => p.Name == "SourceCode")?.Getter() as string ?? "";
            _dllRefs = ParseSemicolon((node.GetNodeProperties().FirstOrDefault(p => p.Name == "ExtraReferences")?.Getter() as string) ?? "");
            _nugetPkgs = ParseComma((node.GetNodeProperties().FirstOrDefault(p => p.Name == "NuGetPackages")?.Getter() as string) ?? "");

            Text = "C# 代码编辑器 - VisualCutter";
            Size = new Size(1200, 750);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);
            MinimizeBox = false;

            BuildMenu();
            BuildContent();
            Load += OnFormLoad;
            Resize += (s, e) => LayoutContent();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            LayoutContent();
        }

        private void LayoutContent()
        {
            if (_mainMenu == null) return;
            // find the main container (last control added, Panel docked None)
            var menuHeight = _mainMenu.Height;
            // the mainPanel is at Controls[1] (menu is Controls[0])
            if (Controls.Count >= 2 && Controls[1] is Panel mainPanel)
            {
                mainPanel.Location = new Point(0, menuHeight);
                mainPanel.Size = new Size(ClientSize.Width, ClientSize.Height - menuHeight);
            }
        }

        private static List<string> ParseSemicolon(string s) =>
            string.IsNullOrWhiteSpace(s) ? new List<string>() : s.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

        private static List<string> ParseComma(string s) =>
            string.IsNullOrWhiteSpace(s) ? new List<string>() : s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

        private void BuildMenu()
        {
            _mainMenu = new MenuStrip { Font = new Font("Microsoft YaHei", 9F) };

            var miFile = new ToolStripMenuItem("文件(&F)");
            miFile.DropDownItems.Add("重置代码", null, (s, e) =>
            {
                if (MessageBox.Show("将丢失所有修改，确定重置？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    _editor.Text = GetDefaultTemplate();
            });
            miFile.DropDownItems.Add(new ToolStripSeparator());
            miFile.DropDownItems.Add("保存并关闭", null, (s, e) => { DialogResult = DialogResult.OK; Close(); });
            miFile.DropDownItems.Add("取消", null, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });

            var miEdit = new ToolStripMenuItem("编辑(&E)");
            miEdit.DropDownItems.Add("撤销", null, (s, e) => _editor.Undo());
            miEdit.DropDownItems.Add("重做", null, (s, e) => _editor.Redo());

            var miRef = new ToolStripMenuItem("引用(&R)");
            miRef.DropDownItems.Add("管理外部引用...", null, (s, e) =>
            {
                using (var dlg = new ReferenceManager(_dllRefs, _nugetPkgs))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _dllRefs = dlg.DllReferences;
                        _nugetPkgs = dlg.NuGetPackages;
                    }
                }
            });

            var miBuild = new ToolStripMenuItem("构建(&B)");
            var miCompile = new ToolStripMenuItem("编译测试", null, (s, e) => TestCompile());
            _miDebug = new ToolStripMenuItem("Debug", null, (s, e) => { _miDebug.Checked = true; _miRelease.Checked = false; });
            _miRelease = new ToolStripMenuItem("Release", null, (s, e) => { _miRelease.Checked = true; _miDebug.Checked = false; });
            _miDebug.Checked = true;
            miBuild.DropDownItems.Add(miCompile);
            miBuild.DropDownItems.Add(new ToolStripSeparator());
            miBuild.DropDownItems.Add(_miDebug);
            miBuild.DropDownItems.Add(_miRelease);

            var miHelp = new ToolStripMenuItem("帮助(&H)");
            miHelp.DropDownItems.Add("模板说明", null, (s, e) => MessageBox.Show(GetTemplateHelp(), "模板说明", MessageBoxButtons.OK, MessageBoxIcon.Information));

            _mainMenu.Items.Add(miFile);
            _mainMenu.Items.Add(miEdit);
            _mainMenu.Items.Add(miRef);
            _mainMenu.Items.Add(miBuild);
            _mainMenu.Items.Add(miHelp);

            Controls.Add(_mainMenu);
            MainMenuStrip = _mainMenu;
        }

        private string GetTemplateHelp()
        {
            return @"VisualCutter 代码模板说明
──────────────────────────

1. 系统扫描 UserCode 类的 public 字段, 与节点 Pin 匹配:
   · 输入 Pin 同名字段 → Execute() 前自动赋值
   · 输出 Pin 同名字段 → Execute() 后自动读取为输出值

2. Context 字段自动注入, 可用方法:
   · Context.Log(""msg"")        输出普通日志
   · Context.LogWarning(""msg"") 输出警告
   · Context.LogError(""msg"")   输出错误

3. 添加新的输入/输出:
   · 在属性面板添加 Pin (指定名称和类型)
   · 在 UserCode 中声明同名 + 同类型的 public 字段
   · 系统自动绑定

4. 引入外部库:
   · 菜单 引用 → 管理外部引用
   · DLL 引用: 选择 .dll 文件路径
   · NuGet 包: 输入包 ID (如 Newtonsoft.Json), 编译时下载

5. 构建模式:
   · Debug: 包含调试信息
   · Release: 优化字节码
";
        }

        private string GetDefaultTemplate()
        {
            var inputs = _node.Inputs;
            var outputs = _node.Outputs;

            var hasNs = new HashSet<string>();
            foreach (var p in inputs) AddNamespacesForType(hasNs, p.DataType);
            foreach (var p in outputs) AddNamespacesForType(hasNs, p.DataType);

            var sb = new System.Text.StringBuilder();

            // using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using OpenCvSharp;");
            sb.AppendLine("using VisualCutterForm.Lib.Flow;");
            sb.AppendLine("using VisualCutterForm.Lib.Flow.Data;");
            sb.AppendLine();

            sb.AppendLine(DefaultTemplate);
            sb.AppendLine();

            foreach (var pin in inputs)
            {
                var csType = FlowPinTypeToCSharp(pin.DataType);
                sb.AppendLine($"    // 输入: {pin.Name}");
                sb.AppendLine($"    public {csType} {pin.Name};");
                sb.AppendLine();
            }

            sb.AppendLine("    // === 输出字段 (系统自动读取) ===");
            foreach (var pin in outputs)
            {
                var csType = FlowPinTypeToCSharp(pin.DataType);
                sb.AppendLine($"    // 输出: {pin.Name}");
                sb.AppendLine($"    public {csType} {pin.Name};");
                sb.AppendLine();
            }

            sb.AppendLine(@"    // Context 对象 (日志输出)
    public FlowContext Context;

    public void Execute()
    {
        // 在此编写处理代码
        Context.Log(""执行开始"");

        // 示例: 图像处理
        // if (Source != null)
        // {
        //     Result = Source.Clone();
        //     Context.Log($""图像尺寸: {Source.Width}x{Source.Height}"");
        // }

        Context.Log(""执行完毕"");
    }
}");
            return sb.ToString();
        }

        private static void AddNamespacesForType(HashSet<string> set, Type t)
        {
            if (t == null) return;
            var ns = t.Namespace;
            if (!string.IsNullOrEmpty(ns) && ns != "System")
                set.Add(ns);
        }

        private static string FlowPinTypeToCSharp(Type t)
        {
            if (t == typeof(string)) return "string";
            if (t == typeof(int)) return "int";
            if (t == typeof(double)) return "double";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(byte[])) return "byte[]";
            if (t == typeof(Bitmap)) return "Bitmap";
            if (t == typeof(OpenCvSharp.Mat)) return "Mat";
            if (t == typeof(OpenCvSharp.Point2d)) return "Point2d";
            if (t == typeof(Lib.Flow.Data.AcquisitionResult)) return "AcquisitionResult";
            if (t != null) return t.Name;
            return "object";
        }

        private void BuildContent()
        {
            var leftPanel = CreatePinPanel();
            var rightPanel = CreateSnippetPanel();

            // use NSimSun for proper CJK monospace
            Font editorFont;
            try { editorFont = new Font("NSimSun", 11F); }
            catch { editorFont = new Font("Microsoft YaHei", 10F); }

            _editor = new FastColoredTextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Font = editorFont,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                AutoIndent = true,
                ShowLineNumbers = true,
                WordWrap = false,
            };

            var code = string.IsNullOrWhiteSpace(_originalCode) ? GetDefaultTemplate() : _originalCode;
            _editor.Text = code;
            _editor.ClearUndo();
            _editor.IsChanged = false;

            // C# syntax highlight styles
            var kwBrush = new SolidBrush(Color.FromArgb(86, 156, 214));
            var cmBrush = new SolidBrush(Color.FromArgb(87, 166, 74));
            var stBrush = new SolidBrush(Color.FromArgb(214, 157, 133));
            var nuBrush = new SolidBrush(Color.FromArgb(181, 206, 168));
            var keywordStyle = new TextStyle(kwBrush, null, FontStyle.Regular);
            var commentStyle = new TextStyle(cmBrush, null, FontStyle.Regular);
            var stringStyle = new TextStyle(stBrush, null, FontStyle.Regular);
            var numberStyle = new TextStyle(nuBrush, null, FontStyle.Regular);

            // apply syntax to full text initially
            _editor.Range.ClearStyle(keywordStyle, commentStyle, stringStyle, numberStyle);
            _editor.Range.SetStyle(keywordStyle,
                @"\b(public|private|protected|internal|static|class|struct|void|int|long|float|double|bool|byte|string|object|if|else|for|foreach|while|do|return|new|null|true|false|using|namespace|var|try|catch|throw|ref|out|in|readonly|const|async|await|Task|override|virtual|abstract|sealed|base|this|typeof|sizeof|is|as|enum|interface|delegate|event|lock|switch|case|default|break|continue|goto|get|set|value|add|remove)\b");
            _editor.Range.SetStyle(commentStyle, @"//.*$", RegexOptions.Multiline);
            _editor.Range.SetStyle(stringStyle, @"""[^""\\\r\n]*(?:\\.[^""\\\r\n]*)*""");
            _editor.Range.SetStyle(numberStyle, @"\b\d+\.?\d*f?\b");

            // incremental highlight on text change
            _editor.TextChanged += (s, e) =>
            {
                UpdatePinTreeHighlight();
                var range = e.ChangedRange;
                if (range == null) return;
                range.ClearStyle(keywordStyle, commentStyle, stringStyle, numberStyle);
                range.SetStyle(keywordStyle,
                    @"\b(public|private|protected|internal|static|class|struct|void|int|long|float|double|bool|byte|string|object|if|else|for|foreach|while|do|return|new|null|true|false|using|namespace|var|try|catch|throw|ref|out|in|readonly|const|async|await|Task|override|virtual|abstract|sealed|base|this|typeof|sizeof|is|as|enum|interface|delegate|event|lock|switch|case|default|break|continue|goto|get|set|value|add|remove)\b");
                range.SetStyle(commentStyle, @"//.*$", RegexOptions.Multiline);
                range.SetStyle(stringStyle, @"""[^""\\\r\n]*(?:\\.[^""\\\r\n]*)*""");
                range.SetStyle(numberStyle, @"\b\d+\.?\d*f?\b");
            };

            _editor.SelectionChanged += (s, e) => UpdatePinTreeHighlight();

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

            var logPanel = CreatePanelHeader("编译日志", _logBox);

            var threeCol = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
            };
            threeCol.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66F));  // Pin 1/6
            threeCol.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66.67F));  // Editor 4/6
            threeCol.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));  // Snippet 1/6
            threeCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            threeCol.Controls.Add(leftPanel, 0, 0);
            threeCol.Controls.Add(_editor, 1, 0);
            threeCol.Controls.Add(rightPanel, 2, 0);

            var editorSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 520,
            };

            editorSplit.Panel1.Controls.Add(threeCol);
            editorSplit.Panel2.Controls.Add(logPanel);

            var mainPanel = new Panel { Location = new Point(0, 0), Size = new Size(ClientSize.Width, ClientSize.Height) };
            mainPanel.Controls.Add(editorSplit);
            Controls.Add(mainPanel);
        }

        private Control CreatePinPanel()
        {
            _pinTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei", 8F),
                ShowLines = true,
            };

            var inputsNode = new TreeNode("输入 Pin") { ForeColor = Color.FromArgb(52, 152, 219) };
            foreach (var pin in _node.Inputs)
            {
                var node = new TreeNode($"{pin.Name} ({PinTypeLabel(pin.DataType)})")
                {
                    Tag = pin,
                    ForeColor = Color.FromArgb(180, 200, 220),
                };
                inputsNode.Nodes.Add(node);
            }

            var outputsNode = new TreeNode("输出 Pin") { ForeColor = Color.FromArgb(46, 204, 113) };
            foreach (var pin in _node.Outputs)
            {
                var node = new TreeNode($"{pin.Name} ({PinTypeLabel(pin.DataType)})")
                {
                    Tag = pin,
                    ForeColor = Color.FromArgb(180, 220, 180),
                };
                outputsNode.Nodes.Add(node);
            }

            _pinTree.Nodes.Add(inputsNode);
            _pinTree.Nodes.Add(outputsNode);
            inputsNode.Expand();
            outputsNode.Expand();

            _pinTree.NodeMouseDoubleClick += (s, e) =>
            {
                if (e.Node?.Tag is NodePin pin)
                    InsertAtCursor(pin.Name);
            };

            return CreatePanelHeader("Pin 列表", _pinTree);
        }

        private Control CreateSnippetPanel()
        {
            _snippetTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei", 8F),
                ShowLines = true,
            };

            var opencvMat = new TreeNode("OpenCV Mat 方法") { ForeColor = Color.FromArgb(86, 156, 214) };
            AddSnippet(opencvMat, "Clone()", "mat.Clone();");
            AddSnippet(opencvMat, "CvtColor(code)", "mat.CvtColor(ColorConversionCodes.BGR2GRAY);");
            AddSnippet(opencvMat, "Threshold(t,max,type)", "mat.Threshold(128, 255, ThresholdTypes.Binary);");
            AddSnippet(opencvMat, "Resize(w,h)", "mat.Resize(new OpenCvSharp.Size(640, 480));");
            AddSnippet(opencvMat, "Crop(Rect)", "mat[new OpenCvSharp.Rect(0,0,100,100)];");
            AddSnippet(opencvMat, "InRange(low,high)", "mat.InRange(new Scalar(0,0,0), new Scalar(255,255,255));");
            AddSnippet(opencvMat, "GaussianBlur(...)", "mat.GaussianBlur(new OpenCvSharp.Size(5,5), 0);");
            AddSnippet(opencvMat, "Width/Height", "int w = mat.Width; int h = mat.Height;");

            var opencvGlobal = new TreeNode("OpenCV 全局") { ForeColor = Color.FromArgb(214, 157, 86) };
            AddSnippet(opencvGlobal, "Cv2.ImRead/Write", "Cv2.ImWrite(@\"C:\\out.png\", Result);");
            AddSnippet(opencvGlobal, "Cv2.FindContours", "Cv2.FindContours(binary, out var cs, out var h, RetrievalModes.External, ContourApproximationModes.ApproxSimple);");
            AddSnippet(opencvGlobal, "Cv2.Rectangle/Line", "Cv2.Rectangle(mat, new Rect(0,0,10,10), Scalar.Red, 2);");
            AddSnippet(opencvGlobal, "Cv2.PutText", "Cv2.PutText(mat, \"txt\", new Point(0,20), HersheyFonts.HersheySimplex, 0.5, Scalar.Green);");
            AddSnippet(opencvGlobal, "Cv2.BitwiseAnd/Or", "Cv2.BitwiseAnd(src1, src2, dst);");

            var context = new TreeNode("Context 日志") { ForeColor = Color.FromArgb(155, 200, 120) };
            AddSnippet(context, "Log(msg)", "Context.Log($\"info: ...\");");
            AddSnippet(context, "LogWarning(msg)", "Context.LogWarning($\"warn: ...\");");
            AddSnippet(context, "LogError(msg)", "Context.LogError($\"err: ...\");");

            var variables = new TreeNode("当前 Pin 变量") { ForeColor = Color.FromArgb(180, 180, 180) };
            foreach (var pin in _node.Inputs)
                AddSnippet(variables, $"{pin.Name} ({PinTypeLabel(pin.DataType)})", $"{pin.Name}");
            foreach (var pin in _node.Outputs)
                AddSnippet(variables, $"{pin.Name} ({PinTypeLabel(pin.DataType)})", $"{pin.Name}");

            _snippetTree.Nodes.Add(opencvMat);
            _snippetTree.Nodes.Add(opencvGlobal);
            _snippetTree.Nodes.Add(context);
            _snippetTree.Nodes.Add(variables);
            opencvMat.Expand();
            opencvGlobal.Expand();
            context.Expand();
            variables.Expand();

            _snippetTree.NodeMouseDoubleClick += (s, e) =>
            {
                if (e.Node?.Tag is string snippet)
                    InsertAtCursor(snippet);
            };

            return CreatePanelHeader("代码提示", _snippetTree);
        }

        private static void AddSnippet(TreeNode parent, string label, string snippet)
        {
            parent.Nodes.Add(new TreeNode(label) { Tag = snippet, ForeColor = Color.FromArgb(180, 180, 180) });
        }

        private void InsertAtCursor(string text)
        {
            _editor.InsertText(text);
            _editor.Focus();
        }

        private void UpdatePinTreeHighlight()
        {
            if (_pinTree == null || _editor == null) return;
            try
            {
                var line = _editor.GetLine(_editor.Selection.Start.iLine);
                var lineText = line.Text;
                foreach (TreeNode rootNode in _pinTree.Nodes)
                {
                    foreach (TreeNode pinNode in rootNode.Nodes)
                    {
                        var pinName = pinNode.Text.Split(' ')[0];
                        pinNode.NodeFont = lineText.Contains(pinName)
                            ? new Font(_pinTree.Font, FontStyle.Bold)
                            : null;
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"FCTB highlight error: {ex.Message}"); }
        }

        private static string PinTypeLabel(Type t)
        {
            if (t == typeof(string)) return "string";
            if (t == typeof(int)) return "int";
            if (t == typeof(double)) return "double";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(byte[])) return "byte[]";
            if (t == typeof(Bitmap)) return "Bitmap";
            if (t == typeof(OpenCvSharp.Mat)) return "Mat";
            if (t == typeof(OpenCvSharp.Point2d)) return "Point2d";
            if (t == typeof(Lib.Flow.Data.AcquisitionResult)) return "AcqResult";
            return t?.Name ?? "?";
        }

        private Panel CreatePanelHeader(string title, Control content)
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.FromArgb(50, 50, 50),
            };
            var lbl = new Label
            {
                Text = title,
                Location = new Point(6, 3),
                AutoSize = true,
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Microsoft YaHei", 8F),
                BackColor = Color.Transparent,
            };
            header.Controls.Add(lbl);

            panel.Controls.Add(content);   // Dock=Fill added first
            panel.Controls.Add(header);     // Dock=Top added second, takes priority
            return panel;
        }

        private void TestCompile()
        {
            _logBox.Clear();
            AppendLog("编译中...", Color.FromArgb(200, 200, 0));

            try
            {
                var node = new Lib.Flow.Nodes.ComputationNode
                {
                    SourceCode = _editor.Text,
                    ExtraReferences = ExtraReferences,
                    NuGetPackages = NuGetPackages,
                    IsDebug = IsDebug,
                };

                foreach (var pin in _node.Inputs)
                {
                    if (!node.Inputs.Any(p => p.Name == pin.Name))
                        node.AddInputPin(pin.Name, pin.DataType);
                }
                foreach (var pin in _node.Outputs)
                {
                    if (!node.Outputs.Any(p => p.Name == pin.Name))
                        node.AddOutputPin(pin.Name, pin.DataType);
                }

                var ok = node.Compile();
                if (ok)
                {
                    AppendLog("编译成功", Color.FromArgb(100, 200, 100));
                }
                else
                {
                    foreach (var err in node.CompileErrors)
                        AppendLog(err, Color.FromArgb(231, 76, 60));
                }
            }
            catch (Exception ex)
            {
                AppendLog("编译异常: " + ex.Message, Color.FromArgb(231, 76, 60));
            }
        }

        private void AppendLog(string text, Color color)
        {
            var ts = DateTime.Now.ToString("HH:mm:ss.fff");
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor = Color.FromArgb(120, 120, 120);
            _logBox.AppendText($"[{ts}] ");
            _logBox.SelectionColor = color;
            _logBox.AppendText(text + "\n");
            _logBox.ScrollToCaret();
        }
    }
}
