using System.Numerics;

namespace L2Viewer.UnrFile;

internal static class UnrMeshEmitterObjectReader
{
    public static UnrMeshEmitterObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        UnrFileObjectReference? staticMeshReference = null;
        var useMeshBlendMode = false;
        var renderTwoSided = false;
        float? opacity = null;
        float? fadeOutStartTime = null;
        var fadeOut = false;
        float? fadeInEndTime = null;
        var fadeIn = false;
        int? maxParticles = null;
        string? nameValue = null;
        var spinParticles = false;
        UnrRangeVector? spinsPerSecondRange = null;
        UnrRangeVector? startSizeRange = null;
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
            if (!Enum.TryParse<UnrMeshEmitterPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrMeshEmitterPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.UseMeshBlendMode:
                    useMeshBlendMode = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.RenderTwoSided:
                    renderTwoSided = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.Opacity:
                    opacity = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.FadeOutStartTime:
                    fadeOutStartTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.FadeOut:
                    fadeOut = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.FadeInEndTime:
                    fadeInEndTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.FadeIn:
                    fadeIn = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.MaxParticles:
                    maxParticles = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.Name:
                    nameValue = reader.ReadStringProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.SpinParticles:
                    spinParticles = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.SpinsPerSecondRange:
                    spinsPerSecondRange = ReadRangeVectorStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.StartSizeRange:
                    startSizeRange = ReadRangeVectorStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.LifetimeRange:
                    lifetimeRange = ReadRangeStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.StartVelocityRange:
                    startVelocityRange = ReadRangeVectorStruct(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.WarmupTicksPerSecond:
                    warmupTicksPerSecond = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.RelativeWarmupTime:
                    relativeWarmupTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrMeshEmitterPropertyKind.ColorScale:
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
        return new UnrMeshEmitterObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            StaticMeshReference = staticMeshReference,
            UseMeshBlendMode = useMeshBlendMode,
            RenderTwoSided = renderTwoSided,
            Opacity = opacity,
            FadeOutStartTime = fadeOutStartTime,
            FadeOut = fadeOut,
            FadeInEndTime = fadeInEndTime,
            FadeIn = fadeIn,
            MaxParticles = maxParticles,
            NameValue = nameValue,
            SpinParticles = spinParticles,
            SpinsPerSecondRange = spinsPerSecondRange,
            StartSizeRange = startSizeRange,
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

    private enum UnrMeshEmitterPropertyKind
    {
        StaticMesh,
        UseMeshBlendMode,
        RenderTwoSided,
        ColorScale,
        Opacity,
        FadeOutStartTime,
        FadeOut,
        FadeInEndTime,
        FadeIn,
        MaxParticles,
        Name,
        SpinParticles,
        SpinsPerSecondRange,
        StartSizeRange,
        LifetimeRange,
        StartVelocityRange,
        WarmupTicksPerSecond,
        RelativeWarmupTime
    }
}
