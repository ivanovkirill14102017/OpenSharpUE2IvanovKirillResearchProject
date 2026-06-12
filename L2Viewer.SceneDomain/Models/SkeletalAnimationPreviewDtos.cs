using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.SceneDomain.Models;

public sealed record SkeletalAnimationSequenceInfo(
    string Name,
    float DurationSeconds,
    int NumFrames,
    int TrackCount);

public sealed record SkeletalBoneSegment(Vector3 Start, Vector3 End);

public sealed record SkeletalDebugOverlay(
    IReadOnlyList<SkeletalBoneSegment> Segments,
    Vector3 BoundsMin,
    Vector3 BoundsMax);

public sealed record SkeletalDebugBoneSnapshot(
    string Name,
    string? ParentName,
    Vector3 WorldLocation,
    Quaternion WorldRotation,
    Vector3 ChannelLocation,
    Quaternion ChannelRotation);

public sealed record SkeletalDebugVertexSnapshot(
    int Index,
    Vector3 RestWorld,
    Vector3 DeformedWorld,
    IReadOnlyList<(string BoneName, float Weight)> Influences);

public sealed record SkeletalDebugFrameSnapshot(
    int FrameIndex,
    IReadOnlyList<SkeletalDebugBoneSnapshot> Bones,
    IReadOnlyList<SkeletalDebugVertexSnapshot> Vertices,
    Vector3 BoundsMin,
    Vector3 BoundsMax);

public interface ISkeletalAnimationPreviewSession
{
    IReadOnlyList<SkeletalAnimationSequenceInfo> Animations { get; }

    MeshData BuildBindPoseMesh();

    SkeletalDebugOverlay BuildBindPoseOverlay();

    SkeletalDebugFrameSnapshot CaptureBindPoseDebugFrame(int sampleVertexCount = 64);

    MeshData? BuildAnimatedMesh(
        string sequenceName,
        double elapsedSeconds,
        bool loop,
        out bool finished);

    MeshData? BuildAnimatedMeshFrame(
        string sequenceName,
        int frameIndex);

    SkeletalDebugOverlay? BuildAnimatedOverlay(string sequenceName, double elapsedSeconds, bool loop, out bool finished);

    SkeletalDebugOverlay? BuildAnimatedOverlayFrame(string sequenceName, int frameIndex);

    SkeletalDebugFrameSnapshot? CaptureAnimatedDebugFrame(string sequenceName, int frameIndex, int sampleVertexCount = 64);

    SkeletalDebugFrameSnapshot? CaptureAnimatedDebugFrameAtElapsed(string sequenceName, double elapsedSeconds, bool loop, int sampleVertexCount = 64);
}

public sealed record SceneSkeletalPreviewData(
    string MeshObjectName,
    string AnimationObjectName,
    string Source,
    string Details,
    SceneSkeletalAsset Asset,
    ISkeletalAnimationPreviewSession Session);
