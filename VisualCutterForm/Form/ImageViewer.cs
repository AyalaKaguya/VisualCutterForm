using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualCutterForm
{
    public class ImageViewer : UserControl
    {
        private readonly Panel _toolbarPanel;
        private readonly Button _btnZoomIn;
        private readonly Button _btnZoomOut;
        private readonly Button _btnFit;
        private readonly Button _btnActual;
        private readonly Label _lblZoom;

        private Bitmap _sourceImage;
        private float _zoom = 1f;
        private bool _fitToScreen = true;
        private Point _scrollOffset;

        private bool _isPanning;
        private Point _panStart;
        private Point _panScrollStart;

        private const float MinZoom = 0.05f;
        private const float MaxZoom = 20f;
        private const float ZoomStep = 1.25f;

        private static readonly Color DarkBg = Color.FromArgb(40, 40, 40);

        public Image Image
        {
            get => _sourceImage;
            set => SetImage(value as Bitmap);
        }

        public ImageViewer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer, true);
            BackColor = DarkBg;

            // ── Toolbar at top-right ──
            _toolbarPanel = new Panel
            {
                Size = new Size(180, 28),
                BackColor = Color.FromArgb(200, 30, 30, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            _btnZoomOut = MakeToolBtn("−", 4);
            _lblZoom = new Label
            {
                Text = "适应",
                Location = new Point(40, 4),
                Size = new Size(50, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _btnZoomIn = MakeToolBtn("+", 90);
            _btnFit = MakeToolBtn("⊡", 130);
            _btnActual = new Button
            {
                Text = "1:1",
                Location = new Point(152, 2),
                Size = new Size(26, 24),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
            };

            _btnZoomOut.Click += (s, e) => ZoomOut();
            _btnZoomIn.Click += (s, e) => ZoomIn();
            _btnFit.Click += (s, e) => ZoomFit();
            _btnActual.Click += (s, e) => ZoomActual();

            _toolbarPanel.Controls.Add(_btnZoomOut);
            _toolbarPanel.Controls.Add(_lblZoom);
            _toolbarPanel.Controls.Add(_btnZoomIn);
            _toolbarPanel.Controls.Add(_btnFit);
            _toolbarPanel.Controls.Add(_btnActual);
            Controls.Add(_toolbarPanel);

            Resize += (s, e) => PositionToolbar();
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x020A) // WM_MOUSEWHEEL
            {
                var clientPos = PointToClient(MousePosition);
                if (ClientRectangle.Contains(clientPos) && _sourceImage != null)
                {
                    int delta = (short)((uint)m.WParam >> 16);
                    if (ModifierKeys.HasFlag(Keys.Control))
                    {
                        float factor = delta > 0 ? 1.12f : 1f / 1.12f;
                        ZoomAtCursor(factor, clientPos);
                    }
                    else if (!_fitToScreen)
                    {
                        _scrollOffset.Y -= delta / 2;
                        ClampScroll();
                        Invalidate();
                    }
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private Button MakeToolBtn(string text, int x)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 2),
                Size = new Size(32, 24),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
            };
        }

        private void PositionToolbar()
        {
            _toolbarPanel.Location = new Point(
                ClientSize.Width - _toolbarPanel.Width - 4, 4);
            _toolbarPanel.BringToFront();
        }

        public void SetImage(Bitmap bmp)
        {
            _sourceImage?.Dispose();
            _sourceImage = bmp;
            _scrollOffset = Point.Empty;
            Invalidate();
        }

        public void ZoomIn()
        {
            _fitToScreen = false;
            _zoom = Math.Min(_zoom * ZoomStep, MaxZoom);
            ClampScroll();
            UpdateLabel();
            Invalidate();
        }

        public void ZoomOut()
        {
            _fitToScreen = false;
            _zoom = Math.Max(_zoom / ZoomStep, MinZoom);
            ClampScroll();
            UpdateLabel();
            Invalidate();
        }

        public void ZoomFit()
        {
            _fitToScreen = true;
            _scrollOffset = Point.Empty;
            UpdateLabel();
            Invalidate();
        }

        public void ZoomActual()
        {
            _fitToScreen = false;
            _zoom = 1f;
            ClampScroll();
            UpdateLabel();
            Invalidate();
        }

        private void ZoomAtCursor(float factor, Point cursorPos)
        {
            if (_sourceImage == null) return;

            if (_fitToScreen)
            {
                float fitW = (float)ClientSize.Width / _sourceImage.Width;
                float fitH = (float)ClientSize.Height / _sourceImage.Height;
                _zoom = Math.Min(fitW, fitH);
                _fitToScreen = false;
            }

            float oldZoom = _zoom;
            _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom * factor));

            float relX = (cursorPos.X + _scrollOffset.X) / oldZoom;
            float relY = (cursorPos.Y + _scrollOffset.Y) / oldZoom;

            _scrollOffset = new Point(
                (int)(relX * _zoom - cursorPos.X),
                (int)(relY * _zoom - cursorPos.Y));

            ClampScroll();
            UpdateLabel();
            Invalidate();
        }

        private void ClampScroll()
        {
            if (_sourceImage == null) return;
            int imgW = (int)(_sourceImage.Width * _zoom);
            int imgH = (int)(_sourceImage.Height * _zoom);
            _scrollOffset.X = Math.Max(0, Math.Min(_scrollOffset.X, imgW - ClientSize.Width));
            _scrollOffset.Y = Math.Max(0, Math.Min(_scrollOffset.Y, imgH - ClientSize.Height));
            if (imgW <= ClientSize.Width) _scrollOffset.X = 0;
            if (imgH <= ClientSize.Height) _scrollOffset.Y = 0;
        }

        private void UpdateLabel()
        {
            _lblZoom.Text = _fitToScreen ? "适应" : $"{_zoom * 100:F0}%";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            if (_sourceImage == null) return;

            if (_fitToScreen)
            {
                float scaleW = (float)ClientSize.Width / _sourceImage.Width;
                float scaleH = (float)ClientSize.Height / _sourceImage.Height;
                float scale = Math.Min(scaleW, scaleH);
                int w = (int)(_sourceImage.Width * scale);
                int h = (int)(_sourceImage.Height * scale);
                int x = (ClientSize.Width - w) / 2;
                int y = (ClientSize.Height - h) / 2;
                g.DrawImage(_sourceImage, x, y, w, h);
            }
            else
            {
                int imgW = (int)(_sourceImage.Width * _zoom);
                int imgH = (int)(_sourceImage.Height * _zoom);
                int x = (ClientSize.Width - imgW) / 2 - _scrollOffset.X;
                int y = (ClientSize.Height - imgH) / 2 - _scrollOffset.Y;

                if (imgW < ClientSize.Width) x = (ClientSize.Width - imgW) / 2;
                if (imgH < ClientSize.Height) y = (ClientSize.Height - imgH) / 2;

                g.DrawImage(_sourceImage, x, y, imgW, imgH);
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _sourceImage != null && !_fitToScreen)
            {
                _isPanning = true;
                _panStart = e.Location;
                _panScrollStart = _scrollOffset;
                Cursor = Cursors.Hand;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;
            _scrollOffset = new Point(
                _panScrollStart.X + (_panStart.X - e.X),
                _panScrollStart.Y + (_panStart.Y - e.Y));
            ClampScroll();
            Invalidate();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }
    }
}
