using L2Viewer.SceneDomain.Models;
using L2Viewer.UsxFile;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneMaterialResolver
{
    private readonly string _clientRoot;
    private readonly BspTextureManager _textureManager;
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _packageIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ResolvedMaterialGraph?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _indexBuilt;

    public SceneMaterialResolver(string clientRoot, BspTextureManager textureManager)
    {
        _clientRoot = clientRoot;
        _textureManager = textureManager;
    }

    public string ClientRoot => _clientRoot;

    public IReadOnlyDictionary<string, ResolvedMaterialGraph?> ResolveMany(string mapPath, IEnumerable<SceneMaterialRequest> requests)
    {
        var result = new Dictionary<string, ResolvedMaterialGraph?>(StringComparer.OrdinalIgnoreCase);
        foreach (var request in requests
                     .Where(x => !string.IsNullOrWhiteSpace(x.ObjectName))
                     .Distinct())
        {
            var key = SceneReferenceUtilities.BuildReference(mapPath, request.PackageName, request.ObjectName);
            result[key] = ResolveSingle(mapPath, request.PackageName, request.ObjectName);
        }

        return result;
    }

    private ResolvedMaterialGraph? ResolveSingle(string mapPath, string? packageName, string objectName)
    {
        var packageKey = string.IsNullOrWhiteSpace(packageName)
            ? Path.GetFileNameWithoutExtension(mapPath)
            : packageName;
        var cacheKey = $"{packageKey}.{objectName}";
        lock (_sync)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
        }

        EnsureIndex();
        string? packagePath;
        if (string.IsNullOrWhiteSpace(packageName) ||
            string.Equals(packageName, Path.GetFileNameWithoutExtension(mapPath), StringComparison.OrdinalIgnoreCase))
        {
            packagePath = mapPath;
        }
        else if (!_packageIndex.TryGetValue(packageName, out packagePath))
        {
            packagePath = null;
        }

        var material = packagePath is null
            ? null
            : MaterialGraphResolver.ResolveMaterialGraph(
                packagePath,
                objectName,
                ResolveTexture,
                _packageIndex);

        lock (_sync)
        {
            _cache[cacheKey] = material;
        }

        return material;
    }

    private TextureData? ResolveTexture(string packageName, string objectName)
    {
        return _textureManager.ResolveMany([new SceneTextureRequest(packageName, objectName)])
            .TryGetValue($"{packageName}.{objectName}", out var resolved)
            ? resolved.Texture
            : null;
    }

    private void EnsureIndex()
    {
        lock (_sync)
        {
            if (_indexBuilt)
            {
                return;
            }

            foreach (var pair in ScenePackageIndexer.BuildResourcePackageIndex(_clientRoot))
            {
                _packageIndex[pair.Key] = pair.Value;
            }

            _indexBuilt = true;
        }
    }
}

public sealed record SceneMaterialRequest(string? PackageName, string ObjectName);
