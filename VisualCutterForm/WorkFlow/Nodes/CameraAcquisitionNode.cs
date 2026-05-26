using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using VisualMaster.Api;
using CameraFrameSnapshot = VisualMaster.CameraLink.Api.CameraFrameSnapshot;
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
        [NodeProperty("相机设备", Category = "取像")]
        public string SlotId { get; set; }

        [NodeProperty("触发模式", Category = "取像")]
        public AcquisitionTriggerMode Trigger { get; set; } = AcquisitionTriggerMode.HardTrigger;

        [NodeProperty("超时(ms)", Category = "取像", DefaultValue = 3000, Min = 100, Max = 60000)]
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>
        /// 流程级曝光覆盖（微秒）。设为 -1 表示不覆盖，使用相机当前设置。
        /// </summary>
        [NodeProperty("曝光覆盖(us)", Category = "取像", DefaultValue = -1, Min = -1, Max = 1000000)]
        public int ExposureOverrideUs { get; set; } = -1;

        /// <summary>
        /// 流程级增益覆盖（dB × 10，即 100 = 10.0 dB）。设为 -1 表示不覆盖。
        /// </summary>
        [NodeProperty("增益覆盖(dB×10)", Category = "取像", DefaultValue = -1, Min = -1, Max = 500)]
        public int GainOverrideDb10 { get; set; } = -1;

        [NodeOutput("取像结果", Description = "AcquisitionResult")]
        public AcquisitionResult Result { get; set; }

        public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 优先使用 context.Services，向后兼容保留 GetVariable 路径
            var services = context.Services ?? context.GetVariable<IFlowServiceProvider>("VisionController");
            if (services == null)
                throw new InvalidOperationException("IFlowServiceProvider not found in context.");

            var slotId = !string.IsNullOrEmpty(SlotId)
                ? SlotId
                : services.GetFirstCameraDeviceId();

            if (string.IsNullOrEmpty(slotId))
                throw new InvalidOperationException("No camera slot available.");

            // 应用流程级参数覆盖
            ApplyParameterOverrides(services, slotId);

            if (Trigger == AcquisitionTriggerMode.HardTrigger)
            {
                // 优先消费本次触发帧（同一设备的触发快照）
                if (context.TryGetTriggeredFrameClone(out var triggeredBmp, slotId))
                {
                    using (triggeredBmp)
                    {
                        Result = CreateResultFromBitmap(triggeredBmp, services, slotId, "TriggerFrame");
                    }
                    // 将触发快照注册到 context
                    if (context.Trigger?.CameraSnapshot != null)
                        context.RegisterSnapshot(slotId, context.Trigger.CameraSnapshot);
                    return;
                }

                var afterSequence = services.GetLatestFrameSequenceNumber(slotId);
                using (var snapshot = services.WaitForNextFrameSnapshot(slotId, afterSequence, TimeoutMs))
                {
                    if (snapshot == null)
                        throw new TimeoutException("Timeout waiting for frame from camera.");

                    context.RegisterSnapshot(slotId, snapshot);
                    using (var bmp = snapshot.CloneFrame())
                    {
                        Result = CreateResultFromBitmap(bmp, services, slotId, "HardTrigger");
                    }
                }
            }
            else
            {
                if (!services.IsCameraConnected(slotId))
                    throw new InvalidOperationException("Camera is not connected.");

                var afterSequence = services.GetLatestFrameSequenceNumber(slotId);
                services.TriggerSoftware(slotId);

                using (var snapshot = services.WaitForNextFrameSnapshot(slotId, afterSequence, TimeoutMs))
                {
                    if (snapshot == null)
                        throw new TimeoutException("Soft trigger grab timed out.");

                    context.RegisterSnapshot(slotId, snapshot);
                    using (var bmp = snapshot.CloneFrame())
                    {
                        Result = CreateResultFromBitmap(bmp, services, slotId, "SoftTrigger");
                    }
                }
            }
        }

        private void ApplyParameterOverrides(IFlowServiceProvider services, string slotId)
        {
            if (ExposureOverrideUs < 0 && GainOverrideDb10 < 0)
                return;

            var current = services.GetCameraSettings(slotId);
            if (current == null) return;

            var updated = current.Clone();
            bool changed = false;

            if (ExposureOverrideUs >= 0)
            {
                updated.ExposureTimeUs = ExposureOverrideUs;
                changed = true;
            }
            if (GainOverrideDb10 >= 0)
            {
                updated.GainRaw = GainOverrideDb10 / 10.0;
                changed = true;
            }

            if (changed)
                services.UpdateCameraSettings(slotId, updated);
        }

        private static AcquisitionResult CreateResultFromBitmap(Bitmap bmp, IFlowServiceProvider services, string slotId, string triggerMode)
        {
            var mat = ImageConverter.BitmapToMat(bmp);
            try
            {
                return new AcquisitionResult(mat)
                {
                    CameraSerial = services.GetCameraAssignedSerial(slotId) ?? services.GetCameraDisplayName(slotId) ?? slotId,
                    Timestamp = DateTime.Now,
                    Width = bmp.Width,
                    Height = bmp.Height,
                    TriggerModeUsed = triggerMode,
                };
            }
            finally
            {
                mat.Dispose();
            }
        }
    }
}

