namespace L2Viewer.SceneDomain.Models;

public sealed class SceneLightData
{
    public required int ExportIndex { get; init; }
    public required string StableName { get; init; }
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
    public required string StableName { get; init; }
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
    public required string StableName { get; init; }
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
    public required string StableName { get; init; }
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public string? Tag { get; init; }
    public Vector3? WorldLocation { get; init; }
    public Vector3? WorldRotationUnrealRaw { get; init; }
    public Vector3? WorldRotationEulerDegrees { get; init; }
    public string? StaticMeshReference { get; init; }
    public string? MeshReference { get; init; }
    public string? TextureReference { get; init; }
    public float? TexUPanSpeed { get; init; }
    public float? TexVPanSpeed { get; init; }
    public string[] LensFlareReferences { get; init; } = [];
    public float[] LensFlareOffset { get; init; } = [];
    public float[] LensFlareScale { get; init; } = [];
}

public sealed class SceneSkyEnvironmentData
{
    public required SceneSkyZoneData[] SkyZones { get; init; }
    public required SceneSunData[] Suns { get; init; }
    public required SceneMoonData[] Moons { get; init; }
    public required SceneSkySourceReferenceData[] SourceReferences { get; init; }
    public required SceneSkySurfaceMaterialData[] SurfaceMaterials { get; init; }
}

public sealed class SceneSkySourceReferenceData
{
    public required string Role { get; init; }
    public required string Reference { get; init; }
    public required string PackageName { get; init; }
    public required string ObjectName { get; init; }
    public required string ClassName { get; init; }
    public string? PackagePath { get; init; }
    public string? ClientRelativePath { get; init; }
    public string? Uri { get; init; }
}

public sealed class SceneSkySurfaceMaterialData
{
    public required int ModelExportIndex { get; init; }
    public required string ModelName { get; init; }
    public required string MaterialReference { get; init; }
    public required uint PolyFlags { get; init; }
    public required string[] PolyFlagNames { get; init; }
    public required int SurfaceCount { get; init; }
    public required bool Environment { get; init; }
    public required bool FakeBackdrop { get; init; }
    public required bool Unlit { get; init; }
}
