namespace L2Viewer.SceneDomain.Models;

public sealed class SceneMapContextData
{
    public required string SourcePath { get; init; }
    public required string MapKey { get; init; }
    public required string WorldModelName { get; init; }
    public required int WorldModelExportIndex { get; init; }
    public required Vector3 WorldBoundsMin { get; init; }
    public required Vector3 WorldBoundsMax { get; init; }
    public required int[] ForcedOutdoorZoneNumbers { get; init; }
    public required SceneMapProbeNodeData[] Nodes { get; init; }
    public required SceneMapZoneData[] Zones { get; init; }
    public required float MapAverageIndoorFogEnd { get; init; }
    public required float LevelDistanceFogEnd { get; init; }
    public required bool HasSunRotation { get; init; }
    public required Vector3 PrimarySunEulerDegrees { get; init; }
    public required bool HasMoonRotation { get; init; }
    public required Vector3 PrimaryMoonEulerDegrees { get; init; }
}

public sealed class SceneMapProbeNodeData
{
    public required Vector3 Normal { get; init; }
    public required float PlaneW { get; init; }
    public required int FrontNodeIndex { get; init; }
    public required int BackNodeIndex { get; init; }
    public required int PlaneNodeIndex { get; init; }
    public required byte Zone0 { get; init; }
    public required byte Zone1 { get; init; }
    public required int LeafIndex0 { get; init; }
    public required int LeafIndex1 { get; init; }
}

public sealed class SceneMapZoneData
{
    public required int ZoneNumber { get; init; }
    public required bool SunAffect { get; init; }
    public required string ZoneTag { get; init; }
    public required float? DistanceFogEnd { get; init; }
    public required bool DistanceFogEnabled { get; init; }
    public required bool TerrainZone { get; init; }
}
