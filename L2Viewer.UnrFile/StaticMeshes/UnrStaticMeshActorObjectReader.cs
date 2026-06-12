namespace L2Viewer.UnrFile;

internal static class UnrStaticMeshActorObjectReader
{
    public static UnrStaticMeshActorObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        UnrFileObjectReference? stepSound1Reference = null;
        UnrFileObjectReference? stepSound2Reference = null;
        UnrFileObjectReference? stepSound3Reference = null;
        int? l2CurrentLod = null;
        float? l2LodViewDuration = null;
        int? l2ServerObjectRealId = null;
        int? l2ServerObjectId = null;
        byte? l2ServerObjectType = null;
        byte? physics = null;
        byte? style = null;
        UnrFileObjectReference? staticMeshReference = null;
        UnrFileScale? mainScale = null;
        UnrFileScale? postScale = null;
        UnrFileScale? tempScale = null;
        var dynamicActorFilterState = false;
        var lightChanged = false;
        var updateShadow = false;
        var staticActor = false;
        var stasis = false;
        var fixedRotationDir = false;
        Vector3? rotationRate = null;
        var deleteMe = false;
        var pendingDelete = false;
        var selected = false;
        var hiddenEd = false;
        var staticLighting = false;
        var disableSorting = false;
        var agitDefaultStaticMesh = false;
        int? agitId = null;
        int? accessoryIndex = null;
        UnrAccessoryTypeEntry[] accessoryTypeList = [];
        UnrFileObjectReference[] skins = [];
        UnrFileObjectReference? levelReference = null;
        UnrPointRegion? region = null;
        string? forcedRegionTag = null;
        int? forcedRegion = null;
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
        var prePivot = Vector3.Zero;
        UnrFileObjectReference? staticMeshInstanceReference = null;
        Vector3? swayRotationOrig = null;
        float? collisionRadius = null;
        var collideActors = false;
        var blockActors = false;
        var blockPlayers = false;
        var blockZeroExtentTraces = false;
        var blockNonZeroExtentTraces = false;
        var blockKarma = false;
        var unlit = false;
        var shadowCast = false;
        var ignoredRange = false;
        Vector3? colLocation = null;
        UnrTextureModifyInfo? texModifyInfo = null;

        var unknownProperties = new List<UnrFileUnknownProperty>();

