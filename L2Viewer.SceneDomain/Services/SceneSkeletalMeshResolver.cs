using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

[ForExternalUse]
public sealed class SceneSkeletalMeshResolver
{
    private readonly SceneSkeletalAssetBuilder _assetBuilder = new();
    private readonly object _sync = new();
    private readonly Dictionary<string, SceneSkeletalPreviewData> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SceneSkeletalAsset ResolveAssetExport(string packagePath, int exportIndex)
    {
        return _assetBuilder.BuildExport(packagePath, exportIndex);
    }

    public SceneSkeletalAsset ResolveAssetNamed(string packagePath, string meshName)
    {
        return _assetBuilder.BuildNamed(packagePath, meshName);
    }

    public SceneSkeletalPreviewData ResolveExport(string packagePath, int exportIndex)
    {
        var key = $"{Path.GetFullPath(packagePath)}#{exportIndex}";
        lock (_sync)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var resolved = BuildPreview(_assetBuilder.BuildExport(packagePath, exportIndex));
        lock (_sync)
        {
            _cache[key] = resolved;
        }

        return resolved;
    }

    public SceneSkeletalPreviewData ResolveNamed(string packagePath, string meshName)
    {
        var asset = _assetBuilder.BuildNamed(packagePath, meshName);
        return ResolveExport(packagePath, asset.MeshExportIndex);
    }

    private static SceneSkeletalPreviewData BuildPreview(SceneSkeletalAsset asset)
    {
        var session = ActorXSkeletalAnimationPreviewSession.Create(asset);

        return new SceneSkeletalPreviewData(
            asset.MeshObjectName,
            asset.AnimationObjectName,
            asset.Source,
            asset.Details,
            asset,
            session);
    }
}
