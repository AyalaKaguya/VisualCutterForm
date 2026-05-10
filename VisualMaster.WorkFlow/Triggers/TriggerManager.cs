using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VisualMaster.WorkFlow.Triggers
{
    public class TriggerManager : IDisposable
    {
        private readonly IFlowServiceProvider _services;
        private readonly FlowExecutor _executor;
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

                IDisposable binding = null;
                switch (entry.SourceType)
                {
                    case TriggerSourceType.CameraFrame:
                        binding = BindCameraFrame(entry);
                        break;
                    case TriggerSourceType.Timer:
                        binding = BindTimer(entry);
                        break;
                    case TriggerSourceType.SerialMatch:
                        binding = BindSerialMatch(entry);
                        break;
                }
                if (binding != null)
                    _bindings.Add(binding);
            }
        }

        public async Task FireManual(Guid triggerId)
        {
            if (_disposed) return;
            await _executor.TriggerSubGraph(triggerId);
        }

        public void Deactivate()
        {
            foreach (var b in _bindings)
            {
                try { b.Dispose(); } catch { }
            }
            _bindings.Clear();
        }

        private IDisposable BindCameraFrame(TriggerEntry entry)
        {
            if (string.IsNullOrEmpty(entry.CameraSlotId)) return null;

            var fifo = _services.GetFifo(entry.CameraSlotId);
            if (fifo == null) return null;

            var semaphore = new SemaphoreSlim(entry.MaxConcurrent, entry.MaxConcurrent);
            var capturedId = entry.TargetSubGraphId;

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
                    _executor.EmitLog($"[错误] 相机触发器 [{entry.Name}]: {ex.Message}");
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
                semaphore.Dispose();
            });
        }

        private IDisposable BindTimer(TriggerEntry entry)
        {
            var semaphore = new SemaphoreSlim(entry.MaxConcurrent, entry.MaxConcurrent);
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
                semaphore.Dispose();
            });
        }

        private IDisposable BindSerialMatch(TriggerEntry entry)
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

            var semaphore = new SemaphoreSlim(entry.MaxConcurrent, entry.MaxConcurrent);
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
                semaphore.Dispose();
            });
        }

        public void Dispose()
        {
            _disposed = true;
            Deactivate();
        }

        private class BindingDisposer : IDisposable
        {
            private readonly Action _onDispose;
            public BindingDisposer(Action onDispose) { _onDispose = onDispose; }
            public void Dispose() { _onDispose?.Invoke(); }
        }
    }
}