        void ReadProperty(StreamPropertyTag tag)
        {
            if (!Enum.TryParse<UnrStaticMeshActorPropertyKind>(tag.Name, ignoreCase: true, out var kind))
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
                case UnrStaticMeshActorPropertyKind.StepSound_1:
                    stepSound1Reference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.StepSound_2:
                    stepSound2Reference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.StepSound_3:
                    stepSound3Reference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.L2CurrentLod:
                    l2CurrentLod = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.L2LodViewDuration:
                    l2LodViewDuration = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.L2ServerObjectRealID:
                    l2ServerObjectRealId = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.L2ServerObjectID:
                    l2ServerObjectId = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.L2ServerObjectType:
                    l2ServerObjectType = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Physics:
                    physics = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Style:
                    style = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bLightChanged:
                    lightChanged = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bUpdateShadow:
                    updateShadow = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bStatic:
                    staticActor = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bStasis:
                    stasis = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bFixedRotationDir:
                    fixedRotationDir = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.RotationRate:
                    rotationRate = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bDeleteMe:
                    deleteMe = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bSelected:
                    selected = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bPendingDelete:
                    pendingDelete = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bHiddenEd:
                    hiddenEd = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bStaticLighting:
                    staticLighting = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bDisableSorting:
                    disableSorting = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bAgitDefaultStaticMesh:
                    agitDefaultStaticMesh = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.AgitID:
                    agitId = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.AccessoryIndex:
                    accessoryIndex = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.AccessoryTypeList:
                    accessoryTypeList = ReadAccessoryTypeList(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Skins:
                    skins = reader.ReadObjectReferenceArrayProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.ForcedRegionTag:
                    forcedRegionTag = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.ForcedRegion:
                    forcedRegion = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bSunAffect:
                    sunAffect = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Touching:
                    touchingReferences = ReadTouchingReferences(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Base:
                    baseReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Owner:
                    ownerReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Mesh:
                    meshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Texture:
                    textureReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrStaticMeshActorPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrStaticMeshActorPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrStaticMeshActorPropertyKind.StaticMeshInstance:
                    staticMeshInstanceReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.SwayRotationOrig:
                    swayRotationOrig = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.CollisionRadius:
                    collisionRadius = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bCollideActors:
                    collideActors = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bBlockActors:
                    blockActors = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bBlockPlayers:
                    blockPlayers = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bBlockZeroExtentTraces:
                    blockZeroExtentTraces = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bBlockNonZeroExtentTraces:
                    blockNonZeroExtentTraces = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bBlockKarma:
                    blockKarma = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bUnlit:
                    unlit = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bShadowCast:
                    shadowCast = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.bIgnoredRange:
                    ignoredRange = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.ColLocation:
                    colLocation = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrStaticMeshActorPropertyKind.TexModifyInfo:
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
            var consumed = reader.BaseStream.Position - payloadStart;
            tag.EnsurePropertyFullyConsumed(consumed, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has no terminating None property before EOF.");
        }

        reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        return new UnrStaticMeshActorObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            StepSound1Reference = stepSound1Reference,
            StepSound2Reference = stepSound2Reference,
            StepSound3Reference = stepSound3Reference,
            L2CurrentLod = l2CurrentLod,
            L2LodViewDuration = l2LodViewDuration,
            L2ServerObjectRealId = l2ServerObjectRealId,
            L2ServerObjectId = l2ServerObjectId,
            L2ServerObjectType = l2ServerObjectType,
            Physics = physics,
            Style = style,
            StaticMeshReference = staticMeshReference,
            MainScale = mainScale,
            PostScale = postScale,
            TempScale = tempScale,
            DynamicActorFilterState = dynamicActorFilterState,
            LightChanged = lightChanged,
            UpdateShadow = updateShadow,
            StaticActor = staticActor,
            Stasis = stasis,
            FixedRotationDir = fixedRotationDir,
            RotationRate = rotationRate,
            DeleteMe = deleteMe,
            PendingDelete = pendingDelete,
            Selected = selected,
            HiddenEd = hiddenEd,
            StaticLighting = staticLighting,
            DisableSorting = disableSorting,
            AgitDefaultStaticMesh = agitDefaultStaticMesh,
            AgitId = agitId,
            AccessoryIndex = accessoryIndex,
            AccessoryTypeList = accessoryTypeList,
            Skins = skins,
            LevelReference = levelReference,
            Region = region,
            ForcedRegionTag = forcedRegionTag,
            ForcedRegion = forcedRegion,
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
            PrePivot = prePivot,
            StaticMeshInstanceReference = staticMeshInstanceReference,
            SwayRotationOrig = swayRotationOrig,
            CollisionRadius = collisionRadius,
            CollideActors = collideActors,
            BlockActors = blockActors,
            BlockPlayers = blockPlayers,
            BlockZeroExtentTraces = blockZeroExtentTraces,
            BlockNonZeroExtentTraces = blockNonZeroExtentTraces,
            BlockKarma = blockKarma,
            Unlit = unlit,
            ShadowCast = shadowCast,
            IgnoredRange = ignoredRange,
            ColLocation = colLocation,
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

    private static UnrAccessoryTypeEntry[] ReadAccessoryTypeList(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeArray || tag.DataSize < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid AccessoryTypeList payload.");
        }

        var payload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
        var position = 0;
        if (!PackageReader.TryReadCompactIndex(payload, ref position, out var count) || count < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid AccessoryTypeList count.");
        }

        var values = new UnrAccessoryTypeEntry[count];
        for (var i = 0; i < count; i++)
        {
            int? depth = null;
            UnrFileObjectReference? meshReference = null;
            var hasTerminator = false;

            while (position < payload.Length)
            {
                if (!PackageReader.TryReadPropertyTag(payload, package.Names, ref position, out var rawNestedTag))
                {
                    throw new PackageReadException(
                        $"{className} export {exportIndex} ({objectName}) has invalid AccessoryTypeList entry {i}.");
                }

                var nestedTag = rawNestedTag.ToStreamPropertyTag();
                if (nestedTag.IsTerminator)
                {
                    hasTerminator = true;
                    break;
                }

                switch (nestedTag.Name)
                {
                    case "Depth":
                        depth = ReadNestedInt32(payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                        break;
                    case "Mesh":
                        meshReference = ReadNestedObjectReference(package, payload, ref position, nestedTag, className, exportIndex, objectName, tag.Name);
                        break;
                    default:
                        throw new PackageReadException(
                            $"{className} export {exportIndex} ({objectName}) has unsupported AccessoryTypeList property '{nestedTag.Name}' in '{tag.Name}'.");
                }
            }

            if (!hasTerminator)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has unterminated AccessoryTypeList entry {i}.");
            }

            values[i] = new UnrAccessoryTypeEntry
            {
                Depth = depth,
                MeshReference = meshReference
            };
        }

        if (position != payload.Length)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has trailing bytes in AccessoryTypeList.");
        }

        return values;
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

    private static bool ReadStrictBool(StreamPropertyTag tag, string className, int exportIndex, string objectName)
    {
        return tag.ThrowIfNotTagEncodedBool(
            $"{className} export {exportIndex} ({objectName}) has invalid bool payload in '{tag.Name}'.");
    }

    private enum UnrStaticMeshActorPropertyKind
    {
        StepSound_1,
        StepSound_2,
        StepSound_3,
        L2CurrentLod,
        L2LodViewDuration,
        L2ServerObjectRealID,
        L2ServerObjectID,
        L2ServerObjectType,
        Physics,
        Style,
        StaticMesh,
        MainScale,
        PostScale,
        TempScale,
        bDynamicActorFilterState,
        bLightChanged,
        bUpdateShadow,
        bStatic,
        bStasis,
        bFixedRotationDir,
        RotationRate,
        bDeleteMe,
        bSelected,
        bPendingDelete,
        bHiddenEd,
        bStaticLighting,
        bDisableSorting,
        bAgitDefaultStaticMesh,
        AgitID,
        AccessoryIndex,
        AccessoryTypeList,
        Skins,
        Level,
        Region,
        ForcedRegionTag,
        ForcedRegion,
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
        PrePivot,
        StaticMeshInstance,
        SwayRotationOrig,
        CollisionRadius,
        bCollideActors,
        bBlockActors,
        bBlockPlayers,
        bBlockZeroExtentTraces,
        bBlockNonZeroExtentTraces,
        bBlockKarma,
        bUnlit,
        bShadowCast,
        bIgnoredRange,
        ColLocation,
        TexModifyInfo
    }
}
