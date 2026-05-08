using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using VisualMaster.Api;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.WorkFlow.Nodes
{
    public enum AcquisitionTriggerMode
    {
        HardTrigger,
        SoftTrigger,
    }

    [NodeCategory("取像", "相机取像")]
    public class CameraAcquisitionNode : FlowNode
    {
        [NodeProperty("相机槽位ID", Category = "取像")]
        public string SlotId { get; set; }

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

            var manager = context.GetVariable<ICameraManager>("CameraManager");
            if (manager == null)
                throw new InvalidOperationException("CameraManager not found in context.");

            var slot = manager.GetSlot(SlotId);
            if (slot == null)
                throw new InvalidOperationException($"Camera slot '{SlotId}' not found.");

            if (Trigger == AcquisitionTriggerMode.HardTrigger)
            {
                if (slot.Fifo == null)
                    throw new InvalidOperationException("Camera FIFO not available.");

                var bmp = slot.Fifo.TryDequeue(TimeoutMs);
                if (bmp == null)
                    throw new TimeoutException("Timeout waiting for frame from camera.");

                using (bmp)
                {
                    var mat = ImageConverter.BitmapToMat(bmp);
                    Result = new AcquisitionResult(mat)
                    {
                        CameraSerial = slot.AssignedSerial,
                        Timestamp = DateTime.Now,
                        TriggerModeUsed = "HardTrigger",
                    };
                    mat.Dispose();
                }
            }
            else
            {
                if (slot.Camera == null)
                    throw new InvalidOperationException($"Camera slot '{SlotId}' is not open.");

                slot.Camera.TriggerSoftware();

                bool got = slot.Camera.TryGrabImage(out Bitmap bmp, TimeoutMs);
                if (!got || bmp == null)
                    throw new TimeoutException("Soft trigger grab timed out.");

                using (bmp)
                {
                    var mat = ImageConverter.BitmapToMat(bmp);
                    Result = new AcquisitionResult(mat)
                    {
                        CameraSerial = slot.AssignedSerial,
                        Timestamp = DateTime.Now,
                        TriggerModeUsed = "SoftTrigger",
                    };
                    mat.Dispose();
                }
            }
        }
    }
}
