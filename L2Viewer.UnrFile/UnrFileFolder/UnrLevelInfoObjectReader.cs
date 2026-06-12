namespace L2Viewer.UnrFile;

internal static class UnrLevelInfoObjectReader
{
    public static UnrLevelInfoObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        Vector3? location = null;
        Vector3? rotation = null;
        var drawScale = 1f;
        var drawScale3D = Vector3.One;
        var prePivot = Vector3.Zero;
        UnrFileScale? mainScale = null;
        UnrFileScale? postScale = null;
        UnrFileScale? tempScale = null;
        string? tagName = null;
        string? eventName = null;
        string? groupName = null;
        UnrFileObjectReference? brushReference = null;
        UnrFileObjectReference? baseReference = null;
        UnrFileObjectReference? levelReference = null;
        UnrFileObjectReference? ownerReference = null;
        UnrFileObjectReference? meshReference = null;
        UnrFileObjectReference? textureReference = null;
        UnrFileObjectReference? physicsVolumeReference = null;
        UnrFileObjectReference? staticMeshReference = null;
        Vector3? ambientVector = null;
        byte? ambientBrightness = null;
        float? distanceFogEnd = null;
        UnrPointRegion? region = null;
        UnrTextureModifyInfo? texModifyInfo = null;
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void AddUnknownProperty(StreamPropertyTag tag)
        {
            var unknown = new UnrFileUnknownProperty
            {
                Order = unknownProperties.Count,
                Name = tag.Name,
                Type = tag.Type,
                DataSize = tag.DataSize,
                StructName = tag.StructName,
                BoolValue = tag.BoolValue
            };

            switch (tag.Type)
            {
                case PackageReader.PropertyTypeBool when tag.DataSize == 0:
                    unknownProperties.Add(unknown);
                    return;
                case PackageReader.PropertyTypeFloat:
                    unknownProperties.Add(new UnrFileUnknownProperty
                    {
                        Order = unknown.Order,
                        Name = unknown.Name,
                        Type = unknown.Type,
                        DataSize = unknown.DataSize,
                        StructName = unknown.StructName,
                        BoolValue = unknown.BoolValue,
                        FloatValue = reader.ReadFloatProperty(tag, className, exportIndex, objectName)
                    });
                    return;
                case PackageReader.PropertyTypeInt:
                    unknownProperties.Add(new UnrFileUnknownProperty
                    {
                        Order = unknown.Order,
                        Name = unknown.Name,
                        Type = unknown.Type,
                        DataSize = unknown.DataSize,
                        StructName = unknown.StructName,
                        BoolValue = unknown.BoolValue,
                        IntValue = reader.ReadIntProperty(tag, className, exportIndex, objectName)
                    });
                    return;
                case PackageReader.PropertyTypeByte:
                    unknownProperties.Add(new UnrFileUnknownProperty
                    {
                        Order = unknown.Order,
                        Name = unknown.Name,
                        Type = unknown.Type,
                        DataSize = unknown.DataSize,
                        StructName = unknown.StructName,
                        BoolValue = unknown.BoolValue,
                        ByteValue = reader.ReadByteProperty(tag, className, exportIndex, objectName)
                    });
                    return;
                case PackageReader.PropertyTypeName:
                    unknownProperties.Add(new UnrFileUnknownProperty
                    {
                        Order = unknown.Order,
                        Name = unknown.Name,
                        Type = unknown.Type,
                        DataSize = unknown.DataSize,
                        StructName = unknown.StructName,
                        BoolValue = unknown.BoolValue,
                        NameValue = reader.ReadNameProperty(package, tag, className, exportIndex, objectName)
                    });
                    return;
                case PackageReader.PropertyTypeObject:
                    unknownProperties.Add(new UnrFileUnknownProperty
                    {
                        Order = unknown.Order,
                        Name = unknown.Name,
                        Type = unknown.Type,
                        DataSize = unknown.DataSize,
                        StructName = unknown.StructName,
                        BoolValue = unknown.BoolValue,
                        ObjectReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName)
                    });
                    return;
                case PackageReader.PropertyTypeStruct when tag.StructName.Is("Scale"):
                    unknownProperties.Add(new UnrFileUnknownProperty
                    {
                        Order = unknown.Order,
                        Name = unknown.Name,
                        Type = unknown.Type,
                        DataSize = unknown.DataSize,
                        StructName = unknown.StructName,
                        BoolValue = unknown.BoolValue,
                        ScaleValue = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName)
                    });
                    return;
                default:
                    unknownProperties.Add(new UnrFileUnknownProperty
                    {
                        Order = unknown.Order,
                        Name = unknown.Name,
                        Type = unknown.Type,
                        DataSize = unknown.DataSize,
                        StructName = unknown.StructName,
                        BoolValue = unknown.BoolValue,
                        RawHex = UnrCompat.ToHexString(reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names))
                    });
                    return;
            }
        }

        void ReadProperty(StreamPropertyTag tag)
        {
            if (tag.IsFullyEncodedInTag())
            {
                return;
            }

            if (!Enum.TryParse<UnrLevelInfoPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrLevelInfoPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrLevelInfoPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrLevelInfoPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrLevelInfoPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Event:
                    eventName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Brush:
                    brushReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Base:
                    baseReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Level:
                    levelReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Owner:
                    ownerReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Mesh:
                    meshReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Texture:
                    textureReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.AmbientVector:
                    ambientVector = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.AmbientBrightness:
                    ambientBrightness = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.DistanceFogEnd:
                    distanceFogEnd = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.TexModifyInfo:
                    texModifyInfo = UnrStructPropertyReader.ReadTextureModifyInfoProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelInfoPropertyKind.TimeSeconds:
                case UnrLevelInfoPropertyKind.Summary:
                case UnrLevelInfoPropertyKind.VisibleGroups:
                case UnrLevelInfoPropertyKind.CameraLocationDynamic:
                case UnrLevelInfoPropertyKind.CameraLocationTop:
                case UnrLevelInfoPropertyKind.CameraLocationFront:
                case UnrLevelInfoPropertyKind.CameraLocationSide:
                case UnrLevelInfoPropertyKind.CameraRotationDynamic:
                case UnrLevelInfoPropertyKind.NavigationPointList:
                    AddUnknownProperty(tag);
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
            ReadProperty(tag);
            var consumed = reader.BaseStream.Position - payloadStart;
            tag.EnsurePropertyFullyConsumed(consumed, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has no terminating None property before EOF.");
        }

        reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        return new UnrLevelInfoObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            MainScale = mainScale,
            PostScale = postScale,
            TempScale = tempScale,
            Location = location,
            Rotation = rotation,
            DrawScale = drawScale,
            DrawScale3D = drawScale3D,
            PrePivot = prePivot,
            Group = groupName,
            Tag = tagName,
            Event = eventName,
            BrushReference = brushReference,
            BaseReference = baseReference,
            LevelReference = levelReference,
            OwnerReference = ownerReference,
            MeshReference = meshReference,
            TextureReference = textureReference,
            PhysicsVolumeReference = physicsVolumeReference,
            StaticMeshReference = staticMeshReference,
            AmbientVector = ambientVector,
            AmbientBrightness = ambientBrightness,
            DistanceFogEnd = distanceFogEnd,
            Region = region,
            TexModifyInfo = texModifyInfo,
            UnknownProperties = unknownProperties.ToArray()
        };
    }

    private enum UnrLevelInfoPropertyKind
    {
        Location,
        Rotation,
        DrawScale,
        DrawScale3D,
        PrePivot,
        MainScale,
        PostScale,
        TempScale,
        Tag,
        Event,
        Group,
        Brush,
        Base,
        Level,
        Region,
        Owner,
        Mesh,
        Texture,
        PhysicsVolume,
        StaticMesh,
        TimeSeconds,
        Summary,
        VisibleGroups,
        CameraLocationDynamic,
        CameraLocationTop,
        CameraLocationFront,
        CameraLocationSide,
        CameraRotationDynamic,
        NavigationPointList,
        AmbientVector,
        AmbientBrightness,
        DistanceFogEnd,
        TexModifyInfo
    }
}
