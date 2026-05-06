using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisualCutterForm.Lib.Flow.Data;

namespace VisualCutterForm.Lib.Flow.Nodes
{
    [NodeCategory("通信", "串口发送")]
    public class SerialSendNode : FlowNode
    {
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
            var vc = context.GetVariable<VisionController>("VisionController");
            if (vc == null)
                throw new InvalidOperationException("VisionController not found in context.");

            if (!string.IsNullOrEmpty(SerialPort) && !vc.IsSerialOpen(SerialPort))
            {
                vc.ConnectSerial(SerialPort, BaudRate);
            }

            if (!vc.IsSerialOpen(SerialPort))
                throw new InvalidOperationException($"Serial port {SerialPort} is not connected.");

            var pinText = FindInput("发送文本");
            var pinBytes = FindInput("发送字节");

            if (pinText != null && pinText.IsConnected)
            {
                var val = pinText.GetValue(context);
                if (val is string s && !string.IsNullOrEmpty(s))
                {
                    vc.OutputResult(SerialPort, s);
                    return;
                }
            }

            if (pinBytes != null && pinBytes.IsConnected)
            {
                var val = pinBytes.GetValue(context);
                if (val is byte[] b && b.Length > 0)
                {
                    vc.OutputResult(SerialPort, b);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(TextToSend))
            {
                vc.OutputResult(SerialPort, TextToSend);
            }
            else if (BytesToSend != null && BytesToSend.Length > 0)
            {
                vc.OutputResult(SerialPort, BytesToSend);
            }
        }
    }
}
