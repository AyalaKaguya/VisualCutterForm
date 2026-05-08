using VisualMaster.Api;
using System;
using System.Drawing;
using System.Threading;
using MvCameraControl;

namespace VisualMaster.CameraLink
{
    public class MvsCamera : ICamera
    {
        private IDevice _device;
        private readonly CameraInfo _info;
        private CameraSettings _appliedSettings;
        private bool _isGrabbing;
        private Bitmap _latestFrame;
        private readonly object _frameLock = new object();
        private volatile bool _disposed;

        public CameraInfo Info => _info;
        public bool IsOpen => _device != null;

        public event EventHandler<Bitmap> ImageAcquired;
        public event EventHandler Disconnected;

        public MvsCamera(CameraInfo info)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
        }

        public void Open()
        {
            if (_device != null) return;

            var devInfo = _info.RawInfo as IDeviceInfo;
            if (devInfo == null)
                throw new InvalidOperationException("CameraInfo has no associated device info.");

            _device = DeviceFactory.CreateDevice(devInfo);
            _device.Open();

            _device.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
            _device.Parameters.SetEnumValueByString("TriggerMode", "Off");

            if (_device is IGigEDevice gigeDev)
            {
                gigeDev.GetOptimalPacketSize(out int packetSize);
                _device.Parameters.SetIntValue("GevSCPSPacketSize", (long)packetSize);
            }

            _device.StreamGrabber.FrameGrabedEvent += OnFrameGrabbed;
        }

        public void Close()
        {
            if (_device == null) return;

            StopGrabbing();

            _device.StreamGrabber.FrameGrabedEvent -= OnFrameGrabbed;
            _device.Close();
            _device.Dispose();
            _device = null;

            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
            }
        }

        public void StartGrabbing()
        {
            if (_device == null || _isGrabbing) return;

            _device.StreamGrabber.SetImageNodeNum(5u);
            _device.StreamGrabber.StartGrabbing();
            _isGrabbing = true;
        }

        public void StopGrabbing()
        {
            if (_device == null || !_isGrabbing) return;

            _device.StreamGrabber.StopGrabbing();
            _isGrabbing = false;
        }

        public void TriggerSoftware()
        {
            if (_device == null) return;

            _device.Parameters.SetEnumValueByString("TriggerMode", "On");
            _device.Parameters.SetEnumValueByString("TriggerSource", "Software");
            _device.Parameters.SetCommandValue("TriggerSoftware");
        }

        public bool TryGrabImage(out Bitmap bitmap, int timeoutMs = 2000)
        {
            bitmap = null;

            if (_device == null) return false;
            if (!_isGrabbing) return false;

            int ret = _device.StreamGrabber.GetImageBuffer((uint)timeoutMs, out IFrameOut frame);
            if (ret != MvError.MV_OK) return false;

            try
            {
                bitmap = frame.Image.ToBitmap();
                return bitmap != null;
            }
            catch
            {
                return false;
            }
            finally
            {
                _device.StreamGrabber.FreeImageBuffer(frame);
            }
        }

        public Bitmap GetLatestFrame()
        {
            lock (_frameLock)
            {
                if (_latestFrame == null) return null;
                return new Bitmap(_latestFrame);
            }
        }

        public void ApplySettings(CameraSettings settings)
        {
            if (_device == null || settings == null) return;

            _appliedSettings = settings;

            switch (settings.TriggerMode)
            {
                case TriggerModeEnum.Software:
                    _device.Parameters.SetEnumValueByString("TriggerMode", "On");
                    _device.Parameters.SetEnumValueByString("TriggerSource", "Software");
                    if (!string.IsNullOrEmpty(settings.TriggerActivation))
                        _device.Parameters.SetEnumValueByString("TriggerActivation", settings.TriggerActivation);
                    break;
                case TriggerModeEnum.Hardware:
                    _device.Parameters.SetEnumValueByString("TriggerMode", "On");
                    _device.Parameters.SetEnumValueByString("TriggerSource", settings.TriggerSource ?? "Line0");
                    if (!string.IsNullOrEmpty(settings.TriggerActivation))
                        _device.Parameters.SetEnumValueByString("TriggerActivation", settings.TriggerActivation);
                    break;
                case TriggerModeEnum.Continuous:
                default:
                    _device.Parameters.SetEnumValueByString("TriggerMode", "Off");
                    break;
            }

            _device.Parameters.SetFloatValue("ExposureTime", (float)settings.ExposureTimeUs);
            _device.Parameters.SetFloatValue("Gain", (float)settings.GainRaw);

            if (settings.Width > 0 && settings.Height > 0)
            {
                _device.Parameters.SetIntValue("Width", settings.Width);
                _device.Parameters.SetIntValue("Height", settings.Height);
            }

            _device.Parameters.SetIntValue("OffsetX", settings.OffsetX);
            _device.Parameters.SetIntValue("OffsetY", settings.OffsetY);

            if (!string.IsNullOrEmpty(settings.PixelFormat))
                _device.Parameters.SetEnumValueByString("PixelFormat", settings.PixelFormat);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Close();
        }

        private void OnFrameGrabbed(object sender, FrameGrabbedEventArgs e)
        {
            try
            {
                var frame = e.FrameOut;
                if (frame?.Image == null) return;

                using (var bmp = frame.Image.ToBitmap())
                {
                    if (bmp == null) return;

                    lock (_frameLock)
                    {
                        _latestFrame?.Dispose();
                        _latestFrame = new Bitmap(bmp);
                    }

                    var clone = new Bitmap(bmp);
                    ImageAcquired?.Invoke(this, clone);
                }
            }
            catch
            {
            }
        }
    }
}
