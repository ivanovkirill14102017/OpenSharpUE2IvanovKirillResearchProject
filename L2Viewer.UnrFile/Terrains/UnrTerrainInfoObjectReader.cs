namespace L2Viewer.UnrFile;

internal static class UnrTerrainInfoObjectReader
{
    public static UnrTerrainInfoObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        Vector3? location = null;
        Vector3? terrainScale = null;
        int? mapX = null;
        int? mapY = null;
        UnrFileObjectReference? terrainMapReference = null;
        uint[] quadVisibilityBitmap = [];
        uint[] quadVisibilityBitmapOriginal = [];
        uint[] edgeTurnBitmap = [];
        uint[] edgeTurnBitmapOriginal = [];
        var layers = new List<UnrTerrainLayer>();
        var decoLayers = new List<UnrTerrainDecoLayer>();
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void AddUnknownProperty(StreamPropertyTag tag)
        {
            switch (tag.Type)
            {
                case PackageReader.PropertyTypeBool when tag.DataSize == 0:
                    unknownProperties.Add(CreateUnknownProperty(tag));
                    return;
                case PackageReader.PropertyTypeFloat:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        floatValue: reader.ReadFloatProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeInt:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        intValue: reader.ReadIntProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeByte:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        byteValue: reader.ReadByteProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeName:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        nameValue: reader.ReadNameProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeObject:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        objectReference: reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeStruct when tag.StructName.Is("Scale"):
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        scaleValue: reader.ReadScaleProperty(package, tag, className, exportIndex, objectName)));
                    return;
                default:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        rawHex: UnrCompat.ToHexString(reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names))));
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
            if (tag.IsFullyEncodedInTag())
            {
                AddUnknownProperty(tag);
                return;
            }

            if (!Enum.TryParse<UnrTerrainInfoPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrTerrainInfoPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.TerrainScale:
                    terrainScale = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.MapX:
                    mapX = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.MapY:
                    mapY = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.TerrainMap:
                    terrainMapReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.QuadVisibilityBitmap:
                    quadVisibilityBitmap = reader.ReadUInt32ArrayProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.QuadVisibilityBitmapOrig:
                    quadVisibilityBitmapOriginal = reader.ReadUInt32ArrayProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.EdgeTurnBitmap:
                    edgeTurnBitmap = reader.ReadUInt32ArrayProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.EdgeTurnBitmapOrig:
                    edgeTurnBitmapOriginal = reader.ReadUInt32ArrayProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.Layers:
                    layers.Add(ReadTerrainLayer(package, reader, tag, className, exportIndex, objectName));
                    return;
                case UnrTerrainInfoPropertyKind.DecoLayers:
                    decoLayers.AddRange(ReadTerrainDecoLayers(package, reader, tag, className, exportIndex, objectName));
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

        var nativeTailSize = checked((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        return new UnrTerrainInfoObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            Location = location,
            TerrainScale = terrainScale,
            MapX = mapX,
            MapY = mapY,
            TerrainMapReference = terrainMapReference,
            QuadVisibilityBitmap = quadVisibilityBitmap,
            QuadVisibilityBitmapOriginal = quadVisibilityBitmapOriginal,
            EdgeTurnBitmap = edgeTurnBitmap,
            EdgeTurnBitmapOriginal = edgeTurnBitmapOriginal,
            Layers = layers
                .OrderBy(x => x.ArrayIndex)
                .ToArray(),
            DecoLayers = decoLayers
                .OrderBy(x => x.Order)
                .ToArray(),
            UnknownProperties = unknownProperties.ToArray(),
            NativeTailSize = nativeTailSize
        };
    }

    private static UnrTerrainLayer ReadTerrainLayer(
        PackageData package,
        BinaryReader reader,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.DataSize < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid Layers payload size.");
        }

        var payloadStart = reader.BaseStream.Position;
        UnrFileObjectReference? textureReference = null;
        UnrFileObjectReference? alphaMapReference = null;
        var hasTerminatingNone = false;

        while (reader.BaseStream.Position - payloadStart < tag.DataSize)
        {
            var nestedTag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (nestedTag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            var nestedPayloadStart = reader.BaseStream.Position;
            if (nestedTag.Name.Is("Texture"))
            {
                textureReference = reader.ReadOptionalObjectReferenceProperty(package, nestedTag, className, exportIndex, objectName);
            }
            else if (nestedTag.Name.Is("AlphaMap"))
            {
                alphaMapReference = reader.ReadOptionalObjectReferenceProperty(package, nestedTag, className, exportIndex, objectName);
            }
            else
            {
                _ = reader.SkipPropertyPayload(nestedTag, className, exportIndex, objectName, package.Names);
            }

            var nestedConsumed = reader.BaseStream.Position - nestedPayloadStart;
            nestedTag.EnsurePropertyFullyConsumed(nestedConsumed, className, exportIndex, objectName);
        }

        var consumed = reader.BaseStream.Position - payloadStart;
        if (consumed != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) layer property '{tag.Name}[{tag.ArrayIndex}]' consumed {consumed} bytes, expected {tag.DataSize}.");
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) layer property '{tag.Name}[{tag.ArrayIndex}]' has no terminating None.");
        }

        return new UnrTerrainLayer
        {
            ArrayIndex = tag.ArrayIndex,
            TextureReference = textureReference,
            AlphaMapReference = alphaMapReference
        };
    }

    private static IReadOnlyList<UnrTerrainDecoLayer> ReadTerrainDecoLayers(
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
                $"{className} export {exportIndex} ({objectName}) has invalid DecoLayers payload.");
        }

        var payloadStart = reader.BaseStream.Position;
        var count = PackageReader.ReadCompactIndex(reader);
        if (count < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid negative DecoLayers count.");
        }

        var layers = new List<UnrTerrainDecoLayer>(count);
        for (var i = 0; i < count; i++)
        {
            layers.Add(ReadTerrainDecoLayer(package, reader, className, exportIndex, objectName, i, tag.DataSize, payloadStart));
        }

        var consumed = checked((int)(reader.BaseStream.Position - payloadStart));
        if (consumed != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) DecoLayers consumed {consumed} bytes, expected {tag.DataSize}.");
        }

        return layers;
    }

    private static UnrTerrainDecoLayer ReadTerrainDecoLayer(
        PackageData package,
        BinaryReader reader,
        string className,
        int exportIndex,
        string objectName,
        int order,
        int parentDataSize,
        long parentPayloadStart)
    {
        var showOnTerrain = 0;
        UnrFileObjectReference? scaleMapReference = null;
        UnrFileObjectReference? densityMapReference = null;
        UnrFileObjectReference? colorMapReference = null;
        UnrFileObjectReference? staticMeshReference = null;
        UnrRangeVector? scaleMultiplier = null;
        UnrFloatRange? fadeoutRadius = null;
        UnrFloatRange? densityMultiplier = null;
        var maxPerQuad = 0;
        var seed = 0;
        var alignToTerrain = 0;
        byte drawOrder = 0;
        var showOnInvisibleTerrain = 0;
        var litDirectional = 0;
        var disregardTerrainLighting = 0;
        var randomYaw = 0;
        var forceRender = 0;
        var hasTerminatingNone = false;

        while (reader.BaseStream.Position - parentPayloadStart < parentDataSize)
        {
            var nestedTag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (nestedTag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            var nestedPayloadStart = reader.BaseStream.Position;
            switch (nestedTag.Name)
            {
                case "ShowOnTerrain":
                    showOnTerrain = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "ScaleMap":
                    scaleMapReference = reader.ReadOptionalObjectReferenceProperty(package, nestedTag, className, exportIndex, objectName);
                    break;
                case "DensityMap":
                    densityMapReference = reader.ReadOptionalObjectReferenceProperty(package, nestedTag, className, exportIndex, objectName);
                    break;
                case "ColorMap":
                    colorMapReference = reader.ReadOptionalObjectReferenceProperty(package, nestedTag, className, exportIndex, objectName);
                    break;
                case "StaticMesh":
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, nestedTag, className, exportIndex, objectName);
                    break;
                case "ScaleMultiplier":
                    scaleMultiplier = ReadRangeVectorStruct(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                case "FadeoutRadius":
                    fadeoutRadius = ReadRangeStruct(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                case "DensityMultiplier":
                    densityMultiplier = ReadRangeStruct(package, reader, nestedTag, className, exportIndex, objectName);
                    break;
                case "MaxPerQuad":
                    maxPerQuad = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "Seed":
                    seed = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "AlignToTerrain":
                    alignToTerrain = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "DrawOrder":
                    drawOrder = reader.ReadByteProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "ShowOnInvisibleTerrain":
                    showOnInvisibleTerrain = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "LitDirectional":
                    litDirectional = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "DisregardTerrainLighting":
                    disregardTerrainLighting = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "RandomYaw":
                    randomYaw = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                case "bForceRender":
                    forceRender = reader.ReadIntProperty(nestedTag, className, exportIndex, objectName);
                    break;
                default:
                    _ = reader.SkipPropertyPayload(nestedTag, className, exportIndex, objectName, package.Names);
                    break;
            }

            var nestedConsumed = reader.BaseStream.Position - nestedPayloadStart;
            nestedTag.EnsurePropertyFullyConsumed(nestedConsumed, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) deco layer {order} has no terminating None.");
        }

        return new UnrTerrainDecoLayer
        {
            Order = order,
            ShowOnTerrain = showOnTerrain,
            ScaleMapReference = scaleMapReference,
            DensityMapReference = densityMapReference,
            ColorMapReference = colorMapReference,
            StaticMeshReference = staticMeshReference,
            ScaleMultiplier = scaleMultiplier,
            FadeoutRadius = fadeoutRadius,
            DensityMultiplier = densityMultiplier,
            MaxPerQuad = maxPerQuad,
            Seed = seed,
            AlignToTerrain = alignToTerrain,
            DrawOrder = drawOrder,
            ShowOnInvisibleTerrain = showOnInvisibleTerrain,
            LitDirectional = litDirectional,
            DisregardTerrainLighting = disregardTerrainLighting,
            RandomYaw = randomYaw,
            ForceRender = forceRender
        };
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
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid Range payload in '{tag.Name}'.");
        }

        var payloadStart = reader.BaseStream.Position;
        float? min = null;
        float? max = null;
        var hasTerminatingNone = false;

        while (reader.BaseStream.Position - payloadStart < tag.DataSize)
        {
            var nestedTag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (nestedTag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            var nestedPayloadStart = reader.BaseStream.Position;
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

            var nestedConsumed = reader.BaseStream.Position - nestedPayloadStart;
            nestedTag.EnsurePropertyFullyConsumed(nestedConsumed, className, exportIndex, objectName);
        }

        var consumed = checked((int)(reader.BaseStream.Position - payloadStart));
        if (consumed != tag.DataSize || !hasTerminatingNone || min is null || max is null)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid Range payload in '{tag.Name}'.");
        }

        return new UnrFloatRange
        {
            Min = min.Value,
            Max = max.Value
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
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid RangeVector payload in '{tag.Name}'.");
        }

        var payloadStart = reader.BaseStream.Position;
        UnrFloatRange? x = null;
        UnrFloatRange? y = null;
        UnrFloatRange? z = null;
        var hasTerminatingNone = false;

        while (reader.BaseStream.Position - payloadStart < tag.DataSize)
        {
            var nestedTag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (nestedTag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            var nestedPayloadStart = reader.BaseStream.Position;
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

            var nestedConsumed = reader.BaseStream.Position - nestedPayloadStart;
            nestedTag.EnsurePropertyFullyConsumed(nestedConsumed, className, exportIndex, objectName);
        }

        var consumed = checked((int)(reader.BaseStream.Position - payloadStart));
        if (consumed != tag.DataSize || !hasTerminatingNone || x is null || y is null || z is null)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid RangeVector payload in '{tag.Name}'.");
        }

        return new UnrRangeVector
        {
            X = x,
            Y = y,
            Z = z
        };
    }

    private enum UnrTerrainInfoPropertyKind
    {
        Location,
        TerrainScale,
        MapX,
        MapY,
        TerrainMap,
        QuadVisibilityBitmap,
        QuadVisibilityBitmapOrig,
        EdgeTurnBitmap,
        EdgeTurnBitmapOrig,
        Layers,
        DecoLayers
    }
}
