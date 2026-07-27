namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public sealed class SceneSkillVisualData
{
    public required int SkillId { get; init; }
    public string? ResolvedEffectStem { get; init; }
    public required IReadOnlyList<string> ResolvedEffectStems { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
    public required IReadOnlyList<SceneSkillNameEntryData> Names { get; init; }
    public required IReadOnlyList<SceneSkillLevelData> Levels { get; init; }
    public required IReadOnlyList<SceneSkillSoundData> Sounds { get; init; }
    public required IReadOnlyList<SceneMobSkillTriggerData> MobTriggers { get; init; }
    public required IReadOnlyList<SceneSkillVisualEffectData> Effects { get; init; }
    public required IReadOnlyList<SceneMobSkillVisualData> MobVisuals { get; init; }
    public required IReadOnlyList<SceneSkillVisualStageData> Stages { get; init; }
}

public sealed class SceneSkillVisualEffectData
{
    public required string Stem { get; init; }
    public required string Source { get; init; }
    public required IReadOnlyList<SceneSkillVisualStageData> Stages { get; init; }
}

public sealed class SceneSkillNameEntryData
{
    public required int SkillLevel { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? DescriptionAdd1 { get; init; }
    public string? DescriptionAdd2 { get; init; }
}

public sealed class SceneSkillLevelData
{
    public required int SkillLevel { get; init; }
    public required int OperType { get; init; }
    public required int MpConsume { get; init; }
    public required int CastRange { get; init; }
    public required int CastStyle { get; init; }
    public required float HitTime { get; init; }
    public required int IsMagic { get; init; }
    public string? AnimationCharacter { get; init; }
    public string? DescriptionToken { get; init; }
    public string? IconName { get; init; }
    public string? IconName2 { get; init; }
    public required int IsEnchanted { get; init; }
    public required int EnchantedSkillId { get; init; }
    public required int HpConsume { get; init; }
}

public sealed class SceneSkillSoundData
{
    public required int SkillLevel { get; init; }
    public required IReadOnlyList<string> SpellEffectSounds { get; init; }
    public required IReadOnlyList<string> ShotEffectSounds { get; init; }
    public required IReadOnlyList<string> ExpEffectSounds { get; init; }
    public required IReadOnlyList<string> CharacterSubSounds { get; init; }
    public required IReadOnlyList<string> CharacterThrowSounds { get; init; }
    public required float SoundVolume { get; init; }
    public required float SoundRadius { get; init; }
}

public sealed class SceneMobSkillTriggerData
{
    public required int NpcId { get; init; }
    public required int SkillId { get; init; }
    public required string SequenceName { get; init; }
    public required string SkillName { get; init; }
    public required string NpcName { get; init; }
    public required string NpcClass { get; init; }
}

public sealed class SceneMobSkillVisualData
{
    public required int NpcId { get; init; }
    public required string NpcClass { get; init; }
    public required string MeshReference { get; init; }
    public required string MeshPackagePath { get; init; }
    public required string SequenceName { get; init; }
    public required string SequenceCategory { get; init; }
    public string? ActorEffectReference { get; init; }
    public string? ActorEffectPackagePath { get; init; }
}

public sealed class SceneSkillVisualStageData
{
    public required string StageKey { get; init; }
    public required int StageOrder { get; init; }
    public required string ObjectName { get; init; }
    public string? SuperClassName { get; init; }
    public required SceneResourceReference StageReference { get; init; }
    public required SceneResourceLocation StageResource { get; init; }
    public required IReadOnlyList<SceneResourceReference> EmitterReferences { get; init; }
    public required IReadOnlyList<SceneResourceLocation> EmitterResources { get; init; }
    public required IReadOnlyList<SceneSkillVisualLayerData> Layers { get; init; }
}

public sealed class SceneSkillVisualLayerData
{
    public required int ExportIndex { get; init; }
    public required string ObjectName { get; init; }
    public required string ClassName { get; init; }
    public string? LayerName { get; init; }
    public required SceneResourceReference LayerReference { get; init; }
    public required SceneResourceLocation LayerResource { get; init; }
    public string? StaticMeshReference { get; init; }
    public SceneResourceReference? StaticMeshResourceReference { get; init; }
    public SceneResourceLocation? StaticMeshResource { get; init; }
    public string? TextureReference { get; init; }
    public SceneResourceReference? TextureResourceReference { get; init; }
    public SceneResourceLocation? TextureResource { get; init; }
    public float? Opacity { get; init; }
    public float? FadeOutStartTime { get; init; }
    public bool FadeOut { get; init; }
    public float? FadeInEndTime { get; init; }
    public bool FadeIn { get; init; }
    public int? MaxParticles { get; init; }
    public UnrFloatRange? LifetimeRange { get; init; }
    public Vector3? Acceleration { get; init; }
    public UnrRangeVector? StartLocationRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
    public UnrRangeVector? StartSpinRange { get; init; }
    public UnrRangeVector? SpinsPerSecondRange { get; init; }
    public UnrParticleColorScale[] ColorScale { get; init; } = [];
    public UnrParticleSizeScale[] SizeScale { get; init; } = [];
}
