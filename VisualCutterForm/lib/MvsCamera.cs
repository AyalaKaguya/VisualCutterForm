using VisualMaster.Api;
using System;
using System.Drawing;
using System.Threading;
using MvCameraControl;

namespace VisualCutterForm.Lib
{
    public class MvsCamera : ICamera
    {
        private IDevice _device;
        private readonly CameraInfo _info;
        private bool _isGrabbing;
        private Bitmap _latestFrame;
        private readonly object _frameLock = new object();
        private volatile bool _disposed;

        public string Name
        {
            get
            {
                if (_info == null) return "Unknown";
                return string.IsNullOrEmpty(_info.UserDefinedName) ? _info.ModelName : _info.UserDefinedName;
            }
        }

        public CameraInfo Info => _info;
        public bool IsOpen => _device != null;

        public event EventHandler<Bitmap> ImageGrabbed;

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

            if (settings.TriggerEnabled)
            {
                _device.Parameters.SetEnumValueByString("TriggerMode", "On");
                _device.Parameters.SetEnumValueByString("TriggerSource", settings.TriggerSource ?? "Line0");
                _device.Parameters.SetEnumValueByString("TriggerActivation", settings.TriggerActivation ?? "RisingEdge");
            }
            else
            {
                _device.Parameters.SetEnumValueByString("TriggerMode", "Off");
            }

            _device.Parameters.SetFloatValue("ExposureTime", settings.ExposureTimeUs);
            _device.Parameters.SetFloatValue("Gain", settings.Gain);

            if (settings.Width > 0 && settings.Height > 0)
            {
                _device.Parameters.SetIntValue("Width", settings.Width);
                _device.Parameters.SetIntValue("Height", settings.Height);
            }

            _device.Parameters.SetIntValue("OffsetX", settings.OffsetX);
            _device.Parameters.SetIntValue("OffsetY", settings.OffsetY);
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
                    ImageGrabbed?.Invoke(this, clone);
                }
            }
            catch
            {
                // best-effort, ignore frame drops
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Close();
        }
    }
}
