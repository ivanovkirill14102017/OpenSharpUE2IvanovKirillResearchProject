using System.Buffers.Binary;
using System.Text;

namespace L2Viewer.PackageCore;

public sealed class PackageReadException : Exception
{
    public PackageReadException(string message) : base(message)
    {
    }
}

public static class PackageReader
{
    public const uint PackageSignature = 0x9E2A83C1;
    public const int PropertyTypeByte = 1;
    public const int PropertyTypeInt = 2;
    public const int PropertyTypeBool = 3;
    public const int PropertyTypeFloat = 4;
    public const int PropertyTypeObject = 5;
    public const int PropertyTypeName = 6;
    public const int PropertyTypeString = 7;
    public const int PropertyTypeClass = 8;
    public const int PropertyTypeArray = 9;
    public const int PropertyTypeStruct = 10;
    private static readonly byte[] Prefix111 = Encoding.Unicode.GetBytes("Lineage2Ver111");
    private static readonly byte[] Prefix121 = Encoding.Unicode.GetBytes("Lineage2Ver121");
    private static readonly byte[] Prefix413 = Encoding.Unicode.GetBytes("Lineage2Ver413");

    public static PackageData LoadPackage(string path)
    {
        using var stream = OpenPackageBodyStream(path, out var wrapper);
        using var reader = new BinaryReader(stream, PackageCompat.Latin1, leaveOpen: true);
        var header = ReadHeader(reader);
        var names = ReadNameTable(reader, header);
        var imports = ReadImportTable(reader, header);
        var exports = ReadExportTable(reader, header);
        return new PackageData(path, wrapper, header, names, imports, exports);
    }

    public static byte[] ReadExportBlob(PackageData package, ExportEntry exportEntry)
    {
        if (exportEntry.SerialOffset is null || exportEntry.SerialSize <= 0)
        {
            return Array.Empty<byte>();
        }

        using var reader = OpenExportReader(package, exportEntry);
        var bytes = reader.ReadBytes(exportEntry.SerialSize);
        if (bytes.Length != exportEntry.SerialSize)
        {
            throw new PackageReadException("Unexpected EOF while reading export payload.");
        }

        return bytes;
    }

    public static BinaryReader OpenExportReader(PackageData package, ExportEntry exportEntry)
    {
        if (exportEntry.SerialOffset is null || exportEntry.SerialSize < 0)
        {
            throw new PackageReadException("Export has no readable payload.");
        }

        var stream = OpenPackageBodyStream(package.Path, out _);
        var start = exportEntry.SerialOffset.Value;
        var end = checked(start + exportEntry.SerialSize);
        if (start < 0 || end > stream.Length)
        {
            stream.Dispose();
            throw new PackageReadException("Export payload points outside package body.");
        }

        stream.Position = start;
        return new BinaryReader(new BoundedReadStream(stream, exportEntry.SerialSize), PackageCompat.Latin1, leaveOpen: false);
    }

    public static string SafeName(IReadOnlyList<string> names, int index)
    {
        return index >= 0 && index < names.Count ? names[index] : $"<name:{index}>";
    }

    public static string ExportClassName(PackageData package, ExportEntry exportEntry)
    {
        var idx = exportEntry.ClassIndex;
        if (idx < 0)
        {
            var importIndex = -idx - 1;
            if (importIndex >= 0 && importIndex < package.Imports.Count)
            {
                return SafeName(package.Names, package.Imports[importIndex].ObjectName);
            }

            return $"<import:{importIndex}>";
        }

        if (idx > 0)
        {
            var exportIndex = idx - 1;
            if (exportIndex >= 0 && exportIndex < package.Exports.Count)
            {
                return SafeName(package.Names, package.Exports[exportIndex].ObjectName);
            }

            return $"<export:{exportIndex}>";
        }

        return "None";
    }

    public static int ReadCompactIndex(BinaryReader reader)
    {
        var b0 = ReadByteExact(reader);
        var negative = (b0 & 0x80) != 0;
        var value = b0 & 0x3F;

        if ((b0 & 0x40) != 0)
        {
            var b1 = ReadByteExact(reader);
            value |= (b1 & 0x7F) << 6;
            if ((b1 & 0x80) != 0)
            {
                var b2 = ReadByteExact(reader);
                value |= (b2 & 0x7F) << 13;
                if ((b2 & 0x80) != 0)
                {
                    var b3 = ReadByteExact(reader);
                    value |= (b3 & 0x7F) << 20;
                    if ((b3 & 0x80) != 0)
                    {
                        var b4 = ReadByteExact(reader);
                        value |= (b4 & 0x1F) << 27;
                    }
                }
            }
        }

        return negative ? -value : value;
    }

