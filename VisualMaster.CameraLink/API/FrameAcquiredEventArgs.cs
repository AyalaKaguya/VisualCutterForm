using System;
using System.Drawing;

namespace VisualMaster.CameraLink.API
{
    /// <summary>相机帧采集完成时的事件参数。</summary>
    public sealed class FrameAcquiredEventArgs : EventArgs
    {
        /// <summary>采集到的原始位图（调用方负责 Dispose）。</summary>
        public Bitmap Frame { get; }

        /// <summary>相机硬件序列号。</summary>
        public string DeviceUniqueId { get; }

        /// <summary>帧的采集时间戳（由适配器记录）。</summary>
        public DateTime AcquiredAt { get; }

        public FrameAcquiredEventArgs(Bitmap frame, string deviceUniqueId, DateTime acquiredAt)
        {
            Frame = frame;
            DeviceUniqueId = deviceUniqueId;
            AcquiredAt = acquiredAt;
        }
    }
}
