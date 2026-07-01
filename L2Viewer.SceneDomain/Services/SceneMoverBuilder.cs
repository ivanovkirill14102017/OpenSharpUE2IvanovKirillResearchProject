using System.Numerics;
using L2Viewer.PackageCore;
using L2Viewer.SceneDTO;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.MaterialServices;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UnrFile;
using L2Viewer.UsxFile;

namespace L2Viewer.SceneDomain.Services;

[ForExternalUse]
public sealed class SceneMoverBuilder
{
    private readonly string? _clientRoot;
    private readonly SceneMaterialResolver? _materialResolver;
    private readonly BspTextureManager? _textureManager;

    public SceneMoverBuilder()
    {
    }

    public SceneMoverBuilder(string clientRoot, SceneMaterialResolver materialResolver, BspTextureManager textureManager)
    {
        _clientRoot = clientRoot;
        _materialResolver = materialResolver;
        _textureManager = textureManager;
    }

    public SceneMoverScene Build(L2Viewer.UnrFile.UnrFile unr)
    {
        var movers = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrMoverObject>()
            .Where(x => x.BrushReference?.ExportIndex is not null)
            .ToList();
        var materialRequests = new List<SceneMaterialRequest>();
        var textureRequests = new List<SceneTextureRequest>();
        foreach (var mover in movers)
        {
            var brushExportIndex = mover.BrushReference!.ExportIndex!.Value;
            var modelObj = unr.ExportObjects.FirstOrDefault(x => x.Export.Index == brushExportIndex)?.Object as UnrModelObject;
            if (modelObj is null)
            {
                continue;
            }

            foreach (var surface in modelObj.Surfaces)
            {
                if (string.IsNullOrWhiteSpace(surface.MaterialReference?.ObjectName))
                {
                    continue;
                }

                materialRequests.Add(new SceneMaterialRequest(surface.MaterialReference.PackageName, surface.MaterialReference.ObjectName));
                if (!string.IsNullOrWhiteSpace(surface.MaterialReference.PackageName))
                {
                    textureRequests.Add(new SceneTextureRequest(surface.MaterialReference.PackageName, surface.MaterialReference.ObjectName));
                }
            }
        }
        var materialLookup = _materialResolver is null
            ? new Dictionary<string, ResolvedMaterialGraph?>(StringComparer.OrdinalIgnoreCase)
            : _materialResolver.ResolveMany(unr.FilePath, materialRequests);
        var directTextureLookup = _textureManager is null
            ? new Dictionary<string, BspTextureManager.ResolvedTexture>(StringComparer.OrdinalIgnoreCase)
            : _textureManager.ResolveMany(textureRequests);

        var builtMovers = new List<SceneMover>(movers.Count);
        var sceneMin = new Vector3(float.MaxValue);
        var sceneMax = new Vector3(float.MinValue);

        foreach (var mover in movers)
        {
            var brushExportIndex = mover.BrushReference!.ExportIndex!.Value;
            var modelObj = unr.ExportObjects.FirstOrDefault(x => x.Export.Index == brushExportIndex)?.Object as UnrModelObject;
            if (modelObj is null)
            {
                continue;
            }

            var transform = Matrix4x4.CreateScale(mover.DrawScale3D * mover.DrawScale) *
                            Matrix4x4.CreateFromYawPitchRoll(
                                (mover.Rotation?.Y ?? 0) * (MathF.PI / 32768f),
                                (mover.Rotation?.X ?? 0) * (MathF.PI / 32768f),
                                (mover.Rotation?.Z ?? 0) * (MathF.PI / 32768f)) *
                            Matrix4x4.CreateTranslation(mover.Location ?? Vector3.Zero);
            var prePivot = mover.PrePivot;
            var brushPolys = L2Viewer.SceneDTO.BspUvResolver.ResolveBrushPolys(unr, modelObj);
            var built = BuildMover(unr.FilePath, mover.ObjectName, mover.ExportIndex, modelObj, brushPolys, transform, prePivot, materialLookup, directTextureLookup);
            if (built is null)
            {
                continue;
            }

            builtMovers.Add(built);
            sceneMin = Vector3.Min(sceneMin, built.WorldBoundsMin);
            sceneMax = Vector3.Max(sceneMax, built.WorldBoundsMax);
        }

        if (builtMovers.Count == 0)
        {
            sceneMin = Vector3.Zero;
            sceneMax = Vector3.Zero;
        }

        return new SceneMoverScene
        {
            SourcePath = unr.FilePath,
            WorldBoundsMin = sceneMin,
            WorldBoundsMax = sceneMax,
            Movers = builtMovers.ToArray()
        };
    }

