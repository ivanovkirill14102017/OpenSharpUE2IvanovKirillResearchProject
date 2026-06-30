namespace L2Viewer.SceneDomain.Models;

public sealed class SceneParticleEmitterData
{
    public required int ExportIndex { get; init; }
    public required string StableName { get; init; }
    public required string Name { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? WorldRotationUnrealRaw { get; init; }
    public Vector3? WorldRotationEulerDegrees { get; init; }
    public float DrawScale { get; init; }
    public Vector3 DrawScale3D { get; init; }
    public bool Directional { get; init; }
    public bool SunAffect { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
    public string[] EmitterReferences { get; init; } = [];
    public SceneSpriteEmitterLayerData[] Layers { get; init; } = [];
    public SceneMeshEmitterLayerData[] MeshLayers { get; init; } = [];
    public SceneBeamEmitterLayerData[] BeamLayers { get; init; } = [];
    public SceneVertMeshEmitterLayerData[] VertMeshLayers { get; init; } = [];
}

public abstract class SceneParticleLayerData
{
    public required string StableName { get; init; }
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public UnrFileUnknownProperty[] UnknownProperties { get; init; } = [];
}

public abstract class SceneTimedParticleLayerData : SceneParticleLayerData
{
    public float? Opacity { get; init; }
    public float? FadeOutStartTime { get; init; }
    public bool FadeOut { get; init; }
    public int? MaxParticles { get; init; }
    public UnrFloatRange? LifetimeRange { get; init; }
    public float? WarmupTicksPerSecond { get; init; }
    public float? RelativeWarmupTime { get; init; }
}

public abstract class SceneColorScaledParticleLayerData : SceneTimedParticleLayerData
{
    public UnrParticleColorScale[] ColorScale { get; init; } = [];
}

public abstract class SceneFadeInParticleLayerData : SceneColorScaledParticleLayerData
{
    public float? FadeInEndTime { get; init; }
    public bool FadeIn { get; init; }
}

public sealed class SceneSpriteEmitterLayerData : SceneColorScaledParticleLayerData
{
    public byte? UseDirectionAs { get; init; }
    public Vector3? Acceleration { get; init; }
    public bool UseColorScale { get; init; }
    public bool WeatherSoundCheck { get; init; }
    public bool SpinParticles { get; init; }
    public Vector3? SpinCCWorCW { get; init; }
    public UnrRangeVector? SpinsPerSecondRange { get; init; }
    public UnrRangeVector? StartSpinRange { get; init; }
    public bool UseSizeScale { get; init; }
    public bool UseRegularSizeScale { get; init; }
    public UnrParticleSizeScale[] SizeScale { get; init; } = [];
    public UnrRangeVector? StartSizeRange { get; init; }
    public bool UniformSize { get; init; }
    public byte? DrawStyle { get; init; }
    public string? TextureReference { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
    public bool BlendBetweenSubdivisions { get; init; }
}

public sealed class SceneMeshEmitterLayerData : SceneFadeInParticleLayerData
{
    public string? StaticMeshReference { get; init; }
    public bool UseMeshBlendMode { get; init; }
    public bool RenderTwoSided { get; init; }
    public bool SpinParticles { get; init; }
    public UnrRangeVector? SpinsPerSecondRange { get; init; }
    public UnrRangeVector? StartSpinRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
}

public sealed class SceneBeamEmitterLayerData : SceneFadeInParticleLayerData
{
    public string? TextureReference { get; init; }
    public byte? DetermineEndPointBy { get; init; }
    public UnrRangeVector? ColorMultiplierRange { get; init; }
    public UnrRangeVector? StartLocationRange { get; init; }
    public UnrFloatRange? SphereRadiusRange { get; init; }
    public UnrRangeVector? StartLocationPolarRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
}

public sealed class SceneVertMeshEmitterLayerData : SceneFadeInParticleLayerData
{
    public string? VertexMeshReference { get; init; }
    public bool UseMeshBlendMode { get; init; }
    public Vector3? Acceleration { get; init; }
    public bool UseColorScale { get; init; }
    public float? ColorScaleRepeats { get; init; }
    public UnrRangeVector? ColorMultiplierRange { get; init; }
    public byte? CoordinateSystem { get; init; }
    public int? CheckLevelOfWeather { get; init; }
    public bool WeatherEffect { get; init; }
    public UnrRangeVector? StartLocationRange { get; init; }
    public bool UseRevolution { get; init; }
    public UnrRangeVector? RevolutionsPerSecondRange { get; init; }
    public bool SpinParticles { get; init; }
    public UnrRangeVector? StartSpinRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
    public byte? DrawStyle { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
}
