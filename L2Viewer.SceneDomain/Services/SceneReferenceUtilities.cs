using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public static class SceneReferenceUtilities
{
    public static string BuildReference(string mapPath, UnrFileObjectReference reference)
    {
        if (reference is null)
        {
            throw new ArgumentNullException(nameof(reference));
        }

        return BuildReference(mapPath, reference.PackageName, reference.ObjectName);
    }

    public static string BuildReference(string mapPath, string? packageName, string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new PackageReadException("Object reference has no object name.");
        }

        var mapStem = Path.GetFileNameWithoutExtension(mapPath);
        if (string.IsNullOrWhiteSpace(packageName) ||
            string.Equals(packageName, mapStem, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(mapStem))
            {
                throw new PackageReadException($"Unable to build canonical reference for '{objectName}' because map path '{mapPath}' has no file name.");
            }

            return $"{mapStem}.{objectName}";
        }

        return $"{packageName}.{objectName}";
    }

    public static string? BuildOptionalReference(string mapPath, UnrFileObjectReference? reference)
    {
        return reference is null ? null : BuildReference(mapPath, reference);
    }

    public static SceneResourceLocation BuildResourceLocation(
        string clientRoot,
        string packagePath,
        string packageName,
        string objectName,
        string className)
    {
        var reference = $"{packageName}.{objectName}";
        var fullPackagePath = Path.GetFullPath(packagePath);
        var fullClientRoot = Path.GetFullPath(clientRoot);
        var relativePath = Path.GetRelativePath(fullClientRoot, fullPackagePath)
            .Replace('\\', '/');
        var uri = $"l2://{Uri.EscapeDataString(relativePath)}#{Uri.EscapeDataString(objectName)}?class={Uri.EscapeDataString(className)}";

        return new SceneResourceLocation
        {
            Reference = reference,
            ClassName = className,
            PackageName = packageName,
            ObjectName = objectName,
            PackagePath = fullPackagePath,
            ClientRelativePath = relativePath,
            Uri = uri
        };
    }

    public static SceneResourceLocation BuildMapResourceLocation(string clientRoot, string mapPath, string objectName, string className)
    {
        var packageName = Path.GetFileNameWithoutExtension(mapPath);
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new PackageReadException($"Unable to build resource location for '{objectName}' because map path '{mapPath}' has no file name.");
        }

        return BuildResourceLocation(clientRoot, mapPath, packageName, objectName, className);
    }

    public static SceneResourceReference BuildFromDbResourceReference(string reference, string className)
    {
        var parsed = ParseFromDbResourceReference(reference);
        return new SceneResourceReference
        {
            Reference = reference,
            ClassName = className,
            PackageName = parsed.PackageName,
            ObjectName = parsed.ObjectName
        };
    }

    public static (string PackageName, string ObjectName) ParseFromDbResourceReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new PackageReadException("Resource reference is empty.");
        }

        var parts = reference.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new PackageReadException($"Resource reference '{reference}' is not in 'Package.Object' form.");
        }

        var packageName = parts[0].Trim();
        var objectName = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(objectName))
        {
            throw new PackageReadException($"Resource reference '{reference}' is not in 'Package.Object' form.");
        }

        return (packageName, objectName);
    }
}
