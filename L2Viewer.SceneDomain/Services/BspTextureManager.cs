using L2Viewer.SceneDomain.Models;
using L2Viewer.UtxFile;

namespace L2Viewer.SceneDomain.Services;

public sealed class BspTextureManager
{
    private readonly string _clientRoot;
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _packageIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ResolvedTexture> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingTextureCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _packageIndexBuilt;

    public BspTextureManager(string clientRoot)
    {
        _clientRoot = clientRoot;
    }

    public IReadOnlyDictionary<string, ResolvedTexture> ResolveMany(IEnumerable<SceneTextureRequest> requests)
    {
        var keys = requests
            .Where(x => !string.IsNullOrWhiteSpace(x.PackageName) && !string.IsNullOrWhiteSpace(x.ObjectName))
            .Select(x => $"{x.PackageName}.{x.ObjectName}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var result = new Dictionary<string, ResolvedTexture>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            var split = key.Split('.', 2);
            var resolved = ResolveSingle(split[0], split[1]);
            if (resolved is not null)
            {
                result[key] = resolved;
            }
        }

        return result;
    }

    private ResolvedTexture? ResolveSingle(string packageName, string objectName)
    {
        var cacheKey = $"{packageName}.{objectName}";
        lock (_sync)
        {
            if (_textureCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            if (_missingTextureCache.Contains(cacheKey))
            {
                return null;
            }
        }

        EnsurePackageIndex();
        if (!_packageIndex.TryGetValue(packageName, out var packagePath))
        {
            lock (_sync)
            {
                _missingTextureCache.Add(cacheKey);
            }

            return null;
        }

        var probe = TextureCodec.ProbeTextureFromPackage(packagePath, objectName);
        if (probe.Texture is null)
        {
            lock (_sync)
            {
                _missingTextureCache.Add(cacheKey);
            }

            return null;
        }

        var resolved = new ResolvedTexture(
            probe.Texture,
            SceneReferenceUtilities.BuildResourceLocation(_clientRoot, packagePath, packageName, objectName, "Texture"));

        lock (_sync)
        {
            _textureCache[cacheKey] = resolved;
        }

        return resolved;
    }

    private void EnsurePackageIndex()
    {
        lock (_sync)
        {
            if (_packageIndexBuilt)
            {
                return;
            }

            IndexPackagesFromDirectory(Path.Combine(_clientRoot, "Textures"), "*.utx", recursive: true);
            IndexPackagesFromDirectory(Path.Combine(_clientRoot, "SysTextures"), "*.utx", recursive: true);
            IndexPackagesFromDirectory(Path.Combine(_clientRoot, "Maps"), "*.utx", recursive: true);
            IndexPackagesFromDirectory(_clientRoot, "*.utx", recursive: false);
            _packageIndexBuilt = true;
        }
    }

    private void IndexPackagesFromDirectory(string root, string searchPattern, bool recursive)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var path in Directory.EnumerateFiles(root, searchPattern, searchOption))
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            if (!_packageIndex.ContainsKey(stem))
            {
                _packageIndex[stem] = path;
            }
        }
    }

    public sealed record ResolvedTexture(TextureData Texture, SceneResourceLocation Resource);
}

public sealed record SceneTextureRequest(string PackageName, string ObjectName);
