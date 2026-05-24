using System.Collections.Generic;
using System.Linq;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Core
{
    public sealed class CommunicationOutputBuilder
    {
        public byte[] Build(CommunicationOutputEventConfig config, IDictionary<string, string> variables)
        {
            var result = new List<byte>();
            if (config == null) return result.ToArray();

            var protocol = config.ProtocolAssembly ?? new CommunicationProtocolAssemblyConfig();
            if (!string.IsNullOrWhiteSpace(protocol.HeaderHex))
                result.AddRange(CommunicationDataConverter.FromHex(protocol.HeaderHex));

            foreach (var segment in config.Segments ?? Enumerable.Empty<CommunicationOutputSegment>())
            {
                string value = segment.Value ?? "";
                var dataType = segment.DataType;
                var byteOrder = segment.ByteOrder;

                if (segment.Kind == CommunicationOutputSegmentKind.Variable && variables != null)
                {
                    variables.TryGetValue(value, out value);
                    var variable = config.Variables?.FirstOrDefault(v => v.Name == segment.Value);
                    if (variable != null)
                    {
                        dataType = variable.DataType;
                        byteOrder = variable.ByteOrder;
                    }
                }

                result.AddRange(CommunicationDataConverter.Encode(value, dataType, byteOrder));
            }

            if (protocol.CrcEnabled && protocol.CrcMethod != CommunicationCrcMethod.None)
                result.AddRange(BuildCrc(protocol, result.ToArray()));

            return result.ToArray();
        }

        private static byte[] BuildCrc(CommunicationProtocolAssemblyConfig protocol, byte[] frame)
        {
            var data = frame ?? new byte[0];
            if (!protocol.CrcIncludesHeader && !string.IsNullOrWhiteSpace(protocol.HeaderHex))
            {
                var headerLength = CommunicationDataConverter.FromHex(protocol.HeaderHex).Length;
                data = data.Skip(headerLength).ToArray();
            }

            switch (protocol.CrcMethod)
            {
                case CommunicationCrcMethod.Sum8:
                    return new[] { unchecked((byte)data.Sum(b => b)) };
                case CommunicationCrcMethod.Xor8:
                    byte xor = 0;
                    foreach (var b in data) xor ^= b;
                    return new[] { xor };
                case CommunicationCrcMethod.ModbusCrc16:
                    return EncodeUInt16(CalculateModbusCrc16(data), protocol.CrcByteOrder);
                case CommunicationCrcMethod.Crc16Ccitt:
                    return EncodeUInt16(CalculateCrc16Ccitt(data), protocol.CrcByteOrder);
                default:
                    return new byte[0];
            }
        }

        private static byte[] EncodeUInt16(ushort value, CommunicationByteOrder order)
        {
            return CommunicationDataConverter.Encode(value.ToString(), CommunicationBlockDataType.UInt16, order);
        }

        private static ushort CalculateModbusCrc16(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (var b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                    crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1);
            }
            return crc;
        }

        private static ushort CalculateCrc16Ccitt(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (var b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                    crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
            }
            return crc;
        }
    }
}
