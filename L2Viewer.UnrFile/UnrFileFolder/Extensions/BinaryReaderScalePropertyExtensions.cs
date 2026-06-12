namespace L2Viewer.UnrFile;

internal static class BinaryReaderScalePropertyExtensions
{
    internal static UnrFileScale ReadScaleProperty(
        this BinaryReader reader,
        PackageData package,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeStruct)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid scale payload type in '{tag.Name}': type={tag.Type}, size={tag.DataSize}, struct={tag.StructName ?? "<null>"}.");
        }

        var remaining = tag.DataSize;
        if (remaining == 18)
        {
            var prefix = reader.ReadByte();
            if (prefix != 0)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid scale struct prefix in '{tag.Name}': prefix={prefix}, size={tag.DataSize}, struct={tag.StructName ?? "<null>"}.");
            }

            remaining--;
        }

        if (remaining is 17 or 24 or 25)
        {
            var scale = new UnrFileScale
            {
                Scale = reader.ReadVector3(),
                SheerRate = reader.ReadSingle(),
                SheerAxis = reader.ReadByte()
            };

            remaining -= 17;
            if (remaining > 0)
            {
                var tail = reader.ReadBytes(remaining);
                if (tail.Length != remaining)
                {
                    throw new PackageReadException(
                        $"{className} export {exportIndex} ({objectName}) hit EOF while reading scale tail in '{tag.Name}'.");
                }
            }

            return scale;
        }

        return reader.ReadNestedScaleProperty(package, tag, className, exportIndex, objectName, remaining);
    }

    private static UnrFileScale ReadNestedScaleProperty(
        this BinaryReader reader,
        PackageData package,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        int remaining)
    {
        var payloadStart = reader.BaseStream.Position;
        Vector3? scaleVector = null;
        float? sheerRate = null;
        byte? sheerAxis = null;
        var hasTerminatingNone = false;

        while (reader.BaseStream.Position - payloadStart < remaining)
        {
            var nestedTag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (nestedTag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            var nestedPayloadStart = reader.BaseStream.Position;
            switch (nestedTag.Name)
            {
                case "Scale":
                    scaleVector = reader.ReadVectorProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "SheerRate":
                    sheerRate = reader.ReadFloatProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "SheerAxis":
                    sheerAxis = reader.ReadByteProperty(nestedTag, className, exportIndex, objectName);
                    break;
                default:
                    throw new PackageReadException(
                        $"{className} export {exportIndex} ({objectName}) has unsupported nested Scale property '{nestedTag.Name}' in '{tag.Name}'.");
            }

            var nestedConsumed = reader.BaseStream.Position - nestedPayloadStart;
            nestedTag.EnsurePropertyFullyConsumed(nestedConsumed, className, exportIndex, objectName);
        }

        var consumed = checked((int)(reader.BaseStream.Position - payloadStart));
        
        if (!hasTerminatingNone)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has no terminating None property in nested Scale.");
        }

        tag.EnsurePropertyFullyConsumed(consumed, className, exportIndex, objectName);

        return new UnrFileScale
        {
            Scale = scaleVector ?? Vector3.One,
            SheerRate = sheerRate ?? 0f,
            SheerAxis = sheerAxis ?? 0
        };
    }
}
