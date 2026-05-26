using System;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationBlockUpdatedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string BlockId { get; }
        public string Address { get; }
        public byte[] Data { get; }
        public DateTime Timestamp { get; }

        public CommunicationBlockUpdatedEventArgs(string deviceId, string blockId, string address, byte[] data)
        {
            DeviceId = deviceId;
            BlockId = blockId;
            Address = address;
            Data = data != null ? (byte[])data.Clone() : new byte[0];
            Timestamp = DateTime.Now;
        }
    }

    public sealed class CommunicationDeviceStatusChangedEventArgs : EventArgs
    {
        public CommunicationDeviceStatusChangedEventArgs(CommunicationDeviceStatus status)
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }

        public CommunicationDeviceStatus Status { get; }
    }
}
