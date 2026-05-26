using System;
using VisualMaster.Api;

namespace VisualCutterForm.Legacy
{
    /// <summary>
    /// 已废弃的相机槽位类。
    /// 配置请使用 <see cref="CameraDeviceConfig"/>，运行时状态请使用 <see cref="CameraDeviceStatus"/>。
    /// </summary>
    [Obsolete("Use CameraDeviceConfig for configuration and CameraDeviceStatus for runtime state.", false)]
    public class CameraSlot
    {
        public string SlotId { get; set; }
        public string SlotName { get; set; }
        public CameraSettings Settings { get; set; }
        public string AssignedSerial { get; set; }
    }
}

