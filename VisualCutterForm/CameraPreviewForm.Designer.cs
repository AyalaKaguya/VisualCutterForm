using System.Drawing;
using System.Windows.Forms;

namespace VisualCutterForm
{
    partial class CameraPreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        private ImageViewer _viewer;
        private Timer _timer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _viewer = new ImageViewer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
            };

            _timer = new Timer { Interval = 33 };

            Controls.Add(_viewer);
        }
    }
}
