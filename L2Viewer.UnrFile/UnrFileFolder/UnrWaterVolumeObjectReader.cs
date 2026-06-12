namespace L2Viewer.UnrFile;

internal static class UnrWaterVolumeObjectReader
{
    public static UnrWaterVolumeObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        UnrFileObjectReference? nextPhysicsVolumeReference = null;
        string? locationName = null;
        UnrFileScale? mainScale = null;
        UnrFileScale? postScale = null;
        UnrFileScale? tempScale = null;
        var dynamicActorFilterState = false;
        var lightChanged = false;
        var deleteMe = false;
        var hiddenEd = false;
        var pendingDelete = false;
        var selected = false;
        UnrFileObjectReference? levelReference = null;
        UnrPointRegion? region = null;
        string? tagName = null;
        string? groupName = null;
        var sunAffect = false;
        UnrFileObjectReference[] touchingReferences = [];
        UnrFileObjectReference? baseReference = null;
        UnrFileObjectReference? ownerReference = null;
        UnrFileObjectReference? meshReference = null;
        UnrFileObjectReference? textureReference = null;
        UnrFileObjectReference? physicsVolumeReference = null;
        Vector3? location = null;
        Vector3? rotation = null;
        var drawScale = 1f;
        var drawScale3D = Vector3.One;
        UnrFileObjectReference? brushReference = null;
        Vector3 prePivot = Vector3.Zero;
        Vector3? colLocation = null;
        UnrFileColor? distanceFogColor = null;
        UnrFileColor? cellophaneColor = null;
        var useDistanceFogColor = false;
        var useCellophane = false;
        var ignoredRange = false;
        UnrTextureModifyInfo? texModifyInfo = null;

        var unknownProperties = new List<UnrFileUnknownProperty>();

        void ReadProperty(StreamPropertyTag tag)
        {
            if (!Enum.TryParse<UnrWaterVolumePropertyKind>(tag.Name, ignoreCase: true, out var kind))
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
                case UnrWaterVolumePropertyKind.NextPhysicsVolume:
                    nextPhysicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.LocationName:
                    locationName = ReadCompactNameProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bLightChanged:
                    lightChanged = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bDeleteMe:
                    deleteMe = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bHiddenEd:
                    hiddenEd = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bSunAffect:
                    sunAffect = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Touching:
                    touchingReferences = ReadTouchingReferences(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Base:
                    baseReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Owner:
                    ownerReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Mesh:
                    meshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Texture:
                    textureReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrWaterVolumePropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrWaterVolumePropertyKind.Brush:
                    brushReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? Vector3.Zero;
                    return;
                case UnrWaterVolumePropertyKind.bPendingDelete:
                    pendingDelete = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.ColLocation:
                    colLocation = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.DistanceFogColor:
                    distanceFogColor = reader.ReadColorStructProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.CellophaneColor:
                    cellophaneColor = reader.ReadColorStructProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bUseDistanceFogColor:
                    useDistanceFogColor = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bUseCellophane:
                    useCellophane = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bSelected:
                    selected = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.bIgnoredRange:
                    ignoredRange = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrWaterVolumePropertyKind.TexModifyInfo:
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
        return new UnrWaterVolumeObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            NextPhysicsVolumeReference = nextPhysicsVolumeReference,
            LocationName = locationName,
            MainScale = mainScale,
            PostScale = postScale,
            TempScale = tempScale,
            DynamicActorFilterState = dynamicActorFilterState,
            LightChanged = lightChanged,
            DeleteMe = deleteMe,
            HiddenEd = hiddenEd,
            PendingDelete = pendingDelete,
            Selected = selected,
            LevelReference = levelReference,
            Region = region,
            Tag = tagName,
            Group = groupName,
            SunAffect = sunAffect,
            TouchingReferences = touchingReferences,
            BaseReference = baseReference,
            OwnerReference = ownerReference,
            MeshReference = meshReference,
            TextureReference = textureReference,
            PhysicsVolumeReference = physicsVolumeReference,
            Location = location,
            Rotation = rotation,
            DrawScale = drawScale,
            DrawScale3D = drawScale3D,
            BrushReference = brushReference,
            PrePivot = prePivot,
            ColLocation = colLocation,
            DistanceFogColor = distanceFogColor,
            CellophaneColor = cellophaneColor,
            UseDistanceFogColor = useDistanceFogColor,
            UseCellophane = useCellophane,
            IgnoredRange = ignoredRange,
            TexModifyInfo = texModifyInfo,
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
            var start = reader.BaseStream.Position;
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

            if (reader.BaseStream.Position <= start)
            {
                throw new PackageReadException($"{className} export {exportIndex} ({objectName}) did not advance while reading Touching.");
            }
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

    private static string? ReadCompactNameProperty(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        var start = reader.BaseStream.Position;
        var nameIndex = PackageReader.ReadCompactIndex(reader);
        var consumed = reader.BaseStream.Position - start;
        if (consumed != tag.DataSize)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has invalid compact name payload in '{tag.Name}'.");
        }

        return nameIndex >= 0 && nameIndex < package.Names.Count
            ? package.Names[nameIndex]
            : null;
    }

    private enum UnrWaterVolumePropertyKind
    {
        NextPhysicsVolume,
        LocationName,
        MainScale,
        PostScale,
        TempScale,
        bDynamicActorFilterState,
        bLightChanged,
        bDeleteMe,
        bHiddenEd,
        Level,
        Region,
        Tag,
        Group,
        bSunAffect,
        Touching,
        Base,
        Owner,
        Mesh,
        Texture,
        PhysicsVolume,
        Location,
        Rotation,
        DrawScale,
        DrawScale3D,
        Brush,
        PrePivot,
        bPendingDelete,
        ColLocation,
        DistanceFogColor,
        CellophaneColor,
        bUseDistanceFogColor,
        bUseCellophane,
        bSelected,
        bIgnoredRange,
        TexModifyInfo
    }
}
