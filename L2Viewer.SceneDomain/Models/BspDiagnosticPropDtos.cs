using L2Viewer.PackageCore;

namespace L2Viewer.SceneDomain.Models;

public sealed class BspDiagnosticPropPlacement
{
    public required int ExportIndex { get; init; }
    public required string ActorName { get; init; }
    public required string ClassName { get; init; }
    public required string StaticMeshReference { get; init; }
    public string? PrimaryTextureReference { get; init; }
    public required MeshData Mesh { get; init; }
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
}
