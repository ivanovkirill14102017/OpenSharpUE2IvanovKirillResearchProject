using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UkxFile;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

internal sealed class SceneSkeletalResourceCatalog
{
    private readonly IReadOnlyDictionary<string, string> _packageIndex;
    private readonly Dictionary<string, UkxSkeletalMeshObject[]> _meshesByPackagePath =
        new(StringComparer.OrdinalIgnoreCase);

    public SceneSkeletalResourceCatalog(string clientRoot)
    {
        _packageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);
    }

    public SceneResourceReference[] FindMissing(IEnumerable<SceneResourceReference> references)
    {
        return references.Where(reference => !Contains(reference)).ToArray();
    }

    private bool Contains(SceneResourceReference reference)
    {
        if (!_packageIndex.TryGetValue(reference.PackageName, out var packagePath) ||
            !packagePath.EndsWith(".ukx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!_meshesByPackagePath.TryGetValue(packagePath, out var meshes))
        {
            meshes = UkxFileReader.Read(packagePath)
                .ExportObjects
                .Select(x => x.Object)
                .OfType<UkxSkeletalMeshObject>()
                .ToArray();
            _meshesByPackagePath[packagePath] = meshes;
        }

        return meshes.Any(mesh => mesh.ObjectName.Is(reference.ObjectName));
    }
}
