namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public enum SceneCharacterBaseClass
{
    HumanFighter = 0,
    HumanMage = 10,
    ElfFighter = 18,
    ElfMage = 25,
    DarkElfFighter = 31,
    DarkElfMage = 38,
    OrcFighter = 44,
    OrcMage = 49,
    DwarvenFighter = 53
}

[ForExternalUse]
public enum SceneCharacterGender
{
    Male = 0,
    Female = 1
}

[ForExternalUse]
public enum SceneCharacterVisualFamily
{
    MaleHumanFighter,
    FemaleHumanFighter,
    MaleHumanMystic,
    FemaleHumanMystic,
    MaleElf,
    FemaleElf,
    MaleDarkElf,
    FemaleDarkElf,
    MaleOrcFighter,
    FemaleOrcFighter,
    MaleOrcMage,
    FemaleOrcMage,
    MaleDwarf,
    FemaleDwarf
}

[ForExternalUse]
public enum SceneCharacterEquipmentSlot
{
    Face,
    Upper,
    Lower,
    Gloves,
    Boots
}

[ForExternalUse]
public sealed class SceneCharacterAppearanceRequest
{
    public required SceneCharacterBaseClass BaseClass { get; init; }
    public required SceneCharacterGender Gender { get; init; }
    public int? UpperItemId { get; init; }
    public int? LowerItemId { get; init; }
    public int? GlovesItemId { get; init; }
    public int? BootsItemId { get; init; }
}

[ForExternalUse]
public sealed class SceneCharacterAppearanceData
{
    public required SceneCharacterBaseClass BaseClass { get; init; }
    public required SceneCharacterGender Gender { get; init; }
    public required SceneCharacterVisualFamily VisualFamily { get; init; }
    public required int CharGrpIndex { get; init; }
    public required SceneResourceReference SkeletonMeshResource { get; init; }
    public required SceneResourceLocation SkeletonMeshLocation { get; init; }
    public required string SkeletonName { get; init; }
    public required int SkeletonBoneCount { get; init; }
    public required IReadOnlyList<SceneCharacterResolvedPartData> Parts { get; init; }
}

public sealed class SceneCharacterResolvedPartData
{
    public required SceneCharacterEquipmentSlot Slot { get; init; }
    public int? ItemId { get; init; }
    public required bool IsBasePart { get; init; }
    public required SceneResourceReference[] MeshResources { get; init; }
    public required SceneResourceReference[] TextureResources { get; init; }
}
