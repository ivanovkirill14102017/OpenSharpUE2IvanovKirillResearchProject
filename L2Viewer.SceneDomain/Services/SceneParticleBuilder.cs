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
            .Select(x => BuildEmitter(unr,x, byExportIndex))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SceneParticleEmitterData BuildEmitter(
        UnrFile.UnrFile unr,
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
            .Select(x => ResolveSpriteEmitterLayer(unr, x, byExportIndex))
            .Where(x => x is not null)
            .Cast<SceneSpriteEmitterLayerData>()
            .ToArray();
        var meshLayers = emitter.Emitters
            .Select(x => ResolveMeshEmitterLayer(unr, x, byExportIndex))
            .Where(x => x is not null)
            .Cast<SceneMeshEmitterLayerData>()
            .ToArray();
        var beamLayers = emitter.Emitters
            .Select(x => ResolveBeamEmitterLayer(unr, x, byExportIndex))
            .Where(x => x is not null)
            .Cast<SceneBeamEmitterLayerData>()
            .ToArray();
        var vertMeshLayers = emitter.Emitters
            .Select(x => ResolveVertMeshEmitterLayer(unr, x, byExportIndex))
            .Where(x => x is not null)
            .Cast<SceneVertMeshEmitterLayerData>()
            .ToArray();

        return new SceneParticleEmitterData
        {
            StableName= SceneStableNameUtility.BuildActorStableName(unr,emitter),
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
            TexModifyInfo = emitter.TexModifyInfo,
            EmitterReferences = layerRefs,
            Layers = layers,
            MeshLayers = meshLayers,
            BeamLayers = beamLayers,
            VertMeshLayers = vertMeshLayers
        };
    }

    private static SceneSpriteEmitterLayerData? ResolveSpriteEmitterLayer(
        UnrFile.UnrFile unr,
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<int, UnrFileObject> byExportIndex)
    {
        return ResolveLayer<UnrSpriteEmitterObject, SceneSpriteEmitterLayerData>(reference, byExportIndex, sprite =>
        {
            var identity = CreateLayerIdentity(sprite);
            var timed = CreateTimedLayerState(sprite);
            var colorScaled = CreateColorScaledLayerState(sprite);

            return new SceneSpriteEmitterLayerData
            {
                StableName = SceneStableNameUtility.BuildActorStableName("SpriteEm", unr, identity),
                ExportIndex = identity.ExportIndex,
                Name = identity.Name,
                UnknownProperties = identity.UnknownProperties,
                UseDirectionAs = sprite.UseDirectionAs,
                Acceleration = sprite.Acceleration,
                UseColorScale = sprite.UseColorScale,
                Opacity = timed.Opacity,
                FadeOutStartTime = timed.FadeOutStartTime,
                FadeOut = timed.FadeOut,
                MaxParticles = timed.MaxParticles,
                WeatherSoundCheck = sprite.WeatherSoundCheck,
                SpinParticles = sprite.SpinParticles,
                SpinCCWorCW = sprite.SpinCCWorCW,
                SpinsPerSecondRange = sprite.SpinsPerSecondRange,
                StartSpinRange = sprite.StartSpinRange,
                UseSizeScale = sprite.UseSizeScale,
                UseRegularSizeScale = sprite.UseRegularSizeScale,
                SizeScale = sprite.SizeScale,
                StartSizeRange = sprite.StartSizeRange,
                UniformSize = sprite.UniformSize,
                DrawStyle = sprite.DrawStyle,
                TextureReference = ToReferenceText(sprite.TextureReference),
                LifetimeRange = timed.LifetimeRange,
                StartVelocityRange = sprite.StartVelocityRange,
                WarmupTicksPerSecond = timed.WarmupTicksPerSecond,
                RelativeWarmupTime = timed.RelativeWarmupTime,
                BlendBetweenSubdivisions = sprite.BlendBetweenSubdivisions,
                ColorScale = colorScaled.ColorScale
            };
        });
    }

    private static SceneMeshEmitterLayerData? ResolveMeshEmitterLayer(
        UnrFile.UnrFile unr,
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<int, UnrFileObject> byExportIndex)
    {
        return ResolveLayer<UnrMeshEmitterObject, SceneMeshEmitterLayerData>(reference, byExportIndex, mesh =>
        {
            var identity = CreateLayerIdentity(mesh);
            var timed = CreateTimedLayerState(mesh);
            var fadeIn = CreateFadeInLayerState(mesh);
            var colorScaled = CreateColorScaledLayerState(mesh);

            return new SceneMeshEmitterLayerData
            {
                StableName = SceneStableNameUtility.BuildActorStableName("MeshEm",unr, identity),
                ExportIndex = identity.ExportIndex,
                Name = identity.Name,
                UnknownProperties = identity.UnknownProperties,
                StaticMeshReference = ToReferenceText(mesh.StaticMeshReference),
                UseMeshBlendMode = mesh.UseMeshBlendMode,
                RenderTwoSided = mesh.RenderTwoSided,
                Opacity = timed.Opacity,
                FadeOutStartTime = timed.FadeOutStartTime,
                FadeOut = timed.FadeOut,
                FadeInEndTime = fadeIn.FadeInEndTime,
                FadeIn = fadeIn.FadeIn,
                MaxParticles = timed.MaxParticles,
                SpinParticles = mesh.SpinParticles,
                SpinsPerSecondRange = mesh.SpinsPerSecondRange,
                StartSizeRange = mesh.StartSizeRange,
                LifetimeRange = timed.LifetimeRange,
                StartVelocityRange = mesh.StartVelocityRange,
                WarmupTicksPerSecond = timed.WarmupTicksPerSecond,
                RelativeWarmupTime = timed.RelativeWarmupTime,
                ColorScale = colorScaled.ColorScale
            };
        });
    }

    private static SceneBeamEmitterLayerData? ResolveBeamEmitterLayer(
        UnrFile.UnrFile unr,
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<int, UnrFileObject> byExportIndex)
    {
        return ResolveLayer<UnrBeamEmitterObject, SceneBeamEmitterLayerData>(reference, byExportIndex, beam =>
        {
            var identity = CreateLayerIdentity(beam);
            var timed = CreateTimedLayerState(beam);
            var fadeIn = CreateFadeInLayerState(beam);
            var colorScaled = CreateColorScaledLayerState(beam);

            return new SceneBeamEmitterLayerData
            {
                StableName = SceneStableNameUtility.BuildActorStableName("BeamEm", unr, identity),
                ExportIndex = identity.ExportIndex,
                Name = identity.Name,
                UnknownProperties = identity.UnknownProperties,
                TextureReference = ToReferenceText(beam.TextureReference),
                DetermineEndPointBy = beam.DetermineEndPointBy,
                ColorScale = colorScaled.ColorScale,
                ColorMultiplierRange = beam.ColorMultiplierRange,
                Opacity = timed.Opacity,
                FadeOutStartTime = timed.FadeOutStartTime,
                FadeOut = timed.FadeOut,
                FadeInEndTime = fadeIn.FadeInEndTime,
                FadeIn = fadeIn.FadeIn,
                MaxParticles = timed.MaxParticles,
                StartLocationRange = beam.StartLocationRange,
                SphereRadiusRange = beam.SphereRadiusRange,
                StartLocationPolarRange = beam.StartLocationPolarRange,
                StartSizeRange = beam.StartSizeRange,
                LifetimeRange = timed.LifetimeRange,
                WarmupTicksPerSecond = timed.WarmupTicksPerSecond,
                RelativeWarmupTime = timed.RelativeWarmupTime
            };
        });
    }

    private static SceneVertMeshEmitterLayerData? ResolveVertMeshEmitterLayer(
        UnrFile.UnrFile unr,
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<int, UnrFileObject> byExportIndex)
    {
        return ResolveLayer<UnrVertMeshEmitterObject, SceneVertMeshEmitterLayerData>(reference, byExportIndex, vertMesh =>
        {
            var identity = CreateLayerIdentity(vertMesh);
            var timed = CreateTimedLayerState(vertMesh);
            var fadeIn = CreateFadeInLayerState(vertMesh);
            var colorScaled = CreateColorScaledLayerState(vertMesh);

            return new SceneVertMeshEmitterLayerData
            {
                StableName = SceneStableNameUtility.BuildActorStableName("VertMeshEm", unr, identity),
                ExportIndex = identity.ExportIndex,
                Name = identity.Name,
                UnknownProperties = identity.UnknownProperties,
                VertexMeshReference = ToReferenceText(vertMesh.VertexMeshReference),
                UseMeshBlendMode = vertMesh.UseMeshBlendMode,
                Acceleration = vertMesh.Acceleration,
                UseColorScale = vertMesh.UseColorScale,
                ColorScale = colorScaled.ColorScale,
                ColorScaleRepeats = vertMesh.ColorScaleRepeats,
                ColorMultiplierRange = vertMesh.ColorMultiplierRange,
                Opacity = timed.Opacity,
                FadeOutStartTime = timed.FadeOutStartTime,
                FadeOut = timed.FadeOut,
                FadeInEndTime = fadeIn.FadeInEndTime,
                FadeIn = fadeIn.FadeIn,
                CoordinateSystem = vertMesh.CoordinateSystem,
                MaxParticles = timed.MaxParticles,
                CheckLevelOfWeather = vertMesh.CheckLevelOfWeather,
                WeatherEffect = vertMesh.WeatherEffect,
                StartLocationRange = vertMesh.StartLocationRange,
                UseRevolution = vertMesh.UseRevolution,
                RevolutionsPerSecondRange = vertMesh.RevolutionsPerSecondRange,
                SpinParticles = vertMesh.SpinParticles,
                StartSpinRange = vertMesh.StartSpinRange,
                StartSizeRange = vertMesh.StartSizeRange,
                DrawStyle = vertMesh.DrawStyle,
                LifetimeRange = timed.LifetimeRange,
                StartVelocityRange = vertMesh.StartVelocityRange,
                WarmupTicksPerSecond = timed.WarmupTicksPerSecond,
                RelativeWarmupTime = timed.RelativeWarmupTime
            };
        });
    }

    private static TScene? ResolveLayer<TUnr, TScene>(
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<int, UnrFileObject> byExportIndex,
        Func<TUnr, TScene> factory)
        where TUnr : UnrFileObject
        where TScene : class
    {
        if (reference?.ExportIndex is null)
        {
            return null;
        }

        if (!byExportIndex.TryGetValue(reference.ExportIndex.Value, out var objectValue) ||
            objectValue is not TUnr layer)
        {
            return null;
        }

        return factory(layer);
    }

    private static SceneParticleLayerIdentity CreateLayerIdentity(IUnrParticleLayerObject layer)
    {
        return new SceneParticleLayerIdentity(
            layer.ExportIndex,
            layer.NameValue ?? layer.ObjectName,
            layer.UnknownProperties);
    }

    private static SceneTimedLayerState CreateTimedLayerState(IUnrTimedParticleLayerObject layer)
    {
        return new SceneTimedLayerState(
            layer.Opacity,
            layer.FadeOutStartTime,
            layer.FadeOut,
            layer.MaxParticles,
            layer.LifetimeRange,
            layer.WarmupTicksPerSecond,
            layer.RelativeWarmupTime);
    }

    private static SceneFadeInLayerState CreateFadeInLayerState(IUnrFadeInParticleLayerObject layer)
    {
        return new SceneFadeInLayerState(layer.FadeInEndTime, layer.FadeIn);
    }

    private static SceneColorScaledLayerState CreateColorScaledLayerState(IUnrColorScaledParticleLayerObject layer)
    {
        return new SceneColorScaledLayerState(layer.ColorScale);
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

    public  readonly record struct SceneParticleLayerIdentity(
        int ExportIndex,
        string Name,
        UnrFileUnknownProperty[] UnknownProperties);

    private readonly record struct SceneTimedLayerState(
        float? Opacity,
        float? FadeOutStartTime,
        bool FadeOut,
        int? MaxParticles,
        UnrFloatRange? LifetimeRange,
        float? WarmupTicksPerSecond,
        float? RelativeWarmupTime);

    private readonly record struct SceneFadeInLayerState(
        float? FadeInEndTime,
        bool FadeIn);

    private readonly record struct SceneColorScaledLayerState(
        UnrParticleColorScale[] ColorScale);
}
