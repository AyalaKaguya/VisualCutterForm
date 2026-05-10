using System;
using System.Drawing;

namespace VisualMaster.Api
{
    public enum TriggerModeEnum
    {
        Continuous,
        Software,
        Hardware,
    }

    public interface ICamera : IDisposable
    {
        CameraInfo Info { get; }
        bool IsOpen { get; }

        void Open();
        void Close();

        void StartGrabbing();
        void StopGrabbing();
        void TriggerSoftware();
        void ApplySettings(CameraSettings settings);
        bool TryGrabImage(out Bitmap bitmap, int timeoutMs = 2000);
        string[] GetAvailablePixelFormats();

        event EventHandler<Bitmap> ImageAcquired;
        event EventHandler Disconnected;
    }
}
