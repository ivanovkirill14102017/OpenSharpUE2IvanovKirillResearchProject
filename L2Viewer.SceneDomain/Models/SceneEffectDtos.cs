namespace L2Viewer.SceneDomain.Models;

public sealed class SceneLightData
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? WorldRotationUnrealRaw { get; init; }
    public Vector3? WorldRotationEulerDegrees { get; init; }
    public float DrawScale { get; init; }
    public Vector3 DrawScale3D { get; init; }
    public float? Brightness { get; init; }
    public byte? Hue { get; init; }
    public byte? Saturation { get; init; }
    public float? Radius { get; init; }
    public float? Cone { get; init; }
    public float? Period { get; init; }
    public float? OnTime { get; init; }
    public float? OffTime { get; init; }
    public bool Directional { get; init; }
}

public sealed class SceneSunData
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? WorldRotationUnrealRaw { get; init; }
    public Vector3? WorldRotationEulerDegrees { get; init; }
    public float? Brightness { get; init; }
    public float? Radius { get; init; }
    public float? LimitMaxRadius { get; init; }
    public bool Directional { get; init; }
    public bool SunAffect { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public Vector3? SwayRotationOrig { get; init; }
    public string[] SkinReferences { get; init; } = [];
}

public sealed class SceneMoonData
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? WorldRotationUnrealRaw { get; init; }
    public Vector3? WorldRotationEulerDegrees { get; init; }
    public float? Radius { get; init; }
    public bool SunAffect { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public string[] SkinReferences { get; init; } = [];
}

public sealed class SceneZoneInfoData : SceneActorBrushData
{
    public bool DistanceFogEnabled { get; init; }
    public float? DistanceFogEnd { get; init; }
    public Vector3? AmbientVector { get; init; }
    public byte? AmbientBrightness { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool SunAffect { get; init; }
    public bool TerrainZone { get; init; }
    public string? ZoneTag { get; init; }
    public string[] TerrainReferences { get; init; } = [];
    public Vector3? SwayRotationOrig { get; init; }
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}

public sealed class SceneSkyZoneData
{
    public required int ExportIndex { get; init; }
    public required string Name { get; init; }
    public Vector3? WorldLocation { get; init; }
    public float? TexUPanSpeed { get; init; }
    public float? TexVPanSpeed { get; init; }
    public string[] LensFlareReferences { get; init; } = [];
    public float[] LensFlareOffset { get; init; } = [];
    public float[] LensFlareScale { get; init; } = [];
}
