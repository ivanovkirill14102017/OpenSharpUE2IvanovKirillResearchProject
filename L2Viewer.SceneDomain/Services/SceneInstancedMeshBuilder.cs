using System.Collections.ObjectModel;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneInstancedMeshBuilder
{
    private readonly SceneStaticMeshResolver _resolver;
    private readonly BspTextureManager _textureManager;
    private const int MaxTerrainDecoSampleResolution = 128;

    public SceneInstancedMeshBuilder(SceneStaticMeshResolver resolver, BspTextureManager textureManager)
    {
        _resolver = resolver;
        _textureManager = textureManager;
    }

    public SceneInstancedMeshResult Build(L2Viewer.UnrFile.UnrFile unr)
    {
        var meshActors = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrActorBaseObject>()
            .Where(IsMeshInstanceActor)
            .Where(x => !ShouldSkipMeshInstanceActor(x))
            .ToList();
        var terrainActors = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrTerrainInfoObject>()
            .ToList();
        var meshReferences = meshActors
            .Where(x => x.StaticMeshReference is not null)
            .Select(x => x.StaticMeshReference!)
            .Concat(terrainActors.SelectMany(x => x.DecoLayers).Where(x => x.StaticMeshReference is not null).Select(x => x.StaticMeshReference!))
            .ToList();
        var resolvedMeshes = _resolver.ResolveMany(unr.FilePath, meshReferences);
        var resourceReferences = meshActors
            .SelectMany(actor => EnumerateActorResourceReferences(actor))
            .ToList();
        var resolvedResources = _resolver.ResolveResourceLocations(unr.FilePath, resourceReferences);
        var terrainTextures = _textureManager.ResolveMany(
            terrainActors
                .SelectMany(EnumerateTerrainTextureRequests)
                .ToArray());

        var uniqueMeshes = new Dictionary<string, SceneStaticMeshDefinition>(StringComparer.OrdinalIgnoreCase);
        var instances = new List<SceneStaticMeshInstance>();
        var terrainDecorations = new List<SceneTerrainDecorationLayer>();

        foreach (var entry in unr.ExportObjects)
        {
            if (entry.Object is UnrTerrainInfoObject terrain)
            {
                AddTerrainDecorationMaps(unr.FilePath, terrain, resolvedMeshes, terrainTextures, uniqueMeshes, terrainDecorations);
                continue;
            }

            if (entry.Object is not UnrActorBaseObject actor || !IsMeshInstanceActor(actor))
            {
                continue;
            }

            if (ShouldSkipMeshInstanceActor(actor))
            {
                continue;
            }

            if (actor.StaticMeshReference is null)
            {
                throw new PackageReadException($"Actor '{actor.ObjectName}' ({actor.ClassName}) has no StaticMesh reference.");
            }

            if (actor.Location is null)
            {
                throw new PackageReadException($"Actor '{actor.ObjectName}' ({actor.ClassName}) has no Location.");
            }

            var meshReference = SceneReferenceUtilities.BuildReference(unr.FilePath, actor.StaticMeshReference);
            var asset = resolvedMeshes[meshReference];
            if (asset.RenderGeometry.Triangles.Count == 0)
            {
                throw new PackageReadException($"Static mesh '{asset.Reference}' for actor '{actor.ObjectName}' has no triangles.");
            }

            var meshResource = asset.MeshResource;
            if (!uniqueMeshes.ContainsKey(meshReference))
            {
                uniqueMeshes[meshReference] = new SceneStaticMeshDefinition
                {
                    Reference = meshReference,
                    MeshResource = meshResource,
                    RenderGeometry = asset.RenderGeometry,
                    CollisionGeometry = asset.CollisionGeometry,
                    CollisionInfo = asset.CollisionInfo,
                    SubMeshes = asset.SubMeshes
                };
            }

            var rotationRaw = actor.Rotation ?? Vector3.Zero;
            instances.Add(CreateInstance(
                actor.ExportIndex,
                actor.ObjectName,
                actor.ClassName,
                meshReference,
                meshResource,
                SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, actor.TextureReference),
                GetTextureResource(unr.FilePath, actor, resolvedResources),
                GetSkinReferences(unr.FilePath, actor),
                GetSkinResources(unr.FilePath, actor, resolvedResources),
                actor.Location.Value,
                rotationRaw,
                SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw),
                SceneTransformUtilities.ComputeActorScale(actor),
                actor.PrePivot));
        }

        var orderedMeshes = new ReadOnlyDictionary<string, SceneStaticMeshDefinition>(
            uniqueMeshes
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase));

        return new SceneInstancedMeshResult
        {
            UniqueMeshes = orderedMeshes,
            Instances = instances
                .OrderBy(x => x.ActorName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            TerrainDecorations = terrainDecorations
                .OrderBy(x => x.TerrainActorName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.LayerIndex)
                .ToArray()
        };
    }

    private void AddTerrainDecorationMaps(
        string mapPath,
        UnrTerrainInfoObject terrain,
        IReadOnlyDictionary<string, SceneStaticMeshDefinition> resolvedMeshes,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> terrainTextures,
        Dictionary<string, SceneStaticMeshDefinition> uniqueMeshes,
        List<SceneTerrainDecorationLayer> terrainDecorations)
    {
        if (terrain.TerrainMapReference?.PackageName is null)
        {
            throw new PackageReadException($"Terrain '{terrain.ObjectName}' has no TerrainMap reference package.");
        }

        if (terrain.Location is null)
        {
            throw new PackageReadException($"Terrain '{terrain.ObjectName}' has no Location.");
        }

        var heightResolved = GetResolvedTerrainTexture(terrain.TerrainMapReference, terrainTextures)
            ?? throw new PackageReadException($"Terrain '{terrain.ObjectName}' height map '{terrain.TerrainMapReference.PackageName}.{terrain.TerrainMapReference.ObjectName}' was not resolved.");
        var terrainScale = NormalizeTerrainScale(terrain.TerrainScale);
        var heightMapReference = SceneReferenceUtilities.BuildReference(mapPath, terrain.TerrainMapReference);

        for (var layerIndex = 0; layerIndex < terrain.DecoLayers.Length; layerIndex++)
        {
            var layer = terrain.DecoLayers[layerIndex];
            if (layer.ShowOnTerrain == 0)
            {
                continue;
            }

            if (layer.StaticMeshReference is null)
            {
                throw new PackageReadException($"Terrain '{terrain.ObjectName}' deco layer {layerIndex} has no StaticMesh reference.");
            }

            if (layer.DensityMapReference?.PackageName is null)
            {
                throw new PackageReadException($"Terrain '{terrain.ObjectName}' deco layer {layerIndex} has no DensityMap reference package.");
            }

            var densityResolved = GetResolvedTerrainTexture(layer.DensityMapReference, terrainTextures)
                ?? throw new PackageReadException($"Terrain '{terrain.ObjectName}' deco layer {layerIndex} density map '{layer.DensityMapReference.PackageName}.{layer.DensityMapReference.ObjectName}' was not resolved.");
            var scaleResolved = layer.ScaleMapReference?.PackageName is null
                ? null
                : GetResolvedTerrainTexture(layer.ScaleMapReference, terrainTextures);
            var colorResolved = layer.ColorMapReference?.PackageName is null
                ? null
                : GetResolvedTerrainTexture(layer.ColorMapReference, terrainTextures);
            var meshReference = SceneReferenceUtilities.BuildReference(mapPath, layer.StaticMeshReference);
            var asset = resolvedMeshes[meshReference];
            if (asset.RenderGeometry.Triangles.Count == 0)
            {
                throw new PackageReadException($"Terrain '{terrain.ObjectName}' deco layer {layerIndex} uses mesh '{asset.Reference}' with no triangles.");
            }

            if (!uniqueMeshes.ContainsKey(meshReference))
            {
                uniqueMeshes[meshReference] = asset;
            }

            terrainDecorations.Add(new SceneTerrainDecorationLayer
            {
                TerrainExportIndex = terrain.ExportIndex,
                TerrainActorName = terrain.ObjectName,
                LayerIndex = layerIndex,
                HeightMapReference = heightMapReference,
                HeightMapResource = heightResolved.Resource,
                MeshReference = meshReference,
                MeshResource = asset.MeshResource,
                DensityMapReference = SceneReferenceUtilities.BuildReference(mapPath, layer.DensityMapReference),
                DensityMapResource = densityResolved.Resource,
                ScaleMapReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, layer.ScaleMapReference),
                ScaleMapResource = scaleResolved?.Resource,
                ColorMapReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, layer.ColorMapReference),
                ColorMapResource = colorResolved?.Resource,
                DensityMapTexture = densityResolved.Texture,
                ScaleMapTexture = scaleResolved?.Texture,
                ColorMapTexture = colorResolved?.Texture,
                MaxPerQuad = Math.Max(1, layer.MaxPerQuad),
                Seed = layer.Seed,
                RandomYaw = layer.RandomYaw != 0,
                TerrainLocation = terrain.Location.Value,
                TerrainScale = terrainScale,
                DensityMultiplier = layer.DensityMultiplier,
                ScaleMultiplier = layer.ScaleMultiplier
            });
        }
    }

    private static SceneStaticMeshInstance CreateInstance(
        int exportIndex,
        string actorName,
        string className,
        string meshReference,
        SceneResourceLocation meshResource,
        string? textureReference,
        SceneResourceLocation? textureResource,
        IReadOnlyList<string> skinReferences,
        IReadOnlyList<SceneResourceLocation> skinResources,
        Vector3 location,
        Vector3 unrealRotationRaw,
        Vector3 rotationEulerDegrees,
        Vector3 scale,
        Vector3 prePivot)
    {
        return new SceneStaticMeshInstance
        {
            ExportIndex = exportIndex,
            ActorName = actorName,
            ClassName = className,
            MeshReference = meshReference,
            MeshResource = meshResource,
            TextureReference = textureReference,
            TextureResource = textureResource,
            SkinReferences = skinReferences,
            SkinResources = skinResources,
            WorldLocation = location,
            UnrealRotationRaw = unrealRotationRaw,
            RotationEulerDegrees = rotationEulerDegrees,
            Scale = scale,
            PrePivot = prePivot
        };
    }

    private static bool IsMeshInstanceActor(UnrActorBaseObject actor)
    {
        return actor is UnrStaticMeshActorObject or UnrMovableStaticMeshActorObject;
    }

    private static bool ShouldSkipMeshInstanceActor(UnrActorBaseObject actor)
    {
        return actor switch
        {
            UnrStaticMeshActorObject staticMeshActor => staticMeshActor.DeleteMe || staticMeshActor.PendingDelete,
            _ => false
        };
    }

    private static IReadOnlyList<string> GetSkinReferences(string mapPath, UnrActorBaseObject actor)
    {
        if (actor is UnrStaticMeshActorObject staticActor)
        {
            return staticActor.Skins.Select(x => SceneReferenceUtilities.BuildReference(mapPath, x)).ToArray();
        }

        return Array.Empty<string>();
    }

    private IReadOnlyList<SceneResourceLocation> GetSkinResources(string mapPath, UnrActorBaseObject actor, IReadOnlyDictionary<string, SceneResourceLocation> resolvedResources)
    {
        if (actor is UnrStaticMeshActorObject staticActor)
        {
            return staticActor.Skins
                .Select(x => resolvedResources[SceneReferenceUtilities.BuildReference(mapPath, x)])
                .ToArray();
        }

        return Array.Empty<SceneResourceLocation>();
    }

    private SceneResourceLocation? GetTextureResource(string mapPath, UnrActorBaseObject actor, IReadOnlyDictionary<string, SceneResourceLocation> resolvedResources)
    {
        return actor.TextureReference is null ? null : resolvedResources[SceneReferenceUtilities.BuildReference(mapPath, actor.TextureReference)];
    }

    private static IEnumerable<UnrFileObjectReference> EnumerateActorResourceReferences(UnrActorBaseObject actor)
    {
        if (actor.TextureReference is not null)
        {
            yield return actor.TextureReference;
        }

        if (actor is UnrStaticMeshActorObject staticActor)
        {
            foreach (var skin in staticActor.Skins)
            {
                yield return skin;
            }
        }
    }

    private static IEnumerable<SceneTextureRequest> EnumerateTerrainTextureRequests(UnrTerrainInfoObject terrain)
    {
        if (terrain.TerrainMapReference?.PackageName is not null)
        {
            yield return new SceneTextureRequest(terrain.TerrainMapReference.PackageName, terrain.TerrainMapReference.ObjectName);
        }

        foreach (var layer in terrain.DecoLayers)
        {
            if (layer.DensityMapReference?.PackageName is not null)
            {
                yield return new SceneTextureRequest(layer.DensityMapReference.PackageName, layer.DensityMapReference.ObjectName);
            }

            if (layer.ScaleMapReference?.PackageName is not null)
            {
                yield return new SceneTextureRequest(layer.ScaleMapReference.PackageName, layer.ScaleMapReference.ObjectName);
            }

            if (layer.ColorMapReference?.PackageName is not null)
            {
                yield return new SceneTextureRequest(layer.ColorMapReference.PackageName, layer.ColorMapReference.ObjectName);
            }
        }
    }

    private static BspTextureManager.ResolvedTexture? GetResolvedTerrainTexture(
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> terrainTextures)
    {
        if (reference?.PackageName is null)
        {
            return null;
        }

        return terrainTextures.GetValueOrDefault($"{reference.PackageName}.{reference.ObjectName}");
    }

    private static Vector3? NormalizeTerrainScale(Vector3? terrainScale)
    {
        if (terrainScale is null)
        {
            return null;
        }

        var scale = terrainScale.Value;
        return new Vector3(
            MathF.Abs(scale.X),
            MathF.Abs(scale.Z),
            MathF.Abs(scale.Y));
    }

}
