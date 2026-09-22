namespace L2Viewer.SceneDomain.Models;

public sealed class ScenePlayableSkillCatalogData
{
    public required IReadOnlyList<ScenePlayableClassSkillData> Classes { get; init; }
}

public sealed class ScenePlayableClassSkillData
{
    public required int ClassId { get; init; }
    public required string ClassName { get; init; }
    public required int ParentClassId { get; init; }
    public required IReadOnlyList<ScenePlayableSkillData> Skills { get; init; }
}

public sealed class ScenePlayableSkillData
{
    public required int SkillId { get; init; }
    public required string SkillName { get; init; }
    public required int MinLevel { get; init; }
}
