namespace L2Viewer.PackageCore;

public static class UnrPackageHelpers
{
    public const uint ObjectFlagHasStack = 0x02000000;


    public static (string ClassName, string ObjectName, string? PackageName)? ResolveObjectRef(PackageData package, int reference)
    {
        if (reference < 0)
        {
            var index = -reference - 1;
            if (index < 0 || index >= package.Imports.Count)
            {
                return null;
            }

            var importEntry = package.Imports[index];
            var className = PackageReader.SafeName(package.Names, importEntry.ClassName);
            var objectName = PackageReader.SafeName(package.Names, importEntry.ObjectName);
            return (className, objectName, ResolveImportPackage(package, index));
        }

        if (reference > 0)
        {
            var index = reference - 1;
            if (index < 0 || index >= package.Exports.Count)
            {
                return null;
            }

            var exportEntry = package.Exports[index];
            return (PackageReader.ExportClassName(package, exportEntry), PackageReader.SafeName(package.Names, exportEntry.ObjectName), null);
        }

        return null;
    }

    public static string? ResolveImportPackage(PackageData package, int importIndex)
    {
        var packageIndex = unchecked((int)package.Imports[importIndex].PackageIndex);
        string? lastPackage = null;
        while (packageIndex < 0)
        {
            var idx = -packageIndex - 1;
            if (idx < 0 || idx >= package.Imports.Count)
            {
                break;
            }

            var parent = package.Imports[idx];
            var className = PackageReader.SafeName(package.Names, parent.ClassName);
            if (string.Equals(className, "Package", StringComparison.OrdinalIgnoreCase))
            {
                lastPackage = PackageReader.SafeName(package.Names, parent.ObjectName);
            }

            packageIndex = unchecked((int)parent.PackageIndex);
        }

        return lastPackage;
    }

}

public sealed record MapActorProps(
    string ClassName,
    string Name,
    Vector3? Location,
    Vector3? Rotation,
    float DrawScale,
    Vector3 DrawScale3D,
    Vector3 PrePivot,
    (string ClassName, string ObjectName, string? PackageName)? StaticMeshRef);

public sealed record UnrParsedProperty(
    string Name,
    int Type,
    int ArrayIndex,
    string? StructName,
    int PayloadOffset,
    int PayloadSize);
