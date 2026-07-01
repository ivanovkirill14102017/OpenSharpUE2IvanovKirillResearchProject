using System.Numerics;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.MaterialServices;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UnrFile;
using L2Viewer.UsxFile;

namespace L2Viewer.SceneDomain.Services.BSPServices;

public sealed class SceneBrushPolyBuilder
{
    private sealed record BrushPolyOwner(
        string Name,
        Vector3? Location,
        Vector3? Rotation,
        float DrawScale,
        Vector3 DrawScale3D,
        Vector3 PrePivot);

    private readonly string? _clientRoot;
    private readonly SceneMaterialResolver? _materialResolver;
    private readonly BspTextureManager? _textureManager;

    public SceneBrushPolyBuilder()
    {
    }

    public SceneBrushPolyBuilder(string clientRoot, SceneMaterialResolver materialResolver, BspTextureManager textureManager)
    {
        _clientRoot = clientRoot;
        _materialResolver = materialResolver;
        _textureManager = textureManager;
    }

    public SceneBrushPolyScene Build(UnrFile.UnrFile unr, bool excludeVolumes = true)
    {
        var textureRequests = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrPolysObject>()
            .SelectMany(x => x.Polys)
            .Where(x => x.TextureReference is not null && !string.IsNullOrWhiteSpace(x.TextureReference.PackageName) && !string.IsNullOrWhiteSpace(x.TextureReference.ObjectName))
            .Select(x => new SceneTextureRequest(x.TextureReference!.PackageName!, x.TextureReference!.ObjectName!))
            .ToArray();
        var materialLookup = _materialResolver is null
            ? new Dictionary<string, ResolvedMaterialGraph?>(StringComparer.OrdinalIgnoreCase)
            : _materialResolver.ResolveMany(
                unr.FilePath,
                textureRequests.Select(x => new SceneMaterialRequest(x.PackageName, x.ObjectName)));
        var directTextureLookup = _textureManager is null
            ? new Dictionary<string, BspTextureManager.ResolvedTexture>(StringComparer.OrdinalIgnoreCase)
            : _textureManager.ResolveMany(textureRequests);

        var expectedBrushActors = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrActorBaseObject>()
            .Where(x => x.BrushReference?.ExportIndex is not null)
            .Where(x => !excludeVolumes || x is not UnrVolumeBaseObject)
            .ToList();

        var polysToOwner = new Dictionary<int, BrushPolyOwner>();
        foreach (var actor in expectedBrushActors)
        {
            TryRegisterOwner(unr, polysToOwner, actor.BrushReference!.ExportIndex!.Value, new BrushPolyOwner(
                actor.ObjectName,
                actor.Location,
                actor.Rotation,
                actor.DrawScale,
                actor.DrawScale3D,
                actor.PrePivot));
        }

        var expectedBrushObjects = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrBrushObject>()
            .Where(x => x.BrushReference?.ExportIndex is not null)
            .ToList();

        foreach (var brush in expectedBrushObjects)
        {
            TryRegisterOwner(unr, polysToOwner, brush.BrushReference!.ExportIndex!.Value, new BrushPolyOwner(
                brush.ObjectName,
                brush.Location,
                brush.Rotation,
                brush.DrawScale,
                brush.DrawScale3D,
                brush.PrePivot));
        }

        var builtActors = new List<SceneBrushPolyActor>();
        var sceneMin = new Vector3(float.MaxValue);
        var sceneMax = new Vector3(float.MinValue);

        foreach (var exportObj in unr.ExportObjects)
        {
            if (exportObj.Object is not UnrPolysObject polys)
            {
                continue;
            }

            if (!polysToOwner.TryGetValue(exportObj.Export.Index, out var owner))
            {
                continue;
            }

            var transform = Matrix4x4.CreateScale(owner.DrawScale3D * owner.DrawScale) *
                            Matrix4x4.CreateFromYawPitchRoll(
                                (owner.Rotation?.Y ?? 0) * (MathF.PI / 32768f),
                                (owner.Rotation?.X ?? 0) * (MathF.PI / 32768f),
                                (owner.Rotation?.Z ?? 0) * (MathF.PI / 32768f)) *
                            Matrix4x4.CreateTranslation(owner.Location ?? Vector3.Zero);
            var built = BuildBrushPoly(unr.FilePath, exportObj.Export.Index, owner.Name, polys, transform, owner.PrePivot, materialLookup, directTextureLookup);
            if (built is null)
            {
                continue;
            }

            builtActors.Add(built);
            sceneMin = Vector3.Min(sceneMin, built.WorldBoundsMin);
            sceneMax = Vector3.Max(sceneMax, built.WorldBoundsMax);
        }

        if (builtActors.Count == 0)
        {
            sceneMin = Vector3.Zero;
            sceneMax = Vector3.Zero;
        }

        return new SceneBrushPolyScene
        {
            SourcePath = unr.FilePath,
            WorldBoundsMin = sceneMin,
            WorldBoundsMax = sceneMax,
            Actors = builtActors.ToArray()
        };
    }

