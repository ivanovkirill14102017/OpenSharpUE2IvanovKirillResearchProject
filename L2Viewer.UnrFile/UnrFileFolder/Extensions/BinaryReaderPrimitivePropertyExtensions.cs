namespace L2Viewer.UnrFile;

internal static class BinaryReaderPrimitivePropertyExtensions
{
    internal static Vector3? ReadVectorProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type == PackageReader.PropertyTypeArray && tag.DataSize == 12)
        {
            return reader.ReadVector3();
        }

        if (tag.Type == PackageReader.PropertyTypeStruct)
        {
            if (tag.DataSize == 12)
            {
                return reader.ReadVector3();
            }

            if (tag.DataSize == 13)
            {
                var prefix = reader.ReadByte();
                if (prefix != 0)
                {
                    throw new PackageReadException(
                        $"{className} export {exportIndex} ({objectName}) has invalid vector struct prefix in '{tag.Name}'.");
                }

                return reader.ReadVector3();
            }
        }

        throw new PackageReadException(
            $"{className} export {exportIndex} ({objectName}) has invalid vector payload in '{tag.Name}'.");
    }

    internal static Vector3? ReadRotatorProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type == PackageReader.PropertyTypeArray && tag.DataSize == 12)
        {
            return reader.ReadRotator3Int32();
        }

        if (tag.Type == PackageReader.PropertyTypeStruct)
        {
            if (tag.DataSize == 12)
            {
                return reader.ReadRotator3Int32();
            }

            if (tag.DataSize == 13)
            {
                var prefix = reader.ReadByte();
                if (prefix != 0)
                {
                    throw new PackageReadException(
                        $"{className} export {exportIndex} ({objectName}) has invalid rotator struct prefix in '{tag.Name}'.");
                }

                return reader.ReadRotator3Int32();
            }
        }

        throw new PackageReadException(
            $"{className} export {exportIndex} ({objectName}) has invalid rotator payload in '{tag.Name}'.");
    }

    internal static float? ReadFloatProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeFloat || tag.DataSize != 4)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid float payload in '{tag.Name}'.");
        }

        return reader.ReadSingle();
    }

    internal static int ReadIntProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeInt || tag.DataSize != 4)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid int payload in '{tag.Name}'.");
        }

        return reader.ReadInt32();
    }

    internal static byte ReadByteProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeByte || tag.DataSize != 1)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid byte payload in '{tag.Name}'.");
        }

        return reader.ReadByte();
    }

    internal static bool ReadIntBooleanProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        var value = reader.ReadIntProperty(tag, className, exportIndex, objectName);
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has non-boolean int payload in '{tag.Name}': value={value}.")
        };
    }

    internal static UnrFileColor ReadColorStructProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeStruct || !tag.StructName.Is("Color") || tag.DataSize != 4)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid color payload in '{tag.Name}': type={tag.Type}, size={tag.DataSize}, struct={tag.StructName ?? "<null>"}.");
        }

        return new UnrFileColor
        {
            R = reader.ReadByte(),
            G = reader.ReadByte(),
            B = reader.ReadByte(),
            A = reader.ReadByte()
        };
    }

    internal static string? ReadNameProperty(
        this BinaryReader reader,
        PackageData package,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeName)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid name payload type in '{tag.Name}'.");
        }

        var start = reader.BaseStream.Position;
        var nameIndex = PackageReader.ReadCompactIndex(reader);
        var consumed = reader.BaseStream.Position - start;
        if (consumed != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid name payload size in '{tag.Name}': consumed={consumed}, expected={tag.DataSize}.");
        }

        return nameIndex >= 0 && nameIndex < package.Names.Count
            ? package.Names[nameIndex]
            : null;
    }

    internal static string ReadStringProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.DataSize < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid string payload size in '{tag.Name}'.");
        }

        var start = reader.BaseStream.Position;
        var length = PackageReader.ReadCompactIndex(reader);
        if (length == 0)
        {
            return string.Empty;
        }

        string value;
        if (length > 0)
        {
            var raw = reader.ReadBytes(length);
            if (raw.Length != length)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) hit EOF while reading string payload in '{tag.Name}'.");
            }

            if (raw.Length > 0 && raw[^1] == 0)
            {
                raw = raw[..^1];
            }

            value = PackageCompat.Latin1.GetString(raw);
        }
        else
        {
            var charCount = -length;
            var rawUtf16 = reader.ReadBytes(charCount * 2);
            if (rawUtf16.Length != charCount * 2)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) hit EOF while reading UTF-16 string payload in '{tag.Name}'.");
            }

            if (rawUtf16.Length >= 2 && rawUtf16[^1] == 0 && rawUtf16[^2] == 0)
            {
                rawUtf16 = rawUtf16[..^2];
            }

            value = Encoding.Unicode.GetString(rawUtf16);
        }

        var consumed = reader.BaseStream.Position - start;
        if (consumed != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid string payload size in '{tag.Name}': consumed={consumed}, expected={tag.DataSize}.");
        }

        return value;
    }

    internal static byte[] SkipPropertyPayload(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        IReadOnlyList<string>? nameTable = null)
    {
        if (tag.DataSize < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid payload size in '{tag.Name}'.");
        }

        var startPos = reader.BaseStream.Position;

        // L2 struct properties serialized via tagged properties often have completely incorrect DataSize.
        // We must manually traverse them until we hit the None terminator.
        if (tag.Type == PackageReader.PropertyTypeStruct && tag.StructName != "Vector" && tag.StructName != "Rotator" && tag.StructName != "Color" && tag.StructName != "Scale" && tag.StructName != "Plane" && tag.StructName != "Quat" && nameTable != null)
        {
            // A tagged struct payload optionally starts with the StructName index if it's not predefined, 
            // but for generic skipping, wait! The StructNameIndex is part of the PropertyTag, not the payload!
            // Wait, no. For StructProperty, the StructName is read inside TryReadPropertyTag.
            // So the stream here is exactly at the first nested property tag.
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var nestedTagStart = reader.BaseStream.Position;
                var nestedTag = reader.ReadPropertyTag(nameTable, className, exportIndex, objectName);
                if (nestedTag.IsTerminator)
                {
                    break;
                }
                reader.SkipPropertyPayload(nestedTag, className, exportIndex, objectName, nameTable);
            }

            var endPos = reader.BaseStream.Position;
            var actualSize = (int)(endPos - startPos);
            reader.BaseStream.Position = startPos;
            var actualBytes = reader.ReadBytes(actualSize);
            return actualBytes;
        }

        var bytes = reader.ReadBytes(tag.DataSize);
        if (bytes.Length != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) hit EOF while reading '{tag.Name}'.");
        }

        return bytes;
    }

    internal static Vector3 ReadVector3(this BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    internal static Vector3 ReadRotator3Int32(this BinaryReader reader)
    {
        return new Vector3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
    }
}
