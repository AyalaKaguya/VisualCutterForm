using System;
using System.Windows;
using VisualMaster.CameraLink.Api;

namespace VisualMaster.CameraLink.UI
{
    public partial class CameraPreviewWindow : Window
    {
        private readonly ICameraManager _manager;
        private readonly string _deviceId;
        private CameraSettings _settingsBeforeContinuous;
        private bool _continuousModeApplied;

        public CameraPreviewWindow(ICameraManager manager, string deviceId)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));

            InitializeComponent();
            Preview.CameraManager = _manager;
            Preview.DeviceId = _deviceId;
            StatusText.Text = "就绪";
        }

        private void OnOpenGrabbingClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _manager.StartGrabbing(_deviceId);
                Preview.StartPreview();
                StatusText.Text = "相机取像已打开";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"打开失败：{ex.Message}";
            }
        }

        private void OnSingleFrameClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var cfg = _manager.GetCameraDevice(_deviceId);
                if (!_manager.IsDeviceGrabbing(_deviceId))
                    _manager.StartGrabbing(_deviceId);

                if (cfg?.Settings?.TriggerMode == TriggerModeEnum.Software)
                    _manager.TriggerSoftware(_deviceId);

                if (_manager.TryGrabImage(_deviceId, out var bitmap, 2000))
                {
                    using (bitmap)
                        Preview.SetBitmap(bitmap);
                    StatusText.Text = "单帧完成";
                }
                else
                {
                    StatusText.Text = "未获取到图像";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"单帧失败：{ex.Message}";
            }
        }

        private void OnContinuousClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyTemporaryContinuousMode();
                Preview.StartPreview();
                StatusText.Text = "连续取像中";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"连续取像失败：{ex.Message}";
            }
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            StopPreviewAndRestore();
            StatusText.Text = "已停止";
        }

        private void ApplyTemporaryContinuousMode()
        {
            var cfg = _manager.GetCameraDevice(_deviceId);
            if (cfg == null)
                throw new InvalidOperationException("找不到相机配置。");

            if (!_continuousModeApplied)
            {
                _settingsBeforeContinuous = cfg.Settings?.Clone() ?? new CameraSettings();
                _continuousModeApplied = true;
            }

            var temp = (_settingsBeforeContinuous ?? new CameraSettings()).Clone();
            temp.TriggerMode = TriggerModeEnum.Continuous;
            _manager.UpdateDeviceSettings(_deviceId, temp);
            _manager.StartGrabbing(_deviceId);
        }

        private void StopPreviewAndRestore()
        {
            try { Preview.StopPreview(); } catch { }
            if (_continuousModeApplied && _settingsBeforeContinuous != null)
            {
                try { _manager.UpdateDeviceSettings(_deviceId, _settingsBeforeContinuous); }
                catch { }
            }
            _continuousModeApplied = false;
            _settingsBeforeContinuous = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopPreviewAndRestore();
            base.OnClosed(e);
        }
    }
}
