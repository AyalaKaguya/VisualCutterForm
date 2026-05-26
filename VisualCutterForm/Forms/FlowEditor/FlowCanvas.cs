using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using VisualMaster.WorkFlow;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.Forms.FlowEditor
{
    public class FlowCanvas : UserControl
    {
        private FlowSubGraph _subGraph;
        private readonly List<FlowNodeView> _nodeViews = new List<FlowNodeView>();
        private readonly Dictionary<Guid, FlowNodeView> _viewIndex = new Dictionary<Guid, FlowNodeView>();

        private Point _offset = new Point(0, 0);
        private Point _dragStart;
        private bool _isDraggingCanvas;
        private FlowNodeView _draggedNode;
        private Point _dragOffset;
        private bool _isConnecting;
        private Point _connectStart;
        private FlowNodeView _connectFromNode;
        private int _connectFromPinIndex;
        private bool _connectIsOutput;
        private float _zoom = 1f;
        private readonly Pen _connectPen = new Pen(Color.FromArgb(46, 204, 113), 1.5f);
        private readonly Pen _dashPen = new Pen(Color.FromArgb(46, 204, 113), 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        private readonly Font _font = new Font("Microsoft YaHei", 9F);

        public FlowSubGraph SubGraph
        {
            get => _subGraph;
            set
            {
                _subGraph = value;
                RebuildViews();
            }
        }

        public List<FlowNodeView> NodeViews => _nodeViews;
        public event Action<FlowNode> NodeSelected;
        public event Action<FlowNode, FlowNode, string, string> ConnectionCreated;
        public event Action<NodeConnection> ConnectionDeleted;
        public event Action DeselectAll;

        public FlowCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(40, 40, 40);
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);
        }

        public void RebuildViews()
        {
            _nodeViews.Clear();
            _viewIndex.Clear();
            if (_subGraph == null) return;

            var hasPos = _subGraph.Nodes.Any(n => n.NodeX != 0 || n.NodeY != 0);

            int x = 20, y = 20;
            foreach (var node in _subGraph.Nodes)
            {
                Point pos;
                if (hasPos && (node.NodeX != 0 || node.NodeY != 0))
                    pos = new Point((int)node.NodeX, (int)node.NodeY);
                else
                    pos = new Point(x, y);

                var view = new FlowNodeView(node, pos);
                _nodeViews.Add(view);
                _viewIndex[node.Id] = view;

                if (!hasPos)
                {
                    x += FlowNodeView.NodeWidth + 30;
                    if (x + FlowNodeView.NodeWidth > Width)
                    {
                        x = 20;
                        y += 200;
                    }
                }
            }

            foreach (var view in _nodeViews)
                view.ComputePinLocations(_offset, _zoom);

            Invalidate();
        }

        public FlowNodeView AddNode(Type nodeType, Point location)
        {
            if (_subGraph == null) return null;

            var node = NodeFactory.CreateNode(nodeType);
            _subGraph.Nodes.Add(node);
            _subGraph.RebuildNodeIndex();
            var canvasPos = ScreenToCanvas(location);
            node.NodeX = canvasPos.X;
            node.NodeY = canvasPos.Y;
            var view = new FlowNodeView(node, canvasPos);
            _nodeViews.Add(view);
            _viewIndex[node.Id] = view;
            Invalidate();
            return view;
        }

        public void RemoveNode(FlowNodeView view)
        {
            if (view == null || _subGraph == null) return;

            var connsToRemove = _subGraph.Connections
                .Where(c => c.FromNodeId == view.Node.Id || c.ToNodeId == view.Node.Id)
                .ToList();
            foreach (var c in connsToRemove)
            {
                _subGraph.Connections.Remove(c);
                ConnectionDeleted?.Invoke(c);
            }

            foreach (var inp in view.Node.Inputs) inp.Disconnect();
            foreach (var outp in view.Node.Outputs)
            {
                foreach (var t in outp.Targets.ToList()) t.Disconnect();
            }

            _subGraph.Nodes.Remove(view.Node);
            _subGraph.RebuildNodeIndex();
            _nodeViews.Remove(view);
            _viewIndex.Remove(view.Node.Id);
            DeselectAll?.Invoke();
            Invalidate();
        }

        private void RemoveConnection(NodeConnection conn)
        {
            if (conn == null || _subGraph == null) return;

            var fromNode = _subGraph.FindNode(conn.FromNodeId);
            var toNode = _subGraph.FindNode(conn.ToNodeId);

            if (fromNode != null && toNode != null)
            {
                var inPin = toNode.FindInput(conn.ToPinName);
                inPin?.Disconnect();
            }

            _subGraph.Connections.Remove(conn);
            ConnectionDeleted?.Invoke(conn);
            Invalidate();
        }

        private NodeConnection HitTestConnection(Point screenPoint)
        {
            if (_subGraph == null) return null;
            const int hitRadius = 8;

            foreach (var conn in _subGraph.Connections)
            {
                var fromView = FindView(_subGraph.FindNode(conn.FromNodeId));
                var toView = FindView(_subGraph.FindNode(conn.ToNodeId));
                if (fromView == null || toView == null) continue;

                int fromIdx = fromView.Node.Outputs.FindIndex(o => o.Name == conn.FromPinName);
                int toIdx = toView.Node.Inputs.FindIndex(i => i.Name == conn.ToPinName);
                if (fromIdx < 0 || toIdx < 0) continue;
                if (fromIdx >= fromView.OutputPinLocations.Count ||
                    toIdx >= toView.InputPinLocations.Count) continue;

                var p1 = fromView.OutputPinLocations[fromIdx];
                var p2 = toView.InputPinLocations[toIdx];

                int mx = (p1.X + p2.X) / 2;
                int my = (p1.Y + p2.Y) / 2;

                int sx = (int)((mx + _offset.X) * _zoom);
                int sy = (int)((my + _offset.Y) * _zoom);

                double dist = Math.Sqrt(Math.Pow(screenPoint.X - sx, 2) + Math.Pow(screenPoint.Y - sy, 2));
                if (dist <= hitRadius * _zoom)
                    return conn;
            }
            return null;
        }

        public FlowNodeView FindView(FlowNode node)
        {
            if (node == null) return null;
            _viewIndex.TryGetValue(node.Id, out var view);
            return view;
        }

        public void ClearSelection()
        {
            foreach (var v in _nodeViews) v.IsSelected = false;
            DeselectAll?.Invoke();
            Invalidate();
        }

        public void ZoomIn()
        {
            _zoom = Math.Min(_zoom * 1.2f, 3f);
            Invalidate();
        }

        public void ZoomOut()
        {
            _zoom = Math.Max(_zoom / 1.2f, 0.3f);
            Invalidate();
        }

        public void ResetView()
        {
            _zoom = 1f;
            _offset = Point.Empty;
            Invalidate();
        }

        private Point ScreenToCanvas(Point screen)
        {
            return new Point(
                (int)(screen.X / _zoom) - _offset.X,
                (int)(screen.Y / _zoom) - _offset.Y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (_subGraph == null) return;

            DrawConnections(g);

            foreach (var view in _nodeViews)
            {
                view.Draw(g, _font, _offset, _zoom);
                view.DrawTiming(g, _font, _offset, _zoom);
            }

            if (_isConnecting)
            {
                var end = PointToClient(MousePosition);
                var start = new Point(
                    (int)((_connectStart.X + _offset.X) * _zoom),
                    (int)((_connectStart.Y + _offset.Y) * _zoom));
                g.DrawLine(_dashPen, start, end);
            }
        }

        private void DrawConnections(Graphics g)
        {
            if (_subGraph == null) return;

            foreach (var conn in _subGraph.Connections)
            {
                var fromView = FindView(_subGraph.FindNode(conn.FromNodeId));
                var toView = FindView(_subGraph.FindNode(conn.ToNodeId));
                if (fromView == null || toView == null) continue;

                var fromPin = fromView.Node.FindOutput(conn.FromPinName);
                var toPin = toView.Node.FindInput(conn.ToPinName);
                if (fromPin == null || toPin == null) continue;

                int fromIdx = fromView.Node.Outputs.IndexOf(fromPin);
                int toIdx = toView.Node.Inputs.IndexOf(toPin);
                if (fromIdx < 0 || toIdx < 0) continue;

                if (fromIdx >= fromView.OutputPinLocations.Count ||
                    toIdx >= toView.InputPinLocations.Count) continue;

                var p1 = fromView.OutputPinLocations[fromIdx];
                var p2 = toView.InputPinLocations[toIdx];

                int x1 = (int)((p1.X + _offset.X) * _zoom);
                int y1 = (int)((p1.Y + _offset.Y) * _zoom);
                int x2 = (int)((p2.X + _offset.X) * _zoom);
                int y2 = (int)((p2.Y + _offset.Y) * _zoom);

                int cx = Math.Abs(x2 - x1) / 2;
                using (var gp = new GraphicsPath())
                {
                    gp.AddBezier(x1, y1,
                        x1 + cx, y1,
                        x2 - cx, y2,
                        x2, y2);
                    g.DrawPath(_connectPen, gp);
                }

                int midX = (x1 + x2) / 2;
                int midY = (y1 + y2) / 2;
                g.FillEllipse(Brushes.White, midX - 3, midY - 3, 6, 6);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Right)
            {
                for (int i = _nodeViews.Count - 1; i >= 0; i--)
                {
                    var view = _nodeViews[i];
                    if (view.HitTest(e.Location, _offset, _zoom))
                    {
                        ClearSelection();
                        view.IsSelected = true;
                        NodeSelected?.Invoke(view.Node);
                        Invalidate();

                        var ctx = new ContextMenuStrip();
                        ctx.Items.Add("删除节点", null, (s2, e2) => RemoveNode(view));
                        ctx.Items.Add("复制节点 ID", null, (s2, e2) => Clipboard.SetText(view.Node.Id.ToString()));
                        ctx.Show(this, e.Location);
                        return;
                    }
                }

                var hitConn = HitTestConnection(e.Location);
                if (hitConn != null)
                {
                    var ctx = new ContextMenuStrip();
                    ctx.Items.Add("删除连线", null, (s2, e2) => RemoveConnection(hitConn));
                    ctx.Show(this, e.Location);
                    return;
                }

                return;
            }

            if (e.Button == MouseButtons.Middle)
            {
                _isDraggingCanvas = true;
                _dragStart = e.Location;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                ClearSelection();

                for (int k = _nodeViews.Count - 1; k >= 0; k--)
                {
                    var view = _nodeViews[k];
                    int pinHit = view.HitTestPin(e.Location, _offset, _zoom);
                    if (pinHit != 0)
                    {
                        _isConnecting = true;
                        _connectFromNode = view;

                        if (pinHit > 0)
                        {
                            _connectIsOutput = true;
                            _connectFromPinIndex = pinHit - 1;
                            if (_connectFromPinIndex < view.OutputPinLocations.Count)
                                _connectStart = view.OutputPinLocations[_connectFromPinIndex];
                        }
                        else
                        {
                            _connectIsOutput = false;
                            _connectFromPinIndex = (-pinHit) - 1;
                            if (_connectFromPinIndex < view.InputPinLocations.Count)
                                _connectStart = view.InputPinLocations[_connectFromPinIndex];
                        }
                        return;
                    }
                }

                for (int j = _nodeViews.Count - 1; j >= 0; j--)
                {
                    var view = _nodeViews[j];
                    if (view.HitTest(e.Location, _offset, _zoom))
                    {
                        view.IsSelected = true;
                        _draggedNode = view;
                        _dragStart = e.Location;
                        _dragOffset = new Point(e.Location.X - (int)((view.Bounds.X + _offset.X) * _zoom),
                            e.Location.Y - (int)((view.Bounds.Y + _offset.Y) * _zoom));
                        NodeSelected?.Invoke(view.Node);
                        Invalidate();
                        return;
                    }
                }

                _isDraggingCanvas = true;
                _dragStart = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDraggingCanvas)
            {
                int dx = e.X - _dragStart.X;
                int dy = e.Y - _dragStart.Y;
                _offset = new Point(_offset.X + (int)(dx / _zoom), _offset.Y + (int)(dy / _zoom));
                _dragStart = e.Location;
                Invalidate();
                return;
            }

            if (_draggedNode != null)
            {
                int nx = (int)((e.X - _dragOffset.X) / _zoom) - _offset.X;
                int ny = (int)((e.Y - _dragOffset.Y) / _zoom) - _offset.Y;
                _draggedNode.UpdateBounds(new Point(nx, ny));
                _draggedNode.Node.NodeX = nx;
                _draggedNode.Node.NodeY = ny;
                Invalidate();
                return;
            }

            if (_isConnecting)
            {
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isConnecting)
            {
                foreach (var view in _nodeViews)
                {
                    if (view == _connectFromNode) continue;

                    int pinHit = view.HitTestPin(e.Location, _offset, _zoom);
                    if (pinHit != 0)
                    {
                        FlowNode fromNode, toNode;
                        string fromPin, toPin;

                        if (_connectIsOutput && pinHit < 0)
                        {
                            fromNode = _connectFromNode.Node;
                            toNode = view.Node;
                            fromPin = _connectFromNode.Node.Outputs[_connectFromPinIndex].Name;
                            toPin = view.Node.Inputs[(-pinHit) - 1].Name;
                        }
                        else if (!_connectIsOutput && pinHit > 0)
                        {
                            fromNode = view.Node;
                            toNode = _connectFromNode.Node;
                            fromPin = view.Node.Outputs[pinHit - 1].Name;
                            toPin = _connectFromNode.Node.Inputs[_connectFromPinIndex].Name;
                        }
                        else
                        {
                            break;
                        }

                        if (fromNode != toNode)
                        {
                            var outPin = fromNode.FindOutput(fromPin);
                            var inPin = toNode.FindInput(toPin);

                            if (outPin != null && inPin != null)
                            {
                                try
                                {
                                    inPin.Connect(outPin);

                                    var existing = _subGraph.Connections.Find(c =>
                                        c.FromNodeId == fromNode.Id && c.FromPinName == fromPin &&
                                        c.ToNodeId == toNode.Id && c.ToPinName == toPin);

                                    if (existing == null)
                                    {
                                        var conn = new NodeConnection(fromNode.Id, fromPin, toNode.Id, toPin);
                                        _subGraph.Connections.Add(conn);
                                    }

                                    ConnectionCreated?.Invoke(fromNode, toNode, fromPin, toPin);
                                }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Connection create error: {ex.Message}"); }
                            }
                        }
                        break;
                    }
                }

                _isConnecting = false;
                Invalidate();
            }

            _draggedNode = null;
            _isDraggingCanvas = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                if (e.Delta > 0) ZoomIn();
                else ZoomOut();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Delete)
            {
                var sel = _nodeViews.Find(v => v.IsSelected);
                if (sel != null)
                    RemoveNode(sel);
            }
        }
    }
}
