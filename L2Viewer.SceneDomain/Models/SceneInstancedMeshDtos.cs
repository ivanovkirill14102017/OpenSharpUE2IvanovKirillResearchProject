using System.Collections.ObjectModel;
using L2Viewer.SceneDomain.Services.MaterialServices;
using L2Viewer.UsxFile;

namespace L2Viewer.SceneDomain.Models;

public sealed class SceneInstancedMeshResult
{
    public required IReadOnlyDictionary<string, SceneStaticMeshDefinition> UniqueMeshes { get; init; }
    public required IReadOnlyList<SceneStaticMeshInstance> Instances { get; init; }
    public required IReadOnlyList<SceneTerrainDecorationLayer> TerrainDecorations { get; init; }
}

public sealed class SceneStaticMeshDefinition
{
    public required string Reference { get; init; }
    public required SceneResourceLocation MeshResource { get; init; }
    public required SceneTriangleMeshData RenderGeometry { get; init; }
    public SceneTriangleMeshData? CollisionGeometry { get; init; }
    public StaticMeshCollisionInfo? CollisionInfo { get; init; }
    public required IReadOnlyList<SceneStaticMeshSubMeshDefinition> SubMeshes { get; init; }
}

public sealed class SceneTriangleMeshData
{
    public required string Name { get; init; }
    public required IReadOnlyList<Triangle> Triangles { get; init; }
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
    public required string SourceNote { get; init; }
}

public sealed class SceneStaticMeshSubMeshDefinition
{
    public required int MaterialId { get; init; }
    public required int TriangleCount { get; init; }
    public string? MaterialReference { get; init; }
    public SceneResourceLocation? MaterialResource { get; init; }
    public ResolvedMaterialGraph? Material { get; init; }
    public MaterialBlendModeHint BlendModeHint { get; init; }
    public string? PrimaryTextureReference { get; init; }
    public SceneResourceLocation? PrimaryTextureResource { get; init; }
    public required IReadOnlyList<string> TextureReferences { get; init; }
    public required IReadOnlyList<SceneResourceLocation> TextureResources { get; init; }
}

public sealed class SceneStaticMeshInstance
{
    public required int ExportIndex { get; init; }
    public required string StableName { get; init; }
    public required string ActorName { get; init; }
    public required string ClassName { get; init; }
    public required string MeshReference { get; init; }
    public required SceneResourceLocation MeshResource { get; init; }
    public string? TextureReference { get; init; }
    public SceneResourceLocation? TextureResource { get; init; }
    public required IReadOnlyList<string> SkinReferences { get; init; }
    public required IReadOnlyList<SceneResourceLocation> SkinResources { get; init; }
    public required Vector3 WorldLocation { get; init; }
    public required Vector3 UnrealRotationRaw { get; init; }
    public required Vector3 RotationEulerDegrees { get; init; }
    public required Vector3 Scale { get; init; }
    public required Vector3 PrePivot { get; init; }
}
