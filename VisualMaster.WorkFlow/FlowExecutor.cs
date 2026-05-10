using VisualMaster.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.WorkFlow.Data;
using VisualMaster.WorkFlow.Nodes;

namespace VisualMaster.WorkFlow
{
    public class FlowExecutor : IDisposable
    {
        private FlowGraph _graph;
        private readonly ConcurrentDictionary<Guid, Task> _runningTasks = new ConcurrentDictionary<Guid, Task>();
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningCts = new ConcurrentDictionary<Guid, CancellationTokenSource>();
        private readonly ConcurrentDictionary<Guid, (ImageFifo fifo, EventHandler<Bitmap> handler, SemaphoreSlim semaphore)> _hardTriggerBindings
            = new ConcurrentDictionary<Guid, (ImageFifo, EventHandler<Bitmap>, SemaphoreSlim)>();
        private dynamic _visionController;
        private volatile bool _disposed;

        public FlowGraph Graph => _graph;
        public bool IsRunning { get; private set; }

        public event EventHandler<string> LogMessage;
        public event EventHandler<Exception> ExecutionError;

        public FlowExecutor(dynamic visionController)
        {
            _visionController = visionController ?? throw new ArgumentNullException(nameof(visionController));
        }

        public void LoadGraph(FlowGraph graph)
        {
            Task.Run(() => StopAsync()).Wait();
            _graph = graph;
            _graph.WireAllConnections();
            PreCompileNodes();

            foreach (var sg in _graph.SubGraphs)
            {
                if (sg.Trigger == SubGraphTrigger.AlwaysRunning)
                {
                    StartSubGraph(sg);
                }
            }
        }

        private void PreCompileNodes()
        {
            if (_graph == null) return;
            foreach (var sg in _graph.SubGraphs)
            {
                foreach (var node in sg.Nodes)
                {
                    if (node is ComputationNode cn)
                    {
                        try { cn.Compile(); }
                        catch { }
                    }
                }
            }
        }

        public void Start()
        {
            if (_graph == null) return;
            IsRunning = true;

            foreach (var sg in _graph.SubGraphs)
            {
                if (sg.Trigger == SubGraphTrigger.HardCameraTrigger)
                {
                    BindHardCameraTrigger(sg);
                }
            }
        }

        public void Stop()
        {
            Task.Run(() => StopAsync()).Wait();
        }

        public async Task StopAsync()
        {
            IsRunning = false;

            UnbindHardCameraTriggers();

            foreach (var kv in _runningCts)
            {
                try { kv.Value.Cancel(); } catch { }
            }

            var tasks = _runningTasks.Values.ToArray();
            if (tasks.Length > 0)
            {
                var allTasks = Task.WhenAll(tasks);
                var timeout = Task.Delay(3000);
                var completed = await Task.WhenAny(allTasks, timeout);
                if (completed == timeout)
                {
                    LogMessage?.Invoke(this, "[警告] 部分节点未能在3秒内停止");
                }
            }

            _runningTasks.Clear();
            _runningCts.Clear();
            foreach (var sg in _graph?.SubGraphs ?? Enumerable.Empty<FlowSubGraph>())
                sg.IsRunning = false;
        }

        public async Task TriggerSubGraph(Guid subGraphId)
        {
            var sg = _graph?.FindSubGraph(subGraphId);
            if (sg == null) return;

            if (sg.Trigger == SubGraphTrigger.AlwaysRunning)
                return;

            await RunSubGraphOnce(sg, CancellationToken.None);
        }

        public async Task TriggerSubGraphByName(string name)
        {
            var sg = _graph?.FindSubGraphByName(name);
            if (sg != null)
                await RunSubGraphOnce(sg, CancellationToken.None);
        }

        public void OnCommunicationTrigger(Guid nodeId, SerialTriggerRule rule)
        {
            if (_graph == null) return;

            if (rule.TargetSubGraphId.HasValue)
            {
                var _ = TriggerSubGraph(rule.TargetSubGraphId.Value);
            }
        }

