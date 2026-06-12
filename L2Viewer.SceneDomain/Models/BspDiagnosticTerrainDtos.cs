using L2Viewer.PackageCore;

namespace L2Viewer.SceneDomain.Models;

public sealed class BspDiagnosticTerrainPlacement
{
    public required int ExportIndex { get; init; }
    public required string ObjectName { get; init; }
    public required MeshData[] Meshes { get; init; }
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
    public string HeightReference { get; init; } = string.Empty;
    public string ColorReference { get; init; } = string.Empty;
    public string HeightChannel { get; init; } = string.Empty;
}
