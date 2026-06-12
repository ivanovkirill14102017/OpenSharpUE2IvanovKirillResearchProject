namespace L2Viewer.UnrFile;

public sealed class UnrBrushObject : UnrFileObject
{
    public bool? UpdateShadow { get; init; }
    public byte? CsgOperValue { get; init; }
    public int? PolyFlagsValue { get; init; }
    public UnrFileScale? MainScale { get; init; }
    public UnrFileScale? PostScale { get; init; }
    public UnrFileScale? TempScale { get; init; }
    public UnrPointRegion? Region { get; init; }
    public UnrTextureModifyInfo? TexModifyInfo { get; init; }
    public Vector3? Location { get; init; }
    public Vector3? Rotation { get; init; }
    public float DrawScale { get; init; } = 1f;
    public Vector3 DrawScale3D { get; init; } = Vector3.One;
    public Vector3 PrePivot { get; init; } = Vector3.Zero;
    public Vector3? SwayRotationOrig { get; init; }
    public string? Tag { get; init; }
    public string? Group { get; init; }
    public string? ForcedRegionTag { get; init; }
    public UnrFileObjectReference? BrushReference { get; init; }
    public UnrFileObjectReference? BaseReference { get; init; }
    public UnrFileObjectReference? LevelReference { get; init; }
    public UnrFileObjectReference? OwnerReference { get; init; }
    public UnrFileObjectReference? MeshReference { get; init; }
    public UnrFileObjectReference? TextureReference { get; init; }
    public UnrFileObjectReference? PhysicsVolumeReference { get; init; }
    public UnrFileUnknownProperty[] UnknownProperties { get; init; } = [];
}
