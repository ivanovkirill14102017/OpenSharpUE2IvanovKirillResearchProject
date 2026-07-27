using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.SkillServices;

namespace L2Viewer.SceneDomain.Services;

[ForExternalUse]
public sealed class SceneSkillVisualBuilder
{
    public SceneSkillVisualData Build(string clientRoot, int skillId)
    {
        if (string.IsNullOrWhiteSpace(clientRoot))
        {
            throw new ArgumentException("Client root is required.", nameof(clientRoot));
        }

        var fullClientRoot = Path.GetFullPath(clientRoot);
        var systemRoot = Path.Combine(fullClientRoot, "system");
        var warnings = new List<string>();

        var skillNamePath = Path.Combine(systemRoot, "skillname-e.dat");
        var skillGrpPath = Path.Combine(systemRoot, "skillgrp.dat");
        var skillSoundPath = Path.Combine(systemRoot, "skillsoundgrp.dat");
        var mobSkillAnimPath = Path.Combine(systemRoot, "MobSkillAnimgrp.dat");
        var lineageEffectPath = Path.Combine(systemRoot, "LineageEffect.u");

        var names = ReadOptional(skillNamePath, warnings, () => DatFileReader.ReadDocument<SkillNameDatDocument>(skillNamePath).Entries
            .Where(x => x.SkillId == skillId)
            .OrderBy(x => x.SkillLevel)
            .Select(x => new SceneSkillNameEntryData
            {
                SkillLevel = x.SkillLevel,
                Name = x.Name,
                Description = NullIfEmpty(x.Description),
                DescriptionAdd1 = NullIfEmpty(x.DescriptionAdd1),
                DescriptionAdd2 = NullIfEmpty(x.DescriptionAdd2)
            })
            .ToArray());

        var levels = ReadOptional(skillGrpPath, warnings, () => DatFileReader.ReadDocument<SkillGrpCt15DatDocument>(skillGrpPath).Entries
            .Where(x => x.SkillId == skillId)
            .OrderBy(x => x.SkillLevel)
            .Select(x => new SceneSkillLevelData
            {
                SkillLevel = x.SkillLevel,
                OperType = x.OperType,
                MpConsume = x.MpConsume,
                CastRange = x.CastRange,
                CastStyle = x.CastStyle,
                HitTime = x.HitTime,
                IsMagic = x.IsMagic,
                AnimationCharacter = NullIfEmpty(x.AnimationCharacter),
                DescriptionToken = NullIfEmpty(x.DescriptionToken),
                IconName = NullIfEmpty(x.IconName),
                IconName2 = NullIfEmpty(x.IconName2),
                IsEnchanted = x.IsEnchanted,
                EnchantedSkillId = x.EnchantedSkillId,
                HpConsume = x.HpConsume
            })
            .ToArray());

        var sounds = ReadOptional(skillSoundPath, warnings, () => DatFileReader.ReadDocument<SkillSoundGrpDatDocument>(skillSoundPath).Entries
            .Where(x => x.SkillId == skillId)
            .OrderBy(x => x.SkillLevel)
            .Select(x => new SceneSkillSoundData
            {
                SkillLevel = (int)x.SkillLevel,
                SpellEffectSounds = x.SpellEffectSounds.Where(static y => !string.IsNullOrWhiteSpace(y)).ToArray(),
                ShotEffectSounds = x.ShotEffectSounds.Where(static y => !string.IsNullOrWhiteSpace(y)).ToArray(),
                ExpEffectSounds = x.ExpEffectSounds.Where(static y => !string.IsNullOrWhiteSpace(y)).ToArray(),
                CharacterSubSounds = x.CharacterSubSounds.Where(static y => !string.IsNullOrWhiteSpace(y)).ToArray(),
                CharacterThrowSounds = x.CharacterThrowSounds.Where(static y => !string.IsNullOrWhiteSpace(y)).ToArray(),
                SoundVolume = x.SoundVolume,
                SoundRadius = x.SoundRadius
            })
            .ToArray());

        var mobTriggers = ReadOptional(mobSkillAnimPath, warnings, () => DatFileReader.ReadDocument<MobSkillAnimGrpDatDocument>(mobSkillAnimPath).Entries
            .Where(x => x.SkillId == skillId)
            .OrderBy(x => x.NpcId)
            .ThenBy(x => x.SequenceName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SceneMobSkillTriggerData
            {
                NpcId = x.NpcId,
                SkillId = x.SkillId,
                SequenceName = x.SequenceName,
                SkillName = x.SkillName,
                NpcName = x.NpcName,
                NpcClass = x.NpcClass
            })
            .ToArray());

        IReadOnlyList<SceneSkillVisualEffectData> effects = [];
        if (File.Exists(lineageEffectPath))
        {
            effects = SceneSkillEffectResolver.ResolveEffects(fullClientRoot, lineageEffectPath, levels, names, sounds, warnings);
        }
        else
        {
            warnings.Add($"Effect package was not found: '{lineageEffectPath}'.");
        }

        var mobVisuals = SceneSkillMobVisualResolver.BuildMobVisuals(fullClientRoot, mobTriggers, warnings);
        var resolvedStems = effects.Select(x => x.Stem).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return new SceneSkillVisualData
        {
            SkillId = skillId,
            ResolvedEffectStem = resolvedStems.FirstOrDefault(),
            ResolvedEffectStems = resolvedStems,
            Warnings = warnings,
            Names = names,
            Levels = levels,
            Sounds = sounds,
            MobTriggers = mobTriggers,
            Effects = effects,
            MobVisuals = mobVisuals,
            Stages = effects.SelectMany(x => x.Stages).ToArray()
        };
    }

    private static T[] ReadOptional<T>(string path, ICollection<string> warnings, Func<T[]> loader)
    {
        if (!File.Exists(path))
        {
            warnings.Add($"Optional DAT file was not found: '{path}'.");
            return [];
        }

        return loader();
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
