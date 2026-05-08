using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.WorkFlow.Nodes
{
    [NodeCategory("通信", "串口接收")]
    [NodeBackground]
    public class SerialReceiveNode : FlowNode
    {
        [NodeProperty("串口", Category = "通信")]
        public string SerialPort { get; set; } = "COM1";

        [NodeProperty("波特率", Category = "通信", DefaultValue = 9600)]
        public int BaudRate { get; set; } = 9600;

        [NodeProperty("触发规则", Category = "通信")]
        public List<SerialTriggerRule> Rules { get; set; } = new List<SerialTriggerRule>();

        [NodeOutput("接收文本", Description = "string")]
        public string ReceivedText { get; set; }

        [NodeOutput("接收字节", Description = "byte[]")]
        public byte[] ReceivedBytes { get; set; }

        [NodeOutput("触发规则ID", Description = "string")]
        public string TriggeredRuleId { get; set; }

        private CancellationTokenSource _cts;
        private Task _listenTask;

        public event Action<SerialTriggerRule, string> RuleTriggered;

        public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            // SerialReceiveNode is background node, not called via regular ExecuteAsync
            await Task.CompletedTask;
        }

        public void StartListening(FlowContext context)
        {
            StopListening();

            dynamic vc = context.GetVariable<object>("VisionController");
            if (vc == null)
                throw new InvalidOperationException("VisionController not found in context.");

            if (!string.IsNullOrEmpty(SerialPort) && !vc.IsSerialOpen(SerialPort))
            {
                vc.ConnectSerial(SerialPort, BaudRate);
            }

            var ports = (System.Collections.Generic.IReadOnlyDictionary<string, ISerialPort>)vc.SerialPorts;
            if (!vc.IsSerialOpen(SerialPort))
                throw new InvalidOperationException($"Serial port {SerialPort} is not connected.");
            ISerialPort serialPort;
            if (!ports.TryGetValue(SerialPort, out serialPort))
                throw new InvalidOperationException($"Serial port {SerialPort} is not connected.");

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _listenTask = Task.Run(() =>
            {
                var dataHandler = new EventHandler<byte[]>((s, data) =>
                {
                    if (token.IsCancellationRequested) return;

                    ProcessReceivedData(data, context);
                });

                var textHandler = new EventHandler<string>((s, text) =>
                {
                    if (token.IsCancellationRequested) return;

                    ProcessReceivedText(text, context);
                });

                serialPort.RawDataReceived += dataHandler;
                serialPort.DataReceived += textHandler;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        Task.Delay(100, token).Wait(token);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    serialPort.RawDataReceived -= dataHandler;
                    serialPort.DataReceived -= textHandler;
                }
            }, token);
        }

        public void StopListening()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _listenTask = null;
        }

        private void ProcessReceivedData(byte[] data, FlowContext context)
        {
            if (Rules == null || Rules.Count == 0) return;

            foreach (var rule in Rules)
            {
                if (rule.Mode == SerialMatchMode.BinaryMatch && rule.MatchesBinary(data))
                {
                    HandleMatch(rule, null, data, context);
                    return;
                }
            }
        }

        private void ProcessReceivedText(string text, FlowContext context)
        {
            if (Rules == null || Rules.Count == 0) return;

            foreach (var rule in Rules)
            {
                if (rule.Mode != SerialMatchMode.BinaryMatch && rule.Matches(text))
                {
                    HandleMatch(rule, text, null, context);
                    return;
                }
            }
        }

        private void HandleMatch(SerialTriggerRule rule, string text, byte[] data, FlowContext context)
        {
            dynamic vc = context.GetVariable<object>("VisionController");

            if (rule.AutoResponseEnabled && vc != null)
            {
                if (!string.IsNullOrEmpty(rule.AutoResponseText) && vc.IsSerialOpen(SerialPort))
                    vc.OutputResult(SerialPort, rule.AutoResponseText);
                else if (rule.AutoResponseBytes != null && rule.AutoResponseBytes.Length > 0 && vc.IsSerialOpen(SerialPort))
                    vc.OutputResult(SerialPort, rule.AutoResponseBytes);
            }

            ReceivedText = text;
            ReceivedBytes = data;
            TriggeredRuleId = rule.RuleId;

            var textPin = FindOutput("接收文本");
            textPin?.SetValue(context, text);

            var bytesPin = FindOutput("接收字节");
            bytesPin?.SetValue(context, data);

            var rulePin = FindOutput("触发规则ID");
            rulePin?.SetValue(context, rule.RuleId);

            RuleTriggered?.Invoke(rule, text);
        }
    }
}
