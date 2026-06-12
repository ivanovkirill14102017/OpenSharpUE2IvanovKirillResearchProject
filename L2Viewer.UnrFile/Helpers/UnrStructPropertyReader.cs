namespace L2Viewer.UnrFile;

internal static class UnrStructPropertyReader
{
    public static UnrPointRegion ReadPointRegionProperty(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeStruct || !tag.StructName.Is("PointRegion"))
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid PointRegion payload in '{tag.Name}'.");
        }

        var payload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
        var position = 0;
        UnrFileObjectReference? zoneReference = null;
        int? leafIndex = null;
        byte? zoneNumber = null;

        while (true)
        {
            if (!PackageReader.TryReadPropertyTag(payload, package.Names, ref position, out var rawNestedTag))
            {
                break;
            }

            var nestedTag = rawNestedTag.ToStreamPropertyTag();

            if (nestedTag.IsTerminator)
            {
                break;
            }

            if (nestedTag.Name.Is("Zone"))
            {
                zoneReference = ReadNestedObjectReference(package, payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                continue;
            }

            if (nestedTag.Name.Is("iLeaf"))
            {
                leafIndex = ReadNestedInt32(payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                continue;
            }

            if (nestedTag.Name.Is("ZoneNumber"))
            {
                zoneNumber = ReadNestedByte(payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                continue;
            }

            break;
        }

        return new UnrPointRegion
        {
            ZoneReference = zoneReference,
            LeafIndex = leafIndex,
            ZoneNumber = zoneNumber
        };
    }

    public static UnrTextureModifyInfo ReadTextureModifyInfoProperty(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeStruct || !tag.StructName.Is("TextureModifyinfo"))
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid TextureModifyinfo payload in '{tag.Name}'.");
        }

        var payload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
        var position = 0;
        bool? useModify = null;
        bool? twoSide = null;
        bool? alphaBlend = null;
        bool? dummy = null;
        UnrFileColor? color = null;
        int? alphaOp = null;
        int? colorOp = null;

        while (true)
        {
            if (!PackageReader.TryReadPropertyTag(payload, package.Names, ref position, out var rawNestedTag))
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid TextureModifyinfo property list in '{tag.Name}'.");
            }

            var nestedTag = rawNestedTag.ToStreamPropertyTag();

            if (nestedTag.IsTerminator)
            {
                break;
            }

            if (nestedTag.Name.Is("bUseModify"))
            {
                useModify = ReadNestedBool(nestedTag);
                continue;
            }

            if (nestedTag.Name.Is("bTwoSide"))
            {
                twoSide = ReadNestedBool(nestedTag);
                continue;
            }

            if (nestedTag.Name.Is("bAlphaBlend"))
            {
                alphaBlend = ReadNestedBool(nestedTag);
                continue;
            }

            if (nestedTag.Name.Is("bDummy"))
            {
                dummy = ReadNestedBool(nestedTag);
                continue;
            }

            if (nestedTag.Name.Is("Color"))
            {
                color = ReadNestedColor(payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                continue;
            }

            if (nestedTag.Name.Is("AlphaOp"))
            {
                alphaOp = ReadNestedInt32(payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                continue;
            }

            if (nestedTag.Name.Is("ColorOp"))
            {
                colorOp = ReadNestedInt32(payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                continue;
            }

            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has unsupported nested TextureModifyinfo property '{nestedTag.Name}' in '{tag.Name}'.");
        }

        return new UnrTextureModifyInfo
        {
            UseModify = useModify,
            TwoSide = twoSide,
            AlphaBlend = alphaBlend,
            Dummy = dummy,
            Color = color,
            AlphaOp = alphaOp,
            ColorOp = colorOp
        };
    }

    private static bool ReadNestedBool(StreamPropertyTag tag)
    {
        return tag.ThrowIfNotTagEncodedBool($"Invalid nested bool property '{tag.Name}'.");
    }

    private static byte ReadNestedByte(
        byte[] payload,
        ref int position,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        if (tag.Type != PackageReader.PropertyTypeByte || tag.DataSize != 1 || position + 1 > payload.Length)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid nested byte '{tag.Name}' in '{parentName}'.");
        }

        return payload[position++];
    }

    private static int ReadNestedInt32(
        byte[] payload,
        ref int position,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        if (tag.Type != PackageReader.PropertyTypeInt || tag.DataSize != 4 || position + 4 > payload.Length)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid nested int '{tag.Name}' in '{parentName}'.");
        }

        var value = BitConverter.ToInt32(payload, position);
        position += 4;
        return value;
    }

    private static UnrFileColor ReadNestedColor(
        byte[] payload,
        ref int position,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        if (tag.Type != PackageReader.PropertyTypeStruct || !tag.StructName.Is("Color") || tag.DataSize != 4 || position + 4 > payload.Length)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid nested color '{tag.Name}' in '{parentName}'.");
        }

        var value = new UnrFileColor
        {
            R = payload[position],
            G = payload[position + 1],
            B = payload[position + 2],
            A = payload[position + 3]
        };

        position += 4;
        return value;
    }

    private static UnrFileObjectReference? ReadNestedObjectReference(
        PackageData package,
        byte[] payload,
        ref int position,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        if (tag.Type != PackageReader.PropertyTypeObject)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid nested object reference '{tag.Name}' in '{parentName}'.");
        }

        var start = position;
        if (!PackageReader.TryReadCompactIndex(payload, ref position, out var rawReference))
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) hit EOF while reading nested object reference '{tag.Name}' in '{parentName}'.");
        }

        if (position - start != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid nested object reference size for '{tag.Name}' in '{parentName}'.");
        }

        if (rawReference == 0)
        {
            return null;
        }

        var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference);
        if (resolved is null)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) points to unresolved nested object reference {rawReference} in '{parentName}.{tag.Name}'.");
        }

        return rawReference < 0
            ? new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Import,
                ImportIndex = -rawReference - 1,
                ExportIndex = null,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            }
            : new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Export,
                ImportIndex = null,
                ExportIndex = rawReference - 1,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            };
    }
}
