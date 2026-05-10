using VisualMaster.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

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
                if (entry.TargetSubGraphId == Guid.Empty) continue;

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
                await _executor.TriggerSubGraph(state.Entry.TargetSubGraphId);
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
            if (string.IsNullOrEmpty(entry.CameraSlotId)) return null;

            var fifo = _services.GetFifo(entry.CameraSlotId);
            if (fifo == null) return null;

            var capturedId = entry.TargetSubGraphId;
            var capturedName = entry.Name;

            async void handler(object s, Bitmap frame)
            {
                if (_disposed) return;
                if (!await semaphore.WaitAsync(0)) return;
                try
                {
                    await _executor.TriggerSubGraph(capturedId);
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

            fifo.FrameEnqueued += handler;
            return new BindingDisposer(() =>
            {
                fifo.FrameEnqueued -= handler;
            });
        }

        private IDisposable BindTimer(TriggerEntry entry, SemaphoreSlim semaphore)
        {
            var capturedId = entry.TargetSubGraphId;
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
                            await _executor.TriggerSubGraph(capturedId);
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
            if (string.IsNullOrEmpty(entry.SerialSlotId) || entry.MatchRule == null) return null;

            var slot = _services.GetSerialSlot(entry.SerialSlotId);
            if (slot == null) return null;

            if (!_services.IsSerialOpen(slot.PortName))
            {
                try { _services.ConnectSerial(slot.PortName, slot.BaudRate); }
                catch { return null; }
            }

            var portsObj = _services.SerialPorts;
            if (!portsObj.TryGetValue(slot.PortName, out var sp) || sp == null) return null;

            var capturedId = entry.TargetSubGraphId;
            var capturedRule = entry.MatchRule;
            var capturedName = entry.Name;

            void textHandler(object s, string text)
            {
                if (_disposed) return;
                if (capturedRule.Matches(text))
                    FireMatch(text, null);
            }

            void dataHandler(object s, byte[] data)
            {
                if (_disposed) return;
                if (capturedRule.MatchesBinary(data))
                    FireMatch(null, data);
            }

            async void FireMatch(string text, byte[] data)
            {
                if (!await semaphore.WaitAsync(0)) return;
                try
                {
                    await _executor.TriggerSubGraph(capturedId);
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
