namespace L2Viewer.SceneDomain.Models;

public sealed class SceneResourceId : IEquatable<SceneResourceId>
{
    private SceneResourceId(string packageName, string objectPath)
    {
        PackageName = packageName;
        ObjectPath = objectPath;
    }

    public string PackageName { get; }
    public string ObjectPath { get; }

    public static SceneResourceId Create(string packageName, string objectPath)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new FormatException("Resource package name is empty.");
        }

        if (string.IsNullOrWhiteSpace(objectPath))
        {
            throw new FormatException("Resource object path is empty.");
        }

        return new SceneResourceId(packageName.Trim(), objectPath.Trim());
    }

    public static SceneResourceId Parse(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new FormatException("Resource reference is empty.");
        }

        var separator = reference.IndexOf('.');
        if (separator <= 0 || separator >= reference.Length - 1)
        {
            throw new FormatException($"Resource reference '{reference}' must have the form Package.ObjectPath.");
        }

        return Create(reference.Substring(0, separator), reference.Substring(separator + 1));
    }

    public bool Equals(SceneResourceId? other)
    {
        return other is not null &&
               StringComparer.OrdinalIgnoreCase.Equals(PackageName, other.PackageName) &&
               StringComparer.OrdinalIgnoreCase.Equals(ObjectPath, other.ObjectPath);
    }

    public override bool Equals(object? obj)
    {
        return obj is SceneResourceId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(PackageName),
            StringComparer.OrdinalIgnoreCase.GetHashCode(ObjectPath));
    }

    public override string ToString()
    {
        return $"{PackageName}.{ObjectPath}";
    }
}

public abstract record SceneTypedResourceReference(SceneResourceId Id)
{
    public override string ToString() => Id.ToString();
}

public sealed record SceneSurfaceResourceReference(SceneResourceId Id) : SceneTypedResourceReference(Id)
{
    public override string ToString() => Id.ToString();
    public static SceneSurfaceResourceReference Parse(string reference) => new(SceneResourceId.Parse(reference));
    public static SceneSurfaceResourceReference Create(string packageName, string objectPath) =>
        new(SceneResourceId.Create(packageName, objectPath));
}

public sealed record SceneSkeletalMeshResourceReference(SceneResourceId Id) : SceneTypedResourceReference(Id)
{
    public override string ToString() => Id.ToString();
    public static SceneSkeletalMeshResourceReference Parse(string reference) => new(SceneResourceId.Parse(reference));
    public static SceneSkeletalMeshResourceReference Create(string packageName, string objectPath) =>
        new(SceneResourceId.Create(packageName, objectPath));
}

public sealed record SceneStaticMeshResourceReference(SceneResourceId Id) : SceneTypedResourceReference(Id)
{
    public override string ToString() => Id.ToString();
    public static SceneStaticMeshResourceReference Parse(string reference) => new(SceneResourceId.Parse(reference));
}

public sealed record SceneVertexMeshResourceReference(SceneResourceId Id) : SceneTypedResourceReference(Id)
{
    public override string ToString() => Id.ToString();
    public static SceneVertexMeshResourceReference Parse(string reference) => new(SceneResourceId.Parse(reference));
}

public sealed record SceneParticleResourceReference(SceneResourceId Id) : SceneTypedResourceReference(Id)
{
    public override string ToString() => Id.ToString();
    public static SceneParticleResourceReference Parse(string reference) => new(SceneResourceId.Parse(reference));
}

public sealed record SceneAnimationResourceReference(SceneResourceId Id) : SceneTypedResourceReference(Id)
{
    public override string ToString() => Id.ToString();
    public static SceneAnimationResourceReference Parse(string reference) => new(SceneResourceId.Parse(reference));
}
