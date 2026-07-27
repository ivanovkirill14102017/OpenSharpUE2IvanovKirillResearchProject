using System.Text;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDomain.Services.SkillServices;

internal static class SceneSkillEffectResolver
{
    public static IReadOnlyList<SceneSkillVisualEffectData> ResolveEffects(
        string clientRoot,
        string lineageEffectPath,
        IReadOnlyList<SceneSkillLevelData> levels,
        IReadOnlyList<SceneSkillNameEntryData> names,
        IReadOnlyList<SceneSkillSoundData> sounds,
        ICollection<string> warnings)
    {
        var rawStages = UnrSkillEffectPackageReader.ReadStages(lineageEffectPath);
        var resourcePackageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);

        var effects = new List<SceneSkillVisualEffectData>();
        foreach (var candidate in BuildEffectCandidates(levels, names, sounds))
        {
            var stages = rawStages
                .Where(x => TryExtractFamilyStageKey(x.ObjectName, candidate.Stem) is not null)
                .Select(x => AdaptStage(clientRoot, lineageEffectPath, x, resourcePackageIndex, warnings))
                .ToArray();
            if (stages.Length == 0)
            {
                continue;
            }

            effects.Add(new SceneSkillVisualEffectData
            {
                Stem = candidate.Stem,
                Source = candidate.Source,
                Stages = stages
            });
        }

        if (effects.Count == 0)
        {
            warnings.Add("No matching LineageEffect families were resolved from skill names or sound effect aliases.");
        }

