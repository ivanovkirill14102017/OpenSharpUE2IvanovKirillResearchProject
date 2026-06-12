using System.Numerics;
using L2Viewer.UsxFile;

namespace L2Viewer.SceneDomain.Models;

public sealed class SceneMoverScene
{
    public required string SourcePath { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
    public required IReadOnlyList<SceneMover> Movers { get; init; }
}

public sealed class SceneMover
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required SceneTriangleMeshData RenderGeometry { get; init; }
    public required IReadOnlyList<SceneMoverSubMesh> SubMeshes { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
}

public sealed class SceneMoverSubMesh
{
    public required int MaterialId { get; init; }
    public required int TriangleCount { get; init; }
    public string? MaterialReference { get; init; }
    public SceneResourceLocation? MaterialResource { get; init; }
    public ResolvedMaterialGraph? Material { get; init; }
    public string? PrimaryTextureReference { get; init; }
    public SceneResourceLocation? PrimaryTextureResource { get; init; }
    public uint PolyFlags { get; init; }
    public uint ColorArgb { get; init; }
}
