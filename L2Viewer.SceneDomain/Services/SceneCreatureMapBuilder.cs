using System.Numerics;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

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
        L2Viewer.UnrFile.UnrFile unr)
    {
        if (unr == null)
        {
            throw new ArgumentNullException(nameof(unr));
        }

        var worldModel = BspWorldModelPolicy.ResolvePreferredWorldModel(unr)
            ?? throw new InvalidOperationException($"World model not found in: {Path.GetFileName(unr.FilePath)}");
        var (worldBoundsMin, worldBoundsMax) = ComputeWorldBoundsFromPoints(worldModel);
        return Build(SceneSpawnVisualDataset.Load(dbRootPath, clientRootPath, quadrant, worldBoundsMin, worldBoundsMax));
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

            results.Add(new SceneCreatureSpawnData
            {
                VisualKey = BuildVisualKey(actorClass.Reference, mesh.Reference, textures.Select(x => x.Reference)),
                SpawnId = spawn.id,
                TemplateId = spawn.npc_templateid,
                SpawnLocationKey = spawn.location,
                DisplayName = string.IsNullOrWhiteSpace(npcName.Name) ? npc.name : npcName.Name,
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
