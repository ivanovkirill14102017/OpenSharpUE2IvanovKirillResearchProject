namespace L2Viewer.UnrFile;

internal static class UnrBrushObjectReader
{
    public static UnrBrushObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        bool? updateShadow = null;
        byte? csgOperValue = null;
        int? polyFlagsValue = null;
        UnrFileScale? mainScale = null;
        UnrFileScale? postScale = null;
        UnrFileScale? tempScale = null;
        UnrPointRegion? region = null;
        UnrTextureModifyInfo? texModifyInfo = null;
        Vector3? location = null;
        Vector3? rotation = null;
        var drawScale = 1f;
        var drawScale3D = Vector3.One;
        var prePivot = Vector3.Zero;
        Vector3? swayRotationOrig = null;
        string? tagName = null;
        string? groupName = null;
        string? forcedRegionTag = null;
        UnrFileObjectReference? brushReference = null;
        UnrFileObjectReference? baseReference = null;
        UnrFileObjectReference? levelReference = null;
        UnrFileObjectReference? ownerReference = null;
        UnrFileObjectReference? meshReference = null;
        UnrFileObjectReference? textureReference = null;
        UnrFileObjectReference? physicsVolumeReference = null;
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void ReadBrushProperty(StreamPropertyTag tag)
        {
            if (tag.IsFullyEncodedInTag())
            {
                return;
            }

            if (!Enum.TryParse<BrushPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has unsupported Brush property '{tag.Name}': type={tag.Type}, size={tag.DataSize}, struct={tag.StructName ?? "<null>"}, bool={tag.BoolValue}.");
            }

            switch (kind)
            {
                case BrushPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.CsgOper:
                    csgOperValue = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.bUpdateShadow:
                    updateShadow = reader.ReadIntBooleanProperty(tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.PolyFlags:
                    polyFlagsValue = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case BrushPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case BrushPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case BrushPropertyKind.SwayRotationOrig:
                    swayRotationOrig = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.ForcedRegionTag:
                    forcedRegionTag = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Brush:
                    brushReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Base:
                    baseReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Level:
                    levelReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.TexModifyInfo:
                    texModifyInfo = UnrStructPropertyReader.ReadTextureModifyInfoProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Owner:
                    ownerReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Mesh:
                    meshReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.Texture:
                    textureReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case BrushPropertyKind.LastRenderTime:
                    reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
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

            if (tag.DataSize < 0)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid negative property size for '{tag.Name}'.");
            }

            var payloadStart = reader.BaseStream.Position;
            ReadBrushProperty(tag);
            var consumed = reader.BaseStream.Position - payloadStart;
            tag.EnsurePropertyFullyConsumed(consumed, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has no terminating None property before EOF.");
        }

        reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        return new UnrBrushObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            UpdateShadow = updateShadow,
            CsgOperValue = csgOperValue,
            PolyFlagsValue = polyFlagsValue,
            MainScale = mainScale,
            PostScale = postScale,
            TempScale = tempScale,
            Region = region,
            TexModifyInfo = texModifyInfo,
            Location = location,
            Rotation = rotation,
            DrawScale = drawScale,
            DrawScale3D = drawScale3D,
            PrePivot = prePivot,
            SwayRotationOrig = swayRotationOrig,
            Tag = tagName,
            Group = groupName,
            ForcedRegionTag = forcedRegionTag,
            BrushReference = brushReference,
            BaseReference = baseReference,
            LevelReference = levelReference,
            OwnerReference = ownerReference,
            MeshReference = meshReference,
            TextureReference = textureReference,
            PhysicsVolumeReference = physicsVolumeReference,
            UnknownProperties = unknownProperties.ToArray()
        };
    }

    private enum BrushPropertyKind
    {
        bUpdateShadow,
        CsgOper,
        PolyFlags,
        MainScale,
        PostScale,
        TempScale,
        Location,
        Rotation,
        DrawScale,
        DrawScale3D,
        PrePivot,
        SwayRotationOrig,
        Tag,
        Group,
        ForcedRegionTag,
        Brush,
        Base,
        Level,
        Region,
        TexModifyInfo,
        Owner,
        Mesh,
        Texture,
        PhysicsVolume,
        LastRenderTime
    }
}
