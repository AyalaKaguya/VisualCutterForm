namespace VisualCutterForm.Lib.Flow
{
    public static class TriggerExtensions
    {
        public static string ToDisplayName(this SubGraphTrigger trigger)
        {
            switch (trigger)
            {
                case SubGraphTrigger.HardCameraTrigger: return "相机触发";
                case SubGraphTrigger.SoftManualTrigger: return "手动触发";
                case SubGraphTrigger.CommunicationTrigger: return "通讯触发";
                case SubGraphTrigger.AlwaysRunning: return "持续运行";
                default: return trigger.ToString();
            }
        }
    }
}
