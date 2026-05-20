using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.WorkFlow.Nodes
{
    [NodeCategory("通信", "串口发送")]
    public class SerialSendNode : FlowNode
    {
        [NodeProperty("串口设备", Category = "通信")]
        public string SlotId { get; set; } = "";

        [NodeProperty("串口", Category = "通信")]
        public string SerialPort { get; set; } = "COM1";

        [NodeProperty("波特率", Category = "通信", DefaultValue = 9600)]
        public int BaudRate { get; set; } = 9600;

        [NodeInput("发送文本", Description = "string")]
        public string TextToSend { get; set; }

        [NodeInput("发送字节", Description = "byte[]")]
        public byte[] BytesToSend { get; set; }

        public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            var services = context.GetVariable<IFlowServiceProvider>("VisionController");
            if (services == null)
                throw new InvalidOperationException("IFlowServiceProvider not found in context.");

            var portName = SerialPort;
            var baud = BaudRate;

            if (!string.IsNullOrEmpty(SlotId))
            {
                var resolvedPortName = services.GetSerialPortName(SlotId);
                if (!string.IsNullOrEmpty(resolvedPortName))
                {
                    portName = resolvedPortName;
                    baud = services.GetSerialBaudRate(SlotId);
                }
            }

            if (!string.IsNullOrEmpty(portName) && !services.IsSerialOpen(portName))
            {
                services.ConnectSerial(portName, baud);
            }

            if (!services.IsSerialOpen(portName))
                throw new InvalidOperationException($"Serial port {portName} is not connected.");

            var pinText = FindInput("发送文本");
            var pinBytes = FindInput("发送字节");

            if (pinText != null && pinText.IsConnected)
            {
                var val = pinText.GetValue(context);
                if (val is string s && !string.IsNullOrEmpty(s))
                {
                    services.OutputResult(portName, s);
                    return;
                }
            }

            if (pinBytes != null && pinBytes.IsConnected)
            {
                var val = pinBytes.GetValue(context);
                if (val is byte[] b && b.Length > 0)
                {
                    services.OutputResult(portName, b);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(TextToSend))
            {
                services.OutputResult(portName, TextToSend);
            }
            else if (BytesToSend != null && BytesToSend.Length > 0)
            {
                services.OutputResult(portName, BytesToSend);
            }

            await Task.CompletedTask;
        }
    }
}
