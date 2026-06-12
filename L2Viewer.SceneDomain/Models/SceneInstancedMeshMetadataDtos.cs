namespace L2Viewer.SceneDomain.Models;

public sealed class SceneInstancedMeshMetadataResult
{
    public required IReadOnlyList<SceneStaticMeshInstanceMetadata> Instances { get; init; }
    public required IReadOnlyList<SceneTerrainDecorationMetadata> TerrainDecorations { get; init; }
}

public sealed class SceneStaticMeshInstanceMetadata
{
    public required int ExportIndex { get; init; }
    public required string ActorName { get; init; }
    public required string ClassName { get; init; }
    public string? MeshReference { get; init; }
    public string? TextureReference { get; init; }
    public required IReadOnlyList<string> SkinReferences { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? UnrealRotationRaw { get; init; }
    public Vector3? RotationEulerDegrees { get; init; }
    public required Vector3 Scale { get; init; }
    public required Vector3 PrePivot { get; init; }
}

public sealed class SceneTerrainDecorationMetadata
{
    public required int TerrainExportIndex { get; init; }
    public required string TerrainActorName { get; init; }
    public required int LayerIndex { get; init; }
    public string? HeightMapReference { get; init; }
    public string? MeshReference { get; init; }
    public string? DensityMapReference { get; init; }
    public string? ScaleMapReference { get; init; }
    public string? ColorMapReference { get; init; }
    public required int MaxPerQuad { get; init; }
    public required int Seed { get; init; }
    public required bool RandomYaw { get; init; }
    public Vector3? TerrainLocation { get; init; }
    public Vector3? TerrainScale { get; init; }
    public UnrFloatRange? DensityMultiplier { get; init; }
    public UnrRangeVector? ScaleMultiplier { get; init; }
}