    public static bool TryReadCompactIndex(ReadOnlySpan<byte> buffer, ref int position, out int result)
    {
        result = 0;
        if (position >= buffer.Length)
        {
            return false;
        }

        var b0 = buffer[position++];
        var negative = (b0 & 0x80) != 0;
        var value = b0 & 0x3F;
        if ((b0 & 0x40) != 0)
        {
            if (position >= buffer.Length) return false;
            var b1 = buffer[position++];
            value |= (b1 & 0x7F) << 6;
            if ((b1 & 0x80) != 0)
            {
                if (position >= buffer.Length) return false;
                var b2 = buffer[position++];
                value |= (b2 & 0x7F) << 13;
                if ((b2 & 0x80) != 0)
                {
                    if (position >= buffer.Length) return false;
                    var b3 = buffer[position++];
                    value |= (b3 & 0x7F) << 20;
                    if ((b3 & 0x80) != 0)
                    {
                        if (position >= buffer.Length) return false;
                        var b4 = buffer[position++];
                        value |= (b4 & 0x1F) << 27;
                    }
                }
            }
        }

        result = negative ? -value : value;
        return true;
    }

    public static int ReadCompactIndex(ReadOnlySpan<byte> buffer, ref int position)
    {
        if (!TryReadCompactIndex(buffer, ref position, out var result))
        {
            throw new PackageReadException("Unexpected EOF while reading compact index.");
        }
        return result;
    }

    public static int DecodePropertySize(ReadOnlySpan<byte> buffer, ref int position, int sizeCode)
    {
        return sizeCode switch
        {
            0 => 1,
            1 => 2,
            2 => 4,
            3 => 12,
            4 => 16,
            5 => ReadByte(buffer, ref position),
            6 => ReadUInt16(buffer, ref position),
            7 => ReadInt32(buffer, ref position),
            _ => throw new PackageReadException($"Invalid property size code: {sizeCode}")
        };
    }

