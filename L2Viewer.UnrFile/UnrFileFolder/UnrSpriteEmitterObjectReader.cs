using System.Numerics;

namespace L2Viewer.UnrFile;

internal static class UnrSpriteEmitterObjectReader
{
    public static UnrSpriteEmitterObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        byte? useDirectionAs = null;
        Vector3? acceleration = null;
        var useColorScale = false;
        float? opacity = null;
        float? fadeOutStartTime = null;
        var fadeOut = false;
        int? maxParticles = null;
        var weatherSoundCheck = false;
        string? nameValue = null;
        var spinParticles = false;
        Vector3? spinCcwOrCw = null;
        UnrRangeVector? spinsPerSecondRange = null;
        UnrRangeVector? startSpinRange = null;
        var useSizeScale = false;
        var useRegularSizeScale = false;
        var sizeScale = new List<UnrParticleSizeScale>();
        UnrRangeVector? startSizeRange = null;
        var uniformSize = false;
        byte? drawStyle = null;
        UnrFileObjectReference? textureReference = null;
        UnrFloatRange? lifetimeRange = null;
        UnrRangeVector? startVelocityRange = null;
        float? warmupTicksPerSecond = null;
        float? relativeWarmupTime = null;
        var blendBetweenSubdivisions = false;
        var colorScale = new List<UnrParticleColorScale>();
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
                RawHex = rawHex
            };
        }

        void ReadProperty(StreamPropertyTag tag)
        {
            if (!Enum.TryParse<UnrSpriteEmitterPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrSpriteEmitterPropertyKind.UseDirectionAs:
                    useDirectionAs = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.Acceleration:
                    acceleration = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.UseColorScale:
                    useColorScale = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.Opacity:
                    opacity = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.FadeOutStartTime:
                    fadeOutStartTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.FadeOut:
                    fadeOut = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.MaxParticles:
                    maxParticles = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.WeatherSoundCheck:
                    weatherSoundCheck = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.Name:
                    nameValue = reader.ReadStringProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.SpinParticles:
                    spinParticles = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.SpinCCWorCW:
                    spinCcwOrCw = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.SpinsPerSecondRange:
                    spinsPerSecondRange = ReadRangeVectorStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.StartSpinRange:
                    startSpinRange = ReadRangeVectorStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.UseSizeScale:
                    useSizeScale = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.UseRegularSizeScale:
                    useRegularSizeScale = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.StartSizeRange:
                    startSizeRange = ReadRangeVectorStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.UniformSize:
                    uniformSize = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.DrawStyle:
                    drawStyle = reader.ReadByteProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.Texture:
                    textureReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.LifetimeRange:
                    lifetimeRange = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.StartVelocityRange:
                    startVelocityRange = ReadRangeVectorStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.WarmupTicksPerSecond:
                    warmupTicksPerSecond = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.RelativeWarmupTime:
                    relativeWarmupTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.BlendBetweenSubdivisions:
                    blendBetweenSubdivisions = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSpriteEmitterPropertyKind.ColorScale:
                    colorScale.AddRange(UnrParticleStructPropertyReader.ReadParticleColorScaleProperties(package, reader, tag, className, exportIndex, objectName));
                    return;
                case UnrSpriteEmitterPropertyKind.SizeScale:
                    sizeScale.AddRange(UnrParticleStructPropertyReader.ReadParticleSizeScaleProperties(package, reader, tag, className, exportIndex, objectName));
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
        return new UnrSpriteEmitterObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            UseDirectionAs = useDirectionAs,
            Acceleration = acceleration,
            UseColorScale = useColorScale,
            Opacity = opacity,
            FadeOutStartTime = fadeOutStartTime,
            FadeOut = fadeOut,
            MaxParticles = maxParticles,
            WeatherSoundCheck = weatherSoundCheck,
            NameValue = nameValue,
            SpinParticles = spinParticles,
            SpinCCWorCW = spinCcwOrCw,
            SpinsPerSecondRange = spinsPerSecondRange,
            StartSpinRange = startSpinRange,
            UseSizeScale = useSizeScale,
            UseRegularSizeScale = useRegularSizeScale,
            SizeScale = sizeScale.OrderBy(x => x.ArrayIndex).ToArray(),
            StartSizeRange = startSizeRange,
            UniformSize = uniformSize,
            DrawStyle = drawStyle,
            TextureReference = textureReference,
            LifetimeRange = lifetimeRange,
            StartVelocityRange = startVelocityRange,
            WarmupTicksPerSecond = warmupTicksPerSecond,
            RelativeWarmupTime = relativeWarmupTime,
            BlendBetweenSubdivisions = blendBetweenSubdivisions,
            ColorScale = colorScale.OrderBy(x => x.ArrayIndex).ToArray(),
            UnknownProperties = unknownProperties.ToArray()
        };
    }

    private static bool ReadStrictBool(StreamPropertyTag tag, string className, int exportIndex, string objectName)
    {
        return tag.ThrowIfNotTagEncodedBool(
            $"{className} export {exportIndex} ({objectName}) has invalid bool payload in '{tag.Name}'.");
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

    private static UnrRangeVector ReadRangeVectorStruct(
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
                    x = ReadRangeStruct(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                case "Y":
                    y = ReadRangeStruct(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                case "Z":
                    z = ReadRangeStruct(package, reader, nestedTag, className, exportIndex, objectName);
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

    private enum UnrSpriteEmitterPropertyKind
    {
        UseDirectionAs,
        Acceleration,
        UseColorScale,
        ColorScale,
        Opacity,
        FadeOutStartTime,
        FadeOut,
        MaxParticles,
        WeatherSoundCheck,
        Name,
        SpinParticles,
        SpinCCWorCW,
        SpinsPerSecondRange,
        StartSpinRange,
        UseSizeScale,
        UseRegularSizeScale,
        SizeScale,
        StartSizeRange,
        UniformSize,
        DrawStyle,
        Texture,
        LifetimeRange,
        StartVelocityRange,
        WarmupTicksPerSecond,
        RelativeWarmupTime,
        BlendBetweenSubdivisions
    }
}
