using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualCutterForm.Lib.Flow;

namespace VisualCutterForm.FlowEditor
{
    public class FlowNodeView
    {
        public FlowNode Node { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsSelected { get; set; }
        public bool IsDragging { get; set; }
        public List<Point> InputPinLocations { get; } = new List<Point>();
        public List<Point> OutputPinLocations { get; } = new List<Point>();

        public const int NodeWidth = 180;
        public const int HeaderHeight = 28;
        public const int PinRadius = 5;
        public const int RowHeight = 20;
        public const int PinGap = 7;

        private static readonly Color[] CategoryColors = new[]
        {
            Color.FromArgb(52, 152, 219),   // Blue
            Color.FromArgb(46, 204, 113),   // Green
            Color.FromArgb(155, 89, 182),   // Purple
            Color.FromArgb(230, 126, 34),   // Orange
        };

        public FlowNodeView(FlowNode node, Point location)
        {
            Node = node;
            UpdateBounds(location);
        }

        public void UpdateBounds(Point location)
        {
            int pinCount = Math.Max(Node.Inputs.Count, Node.Outputs.Count);
            int rows = Math.Max(pinCount, Node.GetNodeProperties().Count);
            int height = HeaderHeight + rows * RowHeight + 12;

            Bounds = new Rectangle(location.X, location.Y, NodeWidth, height);
        }

        public void Draw(Graphics g, Font font, Point offset, float zoom)
        {
            var bounds = ScaleBounds(offset, zoom);
            var color = GetCategoryColor();

            using (var bgBrush = new SolidBrush(Color.FromArgb(245, 245, 245)))
            using (var headerBrush = new SolidBrush(color))
            using (var borderPen = new Pen(IsSelected ? Color.FromArgb(41, 128, 185) : Color.FromArgb(180, 180, 180), IsSelected ? 2f : 1f))
            using (var headerFont = new Font(font.FontFamily, font.Size * zoom, FontStyle.Bold))
            using (var textFont = new Font(font.FontFamily, font.Size * zoom * 0.85f))
            using (var textBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, bounds.X + 3, bounds.Y + 3, bounds.Width, bounds.Height);
                g.FillRectangle(bgBrush, bounds);
                g.DrawRectangle(borderPen, bounds);
                g.FillRectangle(headerBrush, bounds.X, bounds.Y, bounds.Width, HeaderHeight * zoom);
                g.DrawString(Node.Name, headerFont, Brushes.White,
                    bounds.X + 6, bounds.Y + 4);

                var strFmt = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                };

                InputPinLocations.Clear();
                for (int i = 0; i < Node.Inputs.Count; i++)
                {
                    int y = (int)(bounds.Y + HeaderHeight * zoom + 5 + i * RowHeight * zoom);
                    int pinX = bounds.Left + PinGap;
                    int pinY = (int)(y + RowHeight * zoom / 2);

                    var pinRect = new Rectangle(pinX - PinRadius, pinY - PinRadius, PinRadius * 2, PinRadius * 2);
                    InputPinLocations.Add(new Point(
                        (int)(pinX / zoom) - offset.X,
                        (int)(pinY / zoom) - offset.Y));

                    using (var pinBrush = new SolidBrush(Node.Inputs[i].IsConnected ? Color.FromArgb(46, 204, 113) : Color.FromArgb(180, 180, 180)))
                        g.FillEllipse(pinBrush, pinRect);

                    g.DrawString(Node.Inputs[i].Name, textFont, textBrush,
                        new RectangleF(bounds.X + 20, y, bounds.Width - 24, RowHeight * zoom), strFmt);
                }

                OutputPinLocations.Clear();
                for (int i = 0; i < Node.Outputs.Count; i++)
                {
                    int y = (int)(bounds.Y + HeaderHeight * zoom + 5 + i * RowHeight * zoom);
                    int pinX = bounds.Right - PinGap;
                    int pinY = (int)(y + RowHeight * zoom / 2);

                    var pinRect = new Rectangle(pinX - PinRadius, pinY - PinRadius, PinRadius * 2, PinRadius * 2);
                    OutputPinLocations.Add(new Point(
                        (int)(pinX / zoom) - offset.X,
                        (int)(pinY / zoom) - offset.Y));

                    using (var pinBrush = new SolidBrush(Node.Outputs[i].Targets.Count > 0 ? Color.FromArgb(46, 204, 113) : Color.FromArgb(180, 180, 180)))
                        g.FillEllipse(pinBrush, pinRect);

                    strFmt.Alignment = StringAlignment.Far;
                    g.DrawString(Node.Outputs[i].Name, textFont, textBrush,
                        new RectangleF(bounds.X + 4, y, bounds.Width - 24, RowHeight * zoom), strFmt);
                    strFmt.Alignment = StringAlignment.Near;
                }
            }
        }

        /// <summary>
        /// Renders the last execution time overlay on the node.
        /// Call after Draw() for correct z-order.
        /// </summary>
        public void DrawTiming(Graphics g, Font font, Point offset, float zoom)
        {
            if (Node.LastExecutionTimeMs <= 0) return;

            var bounds = ScaleBounds(offset, zoom);
            var text = Node.LastExecutionTimeMs < 1
                ? $"{Node.LastExecutionTimeMs * 1000:F0}μs"
                : $"{Node.LastExecutionTimeMs:F1}ms";

            using (var timeFont = new Font(font.FontFamily, font.Size * zoom * 0.7f))
            {
                var sz = g.MeasureString(text, timeFont);
                var tx = bounds.Right - sz.Width - 6;
                var ty = bounds.Y + 4;
                using (var bg = new SolidBrush(Color.FromArgb(160, 40, 40, 40)))
                {
                    g.FillRectangle(bg, tx - 2, ty - 1, sz.Width + 4, sz.Height + 2);
                }
                using (var timeBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
                    g.DrawString(text, timeFont, timeBrush, tx, ty);
            }
        }

        public Rectangle ScaleBounds(Point offset, float zoom)
        {
            return new Rectangle(
                (int)((Bounds.X + offset.X) * zoom),
                (int)((Bounds.Y + offset.Y) * zoom),
                (int)(Bounds.Width * zoom),
                (int)(Bounds.Height * zoom));
        }

        public int HitTestPin(Point screenPoint, Point offset, float zoom)
        {
            for (int i = 0; i < InputPinLocations.Count; i++)
            {
                var pt = InputPinLocations[i];
                int sx = (int)((pt.X + offset.X) * zoom);
                int sy = (int)((pt.Y + offset.Y) * zoom);
                double dist = Math.Sqrt(Math.Pow(screenPoint.X - sx, 2) + Math.Pow(screenPoint.Y - sy, 2));
                if (dist <= PinRadius * 2)
                    return -(i + 1);
            }

            for (int i = 0; i < OutputPinLocations.Count; i++)
            {
                var pt = OutputPinLocations[i];
                int sx = (int)((pt.X + offset.X) * zoom);
                int sy = (int)((pt.Y + offset.Y) * zoom);
                double dist = Math.Sqrt(Math.Pow(screenPoint.X - sx, 2) + Math.Pow(screenPoint.Y - sy, 2));
                if (dist <= PinRadius * 2)
                    return (i + 1);
            }

            return 0;
        }

        public bool HitTest(Point screenPoint, Point offset, float zoom)
        {
            var bounds = ScaleBounds(offset, zoom);
            return bounds.Contains(screenPoint);
        }

        private Color GetCategoryColor()
        {
            var cat = Node.Category ?? "";
            int hash = Math.Abs(cat.GetHashCode());
            return CategoryColors[hash % CategoryColors.Length];
        }
    }
}