        return effects;
    }

    private static IReadOnlyList<(string Stem, string Source)> BuildEffectCandidates(
        IReadOnlyList<SceneSkillLevelData> levels,
        IReadOnlyList<SceneSkillNameEntryData> names,
        IReadOnlyList<SceneSkillSoundData> sounds)
    {
        var ordered = new List<(string Stem, string Source)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var normalizedName in names
                     .Select(x => NormalizeSkillStem(x.Name))
                     .Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            AddCandidate(normalizedName!, "skill-name", ordered, seen);
        }

        foreach (var soundStem in sounds
                     .SelectMany(x => x.SpellEffectSounds.Concat(x.ShotEffectSounds).Concat(x.ExpEffectSounds))
                     .Select(ExtractEffectStemFromSound)
                     .Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            AddCandidate(soundStem!, "sound-effect", ordered, seen);
        }

        foreach (var descriptionLinkStem in levels
                     .SelectMany(x => ResolveDescriptionLinkedStems(x.DescriptionToken, names, sounds))
                     .Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            AddCandidate(descriptionLinkStem!, "description-token", ordered, seen);
        }

        return ordered;
    }

    private static SceneSkillVisualStageData AdaptStage(
        string clientRoot,
        string lineageEffectPath,
        UnrSkillEffectStageObject stage,
        IReadOnlyDictionary<string, string> resourcePackageIndex,
        ICollection<string> warnings)
    {
        var packageName = Path.GetFileNameWithoutExtension(lineageEffectPath);
        var stageClassName = stage.SuperClassName ?? stage.DeclaredClassName;
        var stageReference = new SceneResourceReference
        {
            Reference = $"{packageName}.{stage.ObjectName}",
            ClassName = stageClassName,
            PackageName = packageName,
            ObjectName = stage.ObjectName
        };
        var stageResource = SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            lineageEffectPath,
            packageName,
            stage.ObjectName,
            stageClassName);

        var layers = stage.Layers
            .Select(x => AdaptLayer(clientRoot, lineageEffectPath, x, resourcePackageIndex, warnings))
            .ToArray();

        return new SceneSkillVisualStageData
        {
            StageKey = stage.StageKey,
            StageOrder = stage.StageOrder,
            ObjectName = stage.ObjectName,
            SuperClassName = stage.SuperClassName,
            StageReference = stageReference,
            StageResource = stageResource,
            EmitterReferences = layers.Select(x => x.LayerReference).ToArray(),
            EmitterResources = layers.Select(x => x.LayerResource).ToArray(),
            Layers = layers
        };
    }

    private static SceneSkillVisualLayerData AdaptLayer(
        string clientRoot,
        string lineageEffectPath,
        UnrSkillEffectLayerObject layer,
        IReadOnlyDictionary<string, string> resourcePackageIndex,
        ICollection<string> warnings)
    {
        var packageName = Path.GetFileNameWithoutExtension(lineageEffectPath);
        var layerReference = new SceneResourceReference
        {
            Reference = $"{packageName}.{layer.ObjectName}",
            ClassName = layer.ClassName,
            PackageName = packageName,
            ObjectName = layer.ObjectName
        };
        var layerResource = SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            lineageEffectPath,
            packageName,
            layer.ObjectName,
            layer.ClassName);

        var staticMeshReference = ToReferenceText(layer.StaticMeshReference);
        var textureReference = ToReferenceText(layer.TextureReference);
        var staticMeshResourceReference = TryBuildResourceReference(staticMeshReference, UnrealClassNames.StaticMesh);
        var textureResourceReference = TryBuildResourceReference(textureReference, UnrealClassNames.Texture);

        return new SceneSkillVisualLayerData
        {
            ExportIndex = layer.ExportIndex,
            ObjectName = layer.ObjectName,
            ClassName = layer.ClassName,
            LayerName = layer.LayerName,
            LayerReference = layerReference,
            LayerResource = layerResource,
            StaticMeshReference = staticMeshReference,
            StaticMeshResourceReference = staticMeshResourceReference,
            StaticMeshResource = TryResolveResourceLocation(staticMeshResourceReference, resourcePackageIndex, clientRoot, warnings),
            TextureReference = textureReference,
            TextureResourceReference = textureResourceReference,
            TextureResource = TryResolveResourceLocation(textureResourceReference, resourcePackageIndex, clientRoot, warnings),
            Opacity = layer.Opacity,
            FadeOutStartTime = layer.FadeOutStartTime,
            FadeOut = layer.FadeOut,
            FadeInEndTime = layer.FadeInEndTime,
            FadeIn = layer.FadeIn,
            MaxParticles = layer.MaxParticles,
            LifetimeRange = layer.LifetimeRange,
            Acceleration = layer.Acceleration,
            StartLocationRange = layer.StartLocationRange,
            StartSizeRange = layer.StartSizeRange,
            StartVelocityRange = layer.StartVelocityRange,
            StartSpinRange = layer.StartSpinRange,
            SpinsPerSecondRange = layer.SpinsPerSecondRange,
            ColorScale = layer.ColorScale,
            SizeScale = layer.SizeScale
        };
    }

    private static IEnumerable<string?> ResolveDescriptionLinkedStems(
        string? descriptionToken,
        IReadOnlyList<SceneSkillNameEntryData> names,
        IReadOnlyList<SceneSkillSoundData> sounds)
    {
        if (string.IsNullOrWhiteSpace(descriptionToken) || !descriptionToken.StartsWith("skill.el.", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        foreach (var stem in names.Select(x => NormalizeSkillStem(x.Name)).Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            yield return stem;
        }

        foreach (var stem in sounds
                     .SelectMany(x => x.SpellEffectSounds.Concat(x.ShotEffectSounds).Concat(x.ExpEffectSounds))
                     .Select(ExtractEffectStemFromSound)
                     .Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            yield return stem;
        }
    }

    private static void AddCandidate(string value, string source, ICollection<(string Stem, string Source)> ordered, ISet<string> seen)
    {
        if (seen.Add(value))
        {
            ordered.Add((value, source));
        }
    }

    private static string? NormalizeSkillStem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasUnderscore = false;
        foreach (var c in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasUnderscore = false;
                continue;
            }

            if (!lastWasUnderscore)
            {
                builder.Append('_');
                lastWasUnderscore = true;
            }
        }

        return builder.ToString().Trim('_');
    }

    private static string? ExtractEffectStemFromSound(string soundReference)
    {
        if (string.IsNullOrWhiteSpace(soundReference))
        {
            return null;
        }

        var token = soundReference[(soundReference.LastIndexOf('.') + 1)..].Trim().ToLowerInvariant();
        foreach (var suffix in new[] { "_shot", "_explotion", "_explosion", "_cast", "_hit", "_start", "_end" })
        {
            if (token.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && token.Length > suffix.Length)
            {
                return token[..^suffix.Length];
            }
        }

        return null;
    }

    private static string? TryExtractFamilyStageKey(string objectName, string stem)
    {
        if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(stem))
        {
            return null;
        }

        var suffixIndex = objectName.LastIndexOf('_');
        if (suffixIndex <= 0 || suffixIndex >= objectName.Length - 1)
        {
            return null;
        }

        var stageKey = objectName[(suffixIndex + 1)..];
        var prefix = objectName[..suffixIndex];
        return prefix.EndsWith($"_{stem}", StringComparison.OrdinalIgnoreCase) ? stageKey : null;
    }

    private static string? ToReferenceText(UnrFileObjectReference? reference)
    {
        if (reference is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(reference.PackageName)
            ? reference.ObjectName
            : $"{reference.PackageName}.{reference.ObjectName}";
    }

    private static SceneResourceReference? TryBuildResourceReference(string? reference, string className)
    {
        return string.IsNullOrWhiteSpace(reference)
            ? null
            : SceneReferenceUtilities.BuildFromDbResourceReference(reference, className);
    }

    private static SceneResourceLocation? TryResolveResourceLocation(
        SceneResourceReference? reference,
        IReadOnlyDictionary<string, string> resourcePackageIndex,
        string clientRoot,
        ICollection<string> warnings)
    {
        if (reference is null)
        {
            return null;
        }

        if (!resourcePackageIndex.TryGetValue(reference.PackageName, out var packagePath))
        {
            warnings.Add($"Package '{reference.PackageName}' was not found while resolving '{reference.Reference}'.");
            return null;
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            packagePath,
            reference.PackageName,
            reference.ObjectName,
            reference.ClassName);
    }
}
