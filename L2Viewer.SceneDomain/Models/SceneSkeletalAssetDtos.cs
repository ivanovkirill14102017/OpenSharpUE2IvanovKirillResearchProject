using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.SceneDomain.Models;

[ForExternalUse]
public sealed class SceneSkeletalAsset
{
    public required string PackagePath { get; init; }
    public required int MeshExportIndex { get; init; }
    public required string MeshObjectName { get; init; }
    public required string AnimationObjectName { get; init; }
    public required string Source { get; init; }
    public required string Details { get; init; }
    public required SceneSkeletalSkeleton Skeleton { get; init; }
    public required SceneSkeletalGeometry Mesh { get; init; }
    public required SceneSkeletalAnimationSet AnimationSet { get; init; }
    public required IReadOnlyList<SceneSkeletalMaterialBinding> MaterialBindings { get; init; }
    public string? PrimaryTextureReference { get; init; }
    public required IReadOnlyList<MaterialTextureInfo> UsedTextures { get; init; }
}

public sealed class SceneSkeletalMaterialBinding
{
    public required int MaterialId { get; init; }
    public string? PackageName { get; init; }
    public string? ObjectName { get; init; }
    public string? TextureReference { get; init; }
    public string? ResolvedPackagePath { get; init; }
}

public sealed class SceneSkeletalSkeleton
{
    public required string Name { get; init; }
    public required IReadOnlyList<SceneSkeletalBone> Bones { get; init; }
}

public sealed class SceneSkeletalBone
{
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required int ParentIndex { get; init; }
    public required Vector3 RawBindPosition { get; init; }
    public required Quaternion RawBindRotation { get; init; }
    public required Vector3 StoredOrigLocation { get; init; }
    public required Quaternion StoredOrigQuaternion { get; init; }
    public required Quaternion PostQuaternion { get; init; }
    public required bool IsRoot { get; init; }
    public required bool DontInvertRoot { get; init; }
}

public sealed class SceneSkeletalGeometry
{
    public required string Name { get; init; }
    public required IReadOnlyList<SceneSkeletalPoint> Points { get; init; }
    public required IReadOnlyList<SceneSkeletalWedge> Wedges { get; init; }
    public required IReadOnlyList<SceneSkeletalFace> Faces { get; init; }
    public required IReadOnlyList<SceneSkeletalWeight> Weights { get; init; }
    public required IReadOnlyList<SceneSkeletalSubMesh> SubMeshes { get; init; }
    public required Vector3 BoundsMin { get; init; }
    public required Vector3 BoundsMax { get; init; }
}

public sealed class SceneSkeletalPoint
{
    public required Vector3 Position { get; init; }
}

public sealed class SceneSkeletalWedge
{
    public required int PointIndex { get; init; }
    public required Vector2 UV { get; init; }
    public required int MaterialIndex { get; init; }
}

public sealed class SceneSkeletalFace
{
    public required int WedgeIndex0 { get; init; }
    public required int WedgeIndex1 { get; init; }
    public required int WedgeIndex2 { get; init; }
    public required int MaterialIndex { get; init; }
}

public sealed class SceneSkeletalWeight
{
    public required float Weight { get; init; }
    public required int PointIndex { get; init; }
    public required int BoneIndex { get; init; }
}

public sealed class SceneSkeletalSubMesh
{
    public required int MaterialId { get; init; }
    public required int FaceCount { get; init; }
}

public sealed class SceneSkeletalAnimationSet
{
    public required string Name { get; init; }
    public required IReadOnlyList<SceneSkeletalAnimationBone> Bones { get; init; }
    public required IReadOnlyList<SceneSkeletalAnimationSequence> Sequences { get; init; }
    public required IReadOnlyList<SceneSkeletalAnimationKey> Keys { get; init; }
}

public sealed class SceneSkeletalAnimationBone
{
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required int ParentIndex { get; init; }
}

public sealed class SceneSkeletalAnimationSequence
{
    public required string Name { get; init; }
    public required int TotalBones { get; init; }
    public required float TrackTime { get; init; }
    public required float AnimRate { get; init; }
    public required int FirstRawFrame { get; init; }
    public required int NumRawFrames { get; init; }
}

public sealed class SceneSkeletalAnimationKey
{
    public required Vector3 Position { get; init; }
    public required Quaternion Orientation { get; init; }
    public required float Time { get; init; }
}
