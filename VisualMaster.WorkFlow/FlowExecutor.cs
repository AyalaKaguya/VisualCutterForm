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
using VisualMaster.WorkFlow.Triggers;

namespace VisualMaster.WorkFlow
{
    public class FlowExecutor : IDisposable
    {
        private FlowGraph _graph;
        private TriggerManager _triggerManager;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningCts = new ConcurrentDictionary<Guid, CancellationTokenSource>();
        private IFlowServiceProvider _services;

        public FlowGraph Graph => _graph;
        public bool IsRunning { get; private set; }

        public event EventHandler<string> LogMessage;
        public event EventHandler<Exception> ExecutionError;

        internal void EmitLog(string message)
        {
            LogMessage?.Invoke(this, message);
        }

        public FlowExecutor(IFlowServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public void LoadGraph(FlowGraph graph)
        {
            Stop();
            _graph = graph;
            _graph.WireAllConnections();
            PreCompileNodes();
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

            _triggerManager?.Dispose();
            _triggerManager = new TriggerManager(_services, this);
            _triggerManager.Activate(_graph.Triggers).Wait();
        }

        public void Stop()
        {
            IsRunning = false;
            _triggerManager?.Dispose();
            _triggerManager = null;

            foreach (var kv in _runningCts)
            {
                try { kv.Value.Cancel(); }
                catch { }
            }

            _runningCts.Clear();
            foreach (var sg in _graph?.SubGraphs ?? Enumerable.Empty<FlowSubGraph>())
                sg.IsRunning = false;
        }

        public async Task TriggerSubGraph(Guid subGraphId)
        {
            var sg = _graph?.FindSubGraph(subGraphId);
            if (sg == null) return;
            await RunSubGraphOnce(sg, CancellationToken.None);
        }

        public async Task TriggerSubGraphByName(string name)
        {
            var sg = _graph?.FindSubGraphByName(name);
            if (sg != null)
                await RunSubGraphOnce(sg, CancellationToken.None);
        }

        private async Task RunSubGraphOnce(FlowSubGraph sg, CancellationToken cancellationToken)
        {
            var context = new FlowContext(sg.Id.ToString());
            context.SetVariable("VisionController", _services);
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
            Stop();
        }
    }
}
