using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace VisualMaster.Api
{
    public class ImageFifo : IDisposable
    {
        private readonly ConcurrentQueue<Bitmap> _queue;
        private readonly object _lock = new object();
        private Bitmap _latestFrame;
        private volatile int _capacity;
        private volatile bool _disposed;

        public int Capacity
        {
            get { return _capacity; }
            set
            {
                if (value < 1) value = 1;
                _capacity = value;
                TrimToCapacity();
            }
        }

        public int Count
        {
            get
            {
                return _queue.Count;
            }
        }

        public bool HasFrame
        {
            get
            {
                return !_queue.IsEmpty;
            }
        }

        public event EventHandler<Bitmap> FrameEnqueued;

        public ImageFifo(int capacity = 10)
        {
            _capacity = Math.Max(1, capacity);
            _queue = new ConcurrentQueue<Bitmap>();
        }

        public void Enqueue(Bitmap frame)
        {
            if (_disposed || frame == null) return;

            var clone = new Bitmap(frame);
            _queue.Enqueue(clone);

            lock (_lock)
            {
                _latestFrame?.Dispose();
                _latestFrame = new Bitmap(frame);
            }

            TrimToCapacity();

            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }

            FrameEnqueued?.Invoke(this, clone);
        }

        public Bitmap TryDequeue(int timeoutMs = -1)
        {
            if (_disposed) return null;

            Bitmap frame = null;
            if (timeoutMs < 0)
            {
                while (!_disposed && !_queue.TryDequeue(out frame))
                {
                    lock (_lock)
                    {
                        Monitor.Wait(_lock, 50);
                    }
                }
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
                    lock (_lock)
                    {
                        Monitor.Wait(_lock, Math.Min(remaining, 100));
                    }
                }
            }

            return frame;
        }

        public Bitmap PeekLatest()
        {
            lock (_lock)
            {
                if (_latestFrame == null) return null;
                return new Bitmap(_latestFrame);
            }
        }

        public Bitmap PeekLatestNoClone()
        {
            lock (_lock)
            {
                return _latestFrame;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
            }

            while (_queue.TryDequeue(out var frame))
            {
                frame?.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }

            Clear();
        }

        private void TrimToCapacity()
        {
            while (_queue.Count > _capacity)
            {
                if (_queue.TryDequeue(out var oldFrame))
                {
                    oldFrame?.Dispose();
                }
            }
        }
    }
}
