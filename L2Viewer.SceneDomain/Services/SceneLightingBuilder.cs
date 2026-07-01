using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneLightingBuilder
{
    public SceneLightData[] BuildLights(L2Viewer.UnrFile.UnrFile unr)
    {
        return unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrLightObject>()
            .Select(x =>
            {
                var rotationRaw = x.Rotation;
                return new SceneLightData
                {
                    ExportIndex = x.ExportIndex,
                    StableName = SceneStableNameUtility.BuildActorStableName(unr, x),
                    Name = x.ObjectName,
                    ClassName = x.ClassName,
                    WorldLocation = x.Location,
                    WorldRotationUnrealRaw = rotationRaw,
                    WorldRotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
                    DrawScale = x.DrawScale,
                    DrawScale3D = x.DrawScale3D,
                    Brightness = x.LightBrightness,
                    Hue = x.LightHue,
                    Saturation = x.LightSaturation,
                    Radius = x.LightRadius,
                    Cone = x.LightCone,
                    Period = x.LightPeriod,
                    OnTime = x.LightOnTime,
                    OffTime = x.LightOffTime,
                    Directional = x.Directional
                };
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public SceneSunData[] BuildSuns(L2Viewer.UnrFile.UnrFile unr)
    {
        var results = new List<SceneSunData>();

        results.AddRange(unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrNSunObject>()
            .Select(x =>
            {
                var rotationRaw = x.Rotation;
                return new SceneSunData
                {
                    ExportIndex = x.ExportIndex,
                    StableName = SceneStableNameUtility.BuildActorStableName(unr, x),
                    Name = x.ObjectName,
                    ClassName = x.ClassName,
                    WorldLocation = x.Location,
                    WorldRotationUnrealRaw = rotationRaw,
                    WorldRotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
                    Brightness = null,
                    Radius = x.Radius,
                    LimitMaxRadius = x.LimitMaxRadius,
                    Directional = x.Directional,
                    SunAffect = x.SunAffect,
                    DynamicActorFilterState = x.DynamicActorFilterState,
                    LightChanged = x.LightChanged,
                    SwayRotationOrig = x.SwayRotationOrig,
                    SkinReferences = x.Skins.Select(ToReferenceText).Where(x => x is not null).Cast<string>().ToArray()
                };
            }));

        results.AddRange(unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrNMovableSunLightObject>()
            .Select(x =>
            {
                var rotationRaw = x.Rotation;
                return new SceneSunData
                {
                    ExportIndex = x.ExportIndex,
                    StableName = SceneStableNameUtility.BuildActorStableName(unr, x),
                    Name = x.ObjectName,
                    ClassName = x.ClassName,
                    WorldLocation = x.Location,
                    WorldRotationUnrealRaw = rotationRaw,
                    WorldRotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
                    Brightness = x.LightBrightness,
                    Radius = null,
                    LimitMaxRadius = null,
                    Directional = false,
                    SunAffect = x.SunAffect,
                    DynamicActorFilterState = x.DynamicActorFilterState,
                    LightChanged = x.LightChanged,
                    SwayRotationOrig = x.SwayRotationOrig,
                    SkinReferences = []
                };
            }));

        return results
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public SceneMoonData[] BuildMoons(L2Viewer.UnrFile.UnrFile unr)
    {
        return unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrNMoonObject>()
            .Select(x =>
            {
                var rotationRaw = x.Rotation;
                return new SceneMoonData
                {
                    ExportIndex = x.ExportIndex,
                    StableName = SceneStableNameUtility.BuildActorStableName( unr, x),
                    Name = x.ObjectName,
                    ClassName = x.ClassName,
                    WorldLocation = x.Location,
                    WorldRotationUnrealRaw = rotationRaw,
                    WorldRotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
                    Radius = x.Radius,
                    SunAffect = x.SunAffect,
                    DynamicActorFilterState = x.DynamicActorFilterState,
                    LightChanged = x.LightChanged,
                    SkinReferences = x.Skins.Select(ToReferenceText).Where(x => x is not null).Cast<string>().ToArray()
                };
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public SceneSkyZoneData[] BuildSkyZones(L2Viewer.UnrFile.UnrFile unr)
    {
        return unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrSkyZoneInfoObject>()
            .Select(x => new SceneSkyZoneData
            {
                ExportIndex = x.ExportIndex,
                StableName = SceneStableNameUtility.BuildActorStableName(unr, x),
                Name = x.ObjectName,
                WorldLocation = x.Location,
                TexUPanSpeed = x.TexUPanSpeed,
                TexVPanSpeed = x.TexVPanSpeed,
                LensFlareReferences = x.LensFlare.Select(ToReferenceText).Where(x => x is not null).Cast<string>().ToArray(),
                LensFlareOffset = x.LensFlareOffset,
                LensFlareScale = x.LensFlareScale
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ToReferenceText(UnrFileObjectReference? reference)
    {
        if (reference is null)
        {
            return null;
        }

        return reference.PackageName is null
            ? reference.ObjectName
            : $"{reference.PackageName}.{reference.ObjectName}";
    }
}