    private SceneMover? BuildMover(
        string mapPath,
        string moverName,
        int exportIndex,
        UnrModelObject model,
        UnrPolysObject? brushPolys,
        Matrix4x4 transform,
        Vector3 prePivot,
        IReadOnlyDictionary<string, ResolvedMaterialGraph?> materialLookup,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> directTextureLookup)
    {
        var buildResult = MoverMeshBuilder.BuildMoverMesh(moverName, model, brushPolys, transform, prePivot);
        if (buildResult is null)
        {
            return null;
        }

        var subMeshes = new List<SceneMoverSubMesh>(buildResult.MaterialSlots.Count);
        foreach (var slot in buildResult.MaterialSlots)
        {
            if (slot.TriangleCount <= 0)
            {
                continue;
            }

            var materialReference = string.IsNullOrWhiteSpace(slot.ObjectName)
                ? $"<null>.MaterialRaw_{slot.MaterialRawReference}"
                : SceneReferenceUtilities.BuildReference(mapPath, slot.PackageName, slot.ObjectName);
            var material = string.IsNullOrWhiteSpace(slot.ObjectName) || _materialResolver is null
                ? null
                : materialLookup.GetValueOrDefault(materialReference);
            var materialResource = material is null ? null : BuildMaterialResource(material);
            var directTexture = _textureManager is not null &&
                                material is null &&
                                !string.IsNullOrWhiteSpace(slot.PackageName) &&
                                !string.IsNullOrWhiteSpace(slot.ObjectName)
                ? directTextureLookup.GetValueOrDefault($"{slot.PackageName}.{slot.ObjectName}")
                : null;
            var orderedTextureSlots = material is null
                ? []
                : MaterialTextureSlotOrdering.OrderTextureSlots(material.TextureSlots);
            var primaryTextureReference = orderedTextureSlots.FirstOrDefault()?.Reference ?? (directTexture is null ? null : SceneReferenceUtilities.BuildReference(mapPath, slot.PackageName!, slot.ObjectName!));
            var primaryTextureResource = orderedTextureSlots.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.PackagePath)) is { } textureSlot
                ? SceneReferenceUtilities.BuildResourceLocation(_clientRoot!, textureSlot.PackagePath!, textureSlot.PackageName, textureSlot.ObjectName, textureSlot.ClassName)
                : directTexture?.Resource;

            subMeshes.Add(new SceneMoverSubMesh
            {
                MaterialId = slot.MaterialId,
                TriangleCount = slot.TriangleCount,
                MaterialReference = materialReference,
                MaterialResource = materialResource,
                Material = material,
                PrimaryTextureReference = primaryTextureReference,
                PrimaryTextureResource = primaryTextureResource,
                PolyFlags = slot.PolyFlags,
                ColorArgb = CreateColor(slot.MaterialRawReference, slot.MaterialId)
            });
        }

        return new SceneMover
        {
            ExportIndex = exportIndex,
            Name = moverName,
            RenderGeometry = ConvertGeometry(buildResult.Mesh),
            SubMeshes = subMeshes.ToArray(),
            WorldBoundsMin = buildResult.Mesh.BBoxMin,
            WorldBoundsMax = buildResult.Mesh.BBoxMax
        };
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

    private static uint CreateColor(int materialRawReference, int colorSeed)
    {
        var seed = materialRawReference == 0 ? colorSeed + 1 : Math.Abs(materialRawReference);
        var hue = (seed * 0.61803398875f) % 1f;
        var i = (int)MathF.Floor(hue * 6f);
        var f = hue * 6f - i;
        var p = 0.92f * (1f - 0.62f);
        var q = 0.92f * (1f - f * 0.62f);
        var t = 0.92f * (1f - (1f - f) * 0.62f);

        var (r, g, b) = (i % 6) switch
        {
            0 => ((byte)(0.92f * 255), (byte)(t * 255), (byte)(p * 255)),
            1 => ((byte)(q * 255), (byte)(0.92f * 255), (byte)(p * 255)),
            2 => ((byte)(p * 255), (byte)(0.92f * 255), (byte)(t * 255)),
            3 => ((byte)(p * 255), (byte)(q * 255), (byte)(0.92f * 255)),
            4 => ((byte)(t * 255), (byte)(p * 255), (byte)(0.92f * 255)),
            _ => ((byte)(0.92f * 255), (byte)(p * 255), (byte)(q * 255)),
        };
        return 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
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
