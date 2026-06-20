namespace L2Viewer.UnrFile;

public interface IUnrParticleLayerObject
{
    int ExportIndex { get; }
    string ObjectName { get; }
    string? NameValue { get; }
    UnrFileUnknownProperty[] UnknownProperties { get; }
}

public interface IUnrTimedParticleLayerObject : IUnrParticleLayerObject
{
    float? Opacity { get; }
    float? FadeOutStartTime { get; }
    bool FadeOut { get; }
    int? MaxParticles { get; }
    UnrFloatRange? LifetimeRange { get; }
    float? WarmupTicksPerSecond { get; }
    float? RelativeWarmupTime { get; }
}

public interface IUnrFadeInParticleLayerObject : IUnrTimedParticleLayerObject
{
    float? FadeInEndTime { get; }
    bool FadeIn { get; }
}

public interface IUnrColorScaledParticleLayerObject : IUnrParticleLayerObject
{
    UnrParticleColorScale[] ColorScale { get; }
}
