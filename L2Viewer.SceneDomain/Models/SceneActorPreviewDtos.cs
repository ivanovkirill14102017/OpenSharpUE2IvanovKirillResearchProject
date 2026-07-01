namespace L2Viewer.SceneDomain.Models;

public sealed class SceneActorPreviewDiagnosticData
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? UnrealRotationRaw { get; init; }
    public Vector3? RotationEulerDegrees { get; init; }
    public Vector3? BoundsMin { get; init; }
    public Vector3? BoundsMax { get; init; }
    public required string PropertiesText { get; init; }
}
