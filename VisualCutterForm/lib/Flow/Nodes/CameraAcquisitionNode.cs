using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using VisualCutterForm.Lib.Flow.Data;

namespace VisualCutterForm.Lib.Flow.Nodes
{
    public enum AcquisitionTriggerMode
    {
        HardTrigger,
        SoftTrigger,
    }

    [NodeCategory("取像", "相机取像")]
    public class CameraAcquisitionNode : FlowNode
    {
        [NodeProperty("相机序列号", Category = "取像")]
        public string CameraSerial { get; set; }

        [NodeProperty("触发模式", Category = "取像")]
        public AcquisitionTriggerMode Trigger { get; set; } = AcquisitionTriggerMode.HardTrigger;

        [NodeProperty("超时(ms)", Category = "取像", DefaultValue = 3000, Min = 100, Max = 60000)]
        public int TimeoutMs { get; set; } = 3000;

        [NodeProperty("等待帧", Category = "取像", DefaultValue = 0, Min = 0, Max = 100)]
        public int SkipFrames { get; set; } = 0;

        [NodeOutput("取像结果", Description = "AcquisitionResult")]
        public AcquisitionResult Result { get; set; }

        public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var vc = context.GetVariable<VisionController>("VisionController");
            if (vc == null)
                throw new InvalidOperationException("VisionController not found in context.");

            if (string.IsNullOrEmpty(CameraSerial))
                CameraSerial = vc.GetFirstActiveSerial();

            if (string.IsNullOrEmpty(CameraSerial))
                throw new InvalidOperationException("No camera selected.");

            if (Trigger == AcquisitionTriggerMode.HardTrigger)
            {
                var fifo = vc.GetFifo(CameraSerial);
                if (fifo == null)
                    throw new InvalidOperationException("Camera FIFO not available.");

                var bmp = fifo.TryDequeue(TimeoutMs);
                if (bmp == null)
                    throw new TimeoutException("Timeout waiting for frame from camera.");

                using (bmp)
                {
                    var mat = ImageConverter.BitmapToMat(bmp);
                    Result = new AcquisitionResult(mat)
                    {
                        CameraSerial = CameraSerial,
                        Timestamp = DateTime.Now,
                        TriggerModeUsed = "HardTrigger",
                    };
                }
            }
            else
            {
                if (vc.Slots.TryGetValue(CameraSerial, out var slot))
                {
                    bool got = slot.Camera.TryGrabImage(out Bitmap bmp, TimeoutMs);
                    if (!got || bmp == null)
                        throw new TimeoutException("Soft trigger grab timed out.");

                    using (bmp)
                    {
                        var mat = ImageConverter.BitmapToMat(bmp);
                        Result = new AcquisitionResult(mat)
                        {
                            CameraSerial = CameraSerial,
                            Timestamp = DateTime.Now,
                            TriggerModeUsed = "SoftTrigger",
                        };
                    }
                }
            }
        }
    }
}
