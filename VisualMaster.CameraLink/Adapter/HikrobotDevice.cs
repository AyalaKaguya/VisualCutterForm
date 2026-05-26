using VisualMaster.CameraLink.Api;
using VisualMaster.CameraLink.API;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using MvCameraControl;

namespace VisualMaster.CameraLink.Adapter
{
    /// <summary>
    /// 海康机器人 MVS SDK 相机设备驱动，实现 <see cref="ICameraDeviceDriver"/>。
    /// 每个实例对应一台物理相机。
    /// </summary>
    internal sealed class HikrobotDevice : ICameraDeviceDriver
    {
        private IDevice _device;
        private readonly DiscoveredCamera _discovered;
        private volatile bool _isGrabbing;
        private volatile bool _disposed;
        private int _consecutiveFailures;
        private const int MaxConsecutiveFailures = 3;

        public string UniqueHardwareId => _discovered.SerialNumber;
        public bool IsOpen => _device != null;
        public bool IsGrabbing => _isGrabbing;

        public event EventHandler<FrameAcquiredEventArgs> FrameAcquired;
        public event EventHandler Disconnected;

        internal HikrobotDevice(DiscoveredCamera discovered)
        {
            _discovered = discovered ?? throw new ArgumentNullException(nameof(discovered));
        }

        // ── 生命周期 ───────────────────────────────────────────────

        public void Open()
        {
            if (_device != null) return;

            var devInfo = _discovered.RawInfo as IDeviceInfo
                ?? throw new InvalidOperationException("DiscoveredCamera 缺少 SDK DeviceInfo。");

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
            _consecutiveFailures = 0;
        }

        public void Close()
        {
            if (_device == null) return;

            StopGrabbing();
            _device.StreamGrabber.FrameGrabedEvent -= OnFrameGrabbed;
            _device.Close();
            _device.Dispose();
            _device = null;
        }

        // ── 采集控制 ──────────────────────────────────────────────

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
            if (_device == null)
                throw new InvalidOperationException("相机未打开。");
            _device.Parameters.SetCommandValue("TriggerSoftware");
        }

        public bool TryGrabImage(out Bitmap bitmap, int timeoutMs)
        {
            bitmap = null;
            if (_device == null || !_isGrabbing) return false;

            int ret = _device.StreamGrabber.GetImageBuffer((uint)timeoutMs, out IFrameOut frame);
            if (ret != MvError.MV_OK) return false;

            try
            {
                bitmap = frame.Image.ToBitmap();
                return bitmap != null;
            }
            catch { return false; }
            finally { _device.StreamGrabber.FreeImageBuffer(frame); }
        }

        // ── 配置 ──────────────────────────────────────────────────

        public void ApplySettings(CameraSettings settings)
        {
            if (_device == null || settings == null) return;

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
                default:
                    _device.Parameters.SetEnumValueByString("TriggerMode", "Off");
                    break;
            }

            _device.Parameters.SetFloatValue("ExposureTime", (float)settings.ExposureTimeUs);
            _device.Parameters.SetFloatValue("Gain", (float)settings.GainRaw);

            if (settings.Width > 0)  _device.Parameters.SetIntValue("Width",  settings.Width);
            if (settings.Height > 0) _device.Parameters.SetIntValue("Height", settings.Height);
            _device.Parameters.SetIntValue("OffsetX", settings.OffsetX);
            _device.Parameters.SetIntValue("OffsetY", settings.OffsetY);

