using System;

namespace VisualMaster.WorkFlow.Data
{
    /// <summary>
    /// 流程单次运行的元数据：运行 ID、子图名称、关联 ID、开始/完成时间。
    /// </summary>
    public sealed class FlowRunMetadata
    {
        public string RunId { get; set; } = Guid.NewGuid().ToString("N");
        public string SubGraphId { get; set; }
        public string SubGraphName { get; set; }
        public string CorrelationId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public bool Succeeded { get; set; }
    }
}
