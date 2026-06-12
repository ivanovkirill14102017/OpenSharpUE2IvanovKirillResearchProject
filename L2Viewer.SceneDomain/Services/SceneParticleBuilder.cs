using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneParticleBuilder
{
    public SceneParticleEmitterData[] BuildEmitters(L2Viewer.UnrFile.UnrFile unr)
    {
        var byExportIndex = unr.ExportObjects
            .ToDictionary(x => x.Export.Index, x => x.Object);

        return unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrEmitterObject>()
            .Select(x => BuildEmitter(x, byExportIndex))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SceneParticleEmitterData BuildEmitter(
        UnrEmitterObject emitter,
        IReadOnlyDictionary<int, UnrFileObject> byExportIndex)
    {
        var rotationRaw = emitter.Rotation;
        var layerRefs = emitter.Emitters
            .Select(ToReferenceText)
            .Where(x => x is not null)
            .Cast<string>()
            .ToArray();

        var layers = emitter.Emitters
            .Select(x => ResolveSpriteEmitterLayer(x, byExportIndex))
            .Where(x => x is not null)
            .Cast<SceneSpriteEmitterLayerData>()
            .ToArray();

        return new SceneParticleEmitterData
        {
            ExportIndex = emitter.ExportIndex,
            Name = emitter.ObjectName,
            WorldLocation = emitter.Location,
            WorldRotationUnrealRaw = rotationRaw,
            WorldRotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
            DrawScale = emitter.DrawScale,
            DrawScale3D = emitter.DrawScale3D,
            Directional = emitter.Directional,
            SunAffect = emitter.SunAffect,
            DynamicActorFilterState = emitter.DynamicActorFilterState,
            SwayRotationOrig = emitter.SwayRotationOrig,
            EmitterReferences = layerRefs,
            Layers = layers
        };
    }

    private static SceneSpriteEmitterLayerData? ResolveSpriteEmitterLayer(
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<int, UnrFileObject> byExportIndex)
    {
        if (reference?.ExportIndex is null)
        {
            return null;
        }

        if (!byExportIndex.TryGetValue(reference.ExportIndex.Value, out var objectValue) ||
            objectValue is not UnrSpriteEmitterObject sprite)
        {
            return null;
        }

        return new SceneSpriteEmitterLayerData
        {
            ExportIndex = sprite.ExportIndex,
            Name = sprite.NameValue ?? sprite.ObjectName,
            UseDirectionAs = sprite.UseDirectionAs,
            Acceleration = sprite.Acceleration,
            UseColorScale = sprite.UseColorScale,
            Opacity = sprite.Opacity,
            FadeOutStartTime = sprite.FadeOutStartTime,
            FadeOut = sprite.FadeOut,
            MaxParticles = sprite.MaxParticles,
            WeatherSoundCheck = sprite.WeatherSoundCheck,
            SpinParticles = sprite.SpinParticles,
            SpinCCWorCW = sprite.SpinCCWorCW,
            SpinsPerSecondRange = sprite.SpinsPerSecondRange,
            StartSpinRange = sprite.StartSpinRange,
            UseSizeScale = sprite.UseSizeScale,
            UseRegularSizeScale = sprite.UseRegularSizeScale,
            StartSizeRange = sprite.StartSizeRange,
            UniformSize = sprite.UniformSize,
            DrawStyle = sprite.DrawStyle,
            TextureReference = ToReferenceText(sprite.TextureReference),
            LifetimeRange = sprite.LifetimeRange,
            StartVelocityRange = sprite.StartVelocityRange,
            WarmupTicksPerSecond = sprite.WarmupTicksPerSecond,
            RelativeWarmupTime = sprite.RelativeWarmupTime,
            BlendBetweenSubdivisions = sprite.BlendBetweenSubdivisions,
            UnknownProperties = sprite.UnknownProperties
        };
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
