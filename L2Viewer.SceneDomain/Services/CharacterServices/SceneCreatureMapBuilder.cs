using System.Numerics;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

[ForExternalUse]
public sealed class SceneCreatureMapBuilder
{
    public SceneCreatureSpawnData[] Build(string dbRootPath, string clientRootPath, string quadrant)
    {
        return Build(SceneSpawnVisualDataset.Load(dbRootPath, clientRootPath, quadrant));
    }

    public SceneCreatureSpawnData[] Build(
        string dbRootPath,
        string clientRootPath,
        string quadrant,
        UnrFile.UnrFile unr)
    {
        if (unr == null)
        {
            throw new ArgumentNullException(nameof(unr));
        }

        var primaryTerrain = ResolvePrimaryTerrain(clientRootPath, unr);
        var dataset = SceneSpawnVisualDataset.Load(
            dbRootPath,
            clientRootPath,
            quadrant,
            primaryTerrain?.WorldMinCorner,
            primaryTerrain?.WorldMaxCorner);
        return Build(dataset);
    }

    internal SceneCreatureSpawnData[] Build(SceneSpawnVisualDataset dataset)
    {
        var results = new List<SceneCreatureSpawnData>(dataset.Spawns.Count);
        foreach (var spawn in dataset.Spawns)
        {
            var npc = dataset.GetNpc(spawn.npc_templateid, spawn.id);
            var visual = dataset.GetVisual(spawn.npc_templateid, spawn.id);
            var npcName = dataset.GetName(spawn.npc_templateid, spawn.id);
            if (string.IsNullOrWhiteSpace(visual.Mesh))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(visual.Class))
            {
                continue;
            }

            var actorClass = dataset.GetResourceLocation(visual.Class, "Class", spawn.npc_templateid, spawn.id);
            var mesh = dataset.GetResourceLocation(visual.Mesh, "SkeletalMesh", spawn.npc_templateid, spawn.id);
            var textures = dataset.GetResourceLocations(
                visual.Textures1.Concat(visual.Textures2).Distinct(StringComparer.OrdinalIgnoreCase),
                "Texture",
                spawn.npc_templateid,
                spawn.id);
            var displayName = string.IsNullOrWhiteSpace(npcName.Name) ? npc.name : npcName.Name;

            results.Add(new SceneCreatureSpawnData
            {
                StableName = SceneStableNameUtility.BuildCreatureStableName(
                    dataset.Quadrant,
                    spawn.location,
                    spawn.id,
                    spawn.npc_templateid,
                    displayName),
                VisualKey = BuildVisualKey(actorClass.Reference, mesh.Reference, textures.Select(x => x.Reference)),
                SpawnId = spawn.id,
                TemplateId = spawn.npc_templateid,
                SpawnLocationKey = spawn.location,
                DisplayName = displayName,
                DbClassName = npc.@class,
                ActorClassResource = actorClass,
                MeshResource = mesh,
                TextureResources = textures,
                Heading = spawn.heading,
                SpawnCount = spawn.count,
                RandomOffsetX = spawn.randomx,
                RandomOffsetY = spawn.randomy,
                CollisionRadius = SceneSpawnVisualDataset.DecimalToSingle(npc.collision_radius),
                CollisionHeight = SceneSpawnVisualDataset.DecimalToSingle(npc.collision_height),
                Position = new Vector3(spawn.locx, spawn.locy, spawn.locz)
            });
        }

        return results
            .OrderBy(x => x.SpawnLocationKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SpawnId)
            .ToArray();
    }

    private static string BuildVisualKey(string actorClass, string mesh, IEnumerable<string> textures)
    {
        return string.Join("|", new[] { actorClass, mesh }.Concat(textures.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
    }

    private static TerrainImportData? ResolvePrimaryTerrain(string clientRootPath, UnrFile.UnrFile unr)
    {
        var terrainBuilder = new TerrainImportBuilder(new BspTextureManager(clientRootPath));
        return terrainBuilder.Build(unr).FirstOrDefault();
    }
}
