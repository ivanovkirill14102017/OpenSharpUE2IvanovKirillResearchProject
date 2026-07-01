using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

internal static class SceneSkeletalAnimationRoutingResolver
{
    public static (IReadOnlyList<SceneSkeletalAnimationRoutingProfile> Profiles, IReadOnlyList<string> Warnings, bool RequiresExplicitConsumerRouting) BuildRoutingMetadata(
        string packagePath,
        string meshObjectName,
        IReadOnlyList<SceneSkeletalAnimationSequence> sequences)
    {
        var warnings = new List<string>();
        var unknownSequences = sequences
            .Where(x => x.RequiresExplicitRouting)
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownSequences.Length > 0)
        {
            warnings.Add($"Unclassified animation sequences require explicit consumer routing: {string.Join(", ", unknownSequences)}.");
        }

        if (!TryResolveClientRoot(packagePath, out var clientRoot))
        {
            warnings.Add($"Client root could not be resolved from package path '{packagePath}'. DAT-based skill triggers were not loaded.");
            return ([], warnings, true);
        }

        var systemRoot = Path.Combine(clientRoot, "system");
        var npcGrpPath = Path.Combine(systemRoot, "npcgrp.dat");
        var npcNamePath = Path.Combine(systemRoot, "npcname-e.dat");
        var mobSkillAnimPath = Path.Combine(systemRoot, "MobSkillAnimgrp.dat");
        if (!File.Exists(npcGrpPath) || !File.Exists(npcNamePath) || !File.Exists(mobSkillAnimPath))
        {
            warnings.Add($"Client system DAT files were not found under '{systemRoot}'. DAT-based skill triggers were not loaded.");
            return ([], warnings, true);
        }

        var packageName = Path.GetFileNameWithoutExtension(packagePath);
        var meshReference = $"{packageName}.{meshObjectName}";
        var npcVisuals = DatFileReader.ReadDocument<NpcGrpDatDocument>(npcGrpPath).Entries
            .Where(x => string.Equals(x.Mesh, meshReference, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Tag)
            .ToArray();
        if (npcVisuals.Length == 0)
        {
            warnings.Add($"No npcgrp.dat entries were found for mesh '{meshReference}'. Consumer should consider all sequences eligible.");
            return ([], warnings, true);
        }

        var npcNames = DatFileReader.ReadDocument<NpcNameDatDocument>(npcNamePath).Entries
            .ToDictionary(x => (int)x.Id);
        var mobSkillAnimations = DatFileReader.ReadDocument<MobSkillAnimGrpDatDocument>(mobSkillAnimPath).Entries
            .GroupBy(x => x.NpcId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.SkillId).ToArray());
        var sequenceCategoryByName = sequences
            .ToDictionary(x => x.Name, x => x.Category, StringComparer.OrdinalIgnoreCase);

        var profiles = new List<SceneSkeletalAnimationRoutingProfile>(npcVisuals.Length);
        foreach (var npcVisual in npcVisuals)
        {
            var npcId = (int)npcVisual.Tag;
            mobSkillAnimations.TryGetValue(npcId, out var triggers);
            triggers ??= [];

            var missingTriggerSequences = triggers
                .Where(x => !sequenceCategoryByName.ContainsKey(x.SequenceName))
                .Select(x => $"{x.SkillId}:{x.SequenceName}")
                .ToArray();
            if (missingTriggerSequences.Length > 0)
            {
                warnings.Add($"NPC {npcId} ({npcVisual.Class}) references unknown animation sequences: {string.Join(", ", missingTriggerSequences)}.");
            }

            npcNames.TryGetValue(npcId, out var npcName);
            profiles.Add(new SceneSkeletalAnimationRoutingProfile
            {
                NpcId = npcId,
                NpcServerName = triggers.Select(x => x.NpcName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                NpcDisplayName = npcName?.Name,
                NpcClass = npcVisual.Class,
                MeshReference = meshReference,
                NpcSpeed = npcVisual.NpcSpeed,
                SuggestedDefaultSequenceNames = BuildDefaultSequenceNames(sequences),
                SuggestedCombatIdleSequenceNames = BuildCombatIdleSequenceNames(sequences),
                SuggestedSkillIdleSequenceNames = BuildSkillIdleSequenceNames(sequences),
                SkillTriggers = triggers
                    .Select(x => new SceneSkeletalSkillAnimationTrigger
                    {
                        SkillId = x.SkillId,
                        SkillName = x.SkillName,
                        SequenceName = x.SequenceName,
                        SequenceCategory = sequenceCategoryByName.TryGetValue(x.SequenceName, out var category) ? category : "unknown",
                        IsSocialLikeSequence = SceneSkeletalAnimationSemantics.IsSocialLikeSequenceName(x.SequenceName)
                    })
                    .ToArray()
            });
        }

        if (profiles.All(x => x.SkillTriggers.Count == 0))
        {
            warnings.Add($"No explicit MobSkillAnimgrp.dat triggers were found for mesh '{meshReference}'. Consumer should consider all sequences eligible.");
        }

        var requiresExplicitConsumerRouting = unknownSequences.Length > 0 ||
                                              warnings.Count > 0 ||
                                              profiles.All(x => x.SkillTriggers.Count == 0);
        return (profiles, warnings, requiresExplicitConsumerRouting);
    }

    private static bool TryResolveClientRoot(string packagePath, out string clientRoot)
    {
        clientRoot = string.Empty;
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            return false;
        }

        var current = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(packagePath)) ?? string.Empty);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "system")))
            {
                clientRoot = current.FullName;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static IReadOnlyList<string> BuildDefaultSequenceNames(IReadOnlyList<SceneSkeletalAnimationSequence> sequences)
    {
        var defaults = sequences
            .Where(x => string.Equals(x.Category, "idle", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return defaults.Length > 0 ? defaults : [];
    }

    private static IReadOnlyList<string> BuildCombatIdleSequenceNames(IReadOnlyList<SceneSkeletalAnimationSequence> sequences)
    {
        return sequences
            .Where(x => string.Equals(x.Category, "combat_idle", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildSkillIdleSequenceNames(IReadOnlyList<SceneSkeletalAnimationSequence> sequences)
    {
        return sequences
            .Where(x => string.Equals(x.Category, "combat_skill_idle", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
