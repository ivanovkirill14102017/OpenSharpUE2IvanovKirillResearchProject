using System.Numerics;

namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public sealed class SceneNpcData
{
    public required int SpawnId { get; init; }
    public required int NpcId { get; init; }
    public required string SpawnLocationKey { get; init; }
    public required string Name { get; init; }
    public required string DbClassName { get; init; }
    public required SceneResourceReference ActorClass { get; init; }
    public required SceneResourceReference Mesh { get; init; }
    public required SceneResourceReference[] Textures { get; init; }
    public required int Heading { get; init; }
    public required int Count { get; init; }
    public required int RandomX { get; init; }
    public required int RandomY { get; init; }
    public required float CollisionRadius { get; init; }
    public required float CollisionHeight { get; init; }
    public required Vector3 Position { get; init; }
}
