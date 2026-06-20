using System.Numerics;

namespace L2Viewer.UnrFile;

internal static class UnrVertMeshEmitterObjectReader
{
    public static UnrVertMeshEmitterObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        reader.ReadStateFrameIfPresent(export.ObjectFlags);
        string? tagName = null;
        string? eventName = null;
        string? groupName = null;
        UnrFileScale? mainScale = null;
        UnrFileScale? postScale = null;
        UnrFileScale? tempScale = null;
        UnrFileObjectReference? brushReference = null;
        UnrFileObjectReference? baseReference = null;
        UnrFileObjectReference? levelReference = null;
        UnrFileObjectReference? ownerReference = null;
        UnrFileObjectReference? meshReference = null;
        UnrFileObjectReference? textureReference = null;
        UnrFileObjectReference? physicsVolumeReference = null;
        UnrFileObjectReference? staticMeshReference = null;
        Vector3? location = null;
        Vector3? rotation = null;
        var drawScale = 1f;
        var drawScale3D = Vector3.One;
        var prePivot = Vector3.Zero;
        UnrFileObjectReference? vertexMeshReference = null;
        var useMeshBlendMode = false;
        Vector3? acceleration = null;
        var useColorScale = false;
        var colorScale = new List<UnrParticleColorScale>();
        float? colorScaleRepeats = null;
        UnrRangeVector? colorMultiplierRange = null;
        float? opacity = null;
        float? fadeOutStartTime = null;
        var fadeOut = false;
        float? fadeInEndTime = null;
        var fadeIn = false;
        byte? coordinateSystem = null;
        int? maxParticles = null;
        int? checkLevelOfWeather = null;
        var weatherEffect = false;
        string? nameValue = null;
        UnrRangeVector? startLocationRange = null;
        var useRevolution = false;
        UnrRangeVector? revolutionsPerSecondRange = null;
        var spinParticles = false;
        UnrRangeVector? startSpinRange = null;
        UnrRangeVector? startSizeRange = null;
        byte? drawStyle = null;
        UnrFloatRange? lifetimeRange = null;
        UnrRangeVector? startVelocityRange = null;
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
                case PackageReader.PropertyTypeName:
                    unknownProperties.Add(CreateUnknownProperty(tag, nameValue: reader.ReadNameProperty(package, tag, className, exportIndex, objectName)));
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
            if (!Enum.TryParse<UnrVertMeshEmitterPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrVertMeshEmitterPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrVertMeshEmitterPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrVertMeshEmitterPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrVertMeshEmitterPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Event:
                    eventName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Brush:
                    brushReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Base:
                    baseReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Owner:
                    ownerReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Mesh:
                    meshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Texture:
                    textureReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.VertexMesh:
                    vertexMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.UseMeshBlendMode:
                    useMeshBlendMode = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Acceleration:
                    acceleration = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.ColorScale:
                    colorScale.AddRange(UnrParticleStructPropertyReader.ReadParticleColorScaleProperties(package, reader, tag, className, exportIndex, objectName));
                    return;
                case UnrVertMeshEmitterPropertyKind.UseColorScale:
                    useColorScale = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.ColorScaleRepeats:
                    colorScaleRepeats = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.ColorMultiplierRange:
                    colorMultiplierRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Opacity:
                    opacity = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.FadeOutStartTime:
                    fadeOutStartTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.FadeOut:
                    fadeOut = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.FadeInEndTime:
                    fadeInEndTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.FadeIn:
                    fadeIn = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.CoordinateSystem:
                    coordinateSystem = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.MaxParticles:
                    maxParticles = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.CheckLevelOfWeather:
                    checkLevelOfWeather = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.WeatherEffect:
                    weatherEffect = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.Name:
                    nameValue = reader.ReadStringProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.StartLocationRange:
                    startLocationRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.UseRevolution:
                    useRevolution = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.RevolutionsPerSecondRange:
                    revolutionsPerSecondRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.SpinParticles:
                    spinParticles = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.StartSpinRange:
                    startSpinRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.StartSizeRange:
                    startSizeRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.DrawStyle:
                    drawStyle = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.LifetimeRange:
                    lifetimeRange = UnrStructPropertyReader.ReadRangeProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.StartVelocityRange:
                    startVelocityRange = UnrStructPropertyReader.ReadRangeVectorProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.WarmupTicksPerSecond:
                    warmupTicksPerSecond = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.RelativeWarmupTime:
                    relativeWarmupTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrVertMeshEmitterPropertyKind.AnimRate:
                case UnrVertMeshEmitterPropertyKind.WeatherRangeTime:
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

        reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        return new UnrVertMeshEmitterObject
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
            VertexMeshReference = vertexMeshReference,
            UseMeshBlendMode = useMeshBlendMode,
            Acceleration = acceleration,
            UseColorScale = useColorScale,
            ColorScale = colorScale.OrderBy(x => x.ArrayIndex).ToArray(),
            ColorScaleRepeats = colorScaleRepeats,
            ColorMultiplierRange = colorMultiplierRange,
            Opacity = opacity,
            FadeOutStartTime = fadeOutStartTime,
            FadeOut = fadeOut,
            FadeInEndTime = fadeInEndTime,
            FadeIn = fadeIn,
            CoordinateSystem = coordinateSystem,
            MaxParticles = maxParticles,
            CheckLevelOfWeather = checkLevelOfWeather,
            WeatherEffect = weatherEffect,
            NameValue = nameValue,
            StartLocationRange = startLocationRange,
            UseRevolution = useRevolution,
            RevolutionsPerSecondRange = revolutionsPerSecondRange,
            SpinParticles = spinParticles,
            StartSpinRange = startSpinRange,
            StartSizeRange = startSizeRange,
            DrawStyle = drawStyle,
            LifetimeRange = lifetimeRange,
            StartVelocityRange = startVelocityRange,
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

    private enum UnrVertMeshEmitterPropertyKind
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
        AnimRate,
        VertexMesh,
        UseMeshBlendMode,
        Acceleration,
        UseColorScale,
        ColorScale,
        ColorScaleRepeats,
        ColorMultiplierRange,
        Opacity,
        FadeOutStartTime,
        FadeOut,
        FadeInEndTime,
        FadeIn,
        CoordinateSystem,
        MaxParticles,
        CheckLevelOfWeather,
        WeatherEffect,
        WeatherRangeTime,
        Name,
        StartLocationRange,
        UseRevolution,
        RevolutionsPerSecondRange,
        SpinParticles,
        StartSpinRange,
        StartSizeRange,
        DrawStyle,
        LifetimeRange,
        StartVelocityRange,
        WarmupTicksPerSecond,
        RelativeWarmupTime
    }
}
