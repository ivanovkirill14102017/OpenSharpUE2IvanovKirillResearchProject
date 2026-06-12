using System.Numerics;

namespace L2Viewer.UnrFile;

internal static class UnrEmitterObjectReader
{
    public static UnrEmitterObject Read(
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
        UnrFileObjectReference[] emitters = [];
        var dynamicActorFilterState = false;
        var sunAffect = false;
        var directional = false;
        Vector3? swayRotationOrig = null;
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void AddUnknownProperty(StreamPropertyTag tag)
        {
            switch (tag.Type)
            {
                case PackageReader.PropertyTypeBool when tag.DataSize == 0:
                    unknownProperties.Add(CreateUnknownProperty(tag));
                    return;
                case PackageReader.PropertyTypeFloat:
                    unknownProperties.Add(CreateUnknownProperty(tag, floatValue: reader.ReadFloatProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeInt:
                    unknownProperties.Add(CreateUnknownProperty(tag, intValue: reader.ReadIntProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeByte:
                    unknownProperties.Add(CreateUnknownProperty(tag, byteValue: reader.ReadByteProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeName:
                    unknownProperties.Add(CreateUnknownProperty(tag, nameValue: reader.ReadNameProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeObject:
                    unknownProperties.Add(CreateUnknownProperty(tag, objectReference: reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeStruct when tag.StructName.Is("Scale"):
                    unknownProperties.Add(CreateUnknownProperty(tag, scaleValue: reader.ReadScaleProperty(package, tag, className, exportIndex, objectName)));
                    return;
                default:
                    unknownProperties.Add(CreateUnknownProperty(tag, rawHex: UnrCompat.ToHexString(reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names))));
                    return;
            }
        }

        UnrFileUnknownProperty CreateUnknownProperty(
            StreamPropertyTag tag,
            byte? byteValue = null,
            int? intValue = null,
            float? floatValue = null,
            string? nameValue = null,
            UnrFileObjectReference? objectReference = null,
            UnrFileScale? scaleValue = null,
            string? rawHex = null)
        {
            return new UnrFileUnknownProperty
            {
                Order = unknownProperties.Count,
                Name = tag.Name,
                Type = tag.Type,
                DataSize = tag.DataSize,
                StructName = tag.StructName,
                BoolValue = tag.BoolValue,
                ByteValue = byteValue,
                IntValue = intValue,
                FloatValue = floatValue,
                NameValue = nameValue,
                ObjectReference = objectReference,
                ScaleValue = scaleValue,
                RawHex = rawHex
            };
        }

        void ReadProperty(StreamPropertyTag tag)
        {
            if (!Enum.TryParse<UnrEmitterPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrEmitterPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrEmitterPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrEmitterPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrEmitterPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Event:
                    eventName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Brush:
                    brushReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Base:
                    baseReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Owner:
                    ownerReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Mesh:
                    meshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Texture:
                    textureReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Emitters:
                    emitters = reader.ReadObjectReferenceArrayProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.bSunAffect:
                    sunAffect = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.bDirectional:
                    directional = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.SwayRotationOrig:
                    swayRotationOrig = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrEmitterPropertyKind.Region:
                case UnrEmitterPropertyKind.TexModifyInfo:
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

            var payloadStart = reader.BaseStream.Position;
            ReadProperty(tag);
            tag.EnsurePropertyFullyConsumed(reader.BaseStream.Position - payloadStart, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) has no terminating None property before EOF.");
        }

        reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        return new UnrEmitterObject
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
            Emitters = emitters,
            DynamicActorFilterState = dynamicActorFilterState,
            SunAffect = sunAffect,
            Directional = directional,
            SwayRotationOrig = swayRotationOrig,
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

    private enum UnrEmitterPropertyKind
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
        Emitters,
        bDynamicActorFilterState,
        bSunAffect,
        bDirectional,
        SwayRotationOrig,
        TexModifyInfo
    }
}
