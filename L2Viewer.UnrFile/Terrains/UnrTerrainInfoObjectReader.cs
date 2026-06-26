namespace L2Viewer.UnrFile;

internal static class UnrTerrainInfoObjectReader
{
    private const int TerrainIntMapSampleWidth = 257;
    private const int TerrainIntMapSampleCount = TerrainIntMapSampleWidth * TerrainIntMapSampleWidth;
    private const int NativeByteBlockCount = 16;
    private const int NativeRasterSize = 512;
    private const int NativeRasterByteCount = NativeRasterSize * NativeRasterSize;
    private const int NativeRasterHeaderPrefixByteCount = 2;
    private const int NativeRasterHeaderInt32Count = 19;

    public static UnrTerrainInfoObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        Vector3? location = null;
        Vector3? rotation = null;
        Vector3? swayRotationOrig = null;
        Vector3? terrainScale = null;
        int? mapX = null;
        int? mapY = null;
        float drawScale = 1f;
        float? tickTime = null;
        int? generatedSectorCounter = null;
        int? numIntMap = null;
        byte[] terrainIntMapPayload = [];
        UnrTerrainIntMapLayer[] terrainIntMaps = [];
        var autoTimeGeneration = false;
        var dynamicActorFilterState = false;
        var lightChanged = false;
        var sunAffect = false;
        string? tagName = null;
        UnrFileObjectReference? terrainMapReference = null;
        UnrFileObjectReference? levelReference = null;
        UnrFileObjectReference? physicsVolumeReference = null;
        UnrPointRegion? region = null;
        UnrTextureModifyInfo? texModifyInfo = null;
        uint[] quadVisibilityBitmap = [];
        uint[] quadVisibilityBitmapOriginal = [];
        uint[] edgeTurnBitmap = [];
        uint[] edgeTurnBitmapOriginal = [];
        UnrFileObjectReference[] terrainSectorReferences = [];
        UnrTerrainNativeByteBlock[] nativeByteBlocks = [];
        UnrTerrainNativeRasterCache? nativeRasterCache = null;
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
            if (!Enum.TryParse<UnrTerrainInfoPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                if (tag.IsFullyEncodedInTag())
                {
                    AddUnknownProperty(tag);
                    return;
                }

                AddUnknownProperty(tag);
                return;
            }

            if (tag.IsFullyEncodedInTag())
            {
                switch (kind)
                {
                    case UnrTerrainInfoPropertyKind.bAutoTimeGeneration:
                        autoTimeGeneration = tag.BoolValue;
                        return;
                    case UnrTerrainInfoPropertyKind.bDynamicActorFilterState:
                        dynamicActorFilterState = tag.BoolValue;
                        return;
                    case UnrTerrainInfoPropertyKind.bLightChanged:
                        lightChanged = tag.BoolValue;
                        return;
                    case UnrTerrainInfoPropertyKind.bSunAffect:
                        sunAffect = tag.BoolValue;
                        return;
                    default:
                        AddUnknownProperty(tag);
                        return;
                }
            }

