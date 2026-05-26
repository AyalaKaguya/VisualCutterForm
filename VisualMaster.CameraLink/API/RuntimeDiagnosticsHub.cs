using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.CameraLink.Api
{
    public sealed class RuntimeDiagnosticsHub
    {
        private readonly object _syncRoot = new object();
        private readonly LinkedList<RuntimeDiagnosticEvent> _events = new LinkedList<RuntimeDiagnosticEvent>();
        private int _capacity;

        public RuntimeDiagnosticsHub(int capacity = 500) { _capacity = Math.Max(50, capacity); }

        public int Capacity
        {
            get { return _capacity; }
            set { lock (_syncRoot) { _capacity = Math.Max(50, value); Trim_NoLock(); } }
        }

        public event EventHandler EventsChanged;

        public void Record(RuntimeDiagnosticEvent diagnosticEvent)
        {
            if (diagnosticEvent == null) return;
            lock (_syncRoot) { _events.AddFirst(diagnosticEvent); Trim_NoLock(); }
            EventsChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyList<RuntimeDiagnosticEvent> GetRecentEvents(int maxCount = 200)
        {
            lock (_syncRoot) { return _events.Take(Math.Max(1, maxCount)).ToList(); }
        }

        public void Clear()
        {
            lock (_syncRoot) { _events.Clear(); }
            EventsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Trim_NoLock() { while (_events.Count > _capacity) _events.RemoveLast(); }
    }
}
