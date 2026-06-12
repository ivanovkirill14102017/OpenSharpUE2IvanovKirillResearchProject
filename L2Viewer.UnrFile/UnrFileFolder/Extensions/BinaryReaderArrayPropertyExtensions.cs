namespace L2Viewer.UnrFile;

internal static class BinaryReaderArrayPropertyExtensions
{
    internal static uint[] ReadUInt32ArrayProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeArray)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid array payload type in '{tag.Name}'.");
        }

        var payloadStart = reader.BaseStream.Position;
        var count = PackageReader.ReadCompactIndex(reader);
        if (count < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid negative array count in '{tag.Name}'.");
        }

        var prefixBytes = checked((int)(reader.BaseStream.Position - payloadStart));
        var remainingBytes = checked(tag.DataSize - prefixBytes);
        if (remainingBytes < 0 || remainingBytes != checked(count * 4))
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid uint32 array payload in '{tag.Name}': count={count}, remaining={remainingBytes}, size={tag.DataSize}.");
        }

        var values = new uint[count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.ReadUInt32();
        }

        return values;
    }

    internal static float[] ReadFloatArrayProperty(
        this BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeArray || tag.DataSize < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid float array payload in '{tag.Name}'.");
        }

        var payloadStart = reader.BaseStream.Position;
        var count = PackageReader.ReadCompactIndex(reader);
        if (count < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid negative float array count in '{tag.Name}'.");
        }

        var prefixBytes = checked((int)(reader.BaseStream.Position - payloadStart));
        var remainingBytes = checked(tag.DataSize - prefixBytes);
        if (remainingBytes < 0 || remainingBytes != checked(count * 4))
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid float array payload in '{tag.Name}': count={count}, remaining={remainingBytes}, size={tag.DataSize}.");
        }

        var values = new float[count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.ReadSingle();
        }

        return values;
    }
}
