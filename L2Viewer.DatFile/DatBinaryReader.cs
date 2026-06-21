using System.Text;

namespace L2Viewer.DatFile;

internal sealed class DatBinaryReader
{
    private readonly byte[] _buffer;
    private int _offset;

    public DatBinaryReader(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public int ReadInt32()
    {
        EnsureAvailable(sizeof(int));
        var value = BitConverter.ToInt32(_buffer, _offset);
        _offset += sizeof(int);
        return value;
    }

    public uint ReadUInt32()
    {
        EnsureAvailable(sizeof(uint));
        var value = BitConverter.ToUInt32(_buffer, _offset);
        _offset += sizeof(uint);
        return value;
    }

    public float ReadSingle()
    {
        EnsureAvailable(sizeof(float));
        var value = BitConverter.ToSingle(_buffer, _offset);
        _offset += sizeof(float);
        return value;
    }

    public string ReadUnicodeString32()
    {
        var length = ReadInt32();
        if (length < 0)
        {
            throw new InvalidDataException($"Negative UNICODE length at offset {_offset - sizeof(int)}.");
        }

        EnsureAvailable(length);
        var value = Encoding.Unicode.GetString(_buffer, _offset, length);
        _offset += length;
        return TrimTerminalNull(value);
    }

    public string ReadAsciiString()
    {
        EnsureAvailable(1);
        var first = _buffer[_offset++];
        var isUnicode = (first & 0x80) != 0;
        var length = first & 0x3F;
        var shift = 6;

        if ((first & 0x40) != 0)
        {
            while (true)
            {
                EnsureAvailable(1);
                var next = _buffer[_offset++];
                length |= (next & 0x7F) << shift;
                if ((next & 0x80) == 0)
                {
                    break;
                }

                shift += 7;
            }
        }

        if (length < 0)
        {
            throw new InvalidDataException("Negative ASCF length is not valid.");
        }

        if (!isUnicode)
        {
            EnsureAvailable(length);
            var value = Encoding.GetEncoding(28591).GetString(_buffer, _offset, length);
            _offset += length;
            return TrimTerminalNull(value);
        }

        var byteLength = checked(length * sizeof(char));
        EnsureAvailable(byteLength);
        var unicodeValue = Encoding.Unicode.GetString(_buffer, _offset, byteLength);
        _offset += byteLength;
        return TrimTerminalNull(unicodeValue);
    }

    public void EnsureFullyConsumedOrSafePackage()
    {
        if (TryConsumeSafePackage())
        {
            return;
        }

        if (_offset != _buffer.Length)
        {
            throw new InvalidDataException($"Decoded DAT buffer still has {_buffer.Length - _offset} unread bytes.");
        }
    }

    private void EnsureAvailable(int length)
    {
        if (_offset + length > _buffer.Length)
        {
            throw new EndOfStreamException($"Unexpected end of DAT buffer at offset {_offset}.");
        }
    }

    private bool TryConsumeSafePackage()
    {
        var remaining = _buffer.Length - _offset;
        if (remaining != 13)
        {
            return false;
        }

        if (_buffer[_offset] != 0x0C)
        {
            return false;
        }

        const string marker = "SafePackage";
        for (var i = 0; i < marker.Length; i++)
        {
            if (_buffer[_offset + 1 + i] != marker[i])
            {
                return false;
            }
        }

        if (_buffer[_offset + 12] != 0)
        {
            return false;
        }

        _offset = _buffer.Length;
        return true;
    }

    private static string TrimTerminalNull(string value)
    {
        return value.EndsWith('\0')
            ? value[..^1]
            : value;
    }
}
