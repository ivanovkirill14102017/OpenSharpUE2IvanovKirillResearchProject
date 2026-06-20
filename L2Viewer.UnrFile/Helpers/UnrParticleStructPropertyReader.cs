namespace L2Viewer.UnrFile;

internal static class UnrParticleStructPropertyReader
{
    public static IReadOnlyList<UnrParticleColorScale> ReadParticleColorScaleProperties(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        var payload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
        if (tag.Type == PackageReader.PropertyTypeArray)
        {
            var position = 0;
            if (!PackageReader.TryReadCompactIndex(payload, ref position, out var count) || count < 0)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid particle color scale array payload in '{tag.Name}'.");
            }

            var result = new List<UnrParticleColorScale>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(ReadParticleColorScaleEntry(
                    package,
                    payload,
                    ref position,
                    tag.ArrayIndex + i,
                    tag.Type,
                    tag.StructName,
                    className,
                    exportIndex,
                    objectName,
                    tag.Name));
            }

            return result;
        }

        if (tag.Type == PackageReader.PropertyTypeStruct)
        {
            var position = 0;
            return
            [
                ReadParticleColorScaleEntry(
                    package,
                    payload,
                    ref position,
                    tag.ArrayIndex,
                    tag.Type,
                    tag.StructName,
                    className,
                    exportIndex,
                    objectName,
                    tag.Name)
            ];
        }

        return
        [
            new UnrParticleColorScale
            {
                ArrayIndex = tag.ArrayIndex,
                Type = tag.Type,
                StructName = tag.StructName,
                RawHex = UnrCompat.ToHexString(payload)
            }
        ];
    }

    public static IReadOnlyList<UnrParticleSizeScale> ReadParticleSizeScaleProperties(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        var payload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
        if (tag.Type == PackageReader.PropertyTypeArray)
        {
            var position = 0;
            if (!PackageReader.TryReadCompactIndex(payload, ref position, out var count) || count < 0)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid particle size scale array payload in '{tag.Name}'.");
            }

            var result = new List<UnrParticleSizeScale>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(ReadParticleSizeScaleEntry(
                    package,
                    payload,
                    ref position,
                    tag.ArrayIndex + i,
                    tag.Type,
                    tag.StructName,
                    className,
                    exportIndex,
                    objectName,
                    tag.Name));
            }

            return result;
        }

        if (tag.Type == PackageReader.PropertyTypeStruct)
        {
            var position = 0;
            return
            [
                ReadParticleSizeScaleEntry(
                    package,
                    payload,
                    ref position,
                    tag.ArrayIndex,
                    tag.Type,
                    tag.StructName,
                    className,
                    exportIndex,
                    objectName,
                    tag.Name)
            ];
        }

        return
        [
            new UnrParticleSizeScale
            {
                ArrayIndex = tag.ArrayIndex,
                Type = tag.Type,
                StructName = tag.StructName,
                RawHex = UnrCompat.ToHexString(payload)
            }
        ];
    }

    private static UnrParticleColorScale ReadParticleColorScaleEntry(
        PackageData package,
        byte[] payload,
        ref int position,
        int arrayIndex,
        int type,
        string? structName,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        float? relativeTime = null;
        UnrFileColor? color = null;
        var entryStart = position;

        while (true)
        {
            if (!PackageReader.TryReadPropertyTag(payload, package.Names, ref position, out var rawNestedTag))
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid particle color scale property list in '{parentName}[{arrayIndex}]'.");
            }

            var nestedTag = rawNestedTag.ToStreamPropertyTag();
            if (nestedTag.IsTerminator)
            {
                break;
            }

            if (nestedTag.Name.Is("RelativeTime"))
            {
                relativeTime = UnrStructPropertyReader.ReadNestedFloat(payload, ref position, nestedTag, className, exportIndex, objectName, parentName);
                continue;
            }

            if (nestedTag.Name.Is("Color"))
            {
                color = UnrStructPropertyReader.ReadNestedColor(payload, ref position, nestedTag, className, exportIndex, objectName, parentName);
                continue;
            }

            UnrStructPropertyReader.SkipNestedPayload(payload, ref position, nestedTag, className, exportIndex, objectName, parentName);
        }

        return new UnrParticleColorScale
        {
            ArrayIndex = arrayIndex,
            Type = type,
            StructName = structName,
            RelativeTime = relativeTime,
            Color = color,
            RawHex = position > entryStart ? UnrCompat.ToHexString(payload[entryStart..position]) : null
        };
    }

    private static UnrParticleSizeScale ReadParticleSizeScaleEntry(
        PackageData package,
        byte[] payload,
        ref int position,
        int arrayIndex,
        int type,
        string? structName,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        float? relativeTime = null;
        float? relativeSize = null;
        var entryStart = position;

        while (true)
        {
            if (!PackageReader.TryReadPropertyTag(payload, package.Names, ref position, out var rawNestedTag))
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid particle size scale property list in '{parentName}[{arrayIndex}]'.");
            }

            var nestedTag = rawNestedTag.ToStreamPropertyTag();
            if (nestedTag.IsTerminator)
            {
                break;
            }

            if (nestedTag.Name.Is("RelativeTime"))
            {
                relativeTime = UnrStructPropertyReader.ReadNestedFloat(payload, ref position, nestedTag, className, exportIndex, objectName, parentName);
                continue;
            }

            if (nestedTag.Name.Is("RelativeSize"))
            {
                relativeSize = UnrStructPropertyReader.ReadNestedFloat(payload, ref position, nestedTag, className, exportIndex, objectName, parentName);
                continue;
            }

            UnrStructPropertyReader.SkipNestedPayload(payload, ref position, nestedTag, className, exportIndex, objectName, parentName);
        }

        return new UnrParticleSizeScale
        {
            ArrayIndex = arrayIndex,
            Type = type,
            StructName = structName,
            RelativeTime = relativeTime,
            RelativeSize = relativeSize,
            RawHex = position > entryStart ? UnrCompat.ToHexString(payload[entryStart..position]) : null
        };
    }
}
