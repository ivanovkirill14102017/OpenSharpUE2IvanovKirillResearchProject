using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;
using L2Viewer.UtxFile;
using L2Viewer.UsxFile;
using L2Viewer.SceneDomain.Services.MaterialServices;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.BSPServices;

public sealed class BspStaticMeshManager
{
    private readonly string _clientRoot;
    private readonly BspTextureManager _textureManager;
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _staticMeshIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _texturePackageIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _packageIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MeshData> _meshCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _indexesBuilt;

    public BspStaticMeshManager(string clientRoot, BspTextureManager textureManager)
    {
        _clientRoot = clientRoot;
        _textureManager = textureManager;
    }

    public IReadOnlyDictionary<string, MeshData> ResolveMany(string mapPath, IEnumerable<UnrFileObjectReference> staticMeshReferences)
    {
        var result = new Dictionary<string, MeshData>(StringComparer.OrdinalIgnoreCase);
        foreach (var staticMeshReference in staticMeshReferences
                     .GroupBy(x => SceneReferenceUtilities.BuildReference(mapPath, x), StringComparer.OrdinalIgnoreCase)
                     .Select(x => x.First()))
        {
            var key = SceneReferenceUtilities.BuildReference(mapPath, staticMeshReference);
            result[key] = ResolveSingle(mapPath, staticMeshReference);
        }

        return result;
    }

    private MeshData ResolveSingle(string mapPath, UnrFileObjectReference staticMeshReference)
    {
        var referenceKey = SceneReferenceUtilities.BuildReference(mapPath, staticMeshReference);
        lock (_sync)
        {
            if (_meshCache.TryGetValue(referenceKey, out var cached))
            {
                return cached ?? throw new PackageReadException($"Static mesh '{referenceKey}' previously failed to resolve.");
            }
        }

        EnsureIndexes();
        MeshData? mesh = null;
        if (string.IsNullOrWhiteSpace(staticMeshReference.PackageName) ||
            string.Equals(staticMeshReference.PackageName, Path.GetFileNameWithoutExtension(mapPath), StringComparison.OrdinalIgnoreCase))
        {
            mesh = StaticMeshCodec.LoadStaticMeshNamed(
                mapPath,
                staticMeshReference.ObjectName,
                _texturePackageIndex,
                ResolveTexture,
                _packageIndex).Mesh;
        }
        else
        {
            if (!_staticMeshIndex.TryGetValue(staticMeshReference.PackageName, out var meshPackagePath))
            {
                throw new PackageReadException($"Static mesh package '{staticMeshReference.PackageName}' was not found for '{referenceKey}'.");
            }

            mesh = StaticMeshCodec.LoadStaticMeshNamed(
                meshPackagePath,
                staticMeshReference.ObjectName,
                _texturePackageIndex,
                ResolveTexture,
                _packageIndex).Mesh;
        }

        if (mesh is null)
        {
            throw new PackageReadException($"Static mesh '{referenceKey}' could not be decoded.");
        }

        lock (_sync)
        {
            _meshCache[referenceKey] = mesh;
        }

        return mesh;
    }

    private TextureData? ResolveTexture(string packageName, string objectName)
    {
        return _textureManager.ResolveMany([new SceneTextureRequest(packageName, objectName)])
            .TryGetValue($"{packageName}.{objectName}", out var resolved)
            ? resolved.Texture
            : null;
    }

    private void EnsureIndexes()
    {
        lock (_sync)
        {
            if (_indexesBuilt)
            {
                return;
            }

            foreach (var pair in ScenePackageIndexer.BuildStaticMeshPackageIndex(_clientRoot))
            {
                _staticMeshIndex[pair.Key] = pair.Value;
            }

            foreach (var pair in ScenePackageIndexer.BuildTexturePackageIndex(_clientRoot))
            {
                _texturePackageIndex[pair.Key] = pair.Value;
            }

            foreach (var pair in ScenePackageIndexer.BuildResourcePackageIndex(_clientRoot))
            {
                _packageIndex[pair.Key] = pair.Value;
            }

            _indexesBuilt = true;
        }
    }
}
