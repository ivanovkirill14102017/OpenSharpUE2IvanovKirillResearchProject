using System.Numerics;

namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public sealed class SceneCreatureSpawnData
{
    public required string StableName { get; init; }
    public required string VisualKey { get; init; }
    public required int SpawnId { get; init; }
    public required int TemplateId { get; init; }
    public required string SpawnLocationKey { get; init; }
    public required string DisplayName { get; init; }
    public required string DbClassName { get; init; }
    public required SceneResourceLocation ActorClassResource { get; init; }
    public required SceneResourceLocation MeshResource { get; init; }
    public required SceneResourceLocation[] TextureResources { get; init; }
    public required int Heading { get; init; }
    public required int SpawnCount { get; init; }
    public required int RandomOffsetX { get; init; }
    public required int RandomOffsetY { get; init; }
    public required float CollisionRadius { get; init; }
    public required float CollisionHeight { get; init; }
    public required Vector3 Position { get; init; }
}
