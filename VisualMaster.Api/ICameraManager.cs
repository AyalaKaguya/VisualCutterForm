using System;
using System.Collections.Generic;

namespace VisualMaster.Api
{
    public interface ICameraManager : IDisposable
    {
        List<CameraInfo> EnumerateCameras();
        List<CameraSlot> Slots { get; }

        CameraSlot AddSlot(string name, CameraSettings settings);
        void RemoveSlot(string slotId);
        ICamera OpenSlot(string slotId, CameraInfo info);
        void CloseSlot(string slotId);
        void TriggerSoftware(string slotId);
        bool IsSlotOpen(string slotId);
        CameraSlot GetSlot(string slotId);

        event EventHandler<CameraSlot> SlotOpened;
        event EventHandler<CameraSlot> SlotClosed;
    }
}
