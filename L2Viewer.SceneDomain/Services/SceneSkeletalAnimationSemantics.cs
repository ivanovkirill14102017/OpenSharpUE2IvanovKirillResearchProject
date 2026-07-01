using L2Viewer.SceneDomain.Models;
using L2Viewer.UkxFile;

namespace L2Viewer.SceneDomain.Services;

internal static class SceneSkeletalAnimationSemantics
{
    public static SceneSkeletalAnimationSet BuildAnimationSet(
        global::L2Viewer.UkxFile.UkxFile ukx,
        UkxMeshAnimationObject sourceAnimation,
        BlenderCompatAnimationSet animationSet)
    {
        var sequenceNames = animationSet.Sequences
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
        var sourceSequenceByName = sourceAnimation.AnimSequences
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        return new SceneSkeletalAnimationSet
        {
            Name = animationSet.Name,
            Bones = animationSet.Bones
                .Select((bone, index) => new SceneSkeletalAnimationBone
                {
                    Index = index,
                    Name = bone.Name,
                    ParentIndex = bone.ParentIndex
                })
                .ToArray(),
            Sequences = animationSet.Sequences.Select(x => new SceneSkeletalAnimationSequence
            {
                Name = x.Name,
                NormalizedName = NormalizeSequenceName(x.Name),
                Category = ClassifySequenceCategory(x.Name),
                TotalBones = x.TotalBones,
                TrackTime = x.TrackTime,
                AnimRate = x.AnimRate,
                FirstRawFrame = x.FirstRawFrame,
                NumRawFrames = x.NumRawFrames,
                SuggestedLoop = IsSuggestedLoop(x.Name),
                IsOneShot = IsOneShot(x.Name),
                RequiresExplicitRouting = IsUnknownSequence(x.Name),
                SuggestedNextSequenceNames = BuildSuggestedNextSequenceNames(x.Name, sequenceNames),
                Notifies = sourceSequenceByName.TryGetValue(x.Name, out var sourceSequence)
                    ? MapNotifies(ukx, sourceSequence.Notifies)
                    : []
            }).ToArray(),
            Keys = animationSet.Keys.Select(x => new SceneSkeletalAnimationKey
            {
                Position = x.Position,
                Orientation = x.Orientation,
                Time = x.Time
            }).ToArray()
        };
    }

    public static string ClassifySequenceCategory(string? name)
    {
        var normalized = NormalizeSequenceName(name);
        if (normalized.Length == 0)
        {
            return "unknown";
        }

        if (normalized.Contains("deathwait", StringComparison.OrdinalIgnoreCase))
        {
            return "death_hold";
        }

        if (normalized.Contains("death", StringComparison.OrdinalIgnoreCase))
        {
            return "death";
        }

        if (normalized.StartsWith("social", StringComparison.OrdinalIgnoreCase))
        {
            return "social";
        }

        if (normalized.Contains("spwait", StringComparison.OrdinalIgnoreCase))
        {
            return "combat_skill_idle";
        }

        if (normalized.Contains("atkwait", StringComparison.OrdinalIgnoreCase))
        {
            return "combat_idle";
        }

        if (normalized == "wait" || normalized.EndsWith("wait", StringComparison.OrdinalIgnoreCase))
        {
            return "idle";
        }

        if (normalized.Contains("run", StringComparison.OrdinalIgnoreCase))
        {
            return "run";
        }

        if (normalized.Contains("walk", StringComparison.OrdinalIgnoreCase))
        {
            return "walk";
        }

        if (normalized.StartsWith("spatk", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("cast", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("spell", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("magic", StringComparison.OrdinalIgnoreCase))
        {
            return "skill";
        }

        if (normalized.StartsWith("atk", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("attack", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("strike", StringComparison.OrdinalIgnoreCase))
        {
            return "attack";
        }

        return "unknown";
    }

    public static bool IsSocialLikeSequenceName(string? name)
    {
        return string.Equals(ClassifySequenceCategory(name), "social", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<SceneSkeletalAnimationNotify> MapNotifies(global::L2Viewer.UkxFile.UkxFile ukx, IReadOnlyList<UkxMeshAnimNotify> notifies)
    {
        return notifies
            .Select(notify =>
            {
                var notifyClassName = ResolveNotifyClassName(ukx, notify);
                return new SceneSkeletalAnimationNotify
                {
                    Time = notify.Time,
                    FunctionName = string.IsNullOrWhiteSpace(notify.FunctionName) ? null : notify.FunctionName,
                    NotifyClassName = notifyClassName,
                    NotifyObjectName = notify.NotifyObjectReference?.ObjectName,
                    ExtraText = string.IsNullOrWhiteSpace(notify.LineageExtraText) ? null : notify.LineageExtraText,
                    IsCombatImpact = string.Equals(notifyClassName, "AnimNotify_AttackItem", StringComparison.OrdinalIgnoreCase),
                    IsProjectileRelease = string.Equals(notifyClassName, "AnimNotify_AttackShot", StringComparison.OrdinalIgnoreCase),
                    IsSoundCue = string.Equals(notifyClassName, "AnimNotify_Sound", StringComparison.OrdinalIgnoreCase)
                };
            })
            .ToArray();
    }

    private static string? ResolveNotifyClassName(global::L2Viewer.UkxFile.UkxFile ukx, UkxMeshAnimNotify notify)
    {
        if (notify.NotifyObjectReference?.ExportIndex is int exportIndex &&
            exportIndex >= 0 &&
            exportIndex < ukx.ExportObjects.Count)
        {
            return ukx.ExportObjects[exportIndex].Object.ClassName;
        }

        return notify.NotifyObjectReference?.ClassName;
    }

    private static string NormalizeSequenceName(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim().ToLowerInvariant();
    }

    private static bool IsSuggestedLoop(string? name)
    {
        return ClassifySequenceCategory(name) switch
        {
            "idle" => true,
            "walk" => true,
            "run" => true,
            "combat_idle" => true,
            "combat_skill_idle" => true,
            "death_hold" => true,
            _ => false
        };
    }

    private static bool IsOneShot(string? name)
    {
        return ClassifySequenceCategory(name) switch
        {
            "attack" => true,
            "skill" => true,
            "social" => true,
            "death" => true,
            _ => false
        };
    }

    private static bool IsUnknownSequence(string? name)
    {
        return string.Equals(ClassifySequenceCategory(name), "unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> BuildSuggestedNextSequenceNames(string? name, IReadOnlyList<string> sequenceNames)
    {
        return ClassifySequenceCategory(name) switch
        {
            "attack" => PreferSequences(sequenceNames, "atkwait", "wait"),
            "skill" => PreferSequences(sequenceNames, "spwait01", "atkwait", "wait"),
            "social" => PreferSequences(sequenceNames, "wait"),
            "death" => PreferSequences(sequenceNames, "deathwait"),
            _ => []
        };
    }

    private static string[] PreferSequences(IReadOnlyList<string> sequenceNames, params string[] candidates)
    {
        var result = new List<string>(candidates.Length);
        foreach (var candidate in candidates)
        {
            var match = sequenceNames.FirstOrDefault(x => string.Equals(x, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
            {
                result.Add(match);
            }
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
