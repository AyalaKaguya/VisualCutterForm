using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.WorkFlow.Nodes
{
    [NodeCategory("通信", "串口接收")]
    public class SerialReceiveNode : FlowNode
    {
        [NodeProperty("串口设备", Category = "通信")]
        public string SlotId { get; set; } = "";

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

        public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string text;
            byte[] data;
            string matchedRuleId;
            if (!context.TryGetTriggeredSerialPayload(out text, out data, out matchedRuleId, SlotId))
                throw new InvalidOperationException("No serial trigger payload available in the current flow context.");

            ReceivedText = text;
            ReceivedBytes = data;
            TriggeredRuleId = matchedRuleId;

            await Task.CompletedTask;
        }
    }
}
