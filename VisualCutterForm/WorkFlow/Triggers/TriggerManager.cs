using VisualMaster.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.WorkFlow.Triggers
{
    public class TriggerManager : IDisposable
    {
        private readonly IFlowServiceProvider _services;
        private readonly FlowExecutor _executor;
        private readonly ConcurrentDictionary<Guid, ActiveTriggerState> _states = new ConcurrentDictionary<Guid, ActiveTriggerState>();
        private readonly List<IDisposable> _bindings = new List<IDisposable>();
        private volatile bool _disposed;

        public TriggerManager(IFlowServiceProvider services, FlowExecutor executor)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public async Task Activate(IReadOnlyList<TriggerEntry> entries)
        {
            Deactivate();
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (!entry.Enabled) continue;
                if (entry.GetTargetSubGraphIds().Count == 0) continue;

                var state = new ActiveTriggerState
                {
                    Entry = entry,
                    Semaphore = new SemaphoreSlim(entry.MaxConcurrent, entry.MaxConcurrent),
                };

                switch (entry.SourceType)
                {
                    case TriggerSourceType.Manual:
                        break;
                    case TriggerSourceType.CameraFrame:
                        state.Binding = BindCameraFrame(entry, state.Semaphore);
                        break;
                    case TriggerSourceType.Timer:
                        state.Binding = BindTimer(entry, state.Semaphore);
                        break;
                    case TriggerSourceType.SerialMatch:
                        state.Binding = BindSerialMatch(entry, state.Semaphore);
                        break;
                }

                _states.TryAdd(entry.Id, state);
                if (state.Binding != null)
                    _bindings.Add(state.Binding);
            }
        }

        public async Task FireManual(Guid triggerId)
        {
            if (_disposed) return;
            if (!_states.TryGetValue(triggerId, out var state)) return;
            if (!await state.Semaphore.WaitAsync(0)) return;

            try
            {
                await DispatchTrigger(state.Entry, CreateTriggerContext(state.Entry));
            }
            catch (Exception ex)
            {
                _executor.EmitLog($"[错误] 手动触发 [{state.Entry.Name}]: {ex.Message}");
            }
            finally
            {
                state.Semaphore.Release();
            }
        }

        public void Deactivate()
        {
            foreach (var b in _bindings)
            {
                try { b.Dispose(); } catch { }
            }
            _bindings.Clear();

            foreach (var kv in _states)
            {
                try { kv.Value.Semaphore.Dispose(); } catch { }
                try { kv.Value.Binding?.Dispose(); } catch { }
            }
            _states.Clear();
        }

        private IDisposable BindCameraFrame(TriggerEntry entry, SemaphoreSlim semaphore)
        {
            if (string.IsNullOrEmpty(entry.CameraDeviceId)) return null;

            var fifo = _services.GetFifo(entry.CameraDeviceId);
            var buffer = fifo?.Buffer;
            if (buffer == null) return null;

            var capturedName = entry.Name;

            async void handler(object s, CameraFrameSnapshot snapshot)
            {
                if (_disposed) return;
                if (!await semaphore.WaitAsync(0)) return;
                try
                {
                    await DispatchTrigger(entry, CreateCameraTriggerContext(entry, snapshot));
                }
                catch (Exception ex)
                {
                    _executor.EmitLog($"[错误] 相机触发器 [{capturedName}]: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }

            buffer.SnapshotPublished += handler;
            return new BindingDisposer(() =>
            {
                buffer.SnapshotPublished -= handler;
            });
        }

        private IDisposable BindTimer(TriggerEntry entry, SemaphoreSlim semaphore)
        {
            var capturedName = entry.Name;
            var interval = Math.Max(1, entry.TimerIntervalMs);

            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var task = Task.Run(async () =>
            {
                while (!_disposed && !token.IsCancellationRequested)
                {
                    if (await semaphore.WaitAsync(0))
                    {
                        try
                        {
                            await DispatchTrigger(entry, CreateTriggerContext(entry));
                        }
                        catch (Exception ex)
                        {
                            _executor.EmitLog($"[错误] 定时触发器 [{capturedName}]: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                    try { await Task.Delay(interval, token); } catch (OperationCanceledException) { break; }
                }
            }, token);

            return new BindingDisposer(() =>
            {
                cts.Cancel();
                try { task.Wait(1000); } catch { }
                cts.Dispose();
            });
        }

        private IDisposable BindSerialMatch(TriggerEntry entry, SemaphoreSlim semaphore)
        {
            if (string.IsNullOrEmpty(entry.SerialDeviceId) || entry.MatchRule == null) return null;

            var portName = _services.GetSerialPortName(entry.SerialDeviceId);
            if (string.IsNullOrEmpty(portName)) return null;

            var baudRate = _services.GetSerialBaudRate(entry.SerialDeviceId);

            if (!_services.IsSerialOpen(portName))
            {
                try { _services.ConnectSerial(portName, baudRate); }
                catch { return null; }
            }

            var portsObj = _services.SerialPorts;
            if (!portsObj.TryGetValue(portName, out var sp) || sp == null) return null;

            var capturedRule = entry.MatchRule;
            var capturedName = entry.Name;

            void textHandler(object s, string text)
            {
                if (_disposed) return;
                if (capturedRule.Matches(text))
                    FireMatch(text, null, capturedRule.RuleId);
            }

            void dataHandler(object s, byte[] data)
            {
                if (_disposed) return;
                if (capturedRule.MatchesBinary(data))
                    FireMatch(null, data, capturedRule.RuleId);
            }

            async void FireMatch(string text, byte[] data, string matchedRuleId)
            {
                if (!await semaphore.WaitAsync(0)) return;
                try
                {
                    await DispatchTrigger(entry, CreateSerialTriggerContext(entry, text, data, matchedRuleId));
                }
                catch (Exception ex)
                {
                    _executor.EmitLog($"[错误] 串口触发器 [{capturedName}]: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }

            sp.DataReceived += textHandler;
            sp.RawDataReceived += dataHandler;

            return new BindingDisposer(() =>
            {
                sp.DataReceived -= textHandler;
                sp.RawDataReceived -= dataHandler;
            });
        }

        public void Dispose()
        {
            _disposed = true;
            Deactivate();
        }

        private static FlowTriggerContext CreateTriggerContext(TriggerEntry entry)
        {
            return new FlowTriggerContext
            {
                TriggerId = entry.Id,
                TriggerName = entry.Name,
                SourceType = entry.SourceType,
                SourceDeviceId = entry.SourceType == TriggerSourceType.CameraFrame ? entry.CameraDeviceId : entry.SerialDeviceId,
                OccurredAt = DateTime.Now,
            };
        }

        private static FlowTriggerContext CreateCameraTriggerContext(TriggerEntry entry, CameraFrameSnapshot snapshot)
        {
            var context = CreateTriggerContext(entry);
            context.CameraSnapshot = snapshot?.AddRef();
            return context;
        }

        private static FlowTriggerContext CreateSerialTriggerContext(TriggerEntry entry, string text, byte[] data, string matchedRuleId)
        {
            var context = CreateTriggerContext(entry);
            context.SerialText = text;
            context.SerialData = data != null ? (byte[])data.Clone() : null;
            context.MatchedRuleId = matchedRuleId;
            return context;
        }

        private async Task DispatchTrigger(TriggerEntry entry, FlowTriggerContext triggerContext)
        {
            var targetIds = entry.GetTargetSubGraphIds();
            if (targetIds.Count == 0)
                return;

            try
            {
                var tasks = new List<Task>(targetIds.Count);
                foreach (var targetId in targetIds)
                {
                    var targetSubGraph = _executor.Graph?.FindSubGraph(targetId);
                    _services.RuntimeDiagnostics?.Record(new RuntimeDiagnosticEvent
                    {
                        EventType = RuntimeDiagnosticEventType.TriggerDispatched,
                        CorrelationId = triggerContext?.CorrelationId,
                        DeviceId = triggerContext?.SourceDeviceId,
                        SnapshotId = triggerContext?.CameraSnapshotId,
                        SnapshotSequence = triggerContext?.CameraSnapshotSequence ?? 0,
                        TriggerId = entry.Id.ToString(),
                        TriggerName = entry.Name,
                        FlowId = targetId.ToString("N"),
                        FlowName = targetSubGraph?.Name,
                        Message = $"触发器已分发到流程: {targetSubGraph?.Name ?? targetId.ToString()}",
                    });
                    tasks.Add(_executor.TriggerSubGraph(targetId, triggerContext?.Clone()));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                triggerContext?.Dispose();
            }
        }

        private class ActiveTriggerState
        {
            public TriggerEntry Entry;
            public SemaphoreSlim Semaphore;
            public IDisposable Binding;
        }

        private class BindingDisposer : IDisposable
        {
            private readonly Action _onDispose;
            public BindingDisposer(Action onDispose) { _onDispose = onDispose; }
            public void Dispose() { _onDispose?.Invoke(); }
        }
    }
}
