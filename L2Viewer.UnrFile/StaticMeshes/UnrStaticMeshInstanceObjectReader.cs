namespace L2Viewer.UnrFile;

internal static class UnrStaticMeshInstanceObjectReader
{
    public static UnrStaticMeshInstanceObject Read(
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

        var remaining = checked((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        var prefixBytes = reader.ReadBytes(Math.Min(remaining, 32));
        if (prefixBytes.Length != Math.Min(remaining, 32))
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) hit EOF while reading native prefix.");
        }

        var tail = reader.ReadBytes(remaining - prefixBytes.Length);
        if (tail.Length != remaining - prefixBytes.Length)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) hit EOF while reading native payload.");
        }

        return new UnrStaticMeshInstanceObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            NativePayloadSize = remaining,
            UnknownPrefixBytes = prefixBytes,
            UnknownProperties = []
        };
    }
}