    private static void TryRegisterOwner(
        UnrFile.UnrFile unr,
        IDictionary<int, BrushPolyOwner> polysToOwner,
        int brushExportIndex,
        BrushPolyOwner owner)
    {
        var modelObj = unr.ExportObjects.FirstOrDefault(x => x.Export.Index == brushExportIndex)?.Object as UnrModelObject;
        if (modelObj?.PolysReference?.ExportIndex is int polysExportIndex)
        {
            polysToOwner.TryAdd(polysExportIndex, owner);
        }
    }

    private SceneBrushPolyActor? BuildBrushPoly(
        string mapPath,
        int exportIndex,
        string name,
        UnrPolysObject polys,
        Matrix4x4 transform,
        Vector3 prePivot,
        IReadOnlyDictionary<string, ResolvedMaterialGraph?> materialLookup,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> directTextureLookup)
    {
        var triangles = new List<Triangle>();
        var materials = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var subMeshes = new List<SceneBrushPolySubMesh>();

        var boundsMin = new Vector3(float.MaxValue);
        var boundsMax = new Vector3(float.MinValue);

        foreach (var poly in polys.Polys)
        {
            if (poly.NumVertices < 3)
            {
                continue;
            }

            var matRefStr = poly.TextureReference is null ? "null" : $"{poly.TextureReference.PackageName}.{poly.TextureReference.ObjectName}";

            if (!materials.TryGetValue(matRefStr, out var materialId))
            {
                materialId = materials.Count;
                materials.Add(matRefStr, materialId);
                
                var materialReference = poly.TextureReference is null ? "null" : SceneReferenceUtilities.BuildReference(mapPath, poly.TextureReference.PackageName, poly.TextureReference.ObjectName);
                var material = poly.TextureReference is null || _materialResolver is null
                    ? null
                    : materialLookup.GetValueOrDefault(materialReference);
                var materialResource = material is null ? null : BuildMaterialResource(material);
                
                var directTexture = _textureManager is not null &&
                    material is null && poly.TextureReference is not null && !string.IsNullOrWhiteSpace(poly.TextureReference.PackageName) && !string.IsNullOrWhiteSpace(poly.TextureReference.ObjectName)
                    ? directTextureLookup.GetValueOrDefault($"{poly.TextureReference.PackageName}.{poly.TextureReference.ObjectName}")
                    : null;
                var orderedTextureSlots = material is null
                    ? []
                    : MaterialTextureSlotOrdering.OrderTextureSlots(material.TextureSlots);
                var primaryTextureReference = orderedTextureSlots.FirstOrDefault()?.Reference ?? (directTexture is null ? null : SceneReferenceUtilities.BuildReference(mapPath, poly.TextureReference!.PackageName, poly.TextureReference!.ObjectName));
                var primaryTextureResource = orderedTextureSlots.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.PackagePath)) is { } slot
                    ? SceneReferenceUtilities.BuildResourceLocation(_clientRoot!, slot.PackagePath!, slot.PackageName, slot.ObjectName, slot.ClassName)
                    : (directTexture?.Resource);

                subMeshes.Add(new SceneBrushPolySubMesh
                {
                    MaterialId = materialId,
                    TriangleCount = 0,
                    MaterialReference = materialReference,
                    MaterialResource = materialResource,
                    Material = material,
                    PrimaryTextureReference = primaryTextureReference,
                    PrimaryTextureResource = primaryTextureResource
                });
            }

