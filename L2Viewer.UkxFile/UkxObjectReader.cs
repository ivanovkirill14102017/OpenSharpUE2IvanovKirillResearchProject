using L2Viewer.PackageCore;

namespace L2Viewer.UkxFile;

internal static class UkxObjectReader
{
    internal static UkxFileObject ReadObject(
        PackageData package,
        ExportEntry exportEntry,
        int exportIndex,
        string className,
        string objectName)
    {
        if (className.Is(UnrealClassNames.SkeletalMesh))
        {
            return UkxSkeletalMeshObjectReader.Read(package, exportEntry, exportIndex, className, objectName);
        }

        if (className.Is(UnrealClassNames.MeshAnimation) ||
            className.Is(UnrealClassNames.Animation))
        {
            return UkxMeshAnimationObjectReader.Read(package, exportEntry, exportIndex, className, objectName);
        }

        return ReadGenericObject(package, exportEntry, exportIndex, className, objectName);
    }

    private static UkxGenericObject ReadGenericObject(
        PackageData package,
        ExportEntry exportEntry,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, exportEntry);
        UkxExportReaderHelpers.SkipObjectStackIfPresent(reader, exportEntry);
        var unknownProperties = UkxPropertyReader.ReadUnknownProperties(package, reader, className, exportIndex, objectName);
        var tail = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        return new UkxGenericObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            UnknownProperties = unknownProperties,
            UnknownNativePayloadBytes = tail
        };
    }
}
