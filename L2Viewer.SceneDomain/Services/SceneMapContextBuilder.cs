using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.BSPServices;
using L2Viewer.SceneDomain.Services.CharacterServices;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneMapContextBuilder
{
    public SceneMapContextData Load(string path)
    {
        var unr = L2Viewer.UnrFile.UnrFileReader.Read(path);
        return Build(unr);
    }

    public SceneMapContextData Load(string path, string dbRootPath, string clientRootPath, string quadrant)
    {
        var unr = L2Viewer.UnrFile.UnrFileReader.Read(path);
        return Build(unr, dbRootPath, clientRootPath, quadrant);
    }

    public SceneMapContextData Build(L2Viewer.UnrFile.UnrFile unr)
    {
        return BuildCore(unr, dbRootPath: null, clientRootPath: null, quadrant: null);
    }

    public SceneMapContextData Build(L2Viewer.UnrFile.UnrFile unr, string dbRootPath, string clientRootPath, string quadrant)
    {
        return BuildCore(unr, dbRootPath, clientRootPath, quadrant);
    }

    private SceneMapContextData BuildCore(L2Viewer.UnrFile.UnrFile unr, string? dbRootPath, string? clientRootPath, string? quadrant)
    {
        var lightingBuilder = new SceneLightingBuilder();
        var fogBuilder = new SceneFogBuilder();
        var creatureBuilder = new SceneCreatureMapBuilder();
        var zones = fogBuilder.BuildFogZones(unr);
        var suns = lightingBuilder.BuildSuns(unr);
        var moons = lightingBuilder.BuildMoons(unr);
        var levelInfo = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrLevelInfoObject>()
            .OrderBy(x => x.ExportIndex)
            .FirstOrDefault();
        var worldModel = BspWorldModelPolicy.ResolvePreferredWorldModel(unr)
            ?? throw new InvalidOperationException($"World model not found in: {Path.GetFileName(unr.FilePath)}");

        var groupedZones = new List<SceneMapZoneData>();
        var indoorFogEnds = new List<float>();
        foreach (var group in zones
                     .Where(x => x.Region != null && x.Region.ZoneNumber.HasValue)
                     .GroupBy(x => (int)x.Region!.ZoneNumber!.Value)
                     .OrderBy(x => x.Key))
        {
            var chosen = group
                .OrderBy(x => x.SunAffect)
                .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.ZoneTag))
                .ThenBy(x => x.ExportIndex)
                .First();

            if (!chosen.SunAffect && chosen.DistanceFogEnd.HasValue && chosen.DistanceFogEnd.Value > 0f)
            {
                indoorFogEnds.Add(chosen.DistanceFogEnd.Value);
            }

            groupedZones.Add(new SceneMapZoneData
            {
                ZoneNumber = group.Key,
                SunAffect = chosen.SunAffect,
                ZoneTag = chosen.ZoneTag ?? string.Empty,
                DistanceFogEnd = chosen.DistanceFogEnd,
                DistanceFogEnabled = chosen.DistanceFogEnabled,
                TerrainZone = chosen.TerrainZone
            });
        }

        if (indoorFogEnds.Count == 0)
        {
            indoorFogEnds.AddRange(zones
                .Where(x => x.DistanceFogEnd.HasValue && x.DistanceFogEnd.Value > 0f)
                .Select(x => x.DistanceFogEnd!.Value));
        }

        var primarySun = suns.FirstOrDefault(x => x.WorldRotationEulerDegrees.HasValue);
        var primaryMoon = moons.FirstOrDefault(x => x.WorldRotationEulerDegrees.HasValue);
        var (worldBoundsMin, worldBoundsMax) = ComputeWorldBoundsFromPoints(worldModel);
        var creatures = string.IsNullOrWhiteSpace(dbRootPath) || string.IsNullOrWhiteSpace(clientRootPath) || string.IsNullOrWhiteSpace(quadrant)
            ? []
            : creatureBuilder.Build(dbRootPath, clientRootPath, quadrant, unr);

        return new SceneMapContextData
        {
            SourcePath = unr.FilePath,
            MapKey = Path.GetFileNameWithoutExtension(unr.FilePath),
            WorldModelName = worldModel.ObjectName,
            WorldModelExportIndex = worldModel.ExportIndex,
            WorldBoundsMin = worldBoundsMin,
            WorldBoundsMax = worldBoundsMax,
            ForcedOutdoorZoneNumbers = BspWorldModelPolicy.ResolveForcedOutdoorZones(unr.FilePath),
            Nodes = worldModel.Nodes.Select(x => new SceneMapProbeNodeData
            {
                Normal = new Vector3(x.Plane.X, x.Plane.Y, x.Plane.Z),
                PlaneW = x.Plane.W,
                FrontNodeIndex = x.FrontNodeIndex,
                BackNodeIndex = x.BackNodeIndex,
                PlaneNodeIndex = x.PlaneNodeIndex,
                Zone0 = x.Zone0,
                Zone1 = x.Zone1,
                LeafIndex0 = x.LeafIndex0,
                LeafIndex1 = x.LeafIndex1
            }).ToArray(),
            Zones = groupedZones.ToArray(),
            MapAverageIndoorFogEnd = indoorFogEnds.Count == 0 ? 0f : indoorFogEnds.Average(),
            LevelDistanceFogEnd = levelInfo?.DistanceFogEnd ?? 0f,
            HasSunRotation = primarySun?.WorldRotationEulerDegrees.HasValue == true,
            PrimarySunEulerDegrees = primarySun?.WorldRotationEulerDegrees ?? default,
            HasMoonRotation = primaryMoon?.WorldRotationEulerDegrees.HasValue == true,
            PrimaryMoonEulerDegrees = primaryMoon?.WorldRotationEulerDegrees ?? default,
            Creatures = creatures
        };
    }

    private static (Vector3 Min, Vector3 Max) ComputeWorldBoundsFromPoints(UnrModelObject model)
    {
        if (model.Points.Length == 0)
        {
            return (Vector3.Zero, Vector3.Zero);
        }

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (var i = 0; i < model.Points.Length; i++)
        {
            min = Vector3.Min(min, model.Points[i]);
            max = Vector3.Max(max, model.Points[i]);
        }

        return (min, max);
    }
}
