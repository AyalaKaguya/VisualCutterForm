using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualCutterForm
{
    public class ImageViewer : UserControl, IMessageFilter
    {
        private readonly Panel _scrollPanel;
        private readonly PictureBox _pictureBox;
        private readonly Panel _toolbar;
        private readonly Button _btnZoomIn;
        private readonly Button _btnZoomOut;
        private readonly Button _btnFit;
        private readonly Button _btnActual;
        private readonly Label _lblZoom;

        private Bitmap _sourceImage;
        private float _zoom = 1f;
        private bool _fitToScreen = true;

        private bool _isPanning;
        private Point _panMouseStart;
        private Point _panScrollStart;

        private bool _toolbarHovered;

        private const float MinZoom = 0.05f;
        private const float MaxZoom = 20f;
        private const float ZoomStep = 1.25f;
        private const float WheelZoomStep = 1.12f;
        private const int WM_MOUSEWHEEL = 0x020A;

        public Image Image
        {
            get => _pictureBox.Image;
            set => SetImage(value as Bitmap);
        }

        public ImageViewer()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(40, 40, 40);

            _scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(40, 40, 40),
            };
            _scrollPanel.MouseDown += OnImageMouseDown;
            _scrollPanel.MouseMove += OnImageMouseMove;
            _scrollPanel.MouseUp += OnImageMouseUp;

            _pictureBox = new PictureBox
            {
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.AutoSize,
                BackColor = Color.FromArgb(40, 40, 40),
            };
            _pictureBox.MouseDown += OnImageMouseDown;
            _pictureBox.MouseMove += OnImageMouseMove;
            _pictureBox.MouseUp += OnImageMouseUp;
            _scrollPanel.Controls.Add(_pictureBox);

            _toolbar = new Panel
            {
                Size = new Size(170, 32),
                BackColor = Color.FromArgb(200, 30, 30, 30),
                Visible = false,
            };
            _toolbar.MouseEnter += (s, e) => _toolbarHovered = true;
            _toolbar.MouseLeave += (s, e) => { _toolbarHovered = false; HideToolbar(); };

            void StyleButton(Button b, string text, float fontSize)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.ForeColor = Color.White;
                b.BackColor = Color.Transparent;
                b.Font = new Font("Segoe UI", fontSize, FontStyle.Bold);
                b.Size = new Size(32, 28);
                b.Text = text;
                b.TextAlign = ContentAlignment.MiddleCenter;
                b.MouseEnter += (s, e) => _toolbarHovered = true;
                b.MouseLeave += (s, e) => _toolbarHovered = false;
            }

            _btnZoomOut = new Button { Location = new Point(4, 2) };
            StyleButton(_btnZoomOut, "−", 11F);
            _btnZoomOut.Click += (s, e) => ZoomOut();

            _lblZoom = new Label
            {
                Text = "适应",
                Location = new Point(38, 6),
                Size = new Size(46, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };

            _btnZoomIn = new Button { Location = new Point(86, 2) };
            StyleButton(_btnZoomIn, "+", 11F);
            _btnZoomIn.Click += (s, e) => ZoomIn();

            _btnFit = new Button { Location = new Point(120, 2) };
            StyleButton(_btnFit, "⊡", 11F);
            _btnFit.Click += (s, e) => ZoomFit();

            _btnActual = new Button { Location = new Point(137, 2) };
            StyleButton(_btnActual, "1:1", 8F);
            _btnActual.Click += (s, e) => ZoomActual();

            _toolbar.Controls.Add(_btnZoomOut);
            _toolbar.Controls.Add(_lblZoom);
            _toolbar.Controls.Add(_btnZoomIn);
            _toolbar.Controls.Add(_btnFit);
            _toolbar.Controls.Add(_btnActual);

            Controls.Add(_scrollPanel);
            Controls.Add(_toolbar);

            Resize += (s, e) => { PositionToolbar(); ApplyFitIfNeeded(); };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Application.AddMessageFilter(this);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Application.RemoveMessageFilter(this);
            base.OnHandleDestroyed(e);
        }

        bool IMessageFilter.PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL && IsHandleCreated && Visible && !Disposing)
            {
                var screenPoint = MousePosition;
                if (ClientRectangle.Contains(PointToClient(screenPoint)))
                {
                    int delta = (short)((uint)m.WParam >> 16);
                    HandleMouseWheel(delta, PointToClient(screenPoint));
                    return true;
                }
            }
            return false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_fitToScreen && _sourceImage != null) return;

            var toolbarRect = new Rectangle(
                ClientSize.Width - _toolbar.Width - 8,
                ClientSize.Height - _toolbar.Height - 6,
                _toolbar.Width + 16,
                _toolbar.Height + 16);

            if (toolbarRect.Contains(e.Location) || _toolbarHovered)
                ShowToolbar(true);
            else
                HideToolbar();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_toolbarHovered)
                HideToolbar();
        }

        public void SetImage(Bitmap bmp)
        {
            _sourceImage = bmp;
            if (bmp == null)
            {
                _pictureBox.Image = null;
                HideToolbar();
                return;
            }

            if (_fitToScreen)
                ApplyFitZoom();
            else
                ApplyZoom();
        }

        public void ZoomIn()
        {
            _fitToScreen = false;
            _zoom = Math.Min(_zoom * ZoomStep, MaxZoom);
            ApplyZoom();
        }

        public void ZoomOut()
        {
            _fitToScreen = false;
            _zoom = Math.Max(_zoom / ZoomStep, MinZoom);
            ApplyZoom();
        }

        public void ZoomFit()
        {
            _fitToScreen = true;
            ApplyFitZoom();
        }

        public void ZoomActual()
        {
            _fitToScreen = false;
            _zoom = 1f;
            ApplyZoom();
        }

        private void ApplyFitZoom()
        {
            if (_sourceImage == null) return;
            _scrollPanel.AutoScroll = false;
            _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _pictureBox.Image = _sourceImage;
            _pictureBox.Dock = DockStyle.Fill;
            _pictureBox.Location = Point.Empty;
            _lblZoom.Text = "适应";
        }

        private void ApplyFitIfNeeded()
        {
            if (_fitToScreen) ApplyFitZoom();
        }

        private void ApplyZoom()
        {
            if (_sourceImage == null) return;
            _pictureBox.Dock = DockStyle.None;
            _scrollPanel.AutoScroll = true;
            _pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            _pictureBox.Image = _sourceImage;
            int w = (int)(_sourceImage.Width * _zoom);
            int h = (int)(_sourceImage.Height * _zoom);
            _pictureBox.Size = new Size(w, h);
            CenterImage();
            _lblZoom.Text = $"{_zoom * 100:F0}%";
        }

        private void CenterImage()
        {
            int x = Math.Max(0, (_scrollPanel.ClientSize.Width - _pictureBox.Width) / 2);
            int y = Math.Max(0, (_scrollPanel.ClientSize.Height - _pictureBox.Height) / 2);
            _pictureBox.Location = new Point(x, y);
        }

        private void PositionToolbar()
        {
            _toolbar.Location = new Point(
                ClientSize.Width - _toolbar.Width - 8,
                ClientSize.Height - _toolbar.Height - 6);
            _toolbar.BringToFront();
        }

        private void ShowToolbar(bool visible)
        {
            if (_toolbar.Visible != visible)
            {
                _toolbar.Visible = visible;
                if (visible) _toolbar.BringToFront();
            }
        }

        private void HideToolbar()
        {
            if (!_toolbarHovered)
                ShowToolbar(false);
        }

        private void HandleMouseWheel(int delta, Point clientPos)
        {
            if (_sourceImage == null) return;

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                float factor = delta > 0 ? WheelZoomStep : 1f / WheelZoomStep;
                ZoomAtCursor(factor, clientPos);
            }
            else if (!_fitToScreen)
            {
                _scrollPanel.AutoScrollPosition = new Point(
                    -_scrollPanel.AutoScrollPosition.X,
                    -_scrollPanel.AutoScrollPosition.Y - delta / 2);
            }
        }

        private void ZoomAtCursor(float factor, Point cursorOnControl)
        {
            if (_sourceImage == null) return;

            if (_fitToScreen)
            {
                float fitW = (float)_scrollPanel.ClientSize.Width / _sourceImage.Width;
                float fitH = (float)_scrollPanel.ClientSize.Height / _sourceImage.Height;
                _zoom = Math.Min(fitW, fitH);
                _fitToScreen = false;
                _scrollPanel.AutoScroll = true;
                _pictureBox.Dock = DockStyle.None;
                _pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
                _pictureBox.Image = _sourceImage;
            }

            _pictureBox.Location = Point.Empty;
            int oldW = _pictureBox.Width;
            int oldH = _pictureBox.Height;

            _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom * factor));

            int newW = (int)(_sourceImage.Width * _zoom);
            int newH = (int)(_sourceImage.Height * _zoom);
            _pictureBox.Size = new Size(newW, newH);

            float relX = (float)cursorOnControl.X / oldW;
            float relY = (float)cursorOnControl.Y / oldH;

            int scrollX = (int)(relX * newW) - cursorOnControl.X;
            int scrollY = (int)(relY * newH) - cursorOnControl.Y;

            _scrollPanel.AutoScrollPosition = new Point(scrollX, scrollY);
            _lblZoom.Text = $"{_zoom * 100:F0}%";
        }

        private void OnImageMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _sourceImage != null && !_fitToScreen)
            {
                _isPanning = true;
                _panMouseStart = e.Location;
                _panScrollStart = new Point(
                    -_scrollPanel.AutoScrollPosition.X,
                    -_scrollPanel.AutoScrollPosition.Y);
                Cursor = Cursors.Hand;
            }
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;

            int dx = _panMouseStart.X - e.X;
            int dy = _panMouseStart.Y - e.Y;
            _scrollPanel.AutoScrollPosition = new Point(
                _panScrollStart.X + dx,
                _panScrollStart.Y + dy);
        }

        private void OnImageMouseUp(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }
    }
}
