namespace L2Viewer.UnrFile;

internal static class UnrL2FogInfoObjectReader
{
    public static UnrL2FogInfoObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        UnrFloatRange? affectRange = null;
        UnrFloatRange? fogRange1 = null;
        UnrFloatRange? fogRange2 = null;
        UnrFloatRange? fogRange3 = null;
        UnrFloatRange? fogRange4 = null;
        UnrFloatRange? fogRange5 = null;
        byte[] colorsPayload = [];
        var dynamicActorFilterState = false;
        UnrFileObjectReference? levelReference = null;
        UnrPointRegion? region = null;
        string? tagName = null;
        var sunAffect = false;
        string? groupName = null;
        UnrFileObjectReference? physicsVolumeReference = null;
        Vector3? location = null;
        Vector3? rotation = null;
        var drawScale = 1f;
        Vector3? swayRotationOrig = null;
        UnrTextureModifyInfo? texModifyInfo = null;
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void AddUnknownProperty(StreamPropertyTag tag)
        {
            unknownProperties.Add(new UnrFileUnknownProperty
            {
                Order = unknownProperties.Count,
                Name = tag.Name,
                Type = tag.Type,
                DataSize = tag.DataSize,
                StructName = tag.StructName,
                BoolValue = tag.BoolValue,
                RawHex = UnrCompat.ToHexString(reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names))
            });
        }

        void ReadProperty(StreamPropertyTag tag)
        {
            if (!Enum.TryParse<UnrL2FogInfoPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrL2FogInfoPropertyKind.AffectRange:
                    affectRange = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.FogRange1:
                    fogRange1 = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.FogRange2:
                    fogRange2 = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.FogRange3:
                    fogRange3 = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.FogRange4:
                    fogRange4 = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.FogRange5:
                    fogRange5 = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.Colors:
                    colorsPayload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
                    return;
                case UnrL2FogInfoPropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.bSunAffect:
                    sunAffect = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrL2FogInfoPropertyKind.SwayRotationOrig:
                    swayRotationOrig = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrL2FogInfoPropertyKind.TexModifyInfo:
                    texModifyInfo = UnrStructPropertyReader.ReadTextureModifyInfoProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
            }
        }

        reader.ReadStateFrameIfPresent(export.ObjectFlags);
        var hasTerminatingNone = false;
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (tag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            var payloadStart = reader.BaseStream.Position;
            ReadProperty(tag);
            tag.EnsurePropertyFullyConsumed(reader.BaseStream.Position - payloadStart, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has no terminating None property before EOF.");
        }

        reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        return new UnrL2FogInfoObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            AffectRange = affectRange,
            FogRange1 = fogRange1,
            FogRange2 = fogRange2,
            FogRange3 = fogRange3,
            FogRange4 = fogRange4,
            FogRange5 = fogRange5,
            ColorsPayload = colorsPayload,
            DynamicActorFilterState = dynamicActorFilterState,
            LevelReference = levelReference,
            Region = region,
            Tag = tagName,
            SunAffect = sunAffect,
            Group = groupName,
            PhysicsVolumeReference = physicsVolumeReference,
            Location = location,
            Rotation = rotation,
            DrawScale = drawScale,
            SwayRotationOrig = swayRotationOrig,
            TexModifyInfo = texModifyInfo,
            UnknownProperties = unknownProperties.ToArray()
        };
    }

    private static bool ReadStrictBool(StreamPropertyTag tag, string className, int exportIndex, string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeBool || tag.DataSize != 0)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has invalid bool payload in '{tag.Name}'.");
        }

        return tag.BoolValue;
    }

    private static UnrFloatRange ReadRangeStruct(
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

    private enum UnrL2FogInfoPropertyKind
    {
        AffectRange,
        FogRange1,
        FogRange2,
        FogRange3,
        FogRange4,
        FogRange5,
        Colors,
        bDynamicActorFilterState,
        Level,
        Region,
        Tag,
        bSunAffect,
        Group,
        PhysicsVolume,
        Location,
        Rotation,
        DrawScale,
        SwayRotationOrig,
        TexModifyInfo
    }
}
