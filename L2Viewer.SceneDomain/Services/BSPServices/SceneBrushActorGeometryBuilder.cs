using L2Viewer.PackageCore;
using L2Viewer.SceneDTO;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.BSPServices;

internal static class SceneBrushActorGeometryBuilder
{
    public static SceneBrushActorGeometryData Build(
        string mapPath,
        IReadOnlyDictionary<int, UnrModelObject> modelsByExportIndex,
        IReadOnlyDictionary<int, UnrPolysObject> polysByExportIndex,
        UnrActorBaseObject actor,
        string geometryName)
    {
        if (actor.BrushReference?.ExportIndex is not int brushExportIndex ||
            !modelsByExportIndex.TryGetValue(brushExportIndex, out var model))
        {
            return new SceneBrushActorGeometryData();
        }

        var polysReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, model.PolysReference);
        if (model.PolysReference?.ExportIndex is not int polysExportIndex ||
            !polysByExportIndex.TryGetValue(polysExportIndex, out var polys))
        {
            return new SceneBrushActorGeometryData
            {
                BrushPolysReference = polysReference,
                BrushModelBoundsMin = model.BoundsValid ? model.BoundsMin : null,
                BrushModelBoundsMax = model.BoundsValid ? model.BoundsMax : null,
                BrushModelBoundsValid = model.BoundsValid
            };
        }

        var geometry = BrushMeshBuilder.BuildBrushActorMesh(actor, polys, geometryName);
        return new SceneBrushActorGeometryData
        {
            BrushPolysReference = polysReference,
            BrushModelBoundsMin = model.BoundsValid ? model.BoundsMin : null,
            BrushModelBoundsMax = model.BoundsValid ? model.BoundsMax : null,
            BrushModelBoundsValid = model.BoundsValid,
            RenderGeometry = geometry is null ? null : ConvertGeometry(geometry),
            WorldBoundsMin = geometry?.BBoxMin,
            WorldBoundsMax = geometry?.BBoxMax
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
}

internal sealed class SceneBrushActorGeometryData
{
    public string? BrushPolysReference { get; init; }
    public Vector3? BrushModelBoundsMin { get; init; }
    public Vector3? BrushModelBoundsMax { get; init; }
    public bool BrushModelBoundsValid { get; init; }
    public SceneTriangleMeshData? RenderGeometry { get; init; }
    public Vector3? WorldBoundsMin { get; init; }
    public Vector3? WorldBoundsMax { get; init; }
}
