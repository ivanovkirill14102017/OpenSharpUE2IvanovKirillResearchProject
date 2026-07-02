using System.Numerics;

namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public enum SceneWorldPointSourceKind
{
    HuntingZoneDat,
    HuntingGroundTeleportHtml,
    AdminTeleportHtml
}

[ForExternalUse]
public sealed class SceneWorldAreaCatalogData
{
    public required string Quadrant { get; init; }
    public required IReadOnlyList<SceneWorldAreaBoundsData> Areas { get; init; }
    public required IReadOnlyList<SceneWorldInterestPointData> InterestPoints { get; init; }
    public required IReadOnlyList<SceneWorldTeleportPointData> TeleportPoints { get; init; }
}

[ForExternalUse]
public sealed class SceneWorldAreaBoundsData
{
    public required uint AreaId { get; init; }
    public required string Name { get; init; }
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
    public required Vector3 Center { get; init; }
    public required int MiniMapX { get; init; }
    public required int MiniMapY { get; init; }
    public required int WorldX { get; init; }
    public required int WorldY { get; init; }
    public required int SizeX { get; init; }
    public required int SizeY { get; init; }
    public required string Map { get; init; }
}

[ForExternalUse]
public sealed class SceneWorldInterestPointData
{
    public required uint PointId { get; init; }
    public required string Name { get; init; }
    public required Vector3 Position { get; init; }
    public required uint? AffiliatedAreaId { get; init; }
    public required string Extra { get; init; }
    public required SceneWorldPointSourceKind SourceKind { get; init; }
    public required string SourceGroup { get; init; }
}

[ForExternalUse]
public sealed class SceneWorldTeleportPointData
{
    public required string Name { get; init; }
    public required Vector3 Position { get; init; }
    public required SceneWorldPointSourceKind SourceKind { get; init; }
    public required string SourceGroup { get; init; }
    public required string SourcePageTitle { get; init; }
    public required string SourceFileRelativePath { get; init; }
}