            for (var v = 2; v < poly.NumVertices; v++)
            {
                var p0 = poly.Vertices[0];
                var p1 = poly.Vertices[v - 1];
                var p2 = poly.Vertices[v];

                var t0 = new Vector2(Vector3.Dot(p0 - poly.Base, poly.TextureU), Vector3.Dot(p0 - poly.Base, poly.TextureV));
                var t1 = new Vector2(Vector3.Dot(p1 - poly.Base, poly.TextureU), Vector3.Dot(p1 - poly.Base, poly.TextureV));
                var t2 = new Vector2(Vector3.Dot(p2 - poly.Base, poly.TextureU), Vector3.Dot(p2 - poly.Base, poly.TextureV));

                var tp0 = Vector3.Transform(p0 - prePivot, transform);
                var tp1 = Vector3.Transform(p1 - prePivot, transform);
                var tp2 = Vector3.Transform(p2 - prePivot, transform);
                var tNorm = Vector3.Normalize(Vector3.TransformNormal(poly.Normal, transform));

                triangles.Add(new Triangle(
                    new TriangleVertex(tp0, t0, tNorm),
                    new TriangleVertex(tp1, t1, tNorm),
                    new TriangleVertex(tp2, t2, tNorm),
                    materialId));

                boundsMin = Vector3.Min(boundsMin, tp0);
                boundsMin = Vector3.Min(boundsMin, tp1);
                boundsMin = Vector3.Min(boundsMin, tp2);
                boundsMax = Vector3.Max(boundsMax, tp0);
                boundsMax = Vector3.Max(boundsMax, tp1);
                boundsMax = Vector3.Max(boundsMax, tp2);
            }
        }

        if (triangles.Count == 0)
        {
            return null;
        }
        
        var finalSubMeshes = new List<SceneBrushPolySubMesh>(subMeshes.Count);
        foreach (var subMesh in subMeshes)
        {
            var count = triangles.Count(x => x.MaterialId == subMesh.MaterialId);
            if (count > 0)
            {
                finalSubMeshes.Add(new SceneBrushPolySubMesh
                {
                    MaterialId = subMesh.MaterialId,
                    TriangleCount = count,
                    MaterialReference = subMesh.MaterialReference,
                    MaterialResource = subMesh.MaterialResource,
                    Material = subMesh.Material,
                    PrimaryTextureReference = subMesh.PrimaryTextureReference,
                    PrimaryTextureResource = subMesh.PrimaryTextureResource
                });
            }
        }

        return new SceneBrushPolyActor
        {
            ExportIndex = exportIndex,
            Name = name,
            RenderGeometry = new SceneTriangleMeshData
            {
                Name = name,
                Triangles = triangles.ToArray(),
                BoundsMin = boundsMin,
                BoundsMax = boundsMax,
                SourceNote = "Brush Polys"
            },
            SubMeshes = finalSubMeshes.ToArray(),
            WorldBoundsMin = boundsMin,
            WorldBoundsMax = boundsMax
        };
    }

    private SceneResourceLocation BuildMaterialResource(ResolvedMaterialGraph material)
    {
        if (string.IsNullOrWhiteSpace(_clientRoot))
        {
            throw new PackageReadException("Material resource resolution requires a client root.");
        }

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
            rootNode.ClassName);
    }
}