            switch (kind)
            {
                case UnrTerrainInfoPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.SwayRotationOrig:
                    swayRotationOrig = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
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
                case UnrTerrainInfoPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrTerrainInfoPropertyKind.TickTime:
                    tickTime = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.GeneratedSectorCounter:
                    generatedSectorCounter = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.NumIntMap:
                    numIntMap = reader.ReadIntProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.TIntMap:
                    terrainIntMapPayload = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
                    terrainIntMaps = ReadTerrainIntMaps(terrainIntMapPayload, package, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.bAutoTimeGeneration:
                    autoTimeGeneration = tag.BoolValue;
                    return;
                case UnrTerrainInfoPropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = tag.BoolValue;
                    return;
                case UnrTerrainInfoPropertyKind.bLightChanged:
                    lightChanged = tag.BoolValue;
                    return;
                case UnrTerrainInfoPropertyKind.bSunAffect:
                    sunAffect = tag.BoolValue;
                    return;
                case UnrTerrainInfoPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.TerrainMap:
                    terrainMapReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrTerrainInfoPropertyKind.TexModifyInfo:
                    texModifyInfo = UnrStructPropertyReader.ReadTextureModifyInfoProperty(package, reader, tag, className, exportIndex, objectName);
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

        var nativeTail = reader.ReadBytes(checked((int)(reader.BaseStream.Length - reader.BaseStream.Position)));
        var nativeTailSize = 0;
        if (nativeTail.Length > 0)
        {
            var parsedNativeData = ReadNativeTail(nativeTail, package, className, exportIndex, objectName);
            terrainSectorReferences = parsedNativeData.TerrainSectorReferences;
            nativeByteBlocks = parsedNativeData.NativeByteBlocks;
            nativeRasterCache = parsedNativeData.NativeRasterCache;
            nativeTailSize = parsedNativeData.RemainingTailSize;
        }

        return new UnrTerrainInfoObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            Location = location,
            Rotation = rotation,
            SwayRotationOrig = swayRotationOrig,
            TerrainScale = terrainScale,
            MapX = mapX,
            MapY = mapY,
            DrawScale = drawScale,
            TickTime = tickTime,
            GeneratedSectorCounter = generatedSectorCounter,
            NumIntMap = numIntMap,
            TerrainIntMapPayload = terrainIntMapPayload,
            TerrainIntMaps = terrainIntMaps,
            AutoTimeGeneration = autoTimeGeneration,
            DynamicActorFilterState = dynamicActorFilterState,
            LightChanged = lightChanged,
            SunAffect = sunAffect,
            Tag = tagName,
            TerrainMapReference = terrainMapReference,
            LevelReference = levelReference,
            PhysicsVolumeReference = physicsVolumeReference,
            Region = region,
            TexModifyInfo = texModifyInfo,
            QuadVisibilityBitmap = quadVisibilityBitmap,
            QuadVisibilityBitmapOriginal = quadVisibilityBitmapOriginal,
            EdgeTurnBitmap = edgeTurnBitmap,
            EdgeTurnBitmapOriginal = edgeTurnBitmapOriginal,
            TerrainSectorReferences = terrainSectorReferences,
            NativeByteBlocks = nativeByteBlocks,
            NativeRasterCache = nativeRasterCache,
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

    private static NativeTailParseResult ReadNativeTail(
        byte[] payload,
        PackageData package,
        string className,
        int exportIndex,
        string objectName)
    {
        using var reader = new BinaryReader(new MemoryStream(payload, writable: false));

        var sectorReferenceCount = PackageReader.ReadCompactIndex(reader);
        if (sectorReferenceCount < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid negative terrain sector reference count in native tail.");
        }

        var terrainSectorReferences = new UnrFileObjectReference?[sectorReferenceCount];
        for (var i = 0; i < terrainSectorReferences.Length; i++)
        {
            var rawReference = PackageReader.ReadCompactIndex(reader);
            terrainSectorReferences[i] = ResolveOptionalObjectReference(
                package,
                rawReference,
                className,
                exportIndex,
                objectName,
                $"native terrain sector reference[{i}]");
        }

        var nativeByteBlockCount = PackageReader.ReadCompactIndex(reader);
        if (nativeByteBlockCount != NativeByteBlockCount)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has native byte block count {nativeByteBlockCount}, expected {NativeByteBlockCount}.");
        }

        var nativeByteBlocks = new UnrTerrainNativeByteBlock[NativeByteBlockCount];
        for (var i = 0; i < nativeByteBlocks.Length; i++)
        {
            var count = PackageReader.ReadCompactIndex(reader);
            if (count < 0)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid negative native byte block count at index {i}.");
            }

            var values = reader.ReadBytes(count);
            if (values.Length != count)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) hit EOF while reading native byte block {i}.");
            }

