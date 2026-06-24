namespace L2Viewer.SceneDomain.Models;

public sealed class SceneNpcData
{
    public string Name { get; set; }
    public string VisualLink{ get; set; }
    public required Vector3 Position { get; init; }
}