        private void StartSubGraph(FlowSubGraph sg)
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var task = Task.Run(async () =>
            {
                while (!_disposed && IsRunning && sg.IsRunning && !token.IsCancellationRequested)
                {
                    try
                    {
                        await RunSubGraphOnce(sg, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        ExecutionError?.Invoke(this, ex);
                    }
                    await Task.Delay(10, token);
                }
            }, token);

            _runningTasks.TryAdd(sg.Id, task);
            _runningCts.TryAdd(sg.Id, cts);
            sg.IsRunning = true;
        }

        private void BindHardCameraTrigger(FlowSubGraph sg)
        {
            var slotId = FindCameraSlotIdInSubGraph(sg);
            if (string.IsNullOrEmpty(slotId)) return;

            ImageFifo fifo = _visionController?.GetFifo(slotId);
            if (fifo == null) return;

            var semaphore = new SemaphoreSlim(1, 1);
            var capturedSg = sg;
            async void handler(object s, Bitmap frame)
            {
                if (_disposed || !IsRunning) return;
                if (!await semaphore.WaitAsync(0)) return;

                try
                {
                    await RunSubGraphOnce(capturedSg, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    ExecutionError?.Invoke(this, ex);
                }
                finally
                {
                    semaphore.Release();
                }
            }

            fifo.FrameEnqueued += handler;
            _hardTriggerBindings.TryAdd(sg.Id, (fifo, handler, semaphore));
        }

        private void UnbindHardCameraTriggers()
        {
            foreach (var kv in _hardTriggerBindings)
            {
                kv.Value.fifo.FrameEnqueued -= kv.Value.handler;
                kv.Value.semaphore.Dispose();
            }
            _hardTriggerBindings.Clear();
        }

        private string FindCameraSlotIdInSubGraph(FlowSubGraph sg)
        {
            foreach (var node in sg.Nodes)
            {
                if (node is CameraAcquisitionNode camNode)
                {
                    if (!string.IsNullOrEmpty(camNode.SlotId))
                        return camNode.SlotId;
                    return _visionController?.GetFirstSlotId() ?? "";
                }
            }
            return _visionController?.GetFirstSlotId() ?? "";
        }

        private async Task RunSubGraphOnce(FlowSubGraph sg, CancellationToken cancellationToken)
        {
            var context = new FlowContext(sg.Id.ToString());
            context.SetVariable("VisionController", _visionController);
            context.OnLog += msg => LogMessage?.Invoke(this, $"[信息] {msg}");
            context.OnLogWarning += msg => LogMessage?.Invoke(this, $"[警告] {msg}");
            context.OnLogError += msg => LogMessage?.Invoke(this, $"[错误] {msg}");

            var levels = sg.GetTopologicalLevels();

            foreach (var level in levels)
            {
                if (level.Count == 0) continue;
                cancellationToken.ThrowIfCancellationRequested();

                var tasks = new List<Task>();
                foreach (var node in level)
                {
                    if (node.IsBackgroundWorker) continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            node.BindInputsToProperties(context);
                            await node.ExecuteAsync(context, cancellationToken);
                            node.WriteOutputsFromProperties(context);
                    foreach (var pin in node.Inputs)
                        pin.LastValue = pin.GetValue(context);
                    foreach (var pin in node.Outputs)
                        pin.LastValue = context.GetPinValue(pin);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            var msg = $"节点 [{node.Name}] 执行失败: {ex.Message}";
                            LogMessage?.Invoke(this, msg);
                            ExecutionError?.Invoke(this, new InvalidOperationException(msg, ex));
                        }
                        finally
                        {
                            sw.Stop();
                            node.LastExecutionTimeMs = sw.Elapsed.TotalMilliseconds;
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(tasks);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Stop();
            UnbindHardCameraTriggers();
            foreach (var cts in _runningCts.Values)
                cts.Dispose();
            _runningCts.Clear();
        }
    }
}
