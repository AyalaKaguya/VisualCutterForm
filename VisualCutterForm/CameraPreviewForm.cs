using System;
using System.Drawing;
using System.Windows.Forms;
using VisualCutterForm.Lib;

namespace VisualCutterForm
{
    public class CameraPreviewForm : Form
    {
        private VisionController _vision;
        private string _slotId;
        private ImageViewer _viewer;
        private Timer _timer;
        private object _lastFrameRef;

        public CameraPreviewForm(VisionController vision, string slotId)
        {
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
            _slotId = slotId;

            Text = $"相机预览 - {slotId}";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            _viewer = new ImageViewer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
            };
            Controls.Add(_viewer);

            _timer = new Timer { Interval = 33 };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            FormClosing += (s, e) =>
            {
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;
            };
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            try
            {
                if (_vision == null || string.IsNullOrEmpty(_slotId)) return;

                var raw = _vision.PeekLatestNoClone(_slotId);
                if (raw != null && raw != _lastFrameRef)
                {
                    _lastFrameRef = raw;
                    _viewer.Image = new Bitmap(raw);
                }
            }
            catch
            {
            }
        }
    }
}