    public static bool TryReadPropertyTag(
        BinaryReader reader,
        IReadOnlyList<string> names,
        out UnrealPropertyTag tag)
    {
        tag = default;
        try
        {
            var nameIndex = ReadCompactIndex(reader);
            if (nameIndex < 0 || nameIndex >= names.Count)
            {
                return false;
            }

            var propertyName = names[nameIndex];
            if (string.Equals(propertyName, "None", StringComparison.OrdinalIgnoreCase))
            {
                tag = new UnrealPropertyTag(propertyName, 0, 0, 0, null, false);
                return true;
            }

            var info = reader.ReadByte();
            var isArray = (info & 0x80) != 0;
            var propertyType = info & 0x0F;
            var sizeCode = (info >> 4) & 0x07;

            string? structName = null;
            if (propertyType == PropertyTypeStruct)
            {
                var structNameIndex = ReadCompactIndex(reader);
                if (structNameIndex < 0 || structNameIndex >= names.Count)
                {
                    return false;
                }

                structName = names[structNameIndex];
            }

            int dataSize;
            switch (sizeCode)
            {
                case 0: dataSize = 1; break;
                case 1: dataSize = 2; break;
                case 2: dataSize = 4; break;
                case 3: dataSize = 12; break;
                case 4: dataSize = 16; break;
                case 5: dataSize = reader.ReadByte(); break;
                case 6: dataSize = reader.ReadUInt16(); break;
                case 7: dataSize = reader.ReadInt32(); break;
                default: return false;
            }

            var arrayIndex = 0;
            var boolValue = false;
            if (propertyType == PropertyTypeBool)
            {
                boolValue = isArray;
            }
            else if (isArray)
            {
                var b = reader.ReadByte();
                if (b < 128)
                {
                    arrayIndex = b;
                }
                else
                {
                    var b2 = reader.ReadByte();
                    if ((b & 0x40) != 0)
                    {
                        var b3 = reader.ReadByte();
                        var b4 = reader.ReadByte();
                        arrayIndex = ((b << 24) | (b2 << 16) | (b3 << 8) | b4) & 0x3FFFFF;
                    }
                    else
                    {
                        arrayIndex = ((b << 8) | b2) & 0x3FFF;
                    }
                }
            }

            tag = new UnrealPropertyTag(propertyName, propertyType, dataSize, arrayIndex, structName, boolValue);
            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
        catch (PackageReadException)
        {
            return false;
        }
    }

    public static bool TryReadPropertyTag(
        ReadOnlySpan<byte> buffer,
        IReadOnlyList<string> names,
        ref int position,
        out UnrealPropertyTag tag)
    {
        tag = default;
        try
        {
            if (!TryReadCompactIndex(buffer, ref position, out var nameIndex)) return false;
            if (nameIndex < 0 || nameIndex >= names.Count)
            {
                return false;
            }

            var propertyName = names[nameIndex];
            if (string.Equals(propertyName, "None", StringComparison.Ordinal))
            {
                tag = new UnrealPropertyTag(propertyName, 0, 0, 0, null, false);
                return true;
            }

            if (position >= buffer.Length) return false;
            var info = buffer[position++];
            var isArray = (info & 0x80) != 0;
            var propertyType = info & 0x0F;
            var sizeCode = (info >> 4) & 0x07;

            string? structName = null;
            if (propertyType == PropertyTypeStruct)
            {
                if (!TryReadCompactIndex(buffer, ref position, out var structNameIndex)) return false;
                if (structNameIndex < 0 || structNameIndex >= names.Count)
                {
                    return false;
                }

                structName = names[structNameIndex];
            }

            var dataSize = TryDecodePropertySize(buffer, ref position, sizeCode, out var decodedSize) ? decodedSize : -1;
            if (dataSize < 0) return false;

            var arrayIndex = 0;
            var boolValue = false;
            if (propertyType == PropertyTypeBool)
            {
                boolValue = isArray;
            }
            else if (isArray)
            {
                if (position >= buffer.Length) return false;
                arrayIndex = ReadPropertyArrayIndex(buffer, ref position);
            }

            tag = new UnrealPropertyTag(propertyName, propertyType, dataSize, arrayIndex, structName, boolValue);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDecodePropertySize(ReadOnlySpan<byte> buffer, ref int position, int sizeCode, out int result)
    {
        result = 0;
        switch (sizeCode)
        {
            case 0: result = 1; return true;
            case 1: result = 2; return true;
            case 2: result = 4; return true;
            case 3: result = 12; return true;
            case 4: result = 16; return true;
            case 5:
                if (position >= buffer.Length) return false;
                result = buffer[position++];
                return true;
            case 6:
                if (position + 1 >= buffer.Length) return false;
                result = BinaryPrimitives.ReadUInt16LittleEndian(buffer[position..]);
                position += 2;
                return true;
            case 7:
                if (position + 3 >= buffer.Length) return false;
                result = BinaryPrimitives.ReadInt32LittleEndian(buffer[position..]);
                position += 4;
                return true;
            default:
                return false;
        }
    }

    public static int? ParseTaggedPropertiesEnd(ReadOnlySpan<byte> buffer, IReadOnlyList<string> names, int start = 0, int maxProperties = 4096)
    {
        var position = start;
        for (var i = 0; i < maxProperties; i++)
        {
            if (!TryReadPropertyTag(buffer, names, ref position, out var tag))
            {
                return null;
            }

            if (tag.IsEnd)
            {
                return position;
            }

            if (position + tag.DataSize > buffer.Length)
            {
                return null;
            }

            position += tag.DataSize;
        }

        return null;
    }

    public static (byte[] Body, string Wrapper) UnwrapLineage2IfNeeded(byte[] raw)
    {
        if (raw.AsSpan().StartsWith(Prefix111))
        {
            var body = raw[Prefix111.Length..];
            var key = DetectXorKeyForPackage(body) ?? 0xAC;
            return (XorBuffer(body, key), $"Lineage2Ver111 (xor=0x{key:X2})");
        }

        if (raw.AsSpan().StartsWith(Prefix121))
        {
            var body = raw[Prefix121.Length..];
            var key = DetectXorKeyForPackage(body) ?? 0x50;
            return (XorBuffer(body, key), $"Lineage2Ver121 (xor=0x{key:X2})");
        }

        if (raw.AsSpan().StartsWith(Prefix413))
        {
            throw new PackageReadException("Lineage2Ver413 detected: decryptor is not implemented in this viewer.");
        }

        return (raw, "raw");
    }

    private static Stream OpenPackageBodyStream(string path, out string wrapper)
    {
        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            var bodySpec = ReadBodySpec(fileStream);
            wrapper = bodySpec.Wrapper;
            return new PackageBodyStream(fileStream, bodySpec.PrefixLength, bodySpec.XorKey);
        }
        catch
        {
            fileStream.Dispose();
            throw;
        }
    }

    private static PackageBodySpec ReadBodySpec(FileStream fileStream)
    {
        var prefixBuffer = new byte[Math.Max(Math.Max(Prefix111.Length, Prefix121.Length), Prefix413.Length)];
        fileStream.Position = 0;
        var prefixRead = fileStream.Read(prefixBuffer, 0, prefixBuffer.Length);
        var prefixSpan = prefixBuffer.AsSpan(0, prefixRead);

        if (prefixSpan.StartsWith(Prefix111))
        {
            return new PackageBodySpec(Prefix111.Length, DetectXorKeyForPackage(fileStream, Prefix111.Length) ?? 0xAC, "Lineage2Ver111");
        }

        if (prefixSpan.StartsWith(Prefix121))
        {
            return new PackageBodySpec(Prefix121.Length, DetectXorKeyForPackage(fileStream, Prefix121.Length) ?? 0x50, "Lineage2Ver121");
        }

        if (prefixSpan.StartsWith(Prefix413))
        {
            throw new PackageReadException("Lineage2Ver413 detected: decryptor is not implemented in this viewer.");
        }

        return new PackageBodySpec(0, null, "raw");
    }

    private static byte[] XorBuffer(byte[] data, int key)
    {
        var result = new byte[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key);
        }

        return result;
    }

    private static int? DetectXorKeyForPackage(byte[] data)
    {
        if (data.Length < 4)
        {
            return null;
        }

        Span<byte> sig = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(sig, PackageSignature);
        for (var key = 0; key < 256; key++)
        {
            if ((byte)(data[0] ^ key) == sig[0] &&
                (byte)(data[1] ^ key) == sig[1] &&
                (byte)(data[2] ^ key) == sig[2] &&
                (byte)(data[3] ^ key) == sig[3])
            {
                return key;
            }
        }

        return null;
    }

    private static int? DetectXorKeyForPackage(FileStream fileStream, int prefixLength)
    {
        Span<byte> encryptedHeader = stackalloc byte[4];
        fileStream.Position = prefixLength;
        var read = fileStream.Read(encryptedHeader);
        if (read < encryptedHeader.Length)
        {
            return null;
        }

        Span<byte> sig = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(sig, PackageSignature);
        for (var key = 0; key < 256; key++)
        {
            if ((byte)(encryptedHeader[0] ^ key) == sig[0] &&
                (byte)(encryptedHeader[1] ^ key) == sig[1] &&
                (byte)(encryptedHeader[2] ^ key) == sig[2] &&
                (byte)(encryptedHeader[3] ^ key) == sig[3])
            {
                return key;
            }
        }

        return null;
    }

    private static PackageHeader ReadHeader(BinaryReader reader)
    {
        var signature = reader.ReadUInt32();
        if (signature != PackageSignature)
        {
            throw new PackageReadException($"Unexpected package signature 0x{signature:X8}.");
        }

        return new PackageHeader(
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32());
    }

    private static IReadOnlyList<string> ReadNameTable(BinaryReader reader, PackageHeader header)
    {
        reader.BaseStream.Position = header.NameOffset;
        var names = new List<string>((int)header.NameCount);
        for (var i = 0; i < header.NameCount; i++)
        {
            names.Add(ReadUnrealString(reader));
            _ = reader.ReadUInt32();
        }

        return names;
    }

    private static IReadOnlyList<ImportEntry> ReadImportTable(BinaryReader reader, PackageHeader header)
    {
        reader.BaseStream.Position = header.ImportOffset;
        var items = new List<ImportEntry>((int)header.ImportCount);
        for (var i = 0; i < header.ImportCount; i++)
        {
            items.Add(new ImportEntry(
                ReadCompactIndex(reader),
                ReadCompactIndex(reader),
                reader.ReadUInt32(),
                ReadCompactIndex(reader)));
        }

        return items;
    }

    private static IReadOnlyList<ExportEntry> ReadExportTable(BinaryReader reader, PackageHeader header)
    {
        reader.BaseStream.Position = header.ExportOffset;
        var items = new List<ExportEntry>((int)header.ExportCount);
        for (var i = 0; i < header.ExportCount; i++)
        {
            var classIndex = ReadCompactIndex(reader);
            var superIndex = ReadCompactIndex(reader);
            var packageIndex = reader.ReadUInt32();
            var objectName = ReadCompactIndex(reader);
            var objectFlags = reader.ReadUInt32();
            var serialSize = ReadCompactIndex(reader);
            int? serialOffset = serialSize > 0 ? ReadCompactIndex(reader) : null;
            items.Add(new ExportEntry(classIndex, superIndex, packageIndex, objectName, objectFlags, serialSize, serialOffset));
        }

        return items;
    }

    private static string ReadUnrealString(BinaryReader reader)
    {
        var length = ReadCompactIndex(reader);
        if (length == 0)
        {
            return string.Empty;
        }

        if (length > 0)
        {
            var raw = reader.ReadBytes(length);
            if (raw.Length != length)
            {
                throw new PackageReadException("Unexpected EOF while reading ANSI Unreal string.");
            }

            if (raw.Length > 0 && raw[^1] == 0)
            {
                raw = raw[..^1];
            }

            return PackageCompat.Latin1.GetString(raw);
        }

        var charCount = -length;
        var rawUtf16 = reader.ReadBytes(charCount * 2);
        if (rawUtf16.Length != charCount * 2)
        {
            throw new PackageReadException("Unexpected EOF while reading UTF-16 Unreal string.");
        }

        if (rawUtf16.Length >= 2 && rawUtf16[^1] == 0 && rawUtf16[^2] == 0)
        {
            rawUtf16 = rawUtf16[..^2];
        }

        return Encoding.Unicode.GetString(rawUtf16);
    }

    private static byte ReadByteExact(BinaryReader reader)
    {
        var value = reader.BaseStream.ReadByte();
        if (value < 0)
        {
            throw new PackageReadException("Unexpected EOF.");
        }

        return (byte)value;
    }

    private static void EnsureRemaining(ReadOnlySpan<byte> buffer, int position, int count)
    {
        if (position + count > buffer.Length)
        {
            throw new PackageReadException("Unexpected EOF.");
        }
    }

    private static int ReadPropertyArrayIndex(ReadOnlySpan<byte> buffer, ref int position)
    {
        var b = ReadByte(buffer, ref position);
        if (b < 128)
        {
            return b;
        }

        var b2 = ReadByte(buffer, ref position);
        if ((b & 0x40) != 0)
        {
            var b3 = ReadByte(buffer, ref position);
            var b4 = ReadByte(buffer, ref position);
            return ((b << 24) | (b2 << 16) | (b3 << 8) | b4) & 0x3FFFFF;
        }

        return ((b << 8) | b2) & 0x3FFF;
    }

    private static byte ReadByte(ReadOnlySpan<byte> buffer, ref int position)
    {
        EnsureRemaining(buffer, position, 1);
        return buffer[position++];
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> buffer, ref int position)
    {
        EnsureRemaining(buffer, position, 2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(buffer[position..]);
        position += 2;
        return value;
    }

    private static int ReadInt32(ReadOnlySpan<byte> buffer, ref int position)
    {
        EnsureRemaining(buffer, position, 4);
        var value = BinaryPrimitives.ReadInt32LittleEndian(buffer[position..]);
        position += 4;
        return value;
    }

    private sealed record PackageBodySpec(int PrefixLength, int? XorKey, string Wrapper);

    private sealed class PackageBodyStream : Stream
    {
        private readonly FileStream _fileStream;
        private readonly int _prefixLength;
        private readonly int? _xorKey;

        public PackageBodyStream(FileStream fileStream, int prefixLength, int? xorKey)
        {
            _fileStream = fileStream;
            _prefixLength = prefixLength;
            _xorKey = xorKey;
            _fileStream.Position = prefixLength;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _fileStream.Length - _prefixLength;

        public override long Position
        {
            get => _fileStream.Position - _prefixLength;
            set => _fileStream.Position = checked(value + _prefixLength);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _fileStream.Read(buffer, offset, count);
            if (_xorKey is int xorKey)
            {
                for (var i = 0; i < read; i++)
                {
                    buffer[offset + i] = (byte)(buffer[offset + i] ^ xorKey);
                }
            }

            return read;
        }

        public override int ReadByte()
        {
            var value = _fileStream.ReadByte();
            if (value < 0)
            {
                return -1;
            }

            return _xorKey is int xorKey ? value ^ xorKey : value;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
            };

            Position = target;
            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class BoundedReadStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _startPosition;
        private readonly long _length;

        public BoundedReadStream(Stream innerStream, long length)
        {
            _innerStream = innerStream;
            _startPosition = innerStream.Position;
            _length = length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _innerStream.Position - _startPosition;
            set
            {
                if (value < 0 || value > _length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _innerStream.Position = _startPosition + value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _length - Position;
            if (remaining <= 0)
            {
                return 0;
            }

            return _innerStream.Read(buffer, offset, (int)Math.Min(count, remaining));
        }

        public override int ReadByte()
        {
            return Position >= _length ? -1 : _innerStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
            };

            Position = target;
            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
