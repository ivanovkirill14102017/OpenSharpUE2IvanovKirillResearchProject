using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.CharacterServices;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.SkillServices;

internal static class SceneSkillMobVisualResolver
{
    public static IReadOnlyList<SceneMobSkillVisualData> BuildMobVisuals(
        string clientRoot,
        IReadOnlyList<SceneMobSkillTriggerData> mobTriggers,
        ICollection<string> warnings)
    {
        if (mobTriggers.Count == 0)
        {
            return [];
        }

        var npcGrpPath = Path.Combine(clientRoot, "system", "npcgrp.dat");
        if (!File.Exists(npcGrpPath))
        {
            warnings.Add($"NPC visual package was not found: '{npcGrpPath}'.");
            return [];
        }

        var npcVisuals = DatFileReader.ReadDocument<NpcGrpDatDocument>(npcGrpPath).Entries;
        var npcVisualById = npcVisuals
            .GroupBy(x => (int)x.Tag)
            .ToDictionary(x => x.Key, x => x.First());
        var resourcePackageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);
        var skeletalBuilder = new SceneSkeletalAssetBuilder();
        var sequenceCategoryByMesh = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SceneMobSkillVisualData>(mobTriggers.Count);

        foreach (var trigger in mobTriggers.OrderBy(x => x.NpcId).ThenBy(x => x.SequenceName, StringComparer.OrdinalIgnoreCase))
        {
            if (!npcVisualById.TryGetValue(trigger.NpcId, out var npcVisual))
            {
                continue;
            }

            if (!TrySplitReference(npcVisual.Mesh, out var meshPackageName, out var meshObjectName) ||
                !resourcePackageIndex.TryGetValue(meshPackageName, out var meshPackagePath))
            {
                continue;
            }

            if (!sequenceCategoryByMesh.TryGetValue(npcVisual.Mesh, out var sequenceCategoryMap))
            {
                var asset = skeletalBuilder.BuildNamed(meshPackagePath, meshObjectName);
                sequenceCategoryMap = asset.AnimationSet.Sequences
                    .ToDictionary(x => x.Name, x => x.Category, StringComparer.OrdinalIgnoreCase);
                sequenceCategoryByMesh[npcVisual.Mesh] = sequenceCategoryMap;
            }

            string? actorEffectPackagePath = null;
            if (TrySplitReference(npcVisual.Effect, out var actorEffectPackageName, out _) &&
                resourcePackageIndex.TryGetValue(actorEffectPackageName, out var resolvedActorEffectPath))
            {
                actorEffectPackagePath = resolvedActorEffectPath;
            }

            result.Add(new SceneMobSkillVisualData
            {
                NpcId = trigger.NpcId,
                NpcClass = trigger.NpcClass,
                MeshReference = npcVisual.Mesh,
                MeshPackagePath = meshPackagePath,
                SequenceName = trigger.SequenceName,
                SequenceCategory = sequenceCategoryMap.TryGetValue(trigger.SequenceName, out var category) ? category : "unknown",
                ActorEffectReference = string.IsNullOrWhiteSpace(npcVisual.Effect) ? null : npcVisual.Effect,
                ActorEffectPackagePath = actorEffectPackagePath
            });
        }

        return result;
    }

    private static bool TrySplitReference(string? reference, out string packageName, out string objectName)
    {
        packageName = string.Empty;
        objectName = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var separatorIndex = reference.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex >= reference.Length - 1)
        {
            return false;
        }

        packageName = reference[..separatorIndex];
        objectName = reference[(separatorIndex + 1)..];
        return true;
    }
}
