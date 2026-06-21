namespace L2Viewer.DatFile;

public abstract record DatDocument(string Path);

public sealed record SkillNameDatDocument(
    string Path,
    IReadOnlyList<SkillNameDatEntry> Entries) : DatDocument(Path);

public sealed record SkillNameDatEntry(
    int SkillId,
    int SkillLevel,
    string Name,
    string Description,
    string DescriptionAdd1,
    string DescriptionAdd2);

public sealed record SkillGrpCt15DatDocument(
    string Path,
    IReadOnlyList<SkillGrpCt15DatEntry> Entries) : DatDocument(Path);

public sealed record SkillGrpCt15DatEntry(
    int SkillId,
    int SkillLevel,
    int OperType,
    int MpConsume,
    int CastRange,
    int CastStyle,
    int Unknown0,
    float HitTime,
    int IsMagic,
    string AnimationCharacter,
    string DescriptionToken,
    string IconName,
    string IconName2,
    int IsEnchanted,
    int EnchantedSkillId,
    int HpConsume,
    int Unknown1,
    int Unknown2,
    int Unknown3);

public sealed record MobSkillAnimGrpDatDocument(
    string Path,
    IReadOnlyList<MobSkillAnimGrpDatEntry> Entries) : DatDocument(Path);

public sealed record MobSkillAnimGrpDatEntry(
    int NpcId,
    int SkillId,
    string SequenceName,
    string SkillName,
    string NpcName,
    string NpcClass);

public sealed record StaticObjectDatDocument(
    string Path,
    IReadOnlyList<StaticObjectDatEntry> Entries) : DatDocument(Path);

public sealed record StaticObjectDatEntry(
    uint Id,
    string Name);

public sealed record ZoneNameDatDocument(
    string Path,
    IReadOnlyList<ZoneNameDatEntry> Entries) : DatDocument(Path);

public sealed record ZoneNameDatEntry(
    uint Id,
    uint ZoneColorId,
    uint XWorldGrid,
    uint YWorldGrid,
    float TopZ,
    float BottomZ,
    string ZoneName,
    IReadOnlyList<int> Coordinates,
    float Unknown01,
    string Map);

public sealed record HuntingZoneDatDocument(
    string Path,
    IReadOnlyList<HuntingZoneDatEntry> Entries) : DatDocument(Path);

public sealed record HuntingZoneDatEntry(
    uint Id,
    uint HuntingType,
    uint Level,
    uint Unknown1,
    float LocationX,
    float LocationY,
    float LocationZ,
    string Extra,
    uint AffiliatedAreaId,
    string Name);

public sealed record VariationEffectGrpDatDocument(
    string Path,
    IReadOnlyList<VariationEffectGrpDatEntry> Entries) : DatDocument(Path);

public sealed record VariationEffectGrpDatEntry(
    uint Unknown0,
    uint Unknown1,
    uint Unknown2,
    uint Unknown3,
    uint Unknown4,
    string Effect,
    string Attribute);
