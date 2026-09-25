using System.Numerics;

namespace L2Viewer.SceneDomain.Models;

public sealed class SceneCreatureSpawnData
{
    public required string StableName { get; init; }
    public required string VisualKey { get; init; }
    public required int SpawnId { get; init; }
    public required int TemplateId { get; init; }
    public required string SpawnLocationKey { get; init; }
    public required string DisplayName { get; init; }
    public required string DbClassName { get; init; }
    public required int RightHandItemId { get; init; }
    public required int LeftHandItemId { get; init; }
    public required SceneResourceLocation ActorClassResource { get; init; }
    public required SceneResourceLocation MeshResource { get; init; }
    public required SceneResourceLocation[] SurfaceResources { get; init; }
    public SceneCreatureAttachedEffectData[] AttachedEffects { get; init; } = [];
    public required int Heading { get; init; }
    public required int SpawnCount { get; init; }
    public required int RandomOffsetX { get; init; }
    public required int RandomOffsetY { get; init; }
    public required float CollisionRadius { get; init; }
    public required float CollisionHeight { get; init; }
    public required Vector3 Position { get; init; }
    public SceneSkeletalMeshResourceReference SkeletalMeshReference => new(MeshResource.ResourceId);
    public SceneSurfaceResourceReference[] SurfaceResourceReferences => SurfaceResources
        .Select(x => new SceneSurfaceResourceReference(x.ResourceId))
        .ToArray();
}

public sealed class SceneCreatureAttachedEffectData
{
    public required string StableName { get; init; }
    public required string SourceVariable { get; init; }
    public required string EffectReference { get; init; }
    public required SceneResourceLocation EffectResource { get; init; }
    public string? BoneName { get; init; }
    public int? BoneIndex { get; init; }
    public Vector3 RelativeLocationUnreal { get; init; }
    public Vector3 RelativeRotationUnrealRaw { get; init; }
    public Vector3 RelativeRotationEulerDegrees { get; init; }
    public required SceneParticleEmitterData Emitter { get; init; }
    public SceneParticleResourceReference ParticleReference => new(EffectResource.ResourceId);
}

public sealed class SceneCreatureVisualData
{
    public required int NpcId { get; init; }
    public required SceneResourceLocation ActorClassResource { get; init; }
    public required SceneResourceLocation MeshResource { get; init; }
    public required SceneResourceLocation[] TextureResources { get; init; }
    public required SceneCreatureAttachedEffectData[] AttachedEffects { get; init; }
}
