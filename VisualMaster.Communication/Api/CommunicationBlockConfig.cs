using System;

namespace VisualMaster.Communication.Api
{
    public sealed class CommunicationBlockConfig
    {
        public string BlockId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string BlockName { get; set; }
        public string Address { get; set; }
        public CommunicationBlockDataType DataType { get; set; } = CommunicationBlockDataType.Bytes;
        public byte[] InitialValue { get; set; }
        public bool PollingEnabled { get; set; }
        public int PollingIntervalMs { get; set; } = 500;
        public int PollingTimeoutMs { get; set; } = 1000;

        public CommunicationBlockConfig Clone()
        {
            return new CommunicationBlockConfig
            {
                BlockId = BlockId,
                Name = Name,
                BlockName = BlockName,
                Address = Address,
                DataType = DataType,
                InitialValue = InitialValue != null ? (byte[])InitialValue.Clone() : null,
                PollingEnabled = PollingEnabled,
                PollingIntervalMs = PollingIntervalMs,
                PollingTimeoutMs = PollingTimeoutMs,
            };
        }
    }
}
