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

    public static UnrFloatRange ReadRangeProperty(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeStruct || !tag.StructName.Is("Range") || tag.DataSize < 0)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has invalid Range payload in '{tag.Name}'.");
        }

        var end = reader.BaseStream.Position + tag.DataSize;
        float? min = null;
        float? max = null;
        while (reader.BaseStream.Position < end)
        {
            var nestedTag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (nestedTag.IsTerminator)
            {
                break;
            }

            switch (nestedTag.Name)
            {
                case "Min":
                    min = reader.ReadFloatProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "Max":
                    max = reader.ReadFloatProperty(nestedTag, className, exportIndex, objectName);
                    break;
                default:
                    _ = reader.SkipPropertyPayload(nestedTag, className, exportIndex, objectName, package.Names);
                    break;
            }
        }

        reader.BaseStream.Position = end;
        return new UnrFloatRange
        {
            Min = min ?? 0f,
            Max = max ?? 0f
        };
    }

    public static UnrRangeVector ReadRangeVectorProperty(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeStruct || !tag.StructName.Is("RangeVector") || tag.DataSize < 0)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has invalid RangeVector payload in '{tag.Name}'.");
        }

        var end = reader.BaseStream.Position + tag.DataSize;
        UnrFloatRange? x = null;
        UnrFloatRange? y = null;
        UnrFloatRange? z = null;
        while (reader.BaseStream.Position < end)
        {
            var nestedTag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (nestedTag.IsTerminator)
            {
                break;
            }

            switch (nestedTag.Name)
            {
                case "X":
                    x = ReadRangeProperty(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                case "Y":
                    y = ReadRangeProperty(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                case "Z":
                    z = ReadRangeProperty(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                default:
                    _ = reader.SkipPropertyPayload(nestedTag, className, exportIndex, objectName, package.Names);
                    break;
            }
        }

        reader.BaseStream.Position = end;
        return new UnrRangeVector
        {
            X = x ?? new UnrFloatRange { Min = 0f, Max = 0f },
            Y = y ?? new UnrFloatRange { Min = 0f, Max = 0f },
            Z = z ?? new UnrFloatRange { Min = 0f, Max = 0f }
        };
    }

    internal static bool ReadNestedBool(StreamPropertyTag tag)
    {
        return tag.ThrowIfNotTagEncodedBool($"Invalid nested bool property '{tag.Name}'.");
    }

    internal static byte ReadNestedByte(
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

    internal static int ReadNestedInt32(
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

    internal static float ReadNestedFloat(
        byte[] payload,
        ref int position,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        if (tag.Type != PackageReader.PropertyTypeFloat || tag.DataSize != 4 || position + 4 > payload.Length)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid nested float '{tag.Name}' in '{parentName}'.");
        }

        var value = BitConverter.ToSingle(payload, position);
        position += 4;
        return value;
    }

    internal static UnrFileColor ReadNestedColor(
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

    internal static UnrFileObjectReference? ReadNestedObjectReference(
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

    internal static void SkipNestedPayload(
        byte[] payload,
        ref int position,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        string parentName)
    {
        if (tag.Type == PackageReader.PropertyTypeBool)
        {
            return;
        }

        if (tag.DataSize < 0 || position + tag.DataSize > payload.Length)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid nested payload '{tag.Name}' in '{parentName}'.");
        }

        position += tag.DataSize;
    }
}
