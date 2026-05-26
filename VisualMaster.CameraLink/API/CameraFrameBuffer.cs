using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace VisualMaster.CameraLink.Api
{
    public sealed class CameraFrameBuffer : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly Queue<CameraFrameSnapshot> _history = new Queue<CameraFrameSnapshot>();
        private CameraFrameSnapshot _latestSnapshot;
        private long _nextSequenceNumber;
        private int _capacity;
        private bool _disposed;

        public CameraFrameBuffer(int capacity = 10)
        {
            _capacity = Math.Max(1, capacity);
        }

        public int Capacity
        {
            get { return _capacity; }
            set { lock (_syncRoot) { _capacity = Math.Max(1, value); TrimHistory_NoLock(); } }
        }

        public int Count { get { lock (_syncRoot) return _history.Count; } }

        public bool HasFrame { get { lock (_syncRoot) return _latestSnapshot != null; } }

        public long LatestSequenceNumber { get { lock (_syncRoot) return _latestSnapshot?.SequenceNumber ?? 0; } }

        public event EventHandler<CameraFrameSnapshot> SnapshotPublished;

        public CameraFrameSnapshot Publish(Bitmap frame, string deviceId = null, string correlationId = null)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            CameraFrameSnapshot snapshot;
            lock (_syncRoot)
            {
                ThrowIfDisposed_NoLock();
                snapshot = new CameraFrameSnapshot(deviceId, ++_nextSequenceNumber, new Bitmap(frame), correlationId);
                _history.Enqueue(snapshot);
                _latestSnapshot = snapshot;
                TrimHistory_NoLock();
                Monitor.PulseAll(_syncRoot);
            }
            SnapshotPublished?.Invoke(this, snapshot);
            return snapshot;
        }

        public CameraFrameSnapshot PeekLatestSnapshot()
        {
            lock (_syncRoot) { ThrowIfDisposed_NoLock(); return _latestSnapshot?.AddRef(); }
        }

        public CameraFrameSnapshot WaitForNextSnapshot(long afterSequenceNumber = 0, int timeoutMs = -1)
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed_NoLock();
                if (_latestSnapshot != null && _latestSnapshot.SequenceNumber > afterSequenceNumber)
                    return _latestSnapshot.AddRef();
                if (timeoutMs == 0) return null;
                if (timeoutMs < 0) { while (!_disposed) { Monitor.Wait(_syncRoot, 50); if (_latestSnapshot != null && _latestSnapshot.SequenceNumber > afterSequenceNumber) return _latestSnapshot.AddRef(); } return null; }
                var deadline = Environment.TickCount + timeoutMs;
                while (!_disposed) { var remaining = deadline - Environment.TickCount; if (remaining <= 0) return null; Monitor.Wait(_syncRoot, Math.Min(remaining, 100)); if (_latestSnapshot != null && _latestSnapshot.SequenceNumber > afterSequenceNumber) return _latestSnapshot.AddRef(); }
                return null;
            }
        }

        public void Clear()
        {
            lock (_syncRoot) { while (_history.Count > 0) _history.Dequeue().Dispose(); _latestSnapshot = null; Monitor.PulseAll(_syncRoot); }
        }

        public void Dispose()
        {
            lock (_syncRoot) { if (_disposed) return; _disposed = true; Monitor.PulseAll(_syncRoot); }
            Clear();
        }

        private void TrimHistory_NoLock() { while (_history.Count > _capacity) _history.Dequeue().Dispose(); }
        private void ThrowIfDisposed_NoLock() { if (_disposed) throw new ObjectDisposedException(nameof(CameraFrameBuffer)); }
    }
}
