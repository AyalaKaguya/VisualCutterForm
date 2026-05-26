namespace VisualMaster.Communication.Api
{
    public enum CommunicationBlockDataType
    {
        Bytes,
        AsciiString,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Single,
        Double,
    }

    public enum CommunicationByteOrder
    {
        BigEndian,
        LittleEndian,
    }

    public enum CommunicationUpdateMode
    {
        Passive,
        Polling,
    }

    public enum CommunicationMatchOperator
    {
        Equals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        ChangedTo,
        ChangedFrom,
        LengthAtLeast,
        Contains,
        RisingEdge,
        FallingEdge,
    }

    public enum CommunicationInputSourceKind
    {
        CommunicationBlock,
    }

    public enum CommunicationInputPayloadKind
    {
        Bytes,
        Text,
        Json,
    }

    public enum CommunicationInputMatchMode
    {
        AllConditions,
        AnyCondition,
    }

    public enum CommunicationOutputSegmentKind
    {
        Constant,
        Variable,
    }

    public enum CommunicationCrcMethod
    {
        None,
        Sum8,
        Xor8,
        ModbusCrc16,
        Crc16Ccitt,
    }

    public enum ByteDisplayMode
    {
        Short,
        Hex,
    }

    public enum CommunicationDeviceRuntimeState
    {
        Disabled,
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
        Faulted,
    }
}
