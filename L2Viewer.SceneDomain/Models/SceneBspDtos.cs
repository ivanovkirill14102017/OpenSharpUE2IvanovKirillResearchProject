namespace L2Viewer.SceneDomain.Models;

using L2Viewer.UsxFile;

public sealed class SceneBspScene
{
    public required string SourcePath { get; init; }
    public required SceneBspModel[] Models { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
}

public sealed class SceneBspModel
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required bool IsWorldModel { get; init; }
    public required int NodeCount { get; init; }
    public required int PolygonCount { get; init; }
    public required int TriangleCount { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
    public required SceneBspChunk[] Chunks { get; init; }
}

public sealed class SceneBspChunk
{
    public required int ChunkIndex { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required bool IsPortalLike { get; init; }
    public required bool IsInvisibleLike { get; init; }
    public required bool HasZoneBoundary { get; init; }
    public required byte[] ZoneNumbers { get; init; }
    public required int[] LeafIndices { get; init; }
    public required int PolygonCount { get; init; }
    public required int TriangleCount { get; init; }
    public required int SurfaceCount { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
    public required SceneBspMeshSection[] MeshSections { get; init; }
}

public sealed class SceneBspMeshSection
{
    public required string Name { get; init; }
    public required int SurfaceCount { get; init; }
    public required int MaterialRawReference { get; init; }
    public required string MaterialReference { get; init; }
    public SceneResourceLocation? MaterialResource { get; init; }
    public string? MaterialPackageName { get; init; }
    public string? MaterialObjectName { get; init; }
    public string? PrimaryTextureReference { get; init; }
    public SceneResourceLocation? PrimaryTextureResource { get; init; }
    public required string[] TextureReferences { get; init; }
    public required SceneResourceLocation[] TextureResources { get; init; }
    public ResolvedMaterialGraph? Material { get; init; }
    public required uint PolyFlags { get; init; }
    public required UnrPolyFlags KnownPolyFlags { get; init; }
    public required uint UnknownPolyFlagsMask { get; init; }
    public required string[] PolyFlagNames { get; init; }
    public required uint ColorArgb { get; init; }
    public required Vector3[] Positions { get; init; }
    public required Vector3[] Normals { get; init; }
    public required Vector2[] TextureCoordinates { get; init; }
    public required int[] Indices { get; init; }
}
