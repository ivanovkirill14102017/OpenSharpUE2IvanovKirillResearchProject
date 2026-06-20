using System.Numerics;

namespace L2Viewer.UnrFile;

internal static class UnrBeamEmitterObjectReader
{
    public static UnrBeamEmitterObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        reader.ReadStateFrameIfPresent(export.ObjectFlags);
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
        byte? determineEndPointBy = null;
        var colorScale = new List<UnrParticleColorScale>();
        UnrRangeVector? colorMultiplierRange = null;
        float? opacity = null;
        float? fadeOutStartTime = null;
        var fadeOut = false;
        float? fadeInEndTime = null;
        var fadeIn = false;
        int? maxParticles = null;
        string? nameValue = null;
        UnrRangeVector? startLocationRange = null;
        UnrFloatRange? sphereRadiusRange = null;
        UnrRangeVector? startLocationPolarRange = null;
        UnrRangeVector? startSizeRange = null;
        UnrFloatRange? lifetimeRange = null;
        float? warmupTicksPerSecond = null;
        float? relativeWarmupTime = null;
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
                case PackageReader.PropertyTypeObject:
                    unknownProperties.Add(CreateUnknownProperty(tag, objectReference: reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeStruct when tag.StructName.Is("Scale"):
                    unknownProperties.Add(CreateUnknownProperty(tag, scaleValue: reader.ReadScaleProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeString:
                case 13:
                    unknownProperties.Add(CreateUnknownProperty(tag, nameValue: reader.ReadStringProperty(tag, className, exportIndex, objectName)));
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
                ArrayIndex = tag.ArrayIndex,
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
            if (!Enum.TryParse<UnrBeamEmitterPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrBeamEmitterPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrBeamEmitterPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrBeamEmitterPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrBeamEmitterPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Event:
                    eventName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Brush:
                    brushReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Base:
                    baseReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Owner:
                    ownerReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Mesh:
                    meshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Texture:
                    textureReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.DetermineEndPointBy:
                    determineEndPointBy = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.ColorScale:
                    colorScale.AddRange(UnrParticleStructPropertyReader.ReadParticleColorScaleProperties(package, reader, tag, className, exportIndex, objectName));
                    return;
                case UnrBeamEmitterPropertyKind.ColorMultiplierRange:
                    colorMultiplierRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Opacity:
                    opacity = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.FadeOutStartTime:
                    fadeOutStartTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.FadeOut:
                    fadeOut = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.FadeInEndTime:
                    fadeInEndTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.FadeIn:
                    fadeIn = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.MaxParticles:
                    maxParticles = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.Name:
                    nameValue = reader.ReadStringProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.StartLocationRange:
                    startLocationRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.SphereRadiusRange:
                    sphereRadiusRange = UnrStructPropertyReader.ReadRangeProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.StartLocationPolarRange:
                    startLocationPolarRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.StartSizeRange:
                    startSizeRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.LifetimeRange:
                    lifetimeRange = UnrStructPropertyReader.ReadRangeProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.WarmupTicksPerSecond:
                    warmupTicksPerSecond = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.RelativeWarmupTime:
                    relativeWarmupTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrBeamEmitterPropertyKind.BeamEndPoints:
                    AddUnknownProperty(tag);
                    return;
            }
        }

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

        void PromoteObjectReference(string propertyName, ref UnrFileObjectReference? value)
        {
            if (value is not null)
            {
                return;
            }

            var index = unknownProperties.FindIndex(x =>
                x.ObjectReference is not null &&
                x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return;
            }

            value = unknownProperties[index].ObjectReference;
            unknownProperties.RemoveAt(index);
        }

        PromoteObjectReference("Texture", ref textureReference);
        PromoteObjectReference("Mesh", ref meshReference);

        reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        return new UnrBeamEmitterObject
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
            DetermineEndPointBy = determineEndPointBy,
            ColorScale = colorScale.OrderBy(x => x.ArrayIndex).ToArray(),
            ColorMultiplierRange = colorMultiplierRange,
            Opacity = opacity,
            FadeOutStartTime = fadeOutStartTime,
            FadeOut = fadeOut,
            FadeInEndTime = fadeInEndTime,
            FadeIn = fadeIn,
            MaxParticles = maxParticles,
            NameValue = nameValue,
            StartLocationRange = startLocationRange,
            SphereRadiusRange = sphereRadiusRange,
            StartLocationPolarRange = startLocationPolarRange,
            StartSizeRange = startSizeRange,
            LifetimeRange = lifetimeRange,
            WarmupTicksPerSecond = warmupTicksPerSecond,
            RelativeWarmupTime = relativeWarmupTime,
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

    private enum UnrBeamEmitterPropertyKind
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
        Owner,
        Mesh,
        Texture,
        PhysicsVolume,
        StaticMesh,
        BeamEndPoints,
        DetermineEndPointBy,
        ColorScale,
        ColorMultiplierRange,
        Opacity,
        FadeOutStartTime,
        FadeOut,
        FadeInEndTime,
        FadeIn,
        MaxParticles,
        Name,
        StartLocationRange,
        SphereRadiusRange,
        StartLocationPolarRange,
        StartSizeRange,
        LifetimeRange,
        WarmupTicksPerSecond,
        RelativeWarmupTime
    }
}
