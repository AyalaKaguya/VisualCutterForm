namespace VisualMaster.CameraLink.API
{
    /// <summary>相机取像模式。</summary>
    public enum CameraAcquireMode
    {
        /// <summary>连续取像，相机自主持续输出图像。</summary>
        Continuous = 0,
        /// <summary>软件触发：调用 TriggerSoftware() 后相机拍摄一帧。</summary>
        Software = 1,
        /// <summary>硬件触发：外部信号边沿触发相机拍摄。</summary>
        Hardware = 2,
    }
}
