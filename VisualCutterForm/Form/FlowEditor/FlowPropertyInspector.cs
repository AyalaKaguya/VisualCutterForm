using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VisualMaster.WorkFlow;

namespace VisualCutterForm.FlowEditor
{
    public class FlowPropertyInspector : UserControl
    {
        private Label _lblNodeName;
        private Label _lblNodeType;
        private FlowNode _selectedNode;
        private FlowSubGraph _selectedSubGraph;
        private TextBox _txtNewPinName;
        private ComboBox _cmbNewPinType;
        private Button _btnAddInput;
        private Button _btnAddOutput;
        private Panel _scrollPanel;

        public event Action<FlowNode, string, object> PropertyChanged;

        public Func<List<string>> GetActiveCameras { get; set; }

        public FlowPropertyInspector()
        {
            BackColor = Color.FromArgb(55, 55, 55);

            var header = new Label
            {
                Text = "属性面板",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Microsoft YaHei", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
            };

            _lblNodeName = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 152, 219),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "未选择节点",
            };

            _lblNodeType = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Microsoft YaHei", 8F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
            };

            _scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(50, 50, 50),
            };

            Controls.Add(_scrollPanel);
            Controls.Add(_lblNodeType);
            Controls.Add(_lblNodeName);
            Controls.Add(header);

            Resize += (s, e) => RebuildScrollContent();
        }

        private void RebuildScrollContent()
        {
            _scrollPanel.Controls.Clear();

            if (_selectedNode == null) return;

            var w = _scrollPanel.ClientSize.Width - 4;
            if (w < 100) w = 200;

            int y = 4;

            var propsHeader = new Label
            {
                Text = "━━ 属性 ━━",
                Location = new Point(4, y),
                Size = new Size(w, 18),
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            _scrollPanel.Controls.Add(propsHeader);
            y += 22;

            var props = _selectedNode.GetNodeProperties();
            foreach (var pd in props)
            {
                var lbl = new Label
                {
                    Text = pd.DisplayName,
                    Location = new Point(8, y + 4),
                    Size = new Size(100, 18),
                    ForeColor = Color.FromArgb(200, 200, 200),
                    Font = new Font("Microsoft YaHei", 8.5F),
                };

                Control editor = CreateEditor(pd);
                if (editor != null)
                {
                    editor.Location = new Point(110, y + 2);
                    editor.Width = Math.Max(w - 118, 50);
                    editor.Tag = pd;
                    _scrollPanel.Controls.Add(lbl);
                    _scrollPanel.Controls.Add(editor);
                    y += 28;
                }
            }

            y += 8;

            var pinsHeader = new Label
            {
                Text = $"━━ 引脚 (入:{_selectedNode.Inputs.Count} 出:{_selectedNode.Outputs.Count}) ━━",
                Location = new Point(4, y),
                Size = new Size(w, 18),
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            _scrollPanel.Controls.Add(pinsHeader);
            y += 22;

            _txtNewPinName = new TextBox
            {
                Location = new Point(8, y + 2),
                Size = new Size(Math.Min(w - 100, 90), 22),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            _scrollPanel.Controls.Add(_txtNewPinName);

            _cmbNewPinType = new ComboBox
            {
                Location = new Point(_txtNewPinName.Right + 4, y + 2),
                Size = new Size(70, 22),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            _cmbNewPinType.Items.AddRange(new object[] { "Mat", "AcqResult", "double", "int", "bool", "string", "Point2d", "Bitmap", "byte[]" });
            _cmbNewPinType.SelectedIndex = 0;
            _scrollPanel.Controls.Add(_cmbNewPinType);

            _btnAddInput = new Button
            {
                Text = "+入",
                Location = new Point(8, y + 26),
                Size = new Size(45, 22),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 152, 219),
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnAddInput.FlatAppearance.BorderSize = 0;
            _btnAddInput.Click += (s, e2) => AddUserPin(true);
            _scrollPanel.Controls.Add(_btnAddInput);

            _btnAddOutput = new Button
            {
                Text = "+出",
                Location = new Point(57, y + 26),
                Size = new Size(45, 22),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Microsoft YaHei", 8F),
            };
            _btnAddOutput.FlatAppearance.BorderSize = 0;
            _btnAddOutput.Click += (s, e2) => AddUserPin(false);
            _scrollPanel.Controls.Add(_btnAddOutput);

            y += 52;
            y += BuildPinList(y, w);

            _scrollPanel.Height = y + 20;
        }

        private int BuildPinList(int startY, int w)
        {
            int y = startY;
            if (_selectedNode == null) return y;

            foreach (var nodePin in _selectedNode.Inputs)
            {
                if (!(nodePin is InputPin pin)) continue;
                var typeLabel = $"【{pin.TypeDisplayName}】";
                var lbl = new Label
                {
                    Text = $"▶ {pin.Name}  {typeLabel}",
                    Location = new Point(8, y + 3),
                    Size = new Size(Math.Min(w - 36, 180), 18),
                    ForeColor = Color.FromArgb(52, 152, 219),
                    Font = new Font("Microsoft YaHei", 8F),
                };
                _scrollPanel.Controls.Add(lbl);

                if (pin.UserDefined)
                {
                    var btn = new Button
                    {
                        Text = "×",
                        Size = new Size(22, 20),
                        Location = new Point(w - 30, y + 2),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.FromArgb(231, 76, 60),
                        BackColor = Color.FromArgb(60, 60, 60),
                        Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold),
                        Tag = new PinRef { Node = _selectedNode, Name = pin.Name },
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Click += OnDeletePinClick;
                    _scrollPanel.Controls.Add(btn);
                }
                y += 22;

                y += AddPinValueRow(w, y, pin);

                if (!pin.IsConnected)
                {
                    var defLabel = new Label
                    {
                        Text = "  默认值:",
                        Location = new Point(16, y + 4),
                        Size = new Size(50, 18),
                        ForeColor = Color.FromArgb(140, 140, 140),
                        Font = new Font("Microsoft YaHei", 7.5F),
                    };
                    var defEdit = CreateDefaultEditor(pin, w);
                    if (defEdit != null)
                    {
                        defEdit.Location = new Point(66, y + 2);
                        _scrollPanel.Controls.Add(defLabel);
                        _scrollPanel.Controls.Add(defEdit);
                        y += 24;
                    }
                }
            }

            foreach (var pin in _selectedNode.Outputs)
            {
                var typeLabel = $"【{pin.TypeDisplayName}】";
                var lbl = new Label
                {
                    Text = $"■ {pin.Name}  {typeLabel}",
                    Location = new Point(8, y + 3),
                    Size = new Size(Math.Min(w - 36, 180), 18),
                    ForeColor = Color.FromArgb(46, 204, 113),
                    Font = new Font("Microsoft YaHei", 8F),
                };
                _scrollPanel.Controls.Add(lbl);

                if (pin.UserDefined)
                {
                    var btn = new Button
                    {
                        Text = "×",
                        Size = new Size(22, 20),
                        Location = new Point(w - 30, y + 2),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.FromArgb(231, 76, 60),
                        BackColor = Color.FromArgb(60, 60, 60),
                        Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold),
                        Tag = new PinRef { Node = _selectedNode, Name = pin.Name },
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Click += OnDeletePinClick;
                    _scrollPanel.Controls.Add(btn);
                }
                y += 22;

                y += AddPinValueRow(w, y, pin);
            }

            return y - startY;
        }

        private int AddPinValueRow(int w, int y, NodePin pin)
        {
            var valText = FormatInspectorPinValue(pin.LastValue);
            if (valText == null) return 0;

            var lbl = new Label
            {
                Text = $"  值: {valText}",
                Location = new Point(16, y + 2),
                Size = new Size(Math.Min(w - 32, 160), 16),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Microsoft YaHei", 7.5F),
                AutoSize = true,
            };
            _scrollPanel.Controls.Add(lbl);
            return 20;
        }

        private static string FormatInspectorPinValue(object val)
        {
            if (val == null) return null;
            if (val is OpenCvSharp.Mat m && !m.IsDisposed && !m.Empty())
                return $"Mat {m.Width}x{m.Height}";
            if (val is System.Drawing.Bitmap bmp)
                return $"Bitmap {bmp.Width}x{bmp.Height}";
            if (val is VisualMaster.WorkFlow.Data.AcquisitionResult ar)
                return $"AcqResult {ar.Width}x{ar.Height}";
            if (val is string s)
                return s.Length > 30 ? $"\"{s.Substring(0, 28)}…\"" : $"\"{s}\"";
            if (val is int i) return i.ToString();
            if (val is float f) return f.ToString("0.##");
            if (val is double d) return d.ToString("0.##");
            if (val is bool b) return b.ToString();
            if (val is long l) return l.ToString();
            return $"({val.GetType().Name})";
        }

        private Control CreateDefaultEditor(InputPin pin, int w)
        {
            if (pin.DataType == typeof(string))
            {
                var txt = new TextBox
                {
                    Size = new Size(Math.Min(w - 80, 120), 20),
                    Text = pin.DefaultValue?.ToString() ?? "",
                    BackColor = Color.FromArgb(70, 70, 70),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Microsoft YaHei", 8F),
                };
                txt.TextChanged += (s, e) =>
                {
                    pin.DefaultValue = txt.Text;
                    PropertyChanged?.Invoke(_selectedNode, pin.Name, txt.Text);
                };
                return txt;
            }

            if (pin.DataType == typeof(int) || pin.DataType == typeof(double) ||
                pin.DataType == typeof(float))
            {
                var num = new NumericUpDown
                {
                    Size = new Size(90, 20),
                    Minimum = -1000000,
                    Maximum = 1000000,
                    Value = Convert.ToDecimal(pin.DefaultValue ?? 0),
                    BackColor = Color.FromArgb(70, 70, 70),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    DecimalPlaces = pin.DataType == typeof(int) ? 0 : 3,
                    Font = new Font("Microsoft YaHei", 8F),
                };
                num.ValueChanged += (s, e) =>
                {
                    pin.DefaultValue = Convert.ChangeType(num.Value, pin.DataType);
                };
                return num;
            }

            return null;
        }

        private void AddUserPin(bool isInput)
        {
            if (_selectedNode == null) return;

            var name = (_txtNewPinName?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name)) return;

            var typeName = _cmbNewPinType?.SelectedItem?.ToString() ?? "Mat";
            var type = PinTypeResolver.Resolve(typeName);
            if (type == null) return;

            if (isInput)
                _selectedNode.AddInputPin(name, type);
            else
                _selectedNode.AddOutputPin(name, type);

            if (_txtNewPinName != null) _txtNewPinName.Text = "";
            RebuildScrollContent();
            PropertyChanged?.Invoke(_selectedNode, "PinsChanged", null);
        }

        private void OnDeletePinClick(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is PinRef ref_)
            {
                ref_.Node.RemovePin(ref_.Name);
                RebuildScrollContent();
                PropertyChanged?.Invoke(_selectedNode, "PinsChanged", null);
            }
        }

        public void ShowNode(FlowNode node)
        {
            if (_selectedNode != null)
                _selectedNode.OnPinsChanged -= OnNodePinsChanged;

            _selectedNode = node;

            if (node == null)
            {
                _lblNodeName.Text = "未选择节点";
                _lblNodeType.Text = "";
                _scrollPanel.Controls.Clear();
                return;
            }

            node.OnPinsChanged += OnNodePinsChanged;
            _lblNodeName.Text = node.Name;
            _lblNodeType.Text = $"{node.Category} · {node.GetType().Name}";
            RebuildScrollContent();
        }

        public void ShowSubGraph(FlowSubGraph sg)
        {
            _selectedSubGraph = sg;
            if (sg == null)
            {
                if (_selectedNode == null)
                {
                    _lblNodeName.Text = "未选择节点 / 子图";
                    _lblNodeType.Text = "";
                }
                _scrollPanel.Controls.Clear();
                return;
            }

            if (_selectedNode != null) return;

            _lblNodeName.Text = sg.Name;
            _lblNodeType.Text = $"子图 · {sg.Nodes.Count} 个节点";

            _scrollPanel.Controls.Clear();

            var innerWidth = _scrollPanel.ClientSize.Width - 16;
            var y = 4;

            AddSubGraphPropertyRow(innerWidth, ref y, "触发模式", sg.Trigger.ToDisplayName());
            AddSubGraphPropertyRow(innerWidth, ref y, "节点数", sg.Nodes.Count.ToString());
            AddSubGraphPropertyRow(innerWidth, ref y, "连线数", sg.Connections.Count.ToString());
            AddSubGraphPropertyRow(innerWidth, ref y, "ID", sg.Id.ToString("N").Substring(0, 8));

            _scrollPanel.Height = y + 20;
        }

        private void AddSubGraphPropertyRow(int width, ref int y, string label, string value)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(8, y + 4),
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Microsoft YaHei", 8.5F),
                AutoSize = true,
            };
            var val = new Label
            {
                Text = value ?? "-",
                Location = new Point(width / 2, y + 4),
                Size = new Size(width / 2 - 8, 18),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Bold),
            };
            _scrollPanel.Controls.Add(lbl);
            _scrollPanel.Controls.Add(val);
            y += 22;
        }

        private void OnNodePinsChanged(FlowNode node)
        {
            if (_selectedNode == node)
            {
                RebuildScrollContent();
            }
        }

        private class PinRef
        {
            public FlowNode Node;
            public string Name;
        }

        private Control CreateEditor(NodePropertyDescriptor pd)
        {
            if (pd.PropertyType == typeof(bool))
            {
                var chk = new CheckBox
                {
                    Size = new Size(140, 22),
                    Checked = pd.Getter() is bool b && b,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(50, 50, 50),
                };
                chk.CheckedChanged += (s, e) =>
                {
                    pd.Setter(chk.Checked);
                    PropertyChanged?.Invoke(_selectedNode, pd.Name, chk.Checked);
                };
                return chk;
            }

            if (pd.PropertyType == typeof(string))
            {
                if (pd.Name == "SourceCode" || pd.Name == "源代码")
                {
                    var btnEdit = new Button
                    {
                        Text = "编辑代码...",
                        Size = new Size(200, 28),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.FromArgb(52, 152, 219),
                        BackColor = Color.FromArgb(60, 60, 60),
                        Font = new Font("Microsoft YaHei", 9F),
                    };
                    btnEdit.FlatAppearance.BorderSize = 0;
                    btnEdit.Click += (s2, e2) =>
                    {
                        using (var editor = new CodeEditorForm(_selectedNode))
                        {
                            if (editor.ShowDialog() == DialogResult.OK)
                            {
                                var code = editor.SourceCode;
                                pd.Setter(code);
                                PropertyChanged?.Invoke(_selectedNode, pd.Name, code);

                                _selectedNode.SetNodeProperty("ExtraReferences", editor.ExtraReferences);
                                _selectedNode.SetNodeProperty("NuGetPackages", editor.NuGetPackages);
                                _selectedNode.SetNodeProperty("IsDebug", editor.IsDebug);

                                PropertyChanged?.Invoke(_selectedNode, "ExtraReferences", editor.ExtraReferences);
                                PropertyChanged?.Invoke(_selectedNode, "NuGetPackages", editor.NuGetPackages);
                            }
                        }
                    };
                    return btnEdit;
                }

                if (pd.Name == "CameraSerial" || pd.Name == "相机序列号")
                {
                    var cmb = new ComboBox
                    {
                        Size = new Size(200, 22),
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        BackColor = Color.FromArgb(70, 70, 70),
                        ForeColor = Color.White,
                    };

                    var activeCameras = GetActiveCameras?.Invoke();
                    if (activeCameras != null)
                    {
                        cmb.Items.Add("(首个活跃相机)");
                        foreach (var ser in activeCameras)
                            cmb.Items.Add(ser);
                    }

                    var curVal = pd.Getter()?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(curVal) && cmb.Items.Contains(curVal))
                        cmb.SelectedItem = curVal;
                    else if (cmb.Items.Count > 0)
                        cmb.SelectedIndex = 0;

                    var serSetter = cmb;
                    cmb.SelectedIndexChanged += (s3, e3) =>
                    {
                        var sel = serSetter.SelectedItem?.ToString();
                        if (sel == "(首个活跃相机)") sel = "";
                        pd.Setter(sel);
                        PropertyChanged?.Invoke(_selectedNode, pd.Name, sel);
                    };
                    return cmb;
                }

                if (pd.Name.ToLower().Contains("port"))
                {
                    var cmb = new ComboBox
                    {
                        Size = new Size(200, 22),
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        BackColor = Color.FromArgb(70, 70, 70),
                        ForeColor = Color.White,
                    };
                    try
                    {
                        cmb.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Serial port enum error: {ex.Message}"); }
                    if (cmb.Items.Count == 0) cmb.Items.Add("COM1");
                    var curVal = pd.Getter()?.ToString();
                    if (!string.IsNullOrEmpty(curVal) && cmb.Items.Contains(curVal))
                        cmb.SelectedItem = curVal;
                    else if (cmb.Items.Count > 0)
                        cmb.SelectedIndex = 0;

                    cmb.SelectedIndexChanged += (s, e) =>
                    {
                        var sel = cmb.SelectedItem?.ToString();
                        pd.Setter(sel);
                        PropertyChanged?.Invoke(_selectedNode, pd.Name, sel);
                    };
                    return cmb;
                }

                if (pd.Name == "ExtraReferences" || pd.Name == "额外引用"
                    || pd.Name == "NuGetPackages" || pd.Name == "NuGet包")
                {
                    var refTxt = new TextBox
                    {
                        Size = new Size(200, 22),
                        Text = pd.Getter()?.ToString() ?? "",
                        BackColor = Color.FromArgb(70, 70, 70),
                        ForeColor = Color.White,
                        BorderStyle = BorderStyle.FixedSingle,
                        ReadOnly = true,
                    };
                    return refTxt;
                }

                var txt = new TextBox
                {
                    Size = new Size(200, 22),
                    Text = pd.Getter()?.ToString() ?? "",
                    BackColor = Color.FromArgb(70, 70, 70),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                };
                txt.TextChanged += (s, e) =>
                {
                    pd.Setter(txt.Text);
                    if (pd.Name == "Name") _lblNodeName.Text = txt.Text;
                    PropertyChanged?.Invoke(_selectedNode, pd.Name, txt.Text);
                };
                return txt;
            }

            if (pd.PropertyType == typeof(int) || pd.PropertyType == typeof(float) ||
                pd.PropertyType == typeof(double) || pd.PropertyType == typeof(long))
            {
                var min = SafeDecimal(pd.Min, decimal.MinValue);
                var max = SafeDecimal(pd.Max, decimal.MaxValue);

                var num = new NumericUpDown
                {
                    Size = new Size(120, 22),
                    Minimum = min,
                    Maximum = max,
                    Value = ClampDecimal(Convert.ToDecimal(pd.Getter() ?? pd.DefaultValue ?? 0), min, max),
                    BackColor = Color.FromArgb(70, 70, 70),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                };
                num.ValueChanged += (s, e) =>
                {
                    var val = Convert.ChangeType(num.Value, pd.PropertyType);
                    pd.Setter(val);
                    PropertyChanged?.Invoke(_selectedNode, pd.Name, val);
                };
                return num;
            }

            if (pd.PropertyType.IsEnum)
            {
                var cmb = new ComboBox
                {
                    Size = new Size(180, 22),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(70, 70, 70),
                    ForeColor = Color.White,
                };
                cmb.Items.AddRange(Enum.GetNames(pd.PropertyType));
                cmb.SelectedItem = pd.Getter()?.ToString();
                cmb.SelectedIndexChanged += (s, e) =>
                {
                    var val = Enum.Parse(pd.PropertyType, cmb.SelectedItem.ToString());
                    pd.Setter(val);
                    PropertyChanged?.Invoke(_selectedNode, pd.Name, val);
                };
                return cmb;
            }

            var defaultTxt = new TextBox
            {
                Size = new Size(200, 22),
                Text = pd.Getter()?.ToString() ?? "",
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
            };
            return defaultTxt;
        }

        private static decimal SafeDecimal(double val, decimal fallback)
        {
            if (double.IsInfinity(val) || double.IsNaN(val)) return fallback;
            if (val < (double)decimal.MinValue) return decimal.MinValue;
            if (val > (double)decimal.MaxValue) return decimal.MaxValue;
            try { return (decimal)val; }
            catch { return fallback; }
        }

        private static decimal ClampDecimal(decimal val, decimal min, decimal max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
