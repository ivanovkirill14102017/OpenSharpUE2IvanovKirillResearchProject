namespace L2Viewer.SceneDomain.Models;

public abstract class SceneVolumeData : SceneActorBrushData
{
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
}

public sealed class SceneWaterVolumeData : SceneVolumeData
{
    public bool SunAffect { get; init; }
    public string? NextPhysicsVolumeReference { get; init; }
    public string? LocationName { get; init; }
    public bool DynamicActorFilterState { get; init; }
    public bool LightChanged { get; init; }
    public bool DeleteMe { get; init; }
    public bool HiddenEd { get; init; }
    public bool PendingDelete { get; init; }
    public bool Selected { get; init; }
    public string[] TouchingReferences { get; init; } = [];
    public Vector3? ColLocation { get; init; }
    public UnrFileColor? DistanceFogColor { get; init; }
    public UnrFileColor? CellophaneColor { get; init; }
    public bool UseDistanceFogColor { get; init; }
    public bool UseCellophane { get; init; }
    public bool IgnoredRange { get; init; }
}

public sealed class ScenePhysicsVolumeData : SceneVolumeData;

public sealed class SceneBlockingVolumeData : SceneVolumeData;

public sealed class SceneConvexVolumeData : SceneVolumeData;

public sealed class SceneDefaultPhysicsVolumeData : SceneVolumeData;

public sealed class SceneMusicVolumeData : SceneVolumeData;
