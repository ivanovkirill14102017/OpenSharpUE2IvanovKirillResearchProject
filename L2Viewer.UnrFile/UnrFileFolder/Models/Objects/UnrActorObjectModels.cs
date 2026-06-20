namespace L2Viewer.UnrFile;

public abstract class UnrVolumeBaseObject : UnrActorBaseObject;

public sealed class UnrMoverObject : UnrActorBaseObject;
public sealed class UnrProjectorObject : UnrActorBaseObject;
public sealed class UnrReachSpecObject : UnrActorBaseObject;
public sealed class UnrConvexVolumeObject : UnrVolumeBaseObject
{
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrAntiPortalActorObject : UnrActorBaseObject;
public sealed class UnrBeamEmitterObject : UnrActorBaseObject, IUnrFadeInParticleLayerObject, IUnrColorScaledParticleLayerObject
{
    public byte? DetermineEndPointBy { get; init; }
    public UnrParticleColorScale[] ColorScale { get; init; } = [];
    public UnrRangeVector? ColorMultiplierRange { get; init; }
    public float? Opacity { get; init; }
    public float? FadeOutStartTime { get; init; }
    public bool FadeOut { get; init; }
    public float? FadeInEndTime { get; init; }
    public bool FadeIn { get; init; }
    public int? MaxParticles { get; init; }
    public string? NameValue { get; init; }
    public UnrRangeVector? StartLocationRange { get; init; }
    public UnrFloatRange? SphereRadiusRange { get; init; }
    public UnrRangeVector? StartLocationPolarRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
    public UnrFloatRange? LifetimeRange { get; init; }
    public float? WarmupTicksPerSecond { get; init; }
    public float? RelativeWarmupTime { get; init; }
}
public sealed class UnrVertMeshEmitterObject : UnrActorBaseObject, IUnrFadeInParticleLayerObject, IUnrColorScaledParticleLayerObject
{
    public UnrFileObjectReference? VertexMeshReference { get; init; }
    public bool UseMeshBlendMode { get; init; }
    public Vector3? Acceleration { get; init; }
    public bool UseColorScale { get; init; }
    public UnrParticleColorScale[] ColorScale { get; init; } = [];
    public float? ColorScaleRepeats { get; init; }
    public UnrRangeVector? ColorMultiplierRange { get; init; }
    public float? Opacity { get; init; }
    public float? FadeOutStartTime { get; init; }
    public bool FadeOut { get; init; }
    public float? FadeInEndTime { get; init; }
    public bool FadeIn { get; init; }
    public byte? CoordinateSystem { get; init; }
    public int? MaxParticles { get; init; }
    public int? CheckLevelOfWeather { get; init; }
    public bool WeatherEffect { get; init; }
    public string? NameValue { get; init; }
    public UnrRangeVector? StartLocationRange { get; init; }
    public bool UseRevolution { get; init; }
    public UnrRangeVector? RevolutionsPerSecondRange { get; init; }
    public bool SpinParticles { get; init; }
    public UnrRangeVector? StartSpinRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
    public byte? DrawStyle { get; init; }
    public UnrFloatRange? LifetimeRange { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
    public float? WarmupTicksPerSecond { get; init; }
    public float? RelativeWarmupTime { get; init; }
}
public sealed class UnrSceneManagerObject : UnrActorBaseObject;
public sealed class UnrL2SeamlessInfoObject : UnrActorBaseObject;
public sealed class UnrLineagePlayerControllerObject : UnrActorBaseObject;
public sealed class UnrInterpolationPointObject : UnrActorBaseObject;
public sealed class UnrActionWarpObject : UnrActorBaseObject;
public sealed class UnrActionMoveCameraObject : UnrActorBaseObject;
public sealed class UnrWaterVolumeObject : UnrVolumeBaseObject
{
    public UnrFileObjectReference? NextPhysicsVolumeReference { get; init; }
    public string? LocationName { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool DeleteMe { get; init; }
    public bool HiddenEd { get; init; }
    public bool PendingDelete { get; init; }
    public bool Selected { get; init; }
    public UnrPointRegion? Region { get; init; }
    public bool SunAffect { get; init; }
    public UnrFileObjectReference[] TouchingReferences { get; init; } = [];
    public Vector3? ColLocation { get; init; }
    public UnrFileColor? DistanceFogColor { get; init; }
    public UnrFileColor? CellophaneColor { get; init; }
    public bool UseDistanceFogColor { get; init; }
    public bool UseCellophane { get; init; }
    public bool IgnoredRange { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrPhysicsVolumeObject : UnrVolumeBaseObject
{
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrBlockingVolumeObject : UnrVolumeBaseObject
{
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrAmbientSoundObject : UnrActorBaseObject;
public sealed class UnrCameraObject : UnrActorBaseObject;
public sealed class UnrDefaultPhysicsVolumeObject : UnrVolumeBaseObject
{
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrPathNodeObject : UnrActorBaseObject;
public sealed class UnrEmitterObject : UnrActorBaseObject
{
    public UnrFileObjectReference[] Emitters { get; init; } = [];
    public bool DynamicActorFilterState { get; init; }
    public bool SunAffect { get; init; }
    public bool Directional { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrLevelInfoObject : UnrActorBaseObject
{
    public Vector3? AmbientVector { get; init; }
    public byte? AmbientBrightness { get; init; }
    public float? DistanceFogEnd { get; init; }
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrL2FogInfoObject : UnrActorBaseObject
{
    public UnrFloatRange? AffectRange { get; init; }
    public UnrFloatRange? FogRange1 { get; init; }
    public UnrFloatRange? FogRange2 { get; init; }
    public UnrFloatRange? FogRange3 { get; init; }
    public UnrFloatRange? FogRange4 { get; init; }
    public UnrFloatRange? FogRange5 { get; init; }
    public byte[] ColorsPayload { get; init; } = [];
    public bool DynamicActorFilterState { get; init; }
    public bool SunAffect { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrLevelSummaryObject : UnrActorBaseObject;
public sealed class UnrLightObject : UnrActorBaseObject
{
    public float? LightBrightness { get; init; }
    public byte? LightHue { get; init; }
    public byte? LightSaturation { get; init; }
    public float? LightRadius { get; init; }
    public float? LightCone { get; init; }
    public float? LightPeriod { get; init; }
    public float? LightOnTime { get; init; }
    public float? LightOffTime { get; init; }
    public bool Directional { get; init; }
}
public sealed class UnrMusicVolumeObject : UnrVolumeBaseObject
{
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrNMoonObject : UnrActorBaseObject
{
    public float? Radius { get; init; }
    public UnrFileObjectReference[] Skins { get; init; } = [];
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool SunAffect { get; init; }
}
public sealed class UnrNMovableSunLightObject : UnrActorBaseObject
{
    public float? LightBrightness { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool SunAffect { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
}
public sealed class UnrNSunObject : UnrActorBaseObject
{
    public float? Radius { get; init; }
    public float? LimitMaxRadius { get; init; }
    public UnrFileObjectReference[] Skins { get; init; } = [];
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool SunAffect { get; init; }
    public bool Directional { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
}
public sealed class UnrPlayerStartObject : UnrActorBaseObject;
public sealed class UnrSkyZoneInfoObject : UnrActorBaseObject
{
    public float? TexUPanSpeed { get; init; }
    public float? TexVPanSpeed { get; init; }
    public UnrFileObjectReference[] LensFlare { get; init; } = [];
    public float[] LensFlareOffset { get; init; } = [];
    public float[] LensFlareScale { get; init; } = [];
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrSpriteEmitterObject : UnrFileObject, IUnrTimedParticleLayerObject, IUnrColorScaledParticleLayerObject
{
    public byte? UseDirectionAs { get; init; }
    public Vector3? Acceleration { get; init; }
    public bool UseColorScale { get; init; }
    public float? Opacity { get; init; }
    public float? FadeOutStartTime { get; init; }
    public bool FadeOut { get; init; }
    public int? MaxParticles { get; init; }
    public bool WeatherSoundCheck { get; init; }
    public string? NameValue { get; init; }
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
    public UnrFileObjectReference? TextureReference { get; init; }
    public UnrFloatRange? LifetimeRange { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
    public float? WarmupTicksPerSecond { get; init; }
    public float? RelativeWarmupTime { get; init; }
    public bool BlendBetweenSubdivisions { get; init; }
    public UnrParticleColorScale[] ColorScale { get; init; } = [];
    public UnrFileUnknownProperty[] UnknownProperties { get; init; } = [];
}
public sealed class UnrStaticMeshActorObject : UnrActorBaseObject
{
    public UnrFileObjectReference? StepSound1Reference { get; init; }
    public UnrFileObjectReference? StepSound2Reference { get; init; }
    public UnrFileObjectReference? StepSound3Reference { get; init; }
    public int? L2CurrentLod { get; init; }
    public float? L2LodViewDuration { get; init; }
    public int? L2ServerObjectRealId { get; init; }
    public int? L2ServerObjectId { get; init; }
    public byte? L2ServerObjectType { get; init; }
    public byte? Physics { get; init; }
    public byte? Style { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool UpdateShadow { get; init; }
    public bool StaticActor { get; init; }
    public bool Stasis { get; init; }
    public bool FixedRotationDir { get; init; }
    public Vector3? RotationRate { get; init; }
    public bool DeleteMe { get; init; }
    public bool PendingDelete { get; init; }
    public bool Selected { get; init; }
    public bool HiddenEd { get; init; }
    public bool StaticLighting { get; init; }
    public bool DisableSorting { get; init; }
    public bool AgitDefaultStaticMesh { get; init; }
    public int? AgitId { get; init; }
    public int? AccessoryIndex { get; init; }
    public UnrAccessoryTypeEntry[] AccessoryTypeList { get; init; } = [];
    public UnrFileObjectReference[] Skins { get; init; } = [];
    public UnrPointRegion? Region { get; init; }
    public string? ForcedRegionTag { get; init; }
    public int? ForcedRegion { get; init; }
    public bool SunAffect { get; init; }
    public UnrFileObjectReference[] TouchingReferences { get; init; } = [];
    public UnrFileObjectReference? StaticMeshInstanceReference { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
    public float? CollisionRadius { get; init; }
    public bool CollideActors { get; init; }
    public bool BlockActors { get; init; }
    public bool BlockPlayers { get; init; }
    public bool BlockZeroExtentTraces { get; init; }
    public bool BlockNonZeroExtentTraces { get; init; }
    public bool BlockKarma { get; init; }
    public bool Unlit { get; init; }
    public bool ShadowCast { get; init; }
    public bool IgnoredRange { get; init; }
    public Vector3? ColLocation { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
public sealed class UnrMovableStaticMeshActorObject : UnrActorBaseObject
{
    public UnrFileObjectReference[] TouchingReferences { get; init; } = [];
    public bool DynamicActorFilterState { get; init; }
    public bool SunAffect { get; init; }
    public UnrPointRegion? Region { get; init; }
    public bool CollideActors { get; init; }
    public bool BlockActors { get; init; }
    public bool BlockPlayers { get; init; }
    public bool BlockZeroExtentTraces { get; init; }
    public bool BlockNonZeroExtentTraces { get; init; }
    public bool BlockKarma { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
    public Vector3? ColLocation { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
    public Vector3? L2RotatorRate { get; init; }
    public Vector3? L2RotatorMax { get; init; }
    public bool UseL2RotatorMaxRandom { get; init; }
    public bool UseL2RotatorRandomStart { get; init; }
    public byte[]? L2AccelRatioPayload { get; init; }
}
public sealed class UnrMeshEmitterObject : UnrFileObject, IUnrFadeInParticleLayerObject, IUnrColorScaledParticleLayerObject
{
    public UnrFileObjectReference? StaticMeshReference { get; init; }
    public bool UseMeshBlendMode { get; init; }
    public bool RenderTwoSided { get; init; }
    public float? Opacity { get; init; }
    public float? FadeOutStartTime { get; init; }
    public bool FadeOut { get; init; }
    public float? FadeInEndTime { get; init; }
    public bool FadeIn { get; init; }
    public int? MaxParticles { get; init; }
    public string? NameValue { get; init; }
    public bool SpinParticles { get; init; }
    public UnrRangeVector? SpinsPerSecondRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
    public UnrFloatRange? LifetimeRange { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
    public float? WarmupTicksPerSecond { get; init; }
    public float? RelativeWarmupTime { get; init; }
    public UnrParticleColorScale[] ColorScale { get; init; } = [];
    public UnrFileUnknownProperty[] UnknownProperties { get; init; } = [];
}
public sealed class UnrStaticMeshInstanceObject : UnrActorBaseObject
{
    public int NativePayloadSize { get; init; }
    public byte[] UnknownPrefixBytes { get; init; } = [];
}
public sealed class UnrTerrainSectorObject : UnrActorBaseObject
{
    public int UnknownInt1 { get; init; }
    public int UnknownInt2 { get; init; }
    public int UnknownInt3 { get; init; }
    public int UnknownInt4 { get; init; }
    public Vector3 BoundsMin { get; init; }
    public Vector3 BoundsMax { get; init; }
    public ushort UnknownShort1 { get; init; }
    public ushort UnknownShort2 { get; init; }
    public int UnknownInt5 { get; init; }
    public int NativePayloadSize { get; init; }
}
public sealed class UnrZoneInfoObject : UnrActorBaseObject
{
    public bool DistanceFog { get; init; }
    public Vector3? AmbientVector { get; init; }
    public byte? AmbientBrightness { get; init; }
    public float? DistanceFogEnd { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool SunAffect { get; init; }
    public bool TerrainZone { get; init; }
    public string? ZoneTag { get; init; }
    public UnrFileObjectReference[] Terrains { get; init; } = [];
    public Vector3? SwayRotationOrig { get; init; }
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}
