using VisualMaster.Api;
using System;

namespace VisualMaster.CameraLink.UI.ViewModels
{
    /// <summary>
    /// 相机参数配置 ViewModel，对 CameraSettings 的各字段双向绑定。
    /// </summary>
    public sealed class CameraConfigViewModel : NotifyBase
    {
        public event EventHandler SettingsChanged;
        public TriggerModeEnum[] TriggerModes { get; } =
            new[] { TriggerModeEnum.Continuous, TriggerModeEnum.Software, TriggerModeEnum.Hardware };

        public string[] TriggerActivations { get; } =
            new[] { "RisingEdge", "FallingEdge", "AnyEdge", "LevelHigh", "LevelLow" };

        public int[] FifoOptions { get; } = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        private string[] _triggerSources = new[] { "Software", "Line0", "Line1", "Line2", "Line3" };
        public string[] TriggerSources
        {
            get => _triggerSources;
            set { _triggerSources = value ?? new string[0]; OnPropertyChanged(); }
        }

        private string[] _pixelFormats = new string[0];
        public string[] PixelFormats
        {
            get => _pixelFormats;
            set { _pixelFormats = value ?? new string[0]; OnPropertyChanged(); }
        }

        private double _exposureTimeUs;
        private double _gainRaw;
        private int _width;
        private int _height;
        private int _offsetX;
        private int _offsetY;
        private string _pixelFormat;
        private TriggerModeEnum _triggerMode;
        private string _triggerSource;
        private string _triggerActivation;
        private int _fifoCapacity;
        private bool _monochromeOutput;
        private bool _isRoiEnabled;

        public bool IsRoiEnabled
        {
            get => _isRoiEnabled;
            set { if (SetField(ref _isRoiEnabled, value)) Notify(); }
        }

        public double ExposureTimeUs
        {
            get => _exposureTimeUs;
            set { if (SetField(ref _exposureTimeUs, value)) Notify(); }
        }

        public double GainRaw
        {
            get => _gainRaw;
            set { if (SetField(ref _gainRaw, value)) Notify(); }
        }

        public int Width
        {
            get => _width;
            set { if (SetField(ref _width, value)) Notify(); }
        }

        public int Height
        {
            get => _height;
            set { if (SetField(ref _height, value)) Notify(); }
        }

        public int OffsetX
        {
            get => _offsetX;
            set { if (SetField(ref _offsetX, value)) Notify(); }
        }

        public int OffsetY
        {
            get => _offsetY;
            set { if (SetField(ref _offsetY, value)) Notify(); }
        }

        public string PixelFormat
        {
            get => _pixelFormat;
            set { if (SetField(ref _pixelFormat, value)) Notify(); }
        }

        public TriggerModeEnum TriggerMode
        {
            get => _triggerMode;
            set
            {
                if (SetField(ref _triggerMode, value))
                {
                    OnPropertyChanged(nameof(IsTriggerSourceEnabled));
                    Notify();
                }
            }
        }

        public string TriggerSource
        {
            get => _triggerSource;
            set { if (SetField(ref _triggerSource, value)) Notify(); }
        }

        public string TriggerActivation
        {
            get => _triggerActivation;
            set { if (SetField(ref _triggerActivation, value)) Notify(); }
        }

        public int FifoCapacity
        {
            get => _fifoCapacity;
            set { if (SetField(ref _fifoCapacity, value)) Notify(); }
        }

        public bool MonochromeOutput
        {
            get => _monochromeOutput;
            set { if (SetField(ref _monochromeOutput, value)) Notify(); }
        }

        /// <summary>触发源/边沿仅在硬件触发模式下可编辑。</summary>
        public bool IsTriggerSourceEnabled => _triggerMode == TriggerModeEnum.Hardware;

        // ── 构造与转换 ────────────────────────────────────────────────

        public CameraConfigViewModel(CameraSettings s)
        {
            LoadFrom(s ?? new CameraSettings());
        }

        public void LoadFrom(CameraSettings s)
        {
            _exposureTimeUs   = s.ExposureTimeUs;
            _gainRaw          = s.GainRaw;
            _width            = s.Width;
            _height           = s.Height;
            _offsetX          = s.OffsetX;
            _offsetY          = s.OffsetY;
            _pixelFormat      = s.PixelFormat ?? "";
            _triggerMode      = s.TriggerMode;
            _triggerSource    = s.TriggerSource ?? "Software";
            _triggerActivation = s.TriggerActivation ?? "RisingEdge";
            _fifoCapacity     = s.FifoCapacity;
            _monochromeOutput = s.MonochromeOutput;
            _isRoiEnabled     = s.Width > 0 || s.Height > 0;

            OnPropertyChanged(null);          // 刷新所有绑定
        }

        public CameraSettings ToSettings() => new CameraSettings
        {
            ExposureTimeUs    = _exposureTimeUs,
            GainRaw           = _gainRaw,
            Width             = _isRoiEnabled ? _width : 0,
            Height            = _isRoiEnabled ? _height : 0,
            OffsetX           = _isRoiEnabled ? _offsetX : 0,
            OffsetY           = _isRoiEnabled ? _offsetY : 0,
            PixelFormat       = _pixelFormat ?? "",
            TriggerMode       = _triggerMode,
            TriggerSource     = _triggerSource ?? "Software",
            TriggerActivation = _triggerActivation ?? "RisingEdge",
            FifoCapacity      = _fifoCapacity,
            MonochromeOutput  = _monochromeOutput,
        };

        private void Notify() => SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
