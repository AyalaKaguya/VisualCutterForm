using System;
using System.Drawing;
using System.Threading;

namespace VisualMaster.CameraLink.Api
{
    public sealed class CameraFrameSnapshot : IDisposable
    {
        private Bitmap _frame;
        private int _referenceCount;

        public CameraFrameSnapshot(string deviceId, long sequenceNumber, Bitmap frame, string correlationId = null)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            SnapshotId = Guid.NewGuid().ToString("N");
            DeviceId = deviceId ?? string.Empty;
            SequenceNumber = sequenceNumber;
            CorrelationId = correlationId ?? string.Empty;
            CapturedAt = DateTime.Now;
            _frame = frame;
            _referenceCount = 1;
        }

        public string SnapshotId { get; }
        public string DeviceId { get; }
        public long SequenceNumber { get; }
        public string CorrelationId { get; }
        public DateTime CapturedAt { get; }
        public Bitmap Frame => _frame;

        public CameraFrameSnapshot AddRef()
        {
            while (true)
            {
                var current = Volatile.Read(ref _referenceCount);
                if (current <= 0) throw new ObjectDisposedException(nameof(CameraFrameSnapshot));
                if (Interlocked.CompareExchange(ref _referenceCount, current + 1, current) == current) return this;
            }
        }

        public Bitmap CloneFrame()
        {
            var frame = _frame;
            return frame != null ? new Bitmap(frame) : null;
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref _referenceCount) != 0) return;
            var frame = Interlocked.Exchange(ref _frame, null);
            frame?.Dispose();
        }
    }
}
