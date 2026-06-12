using System;
using System.Collections.Generic;
using System.Numerics;

namespace L2Viewer.UnrFile;

internal static class UnrNMoonObjectReader
{
    public static UnrNMoonObject Read(
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
        float? radius = null;
        var dynamicActorFilterState = false;
        var lightChanged = false;
        var sunAffect = false;
        var skins = new List<UnrFileObjectReference>();
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void EnsureIndexedCapacity<T>(List<T> values, int index, T filler)
        {
            while (values.Count <= index)
            {
                values.Add(filler);
            }
        }

        void SetSkin(int index, UnrFileObjectReference value)
        {
            EnsureIndexedCapacity(skins, index, value);
            skins[index] = value;
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
            if (!Enum.TryParse<UnrNMoonPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrNMoonPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrNMoonPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrNMoonPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrNMoonPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Event:
                    eventName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Brush:
                    brushReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Base:
                    baseReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Level:
                    levelReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Owner:
                    ownerReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Mesh:
                    meshReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Texture:
                    textureReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Radius:
                    radius = reader.ReadFloatProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.bDynamicActorFilterState:
                    dynamicActorFilterState = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.bLightChanged:
                    lightChanged = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.bSunAffect:
                    sunAffect = ReadStrictBool(tag, className, exportIndex, objectName);
                    return;
                case UnrNMoonPropertyKind.Skins:
                    if (tag.Type == PackageReader.PropertyTypeArray)
                    {
                        skins.AddRange(reader.ReadObjectReferenceArrayProperty(package, tag, className, exportIndex, objectName));
                        return;
                    }

                    SetSkin(tag.ArrayIndex, reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName)!);
                    return;
                case UnrNMoonPropertyKind.Region:
                case UnrNMoonPropertyKind.TexModifyInfo:
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
        return new UnrNMoonObject
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
            Radius = radius,
            Skins = skins.ToArray(),
            DynamicActorFilterState = dynamicActorFilterState,
            LightChanged = lightChanged,
            SunAffect = sunAffect,
            UnknownProperties = unknownProperties.ToArray()
        };
    }

    private static bool ReadStrictBool(StreamPropertyTag tag, string className, int exportIndex, string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeBool || tag.DataSize != 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid bool payload in '{tag.Name}'.");
        }

        return tag.BoolValue;
    }

    private enum UnrNMoonPropertyKind
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
        Radius,
        Skins,
        bDynamicActorFilterState,
        bLightChanged,
        bSunAffect,
        TexModifyInfo
    }
}
