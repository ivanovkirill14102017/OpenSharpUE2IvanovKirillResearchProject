using System;
using System.Collections.Generic;
using System.Numerics;

namespace L2Viewer.UnrFile;

internal static class UnrSkyZoneInfoObjectReader
{
    public static UnrSkyZoneInfoObject Read(
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
        float? texUPanSpeed = null;
        float? texVPanSpeed = null;
        var dynamicActorFilterState = false;
        var lightChanged = false;
        var lensFlare = new List<UnrFileObjectReference>();
        var lensFlareOffset = new List<float>();
        var lensFlareScale = new List<float>();
        UnrPointRegion? region = null;
        UnrTextureModifyInfo? texModifyInfo = null;
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void EnsureIndexedCapacity<T>(List<T> values, int index, T filler)
        {
            while (values.Count <= index)
            {
                values.Add(filler);
            }
        }

        void SetLensFlare(int index, UnrFileObjectReference value)
        {
            EnsureIndexedCapacity(lensFlare, index, value);
            lensFlare[index] = value;
        }

        void SetLensFlareOffset(int index, float value)
        {
            EnsureIndexedCapacity(lensFlareOffset, index, 0f);
            lensFlareOffset[index] = value;
        }

        void SetLensFlareScale(int index, float value)
        {
            EnsureIndexedCapacity(lensFlareScale, index, 0f);
            lensFlareScale[index] = value;
        }

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
            if (!Enum.TryParse<UnrSkyZoneInfoPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrSkyZoneInfoPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrSkyZoneInfoPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrSkyZoneInfoPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrSkyZoneInfoPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Event:
                    eventName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Brush:
                    brushReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Base:
                    baseReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Level:
                    levelReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Owner:
                    ownerReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Mesh:
                    meshReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Texture:
                    textureReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.TexUPanSpeed:
                    texUPanSpeed = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.TexVPanSpeed:
                    texVPanSpeed = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.LensFlare:
                    if (tag.Type == PackageReader.PropertyTypeArray)
                    {
                        lensFlare.AddRange(reader.ReadObjectReferenceArrayProperty(package, tag, className, exportIndex, objectName));
                        return;
                    }

                    SetLensFlare(tag.ArrayIndex, reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName)!);
                    return;
                case UnrSkyZoneInfoPropertyKind.LensFlareOffset:
                    if (tag.Type == PackageReader.PropertyTypeArray)
                    {
                        lensFlareOffset.AddRange(reader.ReadFloatArrayProperty(tag, className, exportIndex, objectName));
                        return;
                    }

                    SetLensFlareOffset(tag.ArrayIndex, reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? 0f);
                    return;
                case UnrSkyZoneInfoPropertyKind.LensFlareScale:
                    if (tag.Type == PackageReader.PropertyTypeArray)
                    {
                        lensFlareScale.AddRange(reader.ReadFloatArrayProperty(tag, className, exportIndex, objectName));
                        return;
                    }

                    SetLensFlareScale(tag.ArrayIndex, reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? 0f);
                    return;
                case UnrSkyZoneInfoPropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.bLightChanged:
                    lightChanged = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.Region:
                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrSkyZoneInfoPropertyKind.TexModifyInfo:
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
        return new UnrSkyZoneInfoObject
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
            TexUPanSpeed = texUPanSpeed,
            TexVPanSpeed = texVPanSpeed,
            LensFlare = lensFlare.ToArray(),
            LensFlareOffset = lensFlareOffset.ToArray(),
            LensFlareScale = lensFlareScale.ToArray(),
            DynamicActorFilterState = dynamicActorFilterState,
            LightChanged = lightChanged,
            Region = region,
            TexModifyInfo = texModifyInfo,
            UnknownProperties = unknownProperties.ToArray()
        };
    }

    private static bool ReadStrictBool(StreamPropertyTag tag, string className, int exportIndex, string objectName)
    {
        return tag.ThrowIfNotTagEncodedBool(
            $"{className} export {exportIndex} ({objectName}) has invalid bool payload in '{tag.Name}'.");
    }

    private enum UnrSkyZoneInfoPropertyKind
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
        TexUPanSpeed,
        TexVPanSpeed,
        LensFlare,
        LensFlareOffset,
        LensFlareScale,
        bDynamicActorFilterState,
        bLightChanged,
        TexModifyInfo
    }
}
