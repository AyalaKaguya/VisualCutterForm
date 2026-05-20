using System;

namespace VisualMaster.WorkFlow.Data
{
    public enum FlowLogLevel
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// 流程运行期间产生的一条结构化日志记录。
    /// </summary>
    public sealed class FlowLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public FlowLogLevel Level { get; set; }
        public string Message { get; set; }
        public string NodeName { get; set; }
    }
}
