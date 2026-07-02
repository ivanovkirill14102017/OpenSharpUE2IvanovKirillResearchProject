namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public enum SceneCharacterEquipmentBodyPart
{
    Underwear,
    Chest,
    Legs,
    Feet,
    Gloves,
    FullArmor,
    Head,
    Face,
    Hair,
    DoubleHair,
    Neck,
    Ears,
    Fingers,
    RightHand,
    LeftHand,
    LeftRightHand
}

[ForExternalUse]
public sealed class SceneCharacterEquipmentCatalogData
{
    public required SceneCharacterBaseClass BaseClass { get; init; }
    public required SceneCharacterGender Gender { get; init; }
    public required SceneCharacterVisualFamily VisualFamily { get; init; }
    public required IReadOnlyList<SceneCharacterEquipmentCatalogSlotData> Slots { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}

[ForExternalUse]
public sealed class SceneCharacterEquipmentCatalogSlotData
{
    public required SceneCharacterPaperdollSlot Slot { get; init; }
    public required IReadOnlyList<SceneCharacterEquipmentCatalogItemData> Items { get; init; }
}

[ForExternalUse]
public sealed class SceneCharacterEquipmentCatalogItemData
{
    public required int ItemId { get; init; }
    public required string DisplayName { get; init; }
    public required SceneCharacterEquipmentBodyPart BodyPart { get; init; }
    public required string BodyPartKey { get; init; }
    public required IReadOnlyList<SceneCharacterPaperdollSlot> PaperdollSlots { get; init; }
    public required IReadOnlyList<SceneCharacterPaperdollSlot> AppearanceSlots { get; init; }
    public required bool IsRenderableWithCurrentAppearanceBuilder { get; init; }
    public required SceneResourceReference[] MeshResources { get; init; }
    public required SceneResourceReference[] TextureResources { get; init; }
}
