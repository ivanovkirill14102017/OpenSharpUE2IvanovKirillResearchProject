namespace L2Viewer.SceneDomain.Models;

public sealed class BspDiagnosticScene
{
    public required string SourcePath { get; init; }
    public required BspDiagnosticModel[] Models { get; init; }
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
}

public sealed class BspDiagnosticModel
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required bool IsWorldModel { get; init; }
    public required int NodeCount { get; init; }
    public required int PolygonCount { get; init; }
    public required int TriangleCount { get; init; }
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
    public required BspDiagnosticChunk[] Chunks { get; init; }
}

public sealed class BspDiagnosticChunk
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
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
    public required BspDiagnosticMeshPart[] MeshParts { get; init; }
}

public sealed class BspDiagnosticMeshPart
{
    public required string Name { get; init; }
    public required int SurfaceCount { get; init; }
    public required int MaterialRawReference { get; init; }
    public string? MaterialPackageName { get; init; }
    public string? MaterialObjectName { get; init; }
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
