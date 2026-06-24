namespace L2Viewer.SceneDomain.Models;

public abstract class SceneActorBrushData
{
    public required int ExportIndex { get; init; }
    public required string StableName { get; init; }
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public UnrFileScale? MainScale { get; init; }
    public UnrFileScale? PostScale { get; init; }
    public UnrFileScale? TempScale { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? WorldRotationUnrealRaw { get; init; }
    public Vector3? WorldRotationEulerDegrees { get; init; }
    public float DrawScale { get; init; }
    public Vector3 DrawScale3D { get; init; }
    public Vector3 PrePivot { get; init; }
    public string? Group { get; init; }
    public string? Tag { get; init; }
    public string? Event { get; init; }
    public string? BrushReference { get; init; }
    public string? BaseReference { get; init; }
    public string? LevelReference { get; init; }
    public string? OwnerReference { get; init; }
    public string? MeshReference { get; init; }
    public string? TextureReference { get; init; }
    public string? PhysicsVolumeReference { get; init; }
    public string? StaticMeshReference { get; init; }
    public string? BrushPolysReference { get; init; }
    public Vector3? BrushModelBoundsMin { get; init; }
    public Vector3? BrushModelBoundsMax { get; init; }
    public bool BrushModelBoundsValid { get; init; }
    public SceneTriangleMeshData? RenderGeometry { get; init; }
    public Vector3? WorldBoundsMin { get; init; }
    public Vector3? WorldBoundsMax { get; init; }
    public UnrFileUnknownProperty[] UnknownProperties { get; init; } = [];
}
