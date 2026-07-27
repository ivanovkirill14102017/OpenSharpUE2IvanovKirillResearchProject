namespace L2Viewer.UnrFile;

public sealed class UnrSkillEffectStageObject
{
    public required string ObjectName { get; init; }
    public required string DeclaredClassName { get; init; }
    public string? SuperClassName { get; init; }
    public required string StageKey { get; init; }
    public required int StageOrder { get; init; }
    public required IReadOnlyList<UnrSkillEffectLayerObject> Layers { get; init; }
}

public sealed class UnrSkillEffectLayerObject
{
    public required int ExportIndex { get; init; }
    public required string ObjectName { get; init; }
    public required string ClassName { get; init; }
    public string? LayerName { get; init; }
    public UnrFileObjectReference? StaticMeshReference { get; init; }
    public UnrFileObjectReference? TextureReference { get; init; }
    public float? Opacity { get; init; }
    public float? FadeOutStartTime { get; init; }
    public bool FadeOut { get; init; }
    public float? FadeInEndTime { get; init; }
    public bool FadeIn { get; init; }
    public int? MaxParticles { get; init; }
    public UnrFloatRange? LifetimeRange { get; init; }
    public Vector3? Acceleration { get; init; }
    public UnrRangeVector? StartLocationRange { get; init; }
    public UnrRangeVector? StartSizeRange { get; init; }
    public UnrRangeVector? StartVelocityRange { get; init; }
    public UnrRangeVector? StartSpinRange { get; init; }
    public UnrRangeVector? SpinsPerSecondRange { get; init; }
    public UnrParticleColorScale[] ColorScale { get; init; } = [];
    public UnrParticleSizeScale[] SizeScale { get; init; } = [];
}
