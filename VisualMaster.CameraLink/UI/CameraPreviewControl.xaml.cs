using System;
using System.Windows.Controls;
using System.Windows.Threading;
using VisualMaster.CameraLink.Api;

namespace VisualMaster.CameraLink.UI
{
    public partial class CameraPreviewControl : UserControl
    {
        private readonly DispatcherTimer _timer;

        public ICameraManager CameraManager { get; set; }
        public string DeviceId { get; set; }

        public CameraPreviewControl()
        {
            InitializeComponent();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += OnTick;
            Unloaded += (s, e) => StopPreview(false);
        }

        public void StartPreview()
        {
            if (CameraManager == null || string.IsNullOrEmpty(DeviceId)) return;
            CameraManager.StartGrabbing(DeviceId);
            _timer.Start();
        }

        public void StopPreview()
        {
            StopPreview(true);
        }

        public void SetBitmap(System.Drawing.Bitmap bitmap)
        {
            Viewer.SetBitmap(bitmap);
        }

        public void SetMat(object mat)
        {
            Viewer.SetMat(mat);
        }

        private void StopPreview(bool stopCamera)
        {
            _timer.Stop();
            if (stopCamera && CameraManager != null && !string.IsNullOrEmpty(DeviceId))
                CameraManager.StopGrabbing(DeviceId);
        }

        private void OnTick(object sender, EventArgs e)
        {
            var fifo = CameraManager?.GetFifo(DeviceId);
            using (var frame = fifo?.PeekLatest())
            {
                if (frame != null)
                    Viewer.SetBitmap(frame);
            }
        }
    }
}
