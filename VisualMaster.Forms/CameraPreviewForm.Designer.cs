using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.Forms
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
            this._viewer = new ImageViewer();
            this._timer = new Timer();
            this.SuspendLayout();
            // 
            // _viewer
            // 
            this._viewer.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this._viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._viewer.Name = "_viewer";
            // 
            // _timer
            // 
            this._timer.Interval = 33;
            // 
            // CameraPreviewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._viewer);
            this.Name = "CameraPreviewForm";
            this.ResumeLayout(false);
        }
    }
}
