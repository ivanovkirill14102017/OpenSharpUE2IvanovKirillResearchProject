namespace L2Viewer.SceneDomain.Models;

public sealed class SceneResourceLocation
{
    public required string Reference { get; init; }
    public required string ClassName { get; init; }
    public required string PackageName { get; init; }
    public required string ObjectName { get; init; }
    public required string PackagePath { get; init; }
    public required string ClientRelativePath { get; init; }
    public required string Uri { get; init; }
}
