namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public sealed class SceneCharacterArchetypeData
{
    public required int ArchetypeIndex { get; init; }
    public required string ArchetypeKey { get; init; }
    public string? RaceKey { get; init; }
    public string? GenderKey { get; init; }
    public string? ClassKey { get; init; }
    public required SceneCharacterSkeletonSummary Skeleton { get; init; }
    public required IReadOnlyList<SceneCharacterPartSlotData> Slots { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}

public sealed class SceneCharacterPartSlotData
{
    public required string SlotName { get; init; }
    public required SceneResourceLocation[] MeshResources { get; init; }
    public required SceneResourceLocation[] TextureResources { get; init; }
    public required SceneCharacterSkeletonSummary Skeleton { get; init; }
}

public sealed class SceneCharacterSkeletonSummary
{
    public string? MeshReference { get; init; }
    public string? PackagePath { get; init; }
    public string? MeshObjectName { get; init; }
    public string? SkeletonName { get; init; }
    public string? RootBoneName { get; init; }
    public int BoneCount { get; init; }
    public required bool Resolved { get; init; }
    public required bool MatchesArchetypePrimarySkeleton { get; init; }
    public string? FailureReason { get; init; }
}
