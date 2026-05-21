using System;
using System.Globalization;
using System.Linq;
using System.Text;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Core
{
    public static class CommunicationDataConverter
    {
        public static string ToHex(byte[] data)
        {
            return data == null ? "" : string.Join(" ", data.Select(b => b.ToString("X2")));
        }

        public static byte[] FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return new byte[0];
            var compact = new string(hex.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
            if (compact.Length % 2 != 0)
                compact = "0" + compact;

            var result = new byte[compact.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber);
            return result;
        }

        public static object Decode(byte[] data, CommunicationBlockDataType type, CommunicationByteOrder order)
        {
            data = data ?? new byte[0];
            var bytes = NormalizeOrder(data, order);

            switch (type)
            {
                case CommunicationBlockDataType.AsciiString: return Encoding.ASCII.GetString(data);
                case CommunicationBlockDataType.Int16: return bytes.Length >= 2 ? BitConverter.ToInt16(bytes, 0) : (short)0;
                case CommunicationBlockDataType.UInt16: return bytes.Length >= 2 ? BitConverter.ToUInt16(bytes, 0) : (ushort)0;
                case CommunicationBlockDataType.Int32: return bytes.Length >= 4 ? BitConverter.ToInt32(bytes, 0) : 0;
                case CommunicationBlockDataType.UInt32: return bytes.Length >= 4 ? BitConverter.ToUInt32(bytes, 0) : 0u;
                case CommunicationBlockDataType.Single: return bytes.Length >= 4 ? BitConverter.ToSingle(bytes, 0) : 0f;
                case CommunicationBlockDataType.Double: return bytes.Length >= 8 ? BitConverter.ToDouble(bytes, 0) : 0d;
                default: return data;
            }
        }

        public static byte[] Encode(string value, CommunicationBlockDataType type, CommunicationByteOrder order)
        {
            byte[] bytes;
            switch (type)
            {
                case CommunicationBlockDataType.AsciiString:
                    bytes = Encoding.ASCII.GetBytes(value ?? "");
                    break;
                case CommunicationBlockDataType.Int16:
                    bytes = BitConverter.GetBytes(short.Parse(value ?? "0", CultureInfo.InvariantCulture));
                    break;
                case CommunicationBlockDataType.UInt16:
                    bytes = BitConverter.GetBytes(ushort.Parse(value ?? "0", CultureInfo.InvariantCulture));
                    break;
                case CommunicationBlockDataType.Int32:
                    bytes = BitConverter.GetBytes(int.Parse(value ?? "0", CultureInfo.InvariantCulture));
                    break;
                case CommunicationBlockDataType.UInt32:
                    bytes = BitConverter.GetBytes(uint.Parse(value ?? "0", CultureInfo.InvariantCulture));
                    break;
                case CommunicationBlockDataType.Single:
                    bytes = BitConverter.GetBytes(float.Parse(value ?? "0", CultureInfo.InvariantCulture));
                    break;
                case CommunicationBlockDataType.Double:
                    bytes = BitConverter.GetBytes(double.Parse(value ?? "0", CultureInfo.InvariantCulture));
                    break;
                default:
                    bytes = FromHex(value);
                    break;
            }

            return NormalizeOrder(bytes, order);
        }

        private static byte[] NormalizeOrder(byte[] data, CommunicationByteOrder order)
        {
            var bytes = data != null ? (byte[])data.Clone() : new byte[0];
            bool needReverse = (order == CommunicationByteOrder.BigEndian && BitConverter.IsLittleEndian)
                || (order == CommunicationByteOrder.LittleEndian && !BitConverter.IsLittleEndian);
            if (needReverse && bytes.Length > 1)
                Array.Reverse(bytes);
            return bytes;
        }
    }
}
