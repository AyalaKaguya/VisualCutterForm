using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace VisualMaster.CameraLink.Api
{
    public class ImageFifo : IDisposable
    {
        private readonly CameraFrameBuffer _buffer;
        private readonly ConcurrentQueue<Bitmap> _queue;
        private readonly object _lock = new object();
        private Bitmap _latestFrame;
        private volatile bool _disposed;

        public int Capacity
        {
            get { return _buffer.Capacity; }
            set { _buffer.Capacity = value; TrimToCapacity(); }
        }

        public int Count => _queue.Count;

        public bool HasFrame => !_queue.IsEmpty;

        public event EventHandler<Bitmap> FrameEnqueued;

        public CameraFrameBuffer Buffer => _buffer;

        public ImageFifo(int capacity = 10)
            : this(new CameraFrameBuffer(capacity))
        {
        }

        public ImageFifo(CameraFrameBuffer buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _queue = new ConcurrentQueue<Bitmap>();
        }

        public void Enqueue(Bitmap frame, string deviceId = null, string correlationId = null)
        {
            if (_disposed || frame == null) return;
            _buffer.Publish(frame, deviceId, correlationId);
            var queueClone = (Bitmap)frame.Clone();
            _queue.Enqueue(queueClone);
            // 克隆一份专供 PeekLatest 使用，使所有权与外部 frame 解耦
            lock (_lock) { _latestFrame?.Dispose(); _latestFrame = (Bitmap)frame.Clone(); }
            TrimToCapacity();
            lock (_lock) { Monitor.PulseAll(_lock); }
            FrameEnqueued?.Invoke(this, queueClone);
        }

        public Bitmap TryDequeue(int timeoutMs = -1)
        {
            if (_disposed) return null;
            Bitmap frame = null;
            if (timeoutMs < 0)
            {
                while (!_disposed && !_queue.TryDequeue(out frame))
                    lock (_lock) { Monitor.Wait(_lock, 50); }
            }
            else if (timeoutMs == 0)
            {
                _queue.TryDequeue(out frame);
                return frame;
            }
            else
            {
                var sw = Stopwatch.StartNew();
                while (!_disposed && !_queue.TryDequeue(out frame))
                {
                    var remaining = timeoutMs - (int)sw.ElapsedMilliseconds;
                    if (remaining <= 0) return null;
                    lock (_lock) { Monitor.Wait(_lock, Math.Min(remaining, 100)); }
                }
            }
            return frame;
        }

        public Bitmap PeekLatest()
        {
            lock (_lock) { return _latestFrame != null ? new Bitmap(_latestFrame) : null; }
        }

        public Bitmap PeekLatestNoClone()
        {
            lock (_lock) { return _latestFrame; }
        }

        public void Clear()
        {
            lock (_lock) { _latestFrame?.Dispose(); _latestFrame = null; }
            while (_queue.TryDequeue(out var frame)) frame?.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_lock) { Monitor.PulseAll(_lock); }
            Clear();
            _buffer.Dispose();
        }

        private void TrimToCapacity()
        {
            while (_queue.Count > _buffer.Capacity)
                if (_queue.TryDequeue(out var oldFrame)) oldFrame?.Dispose();
        }
    }
}
