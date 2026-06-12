using System.Numerics;
using L2Viewer.UsxFile;

namespace L2Viewer.SceneDomain.Models;

public sealed class SceneBrushPolyScene
{
    public required string SourcePath { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
    public required IReadOnlyList<SceneBrushPolyActor> Actors { get; init; }
}

public sealed class SceneBrushPolyActor
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required SceneTriangleMeshData RenderGeometry { get; init; }
    public required IReadOnlyList<SceneBrushPolySubMesh> SubMeshes { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
}

public sealed class SceneBrushPolySubMesh
{
    public required int MaterialId { get; init; }
    public required int TriangleCount { get; init; }
    public string? MaterialReference { get; init; }
    public SceneResourceLocation? MaterialResource { get; init; }
    public ResolvedMaterialGraph? Material { get; init; }
    public string? PrimaryTextureReference { get; init; }
    public SceneResourceLocation? PrimaryTextureResource { get; init; }
}
