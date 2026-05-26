using System;
using System.Collections.Generic;

namespace VisualCutterForm.Legacy
{
    public interface ISerialManager : IDisposable
    {
        List<SerialSlot> Slots { get; }
        SerialSlot AddSlot(string name, SerialSlot config = null);
        void RemoveSlot(string slotId);
        void OpenSlot(string slotId);
        void CloseSlot(string slotId);
        void Send(string slotId, string text);
        void Send(string slotId, byte[] data);
        bool IsSlotOpen(string slotId);

        event EventHandler<SerialSlot> SlotOpened;
        event EventHandler<SerialSlot> SlotClosed;
        event EventHandler<SerialDataEventArgs> DataReceived;
    }

    public class SerialDataEventArgs : EventArgs
    {
        public string SlotId { get; set; }
        public string Text { get; set; }
    }
}
