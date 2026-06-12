namespace L2Viewer.UnrFile;

internal static class BinaryReaderPropertyTagExtensions
{
    internal static void ReadStateFrameIfPresent(this BinaryReader reader, uint objectFlags)
    {
        if ((objectFlags & UnrPackageHelpers.ObjectFlagHasStack) == 0)
        {
            return;
        }

        var node = PackageReader.ReadCompactIndex(reader);
        _ = PackageReader.ReadCompactIndex(reader);
        _ = reader.ReadInt64();
        _ = reader.ReadInt32();

        if (node != 0)
        {
            _ = PackageReader.ReadCompactIndex(reader);
        }
    }

    internal static StreamPropertyTag ReadPropertyTag(
        this BinaryReader reader,
        IReadOnlyList<string> names,
        string className,
        int exportIndex,
        string objectName)
    {
        var nameIndex = PackageReader.ReadCompactIndex(reader);
        if (nameIndex < 0 || nameIndex >= names.Count)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid property name index {nameIndex}.");
        }

        var propertyName = names[nameIndex];
        if (propertyName == "None")
        {
            return new StreamPropertyTag(propertyName, 0, 0, 0, null, false, true);
        }

        var info = reader.ReadByte();
        var isArray = (info & 0x80) != 0;
        var propertyType = info & 0x0F;
        var sizeCode = (info >> 4) & 0x07;

        string? structName = null;
        if (propertyType == PackageReader.PropertyTypeStruct)
        {
            var structNameIndex = PackageReader.ReadCompactIndex(reader);
            if (structNameIndex < 0 || structNameIndex >= names.Count)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid struct name index {structNameIndex} in '{propertyName}'.");
            }

            structName = names[structNameIndex];
        }

        var dataSize = reader.ReadPropertySize(sizeCode);
        var arrayIndex = 0;
        var boolValue = false;
        if (propertyType == PackageReader.PropertyTypeBool)
        {
            boolValue = isArray;
        }
        else if (isArray)
        {
            arrayIndex = reader.ReadPropertyArrayIndex();
        }

        return new StreamPropertyTag(propertyName, propertyType, dataSize, arrayIndex, structName, boolValue, false);
    }

    internal static int ReadPropertySize(this BinaryReader reader, int sizeCode)
    {
        return sizeCode switch
        {
            0 => 1,
            1 => 2,
            2 => 4,
            3 => 12,
            4 => 16,
            5 => reader.ReadByte(),
            6 => reader.ReadUInt16(),
            7 => reader.ReadInt32(),
            _ => throw new PackageReadException($"Invalid property size code: {sizeCode}")
        };
    }

    internal static int ReadPropertyArrayIndex(this BinaryReader reader)
    {
        var b = reader.ReadByte();
        if (b < 128)
        {
            return b;
        }

        var b2 = reader.ReadByte();
        if ((b & 0x40) != 0)
        {
            var b3 = reader.ReadByte();
            var b4 = reader.ReadByte();
            return ((b << 24) | (b2 << 16) | (b3 << 8) | b4) & 0x3FFFFF;
        }

        return ((b << 8) | b2) & 0x3FFF;
    }
}