            if (!string.IsNullOrEmpty(settings.PixelFormat))
                _device.Parameters.SetEnumValueByString("PixelFormat", settings.PixelFormat);
        }

        public CameraSettings ReadSettingsFromDevice()
        {
            if (_device == null) return new CameraSettings();

            var s = new CameraSettings();

            try { _device.Parameters.GetFloatValue("ExposureTime", out IFloatValue ev); s.ExposureTimeUs = ev?.CurValue ?? 5000; } catch { }
            try { _device.Parameters.GetFloatValue("Gain", out IFloatValue gv); s.GainRaw = gv?.CurValue ?? 0; } catch { }
            try { _device.Parameters.GetIntValue("Width",   out IIntValue w); s.Width   = (int)(w?.CurValue ?? 0); } catch { }
            try { _device.Parameters.GetIntValue("Height",  out IIntValue h); s.Height  = (int)(h?.CurValue ?? 0); } catch { }
            try { _device.Parameters.GetIntValue("OffsetX", out IIntValue ox); s.OffsetX = (int)(ox?.CurValue ?? 0); } catch { }
            try { _device.Parameters.GetIntValue("OffsetY", out IIntValue oy); s.OffsetY = (int)(oy?.CurValue ?? 0); } catch { }
            try
            {
                _device.Parameters.GetEnumValue("TriggerMode", out IEnumValue tm);
                bool on = string.Equals(tm?.CurEnumEntry?.Symbolic, "On", StringComparison.OrdinalIgnoreCase);
                if (on)
                {
                    _device.Parameters.GetEnumValue("TriggerSource", out IEnumValue ts);
                    string src = ts?.CurEnumEntry?.Symbolic ?? "";
                    s.TriggerSource = src;
                    s.TriggerMode = src.IndexOf("Line", StringComparison.OrdinalIgnoreCase) >= 0
                        ? TriggerModeEnum.Hardware
                        : TriggerModeEnum.Software;
                }
            }
            catch { }
            try { _device.Parameters.GetEnumValue("PixelFormat", out IEnumValue pf); s.PixelFormat = pf?.CurEnumEntry?.Symbolic ?? ""; } catch { }

            return s;
        }

        public string[] GetAvailablePixelFormats()
        {
            if (_device == null) return Array.Empty<string>();
            try
            {
                _device.Parameters.GetEnumValue("PixelFormat", out IEnumValue enumValue);
                return enumValue?.SupportEnumEntries?
                    .Select(e => e.Symbolic)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray() ?? Array.Empty<string>();
            }
            catch { return Array.Empty<string>(); }
        }

        public string[] GetAvailableTriggerSources()
        {
            if (_device == null) return Array.Empty<string>();
            try
            {
                _device.Parameters.GetEnumValue("TriggerSource", out IEnumValue enumValue);
                return enumValue?.SupportEnumEntries?
                    .Select(e => e.Symbolic)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray() ?? Array.Empty<string>();
            }
            catch { return Array.Empty<string>(); }
        }

        public CameraInfo ToCameraInfo()
        {
            return new CameraInfo
            {
                ModelName           = _discovered.ModelName ?? "",
                SerialNumber        = _discovered.SerialNumber ?? "",
                UserDefinedName     = "",
                ManufacturerName    = _discovered.ManufacturerName ?? "",
                TransportTypeName   = _discovered.TransportType ?? "",
                DeviceVersion       = _discovered.DeviceVersion ?? "",
                IpAddress           = IpStringToUint(_discovered.IpAddress),
                RawInfo             = _discovered.RawInfo,
            };
        }

        private static uint IpStringToUint(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return 0;
            try
            {
                var bytes = System.Net.IPAddress.Parse(ip).GetAddressBytes();
                return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16)
                     | ((uint)bytes[2] << 8)  |  (uint)bytes[3];
            }
            catch { return 0; }
        }

        // ── 帧回调 ────────────────────────────────────────────────

        private void OnFrameGrabbed(object sender, FrameGrabbedEventArgs e)
        {
            try
            {
                var frame = e.FrameOut;
                if (frame?.Image == null)
                {
                    Interlocked.Increment(ref _consecutiveFailures);
                    CheckDisconnection();
                    return;
                }

                using (var bmp = frame.Image.ToBitmap())
                {
                    if (bmp == null)
                    {
                        Interlocked.Increment(ref _consecutiveFailures);
                        CheckDisconnection();
                        return;
                    }
                    var clone = (Bitmap)bmp.Clone();
                    FrameAcquired?.Invoke(this,
                        new FrameAcquiredEventArgs(clone, UniqueHardwareId, DateTime.Now));
                    _consecutiveFailures = 0;
                }
            }
            catch
            {
                Interlocked.Increment(ref _consecutiveFailures);
                CheckDisconnection();
            }
        }

        private void CheckDisconnection()
        {
            if (_consecutiveFailures >= MaxConsecutiveFailures)
            {
                try { StopGrabbing(); } catch { }
                Disconnected?.Invoke(this, EventArgs.Empty);
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
