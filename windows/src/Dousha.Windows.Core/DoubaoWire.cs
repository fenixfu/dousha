using System.Buffers.Binary;
using System.Text;

namespace Dousha.Windows.Core;

internal static class DoubaoWire
{
    internal sealed record Field(byte[]? Bytes, ulong Varint);

    internal sealed class Writer
    {
        private readonly List<byte> _buffer = [];

        public void String(int field, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Bytes(field, Encoding.UTF8.GetBytes(value));
        }

        public void Bytes(int field, byte[] value)
        {
            if (value.Length == 0)
            {
                return;
            }

            Varint((ulong)((field << 3) | 2));
            Varint((ulong)value.Length);
            _buffer.AddRange(value);
        }

        public void Int32(int field, int value)
        {
            if (value == 0)
            {
                return;
            }

            Varint((ulong)(field << 3));
            Varint((uint)value);
        }

        public byte[] ToArray()
        {
            return [.. _buffer];
        }

        private void Varint(ulong value)
        {
            while (value >= 0x80)
            {
                _buffer.Add((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            _buffer.Add((byte)value);
        }
    }

    internal static Dictionary<int, Field> DecodeFields(ReadOnlySpan<byte> data)
    {
        var fields = new Dictionary<int, Field>();
        var index = 0;
        while (index < data.Length)
        {
            var tag = ReadVarint(data, ref index);
            var field = (int)(tag >> 3);
            var wireType = (int)(tag & 0x7);
            if (wireType == 0)
            {
                fields[field] = new Field(null, ReadVarint(data, ref index));
            }
            else if (wireType == 2)
            {
                var length = checked((int)ReadVarint(data, ref index));
                fields[field] = new Field(data.Slice(index, length).ToArray(), 0);
                index += length;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported Doubao wire type {wireType}.");
            }
        }

        return fields;
    }

    private static ulong ReadVarint(ReadOnlySpan<byte> data, ref int index)
    {
        ulong result = 0;
        var shift = 0;
        while (index < data.Length)
        {
            var current = data[index++];
            result |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }

        throw new InvalidOperationException("Truncated Doubao varint.");
    }
}
