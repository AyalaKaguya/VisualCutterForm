using VisualMaster.Api;
using VisualMaster.CameraLink.API;
using System;
using System.Drawing;

namespace VisualMaster.CameraLink.Core
{
    /// <summary>
    /// Core 层对一台相机的完整运行时封装。
    /// 将 <see cref="CameraDeviceConfig"/>（配置）、<see cref="ICameraDeviceDriver"/>（SDK 驱动）
    /// 和 <see cref="CameraFrameBuffer"/>（帧缓冲）聚合在一起，向外暴露统一的操作接口。
    /// </summary>
    internal sealed class ManagedCamera : IDisposable
    {
        // ── 标识与配置 ────────────────────────────────────────────
        public string DeviceId      => Config.DeviceId;
        public CameraDeviceConfig Config { get; }

        // ── SDK 驱动 ──────────────────────────────────────────────
        public ICameraDeviceDriver Driver { get; private set; }
        public DiscoveredCamera Discovery { get; private set; }

        // ── 帧缓冲基础设施 ────────────────────────────────────────
        public CameraFrameBuffer FrameBuffer { get; }
        public ImageFifo Fifo { get; }

        // ── 运行时状态 ────────────────────────────────────────────
        public bool IsConnected  => Driver?.IsOpen    == true;
        public bool IsGrabbing   => Driver?.IsGrabbing == true;
        public CameraInfo LastKnownInfo { get; private set; }

        // ── 诊断钩子 ──────────────────────────────────────────────
        public RuntimeDiagnosticsHub Diagnostics { get; set; }

        public ManagedCamera(CameraDeviceConfig config)
        {
            Config      = config?.Clone() ?? throw new ArgumentNullException(nameof(config));
            FrameBuffer = new CameraFrameBuffer(config.Settings?.FifoCapacity ?? 10);
            Fifo        = new ImageFifo(FrameBuffer);

            FrameBuffer.SnapshotPublished += OnSnapshotPublished;
        }

        // ── 驱动绑定 ──────────────────────────────────────────────

        /// <summary>绑定 SDK 驱动并打开相机。</summary>
        public void Attach(ICameraDeviceDriver driver, DiscoveredCamera discovery)
        {
            if (Driver != null) Detach();

            Driver    = driver ?? throw new ArgumentNullException(nameof(driver));
            Discovery = discovery;

            Driver.FrameAcquired  += OnFrameAcquired;
            Driver.Disconnected   += OnDisconnected;

            Driver.Open();

            // 首次连接时从相机侧读取原始参数（如果配置是空的），再应用保存的配置
            var saved = Config.Settings;
            if (saved.Width == 0 && saved.Height == 0)
            {
                var fromDevice = Driver.ReadSettingsFromDevice();
                fromDevice.TriggerMode    = saved.TriggerMode;
                fromDevice.FifoCapacity   = saved.FifoCapacity;
                fromDevice.MonochromeOutput = saved.MonochromeOutput;
                Config.Settings = fromDevice;
            }
            Driver.ApplySettings(Config.Settings);

            LastKnownInfo = Driver.ToCameraInfo();
            Config.AssignedSerial = driver.UniqueHardwareId;
        }

        /// <summary>分离 SDK 驱动（不释放 Driver 对象本身，由外部管理）。</summary>
        public void Detach()
        {
            if (Driver == null) return;
            Driver.FrameAcquired  -= OnFrameAcquired;
            Driver.Disconnected   -= OnDisconnected;
            try { Driver.StopGrabbing(); Driver.Close(); } catch { }
            Driver    = null;
            Discovery = null;
        }

        // ── 采集控制 ──────────────────────────────────────────────

        public void StartGrabbing()
        {
            if (Driver == null) return;
            Driver.ApplySettings(Config.Settings);
            Driver.StartGrabbing();
        }

        public void StopGrabbing()
        {
            Driver?.StopGrabbing();
        }

        public void TriggerSoftware()
        {
            if (Driver == null)
                throw new InvalidOperationException($"相机 {Config.DisplayName} 未连接。");
            Driver.TriggerSoftware();
        }

        public bool TryGrabImage(out Bitmap bitmap, int timeoutMs)
        {
            bitmap = null;
            if (Driver == null || !IsConnected) return false;
            return Driver.TryGrabImage(out bitmap, timeoutMs);
        }

        public string[] GetAvailablePixelFormats()
        {
            if (Driver == null) return Array.Empty<string>();
            try { return Driver.GetAvailablePixelFormats(); }
            catch { return Array.Empty<string>(); }
        }

        public void ApplySettings(CameraSettings settings)
        {
            if (settings == null) return;
            Config.Settings = settings.Clone();
            FrameBuffer.Capacity = settings.FifoCapacity;
            Fifo.Capacity        = settings.FifoCapacity;
            Driver?.ApplySettings(settings);
        }

        // ── 状态快照 ──────────────────────────────────────────────

        public CameraDeviceStatus ToStatus() => new CameraDeviceStatus
        {
            DeviceId       = Config.DeviceId,
            DisplayName    = Config.DisplayName,
            IsConnected    = IsConnected,
            IsGrabbing     = IsGrabbing,
            AssignedCamera = LastKnownInfo,
            AssignedSerial = Config.AssignedSerial,
        };

        // ── 内部事件处理 ──────────────────────────────────────────

        private void OnFrameAcquired(object sender, FrameAcquiredEventArgs e)
        {
            try
            {
                Bitmap toPublish = e.Frame;

                // 如启用黑白模式，额外转换
                if (Config.Settings?.MonochromeOutput == true)
                    toPublish = ConvertToGrayscale(e.Frame);

                Fifo.Enqueue(toPublish, Config.DeviceId);

                if (toPublish != e.Frame)
                    toPublish?.Dispose();
            }
            catch { }
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            try { Detach(); } catch { }
        }

        private void OnSnapshotPublished(object sender, CameraFrameSnapshot snapshot)
        {
            Diagnostics?.Record(new RuntimeDiagnosticEvent
            {
                EventType        = RuntimeDiagnosticEventType.SnapshotPublished,
                CorrelationId    = snapshot.CorrelationId,
                DeviceId         = Config.DeviceId,
                SnapshotId       = snapshot.SnapshotId,
                SnapshotSequence = snapshot.SequenceNumber,
                Message          = $"快照已发布: {Config.DisplayName}",
            });
        }

        // ── 工具方法 ──────────────────────────────────────────────

        private static Bitmap ConvertToGrayscale(Bitmap source)
        {
            if (source == null) return null;
            var gray = new Bitmap(source.Width, source.Height,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = System.Drawing.Graphics.FromImage(gray))
            {
                var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 },
                });
                var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(cm);
                g.DrawImage(source,
                    new System.Drawing.Rectangle(0, 0, source.Width, source.Height),
                    0, 0, source.Width, source.Height,
                    System.Drawing.GraphicsUnit.Pixel, ia);
            }
            return gray;
        }

        public void Dispose()
        {
            var driver = Driver;
            Detach();
            driver?.Dispose();
            FrameBuffer.SnapshotPublished -= OnSnapshotPublished;
        }
    }
}
