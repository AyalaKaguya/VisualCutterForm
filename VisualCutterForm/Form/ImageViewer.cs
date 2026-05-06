using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualCutterForm
{
    public class ImageViewer : UserControl
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

        private const float MinZoom = 0.05f;
        private const float MaxZoom = 20f;
        private const float ZoomStep = 1.25f;
        private const float WheelZoomStep = 1.12f;

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
            _scrollPanel.MouseEnter += (s, e) => ShowToolbar(true);
            _scrollPanel.MouseLeave += (s, e) => { if (!_toolbar.Bounds.Contains(_toolbar.PointToClient(MousePosition))) ShowToolbar(false); };

            _pictureBox = new PictureBox
            {
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.AutoSize,
                BackColor = Color.FromArgb(40, 40, 40),
            };
            _pictureBox.MouseWheel += OnPictureBoxMouseWheel;
            _pictureBox.MouseDown += OnPictureBoxMouseDown;
            _pictureBox.MouseMove += OnPictureBoxMouseMove;
            _pictureBox.MouseUp += OnPictureBoxMouseUp;
            _scrollPanel.Controls.Add(_pictureBox);

            _toolbar = new Panel
            {
                Size = new Size(170, 32),
                BackColor = Color.FromArgb(180, 30, 30, 30),
                Visible = false,
            };
            _toolbar.MouseEnter += (s, e) => ShowToolbar(true);
            _toolbar.MouseLeave += (s, e) => { if (!_scrollPanel.Bounds.Contains(_scrollPanel.PointToClient(MousePosition))) ShowToolbar(false); };

            void StyleButton(Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.ForeColor = Color.White;
                b.BackColor = Color.Transparent;
                b.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                b.Size = new Size(32, 28);
                b.TextAlign = ContentAlignment.MiddleCenter;
            }

            _btnZoomOut = new Button { Text = "−", Location = new Point(4, 2) };
            StyleButton(_btnZoomOut);
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

            _btnZoomIn = new Button { Text = "+", Location = new Point(86, 2) };
            StyleButton(_btnZoomIn);
            _btnZoomIn.Click += (s, e) => ZoomIn();

            _btnFit = new Button { Text = "⊡", Location = new Point(120, 2) };
            StyleButton(_btnFit);
            _btnFit.Click += (s, e) => ZoomFit();

            _btnActual = new Button { Text = "1:1", Location = new Point(137, 2) };
            StyleButton(_btnActual);
            _btnActual.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            _btnActual.Click += (s, e) => ZoomActual();

            _toolbar.Controls.Add(_btnZoomOut);
            _toolbar.Controls.Add(_lblZoom);
            _toolbar.Controls.Add(_btnZoomIn);
            _toolbar.Controls.Add(_btnFit);
            _toolbar.Controls.Add(_btnActual);

            Controls.Add(_scrollPanel);
            Controls.Add(_toolbar);

            Resize += (s, e) => PositionToolbar();
        }

        public void SetImage(Bitmap bmp)
        {
            _sourceImage = bmp;
            if (bmp == null)
            {
                _pictureBox.Image = null;
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
            _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _pictureBox.Image = _sourceImage;
            _pictureBox.Size = _scrollPanel.ClientSize;
            _pictureBox.Location = Point.Empty;
            _scrollPanel.AutoScroll = false;
            _lblZoom.Text = "适应";
        }

        private void ApplyZoom()
        {
            if (_sourceImage == null) return;
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
            _toolbar.Visible = visible;
        }

        private void OnPictureBoxMouseWheel(object sender, MouseEventArgs e)
        {
            if (_sourceImage == null) return;

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                float factor = e.Delta > 0 ? WheelZoomStep : 1f / WheelZoomStep;
                ZoomAtCursor(factor, e.Location);
            }
            else if (_fitToScreen)
            {
                return;
            }
            else
            {
                var se = new HandledMouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta)
                    { Handled = false };
                base.OnMouseWheel(se as MouseEventArgs);
            }
        }

        private void ZoomAtCursor(float factor, Point cursorOnPictureBox)
        {
            if (_sourceImage == null) return;

            if (_fitToScreen)
            {
                float fitW = (float)_scrollPanel.ClientSize.Width / _sourceImage.Width;
                float fitH = (float)_scrollPanel.ClientSize.Height / _sourceImage.Height;
                _zoom = Math.Min(fitW, fitH);
                _fitToScreen = false;
            }

            float contentX = cursorOnPictureBox.X;
            float contentY = cursorOnPictureBox.Y;
            if (_scrollPanel.AutoScrollPosition.X != 0 || _scrollPanel.AutoScrollPosition.Y != 0)
            {
                // Adjust for scroll: cursor position on picture box
                contentX = cursorOnPictureBox.X;
                contentY = cursorOnPictureBox.Y;
            }

            float relX = contentX / _zoom;
            float relY = contentY / _zoom;

            _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom * factor));

            int w = (int)(_sourceImage.Width * _zoom);
            int h = (int)(_sourceImage.Height * _zoom);
            _pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            _pictureBox.Image = _sourceImage;
            _pictureBox.Size = new Size(w, h);

            int newContentX = (int)(relX * _zoom);
            int newContentY = (int)(relY * _zoom);

            int scrollX = newContentX - cursorOnPictureBox.X;
            int scrollY = newContentY - cursorOnPictureBox.Y;

            _pictureBox.Location = new Point(0, 0);
            _scrollPanel.AutoScrollPosition = new Point(scrollX, scrollY);

            _lblZoom.Text = $"{_zoom * 100:F0}%";
        }

        private void OnPictureBoxMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _sourceImage != null && !_fitToScreen)
            {
                _isPanning = true;
                _panMouseStart = e.Location;
                _panScrollStart = new Point(
                    -_scrollPanel.AutoScrollPosition.X,
                    -_scrollPanel.AutoScrollPosition.Y);
                Cursor = Cursors.Hand;
                _pictureBox.Capture = true;
            }
        }

        private void OnPictureBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;

            int dx = _panMouseStart.X - e.X;
            int dy = _panMouseStart.Y - e.Y;
            _scrollPanel.AutoScrollPosition = new Point(
                _panScrollStart.X + dx,
                _panScrollStart.Y + dy);
        }

        private void OnPictureBoxMouseUp(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
                _pictureBox.Capture = false;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            OnPictureBoxMouseWheel(this, e);
        }
    }
}
