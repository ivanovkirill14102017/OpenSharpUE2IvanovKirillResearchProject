namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public sealed class SceneCharacterAppearanceOptionsData
{
    public required SceneCharacterBaseClass BaseClass { get; init; }
    public required SceneCharacterGender Gender { get; init; }
    public required SceneCharacterVisualFamily VisualFamily { get; init; }
    public required int CharGrpIndex { get; init; }
    public required IReadOnlyList<SceneCharacterFaceOptionData> FaceOptions { get; init; }
    public required IReadOnlyList<SceneCharacterHairStyleOptionData> HairStyleOptions { get; init; }
}

[ForExternalUse]
public sealed class SceneCharacterFaceOptionData
{
    public required int Id { get; init; }
    public required SceneResourceReference[] MeshResources { get; init; }
    public required SceneResourceReference[] TextureResources { get; init; }
}

[ForExternalUse]
public sealed class SceneCharacterHairStyleOptionData
{
    public required int Id { get; init; }
    public required SceneResourceReference[] MeshResources { get; init; }
    public required IReadOnlyList<SceneCharacterHairColorOptionData> HairColorOptions { get; init; }
}

[ForExternalUse]
public sealed class SceneCharacterHairColorOptionData
{
    public required int Id { get; init; }
    public required SceneResourceReference[] TextureResources { get; init; }
}
