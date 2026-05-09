using System;
using System.Collections.Generic;

namespace VisualMaster.Api
{
    public interface ICameraManager : IDisposable
    {
        bool IsInitialized { get; }
        void Initialize();
        List<CameraInfo> EnumerateCameras();
        IReadOnlyList<CameraSlot> Slots { get; }

        CameraSlot AddSlot(string name, CameraSettings settings = null);
        void RemoveSlot(string slotId);
        void OpenSlot(string slotId, CameraInfo info);
        void CloseSlot(string slotId);
        void StartGrabbing(string slotId);
        void StopGrabbing(string slotId);
        void TriggerSoftware(string slotId);
        bool IsSlotOpen(string slotId);

        event EventHandler<CameraSlot> SlotOpened;
        event EventHandler<CameraSlot> SlotClosed;
    }
}
