namespace L2Viewer.UnrFile;

internal static class UnrMovableStaticMeshActorObjectReader
{
    public static UnrMovableStaticMeshActorObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        byte[]? l2AccelRatioPayload = null;
        var useL2RotatorMaxRandom = false;
        var useL2RotatorRandomStart = false;
        Vector3? l2RotatorRate = null;
        Vector3? l2RotatorMax = null;
        UnrFileObjectReference? staticMeshReference = null;
        var dynamicActorFilterState = false;
        UnrFileObjectReference? levelReference = null;
        UnrPointRegion? region = null;
        string? tagName = null;
        string? groupName = null;
        var sunAffect = false;
        UnrFileObjectReference[] touchingReferences = [];
        UnrFileObjectReference? physicsVolumeReference = null;
        Vector3? location = null;
        Vector3? rotation = null;
        var drawScale = 1f;
        Vector3? swayRotationOrig = null;
        var collideActors = false;
        var blockActors = false;
        var blockPlayers = false;
        var blockZeroExtentTraces = false;
        var blockNonZeroExtentTraces = false;
        var blockKarma = false;
        Vector3? colLocation = null;
        UnrTextureModifyInfo? texModifyInfo = null;

        var unknownProperties = new List<UnrFileUnknownProperty>();

        void ReadProperty(StreamPropertyTag tag)
        {
            if (!Enum.TryParse<UnrMovableStaticMeshActorPropertyKind>(tag.Name, ignoreCase: true, out var kind))
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
                return;
            }

            switch (kind)
            {
                case UnrMovableStaticMeshActorPropertyKind.L2AccelRatio:
                    l2AccelRatioPayload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bUseL2RotatorMaxRandom:
                    useL2RotatorMaxRandom = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bUseL2RotatorRandomStart:
                    useL2RotatorRandomStart = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.L2RotatorRate:
                    l2RotatorRate = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.L2RotatorMax:
                    l2RotatorMax = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bSunAffect:
                    sunAffect = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.Touching:
                    touchingReferences = ReadTouchingReferences(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bCollideActors:
                    collideActors = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bBlockActors:
                    blockActors = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bBlockPlayers:
                    blockPlayers = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bBlockZeroExtentTraces:
                    blockZeroExtentTraces = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bBlockNonZeroExtentTraces:
                    blockNonZeroExtentTraces = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.bBlockKarma:
                    blockKarma = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.SwayRotationOrig:
                    swayRotationOrig = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.ColLocation:
                    colLocation = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMovableStaticMeshActorPropertyKind.TexModifyInfo:
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
        return new UnrMovableStaticMeshActorObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            StaticMeshReference = staticMeshReference,
            DynamicActorFilterState = dynamicActorFilterState,
            LevelReference = levelReference,
            Tag = tagName,
            Group = groupName,
            PhysicsVolumeReference = physicsVolumeReference,
            Location = location,
            Rotation = rotation,
            DrawScale = drawScale,
            SunAffect = sunAffect,
            TouchingReferences = touchingReferences,
            Region = region,
            CollideActors = collideActors,
            BlockActors = blockActors,
            BlockPlayers = blockPlayers,
            BlockZeroExtentTraces = blockZeroExtentTraces,
            BlockNonZeroExtentTraces = blockNonZeroExtentTraces,
            BlockKarma = blockKarma,
            SwayRotationOrig = swayRotationOrig,
            ColLocation = colLocation,
            TexModifyInfo = texModifyInfo,
            L2RotatorRate = l2RotatorRate,
            L2RotatorMax = l2RotatorMax,
            UseL2RotatorMaxRandom = useL2RotatorMaxRandom,
            UseL2RotatorRandomStart = useL2RotatorRandomStart,
            L2AccelRatioPayload = l2AccelRatioPayload,
            UnknownProperties = unknownProperties.ToArray()
        };
    }

    private static UnrFileObjectReference[] ReadTouchingReferences(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeArray || tag.DataSize < 0)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has invalid Touching payload.");
        }

        var end = reader.BaseStream.Position + tag.DataSize;
        var values = new List<UnrFileObjectReference>();
        while (reader.BaseStream.Position < end)
        {
            var rawReference = PackageReader.ReadCompactIndex(reader);
            if (rawReference == 0)
            {
                continue;
            }

            var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference)
                ?? throw new PackageReadException($"{className} export {exportIndex} ({objectName}) points to unresolved Touching reference {rawReference}.");

            values.Add(rawReference < 0
                ? new UnrFileObjectReference
                {
                    RawReference = rawReference,
                    Kind = UnrFileReferenceKind.Import,
                    ImportIndex = -rawReference - 1,
                    ExportIndex = null,
                    ClassName = resolved.ClassName,
                    ObjectName = resolved.ObjectName,
                    PackageName = resolved.PackageName
                }
                : new UnrFileObjectReference
                {
                    RawReference = rawReference,
                    Kind = UnrFileReferenceKind.Export,
                    ImportIndex = null,
                    ExportIndex = rawReference - 1,
                    ClassName = resolved.ClassName,
                    ObjectName = resolved.ObjectName,
                    PackageName = resolved.PackageName
                });
        }

        if (reader.BaseStream.Position != end)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has malformed Touching payload.");
        }

        return values.ToArray();
    }

    private static bool ReadStrictBool(StreamPropertyTag tag, string className, int exportIndex, string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeBool || tag.DataSize != 0)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has invalid bool payload in '{tag.Name}'.");
        }

        return tag.BoolValue;
    }

    private enum UnrMovableStaticMeshActorPropertyKind
    {
        L2AccelRatio,
        bUseL2RotatorMaxRandom,
        bUseL2RotatorRandomStart,
        L2RotatorRate,
        L2RotatorMax,
        StaticMesh,
        bDynamicActorFilterState,
        Level,
        Region,
        Tag,
        Group,
        bSunAffect,
        Touching,
        PhysicsVolume,
        Location,
        Rotation,
        DrawScale,
        bCollideActors,
        bBlockActors,
        bBlockPlayers,
        bBlockZeroExtentTraces,
        bBlockNonZeroExtentTraces,
        bBlockKarma,
        SwayRotationOrig,
        ColLocation,
        TexModifyInfo
    }
}