            nativeByteBlocks[i] = new UnrTerrainNativeByteBlock
            {
                ArrayIndex = i,
                Count = count,
                Values = values
            };
        }

        UnrTerrainNativeRasterCache? nativeRasterCache = null;
        var remainingBeforeRaster = checked((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        if (remainingBeforeRaster == NativeRasterHeaderPrefixByteCount + (NativeRasterHeaderInt32Count * sizeof(int)) + NativeRasterByteCount)
        {
            var headerPrefixBytes = reader.ReadBytes(NativeRasterHeaderPrefixByteCount);
            if (headerPrefixBytes.Length != NativeRasterHeaderPrefixByteCount)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) hit EOF while reading native terrain raster header prefix.");
            }

            var headerInt32Values = new int[NativeRasterHeaderInt32Count];
            for (var i = 0; i < headerInt32Values.Length; i++)
            {
                headerInt32Values[i] = reader.ReadInt32();
            }

            var rasterValues = reader.ReadBytes(NativeRasterByteCount);
            if (rasterValues.Length != NativeRasterByteCount)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) hit EOF while reading native terrain raster cache.");
            }

            nativeRasterCache = new UnrTerrainNativeRasterCache
            {
                HeaderPrefixBytes = headerPrefixBytes,
                HeaderInt32Values = headerInt32Values,
                Width = NativeRasterSize,
                Height = NativeRasterSize,
                Values = rasterValues
            };
        }

        var remainingTailSize = checked((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        return new NativeTailParseResult
        {
            TerrainSectorReferences = terrainSectorReferences
                .Where(x => x is not null)
                .Select(x => x!)
                .ToArray(),
            NativeByteBlocks = nativeByteBlocks,
            NativeRasterCache = nativeRasterCache,
            RemainingTailSize = remainingTailSize
        };
    }

    private static UnrFileObjectReference? ResolveOptionalObjectReference(
        PackageData package,
        int rawReference,
        string className,
        int exportIndex,
        string objectName,
        string fieldName)
    {
        if (rawReference == 0)
        {
            return null;
        }

        var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference);
        if (resolved is null)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) points to unresolved object reference {rawReference} in {fieldName}.");
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

    private static UnrTerrainIntMapLayer[] ReadTerrainIntMaps(
        byte[] payload,
        PackageData package,
        string className,
        int exportIndex,
        string objectName)
    {
        if (payload.Length == 0)
        {
            return [];
        }

        using var reader = new BinaryReader(new MemoryStream(payload, writable: false));
        var count = PackageReader.ReadCompactIndex(reader);
        if (count < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid negative TIntMap entry count.");
        }

        var layers = new UnrTerrainIntMapLayer[count];
        for (var i = 0; i < count; i++)
        {
            var entryStart = reader.BaseStream.Position;
            float? time = null;
            byte[] intensityValues = [];
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
                switch (tag.Name)
                {
                    case "Time":
                        time = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                        break;
                    case "Intensity":
                        intensityValues = reader.ReadByteArrayProperty(tag, className, exportIndex, objectName);
                        if (intensityValues.Length != TerrainIntMapSampleCount)
                        {
                            throw new PackageReadException(
                                $"{className} export {exportIndex} ({objectName}) TIntMap[{i}] Intensity has {intensityValues.Length} samples, expected {TerrainIntMapSampleCount}.");
                        }

                        break;
                    default:
                        _ = reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
                        break;
                }

                var consumed = reader.BaseStream.Position - payloadStart;
                tag.EnsurePropertyFullyConsumed(consumed, className, exportIndex, objectName);
            }

            if (!hasTerminatingNone)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) TIntMap[{i}] has no terminating None.");
            }

            var entryEnd = reader.BaseStream.Position;
            reader.BaseStream.Position = entryStart;
            var rawPayload = reader.ReadBytes(checked((int)(entryEnd - entryStart)));
            reader.BaseStream.Position = entryEnd;

            layers[i] = new UnrTerrainIntMapLayer
            {
                ArrayIndex = i,
                Time = time,
                IntensityValues = intensityValues,
                RawPayload = rawPayload
            };
        }

        if (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) TIntMap parser stopped at {reader.BaseStream.Position} of {reader.BaseStream.Length} bytes.");
        }

        return layers;
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
        Rotation,
        SwayRotationOrig,
        TerrainScale,
        MapX,
        MapY,
        DrawScale,
        TickTime,
        GeneratedSectorCounter,
        NumIntMap,
        TIntMap,
        bAutoTimeGeneration,
        bDynamicActorFilterState,
        bLightChanged,
        bSunAffect,
        Tag,
        TerrainMap,
        Level,
        PhysicsVolume,
        Region,
        TexModifyInfo,
        QuadVisibilityBitmap,
        QuadVisibilityBitmapOrig,
        EdgeTurnBitmap,
        EdgeTurnBitmapOrig,
        Layers,
        DecoLayers
    }

    private sealed class NativeTailParseResult
    {
        public UnrFileObjectReference[] TerrainSectorReferences { get; init; } = [];
        public UnrTerrainNativeByteBlock[] NativeByteBlocks { get; init; } = [];
        public UnrTerrainNativeRasterCache? NativeRasterCache { get; init; }
        public int RemainingTailSize { get; init; }
    }
}
