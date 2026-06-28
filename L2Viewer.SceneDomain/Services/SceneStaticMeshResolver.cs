using L2Viewer.SceneDomain.Models;
using L2Viewer.UsxFile;
using System.Collections.Concurrent;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneStaticMeshResolver
{
    private readonly string _clientRoot;
    private readonly BspTextureManager _textureManager;
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _staticMeshIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _texturePackageIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _packageIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SceneStaticMeshDefinition> _assetCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _indexesBuilt;

    public SceneStaticMeshResolver(string clientRoot, BspTextureManager textureManager)
    {
        _clientRoot = clientRoot;
        _textureManager = textureManager;
    }

    public IReadOnlyDictionary<string, SceneStaticMeshDefinition> ResolveMany(string mapPath, IEnumerable<UnrFileObjectReference> staticMeshReferences)
    {
        var uniqueReferences = staticMeshReferences
            .GroupBy(x => SceneReferenceUtilities.BuildReference(mapPath, x), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();
        var result = new ConcurrentDictionary<string, SceneStaticMeshDefinition>(StringComparer.OrdinalIgnoreCase);

        Parallel.ForEach(uniqueReferences, staticMeshReference =>
        {
            var key = SceneReferenceUtilities.BuildReference(mapPath, staticMeshReference);
            result[key] = ResolveSingle(mapPath, staticMeshReference);
        });

        return result
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, SceneResourceLocation> ResolveResourceLocations(string mapPath, IEnumerable<UnrFileObjectReference> references)
    {
        var result = new Dictionary<string, SceneResourceLocation>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in references
                     .GroupBy(x => SceneReferenceUtilities.BuildReference(mapPath, x), StringComparer.OrdinalIgnoreCase)
                     .Select(x => x.First()))
        {
            result[SceneReferenceUtilities.BuildReference(mapPath, reference)] = ResolveResourceLocationSingle(mapPath, reference);
        }

        return result;
    }

    private SceneStaticMeshDefinition ResolveSingle(string mapPath, UnrFileObjectReference staticMeshReference)
    {
        var referenceKey = SceneReferenceUtilities.BuildReference(mapPath, staticMeshReference);
        lock (_sync)
        {
            if (_assetCache.TryGetValue(referenceKey, out var cached))
            {
                return cached ?? throw new PackageReadException($"Static mesh asset '{referenceKey}' previously failed to resolve.");
            }
        }

        EnsureIndexes();
        MeshDecodeDiagnostic diagnostic;
        if (string.IsNullOrWhiteSpace(staticMeshReference.PackageName) ||
            string.Equals(staticMeshReference.PackageName, Path.GetFileNameWithoutExtension(mapPath), StringComparison.OrdinalIgnoreCase))
        {
            diagnostic = StaticMeshCodec.LoadStaticMeshNamed(
                mapPath,
                staticMeshReference.ObjectName,
                _texturePackageIndex,
                ResolveTexture,
                _packageIndex);
        }
        else
        {
            if (!_staticMeshIndex.TryGetValue(staticMeshReference.PackageName, out var meshPackagePath))
            {
                throw new PackageReadException($"Static mesh package '{staticMeshReference.PackageName}' was not found for '{referenceKey}'.");
            }

            diagnostic = StaticMeshCodec.LoadStaticMeshNamed(
                meshPackagePath,
                staticMeshReference.ObjectName,
                _texturePackageIndex,
                ResolveTexture,
                _packageIndex);
        }

        if (diagnostic.Mesh is null)
        {
            throw new PackageReadException($"Static mesh '{referenceKey}' could not be decoded.");
        }

        var materials = ResolveMaterials(unrOrMapPath: mapPath, staticMeshReference, referenceKey);
        var meshResource = BuildMeshResource(mapPath, staticMeshReference);
        var materialResources = materials.Select(x => x is null ? null : BuildMaterialResource(x)).ToArray();
        var renderGeometry = ConvertGeometry(diagnostic.Mesh);
        var collisionGeometry = diagnostic.Collision?.Mesh is null ? null : ConvertGeometry(diagnostic.Collision.Mesh);
        var subMeshes = BuildSubMeshes(materials, materialResources, diagnostic.Mesh);
        var asset = new SceneStaticMeshDefinition
        {
            Reference = referenceKey,
            MeshResource = meshResource,
            RenderGeometry = renderGeometry,
            CollisionGeometry = collisionGeometry,
            CollisionInfo = diagnostic.CollisionInfo,
            SubMeshes = subMeshes
        };

        lock (_sync)
        {
            _assetCache[referenceKey] = asset;
        }

        return asset;
    }

    private static SceneTriangleMeshData ConvertGeometry(MeshData mesh)
    {
        return new SceneTriangleMeshData
        {
            Name = mesh.Name,
            Triangles = mesh.Triangles,
            BoundsMin = mesh.BBoxMin,
            BoundsMax = mesh.BBoxMax,
            SourceNote = mesh.SourceNote
        };
    }

    private IReadOnlyList<SceneStaticMeshSubMeshDefinition> BuildSubMeshes(
        IReadOnlyList<ResolvedMaterialGraph?> materials,
        IReadOnlyList<SceneResourceLocation?> materialResources,
        MeshData renderMesh)
    {
        var triangleCountsByMaterialId = renderMesh.Triangles
            .GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.Count());
        var orderedMaterialIds = triangleCountsByMaterialId.Keys
            .OrderBy(x => x)
            .ToArray();
        var subMeshes = new List<SceneStaticMeshSubMeshDefinition>(orderedMaterialIds.Length);

        foreach (var materialId in orderedMaterialIds)
        {
            var material = materialId >= 0 && materialId < materials.Count ? materials[materialId] : null;
            var orderedTextureSlots = material is null
                ? []
                : MaterialTextureSlotOrdering.OrderTextureSlots(material.TextureSlots);
            var textureReferences = orderedTextureSlots
                .Select(x => x.Reference)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var textureResources = orderedTextureSlots
                .Where(x => !string.IsNullOrWhiteSpace(x.PackagePath))
                .GroupBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
                .Select(x => SceneReferenceUtilities.BuildResourceLocation(
                    _clientRoot,
                    x.First().PackagePath!,
                    x.First().PackageName,
                    x.First().ObjectName,
                    x.First().ClassName))
                .ToArray();

            subMeshes.Add(new SceneStaticMeshSubMeshDefinition
            {
                MaterialId = materialId,
                TriangleCount = triangleCountsByMaterialId[materialId],
                MaterialReference = material?.RootReference,
                MaterialResource = materialId >= 0 && materialId < materialResources.Count ? materialResources[materialId] : null,
                Material = material,
                BlendModeHint = material?.GetKnownTraits().BlendModeHint ?? MaterialBlendModeHint.Unknown,
                PrimaryTextureReference = textureReferences.FirstOrDefault(),
                PrimaryTextureResource = textureResources.FirstOrDefault(),
                TextureReferences = textureReferences,
                TextureResources = textureResources
            });
        }

        return subMeshes;
    }

    private TextureData? ResolveTexture(string packageName, string objectName)
    {
        return _textureManager.ResolveMany([new SceneTextureRequest(packageName, objectName)])
            .TryGetValue($"{packageName}.{objectName}", out var resolved)
            ? resolved.Texture
            : null;
    }

    private SceneResourceLocation ResolveResourceLocationSingle(string mapPath, UnrFileObjectReference reference)
    {
        EnsureIndexes();

        if (string.IsNullOrWhiteSpace(reference.PackageName) ||
            string.Equals(reference.PackageName, Path.GetFileNameWithoutExtension(mapPath), StringComparison.OrdinalIgnoreCase))
        {
            return SceneReferenceUtilities.BuildMapResourceLocation(_clientRoot, mapPath, reference.ObjectName, reference.ClassName);
        }

        if (!_packageIndex.TryGetValue(reference.PackageName, out var packagePath))
        {
            throw new PackageReadException($"Package '{reference.PackageName}' was not found while building resource path for '{reference.ObjectName}'.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            _clientRoot,
            packagePath,
            reference.PackageName,
            reference.ObjectName,
            reference.ClassName);
    }

    private SceneResourceLocation BuildMeshResource(string mapPath, UnrFileObjectReference staticMeshReference)
    {
        if (string.IsNullOrWhiteSpace(staticMeshReference.PackageName) ||
            string.Equals(staticMeshReference.PackageName, Path.GetFileNameWithoutExtension(mapPath), StringComparison.OrdinalIgnoreCase))
        {
            return SceneReferenceUtilities.BuildMapResourceLocation(_clientRoot, mapPath, staticMeshReference.ObjectName, staticMeshReference.ClassName);
        }

        if (!_staticMeshIndex.TryGetValue(staticMeshReference.PackageName, out var meshPackagePath))
        {
            throw new PackageReadException($"Static mesh package '{staticMeshReference.PackageName}' was not found while building resource path.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            _clientRoot,
            meshPackagePath,
            staticMeshReference.PackageName,
            staticMeshReference.ObjectName,
            staticMeshReference.ClassName);
    }

    private SceneResourceLocation BuildMaterialResource(ResolvedMaterialGraph material)
    {
        var rootNode = material.Nodes.FirstOrDefault(x => string.Equals(x.Reference, material.RootReference, StringComparison.OrdinalIgnoreCase))
            ?? material.Nodes.FirstOrDefault()
            ?? throw new PackageReadException($"Material '{material.RootReference}' has no graph nodes.");
        if (string.IsNullOrWhiteSpace(rootNode.PackagePath))
        {
            throw new PackageReadException($"Material '{material.RootReference}' has no package path.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            _clientRoot,
            rootNode.PackagePath,
            material.RootPackageName,
            material.RootObjectName,
            material.RootClassName);
    }

    private SceneResourceLocation? ResolvePrimaryTextureResource(IReadOnlyList<ResolvedMaterialGraph?> materials, string? textureReference)
    {
        if (string.IsNullOrWhiteSpace(textureReference))
        {
            return null;
        }

        foreach (var material in materials)
        {
            if (material is null)
            {
                continue;
            }

            var slot = MaterialTextureSlotOrdering.OrderTextureSlots(material.TextureSlots)
                .FirstOrDefault(x => string.Equals(x.Reference, textureReference, StringComparison.OrdinalIgnoreCase));
            if (slot is not null && !string.IsNullOrWhiteSpace(slot.PackagePath))
            {
                return SceneReferenceUtilities.BuildResourceLocation(
                    _clientRoot,
                    slot.PackagePath,
                    slot.PackageName,
                    slot.ObjectName,
                    slot.ClassName);
            }
        }

        return null;
    }

    private IReadOnlyList<ResolvedMaterialGraph?> ResolveMaterials(string unrOrMapPath, UnrFileObjectReference staticMeshReference, string referenceKey)
    {
        EnsureIndexes();
        if (string.IsNullOrWhiteSpace(staticMeshReference.PackageName) ||
            string.Equals(staticMeshReference.PackageName, Path.GetFileNameWithoutExtension(unrOrMapPath), StringComparison.OrdinalIgnoreCase))
        {
            var localMaterials = StaticMeshCodec.ResolveStaticMeshMaterials(
                unrOrMapPath,
                staticMeshReference.ObjectName,
                ResolveTexture,
                _packageIndex);
            if (localMaterials.Count == 0)
            {
                throw new PackageReadException($"Static mesh '{referenceKey}' has no resolved material graph.");
            }

            return localMaterials;
        }

        if (!_staticMeshIndex.TryGetValue(staticMeshReference.PackageName, out var meshPackagePath))
        {
            throw new PackageReadException($"Static mesh package '{staticMeshReference.PackageName}' was not found while resolving materials for '{referenceKey}'.");
        }

        var materials = StaticMeshCodec.ResolveStaticMeshMaterials(
            meshPackagePath,
            staticMeshReference.ObjectName,
            ResolveTexture,
            _packageIndex);

        if (materials.Count == 0)
        {
            throw new PackageReadException($"Static mesh '{referenceKey}' has no resolved material graph.");
        }

        return materials;
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
