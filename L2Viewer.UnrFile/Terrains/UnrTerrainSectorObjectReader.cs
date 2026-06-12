namespace L2Viewer.UnrFile;

internal static class UnrTerrainSectorObjectReader
{
    public static UnrTerrainSectorObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        reader.ReadStateFrameIfPresent(export.ObjectFlags);

        var tag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
        if (!tag.IsTerminator)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) expected no tagged properties before native payload.");
        }

        var unknownInt1 = reader.ReadInt32();
        var unknownInt2 = reader.ReadInt32();
        var unknownInt3 = reader.ReadInt32();
        var unknownInt4 = reader.ReadInt32();
        var boundsMin = reader.ReadVector3();
        var boundsMax = reader.ReadVector3();
        var unknownShort1 = reader.ReadUInt16();
        var unknownShort2 = reader.ReadUInt16();
        var unknownInt5 = reader.ReadInt32();
        var nativePayloadSize = checked((int)(reader.BaseStream.Length - reader.BaseStream.Position));

        return new UnrTerrainSectorObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            UnknownInt1 = unknownInt1,
            UnknownInt2 = unknownInt2,
            UnknownInt3 = unknownInt3,
            UnknownInt4 = unknownInt4,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            UnknownShort1 = unknownShort1,
            UnknownShort2 = unknownShort2,
            UnknownInt5 = unknownInt5,
            NativePayloadSize = nativePayloadSize,
            UnknownProperties = []
        };
    }
}
