using System;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationDeviceStatus
    {
        public CommunicationDeviceStatus(
            string deviceId,
            string driverName,
            string displayName,
            bool isEnabled,
            bool isConnected,
            CommunicationDeviceRuntimeState state,
            string lastError,
            DateTime lastChangedAt)
        {
            DeviceId = deviceId;
            DriverName = driverName;
            DisplayName = displayName;
            IsEnabled = isEnabled;
            IsConnected = isConnected;
            State = state;
            LastError = lastError;
            LastChangedAt = lastChangedAt;
        }

        public string DeviceId { get; }
        public string DriverName { get; }
        public string DisplayName { get; }
        public bool IsEnabled { get; }
        public bool IsConnected { get; }
        public CommunicationDeviceRuntimeState State { get; }
        public string LastError { get; }
        public DateTime LastChangedAt { get; }
    }
}
