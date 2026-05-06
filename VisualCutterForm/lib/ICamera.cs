using System;
using System.Drawing;

namespace VisualCutterForm.Lib
{
    public interface ICamera : IDisposable
    {
        string Name { get; }
        CameraInfo Info { get; }
        bool IsOpen { get; }

        void Open();
        void Close();

        void StartGrabbing();
        void StopGrabbing();
        bool TryGrabImage(out Bitmap bitmap, int timeoutMs = 2000);

        event EventHandler<Bitmap> ImageGrabbed;
    }
}
