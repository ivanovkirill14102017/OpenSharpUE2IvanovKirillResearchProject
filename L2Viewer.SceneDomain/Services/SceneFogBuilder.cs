using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneFogBuilder
{
    public SceneZoneInfoData[] BuildFogZones(L2Viewer.UnrFile.UnrFile unr)
    {
        var modelsByExportIndex = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrModelObject>()
            .ToDictionary(x => x.ExportIndex);
        var polysByExportIndex = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrPolysObject>()
            .ToDictionary(x => x.ExportIndex);

        return unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrZoneInfoObject>()
            .Select(x =>
            {
                var geometry = SceneBrushActorGeometryBuilder.Build(
                    unr.FilePath,
                    modelsByExportIndex,
                    polysByExportIndex,
                    x,
                    x.ObjectName);
                var rotationRaw = x.Rotation;

                return new SceneZoneInfoData
                {
                    StableName = SceneStableNameUtility.BuildActorStableName(unr, x),
                    ExportIndex = x.ExportIndex,
                    Name = x.ObjectName,
                    ClassName = x.ClassName,
                    MainScale = x.MainScale,
                    PostScale = x.PostScale,
                    TempScale = x.TempScale,
                    WorldLocation = x.Location,
                    WorldRotationUnrealRaw = rotationRaw,
                    WorldRotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
                    DrawScale = x.DrawScale,
                    DrawScale3D = x.DrawScale3D,
                    PrePivot = x.PrePivot,
                    Group = x.Group,
                    Tag = x.Tag,
                    Event = x.Event,
                    BrushReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.BrushReference),
                    BaseReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.BaseReference),
                    LevelReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.LevelReference),
                    OwnerReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.OwnerReference),
                    MeshReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.MeshReference),
                    TextureReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.TextureReference),
                    PhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.PhysicsVolumeReference),
                    StaticMeshReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, x.StaticMeshReference),
                    BrushPolysReference = geometry.BrushPolysReference,
                    BrushModelBoundsMin = geometry.BrushModelBoundsMin,
                    BrushModelBoundsMax = geometry.BrushModelBoundsMax,
                    BrushModelBoundsValid = geometry.BrushModelBoundsValid,
                    RenderGeometry = geometry.RenderGeometry,
                    WorldBoundsMin = geometry.WorldBoundsMin,
                    WorldBoundsMax = geometry.WorldBoundsMax,
                    UnknownProperties = x.UnknownProperties,
                    DistanceFogEnabled = x.DistanceFog,
                    DistanceFogEnd = x.DistanceFogEnd,
                    AmbientVector = x.AmbientVector,
                    AmbientBrightness = x.AmbientBrightness,
                    DynamicActorFilterState = x.DynamicActorFilterState,
                    LightChanged = x.LightChanged,
                    SunAffect = x.SunAffect,
                    TerrainZone = x.TerrainZone,
                    ZoneTag = x.ZoneTag,
                    TerrainReferences = x.Terrains.Select(y => ToReferenceText(unr.FilePath, y)).Where(y => y is not null).Cast<string>().ToArray(),
                    SwayRotationOrig = x.SwayRotationOrig,
                    Region = x.Region,
                    TexModifyInfo = x.TexModifyInfo
                };
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ToReferenceText(string mapPath, UnrFileObjectReference? reference)
    {
        return reference is null ? null : SceneReferenceUtilities.BuildReference(mapPath, reference);
    }
}
