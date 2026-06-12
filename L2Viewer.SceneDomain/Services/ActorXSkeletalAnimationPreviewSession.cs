using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;
using L2Viewer.UkxFile;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace L2Viewer.SceneDomain.Services;

public sealed class ActorXSkeletalAnimationPreviewSession : ISkeletalAnimationPreviewSession
{
    private sealed record RuntimeInfluence(int BoneIndex, float Weight);
    private sealed record RuntimeVertex(Vector3 BindPosition, Vector3 BindNormal, Vector2 UV, int MaterialId, RuntimeInfluence[] Influences);
    private sealed record RuntimeTriangle(int PointIndexA, int PointIndexB, int PointIndexC, Vector2 UvA, Vector2 UvB, Vector2 UvC, int MaterialId);
    private sealed record RuntimeSequence(string Name, float DurationSeconds, int NumFrames, float AnimRate, float TrackTime);

    private readonly BlenderCompatModel _model;
    private readonly BlenderCompatAnimationSet _animationSet;
    private readonly string _meshName;
    private readonly Vector3[] _bindPoints;
    private readonly RuntimeVertex[] _vertices;
    private readonly RuntimeTriangle[] _triangles;
    private readonly RuntimeInfluence[][] _pointInfluences;
    private readonly RuntimeSequence[] _sequences;
    private readonly int[] _parentIndices;
    private readonly Matrix4x4[] _compatRestPoseMatrices;
    private readonly Matrix4x4[] _restPoseMatrices;
    private readonly Matrix4x4[] _inverseRestPoseMatrices;
    private readonly string? _textureRef;
    private readonly IReadOnlyList<MaterialTextureInfo> _usedTextures;

    private ActorXSkeletalAnimationPreviewSession(
        BlenderCompatModel model,
        BlenderCompatAnimationSet animationSet,
        Vector3[] bindPoints,
        RuntimeVertex[] vertices,
        RuntimeTriangle[] triangles,
        RuntimeInfluence[][] pointInfluences,
        RuntimeSequence[] sequences,
        int[] parentIndices,
        Matrix4x4[] compatRestPoseMatrices,
        Matrix4x4[] restPoseMatrices,
        Matrix4x4[] inverseRestPoseMatrices,
        string? textureRef,
        IReadOnlyList<MaterialTextureInfo> usedTextures)
    {
        _model = model;
        _animationSet = animationSet;
        _meshName = model.Name;
        _bindPoints = bindPoints;
        _vertices = vertices;
        _triangles = triangles;
        _pointInfluences = pointInfluences;
        _sequences = sequences;
        _parentIndices = parentIndices;
        _compatRestPoseMatrices = compatRestPoseMatrices;
        _restPoseMatrices = restPoseMatrices;
        _inverseRestPoseMatrices = inverseRestPoseMatrices;
        _textureRef = textureRef;
        _usedTextures = usedTextures;

        Animations = _sequences
            .Select(x => new SkeletalAnimationSequenceInfo(x.Name, x.DurationSeconds, x.NumFrames, model.Skeleton.Bones.Length))
            .ToArray();
    }

    public IReadOnlyList<SkeletalAnimationSequenceInfo> Animations { get; }

    public static ActorXSkeletalAnimationPreviewSession Create(
        BlenderCompatModel model,
        BlenderCompatAnimationSet animationSet,
        MeshData? materialSource = null)
    {
        if (model.Skeleton.Bones.Length == 0 || animationSet.Sequences.Length == 0)
        {
            throw new InvalidOperationException(
                $"ActorX preview requires bones and animation sequences. Mesh='{model.Name}', Bones={model.Skeleton.Bones.Length}, Sequences={animationSet.Sequences.Length}.");
        }

        var bindPoints = model.Mesh.Points.Select(x => x * 100f).ToArray();
        var pointInfluences = BuildInfluenceMap(model.Mesh.Points.Length, model.Mesh.Weights);
        var vertices = BuildVertices(model.Mesh, pointInfluences);
        var triangles = BuildTriangles(model.Mesh);
        if (triangles.Length == 0 || vertices.Length == 0)
        {
            throw new InvalidOperationException($"ActorX preview mesh '{model.Name}' did not produce any renderable vertices.");
        }

        var parentIndices = model.Skeleton.Bones.Select(x => x.ParentIndex).ToArray();
        var compatRestPoseMatrices = ActorXBlenderCompat.BuildImportedRestPoseMatrices(model.Skeleton);
        var restPoseMatrices = compatRestPoseMatrices.Select(ScaleTranslationBy100).ToArray();
        var inverseRestPoseMatrices = new Matrix4x4[restPoseMatrices.Length];
        for (var i = 0; i < restPoseMatrices.Length; i++)
        {
            inverseRestPoseMatrices[i] = Matrix4x4.Invert(restPoseMatrices[i], out var inverse)
                ? inverse
                : Matrix4x4.Identity;
        }

        var sequences = animationSet.Sequences
            .Select(x => new RuntimeSequence(
                x.Name,
                x.AnimRate > 0.0001f && x.NumRawFrames > 0 ? x.NumRawFrames / x.AnimRate : Math.Max(1f, x.TrackTime),
                x.NumRawFrames,
                x.AnimRate,
                x.TrackTime))
            .ToArray();

        return new ActorXSkeletalAnimationPreviewSession(
            model,
            animationSet,
            bindPoints,
            vertices,
            triangles,
            pointInfluences,
            sequences,
            parentIndices,
            compatRestPoseMatrices,
            restPoseMatrices,
            inverseRestPoseMatrices,
            materialSource?.TextureRef,
            materialSource?.UsedTextures ?? []);
    }

    public static ActorXSkeletalAnimationPreviewSession Create(SceneSkeletalAsset asset)
    {
        if (asset is null)
        {
            throw new ArgumentNullException(nameof(asset));
        }

        var model = new BlenderCompatModel(
            asset.MeshObjectName,
            new BlenderCompatSkeleton(
                asset.Skeleton.Name,
                asset.Skeleton.Bones
                    .OrderBy(x => x.Index)
                    .Select(x => new BlenderCompatSkeletonBone(
                        x.Name,
                        x.ParentIndex,
                        x.RawBindPosition,
                        x.RawBindRotation,
                        x.StoredOrigLocation,
                        x.StoredOrigQuaternion,
                        x.PostQuaternion,
                        x.IsRoot,
                        x.DontInvertRoot))
                    .ToArray()),
            new BlenderCompatMesh(
                asset.Mesh.Points.Select(x => x.Position).ToArray(),
                asset.Mesh.Wedges.Select(x => new BlenderCompatMeshWedge(
                    x.PointIndex,
                    x.UV,
                    (byte)Math.Clamp(x.MaterialIndex, byte.MinValue, byte.MaxValue))).ToArray(),
                asset.Mesh.Faces.Select(x => new BlenderCompatMeshFace(
                    x.WedgeIndex0,
                    x.WedgeIndex1,
                    x.WedgeIndex2,
                    (byte)Math.Clamp(x.MaterialIndex, byte.MinValue, byte.MaxValue))).ToArray(),
                asset.Mesh.Weights.Select(x => new BlenderCompatMeshWeight(x.Weight, x.PointIndex, x.BoneIndex)).ToArray()));
        var animationSet = new BlenderCompatAnimationSet(
            asset.AnimationSet.Name,
            asset.AnimationSet.Bones
                .OrderBy(x => x.Index)
                .Select(x => new BlenderCompatAnimationBone(x.Name, x.ParentIndex))
                .ToArray(),
            asset.AnimationSet.Sequences.Select(x => new BlenderCompatAnimationSequence(
                x.Name,
                x.TotalBones,
                x.TrackTime,
                x.AnimRate,
                x.FirstRawFrame,
                x.NumRawFrames)).ToArray(),
            asset.AnimationSet.Keys.Select(x => new BlenderCompatAnimationKey(x.Position, x.Orientation, x.Time)).ToArray());

        return Create(
            model,
            animationSet,
            new MeshData(
                asset.Mesh.Name,
                [],
                asset.Mesh.BoundsMin,
                asset.Mesh.BoundsMax,
                asset.Source,
                asset.PrimaryTextureReference,
                asset.UsedTextures));
    }

    public MeshData BuildBindPoseMesh()
    {
        return BuildVertexSkinnedMesh(BuildSkinMatrices(_restPoseMatrices), "ActorX bind pose preview");
    }

    public SkeletalDebugOverlay BuildBindPoseOverlay()
    {
        return BuildOverlay(_restPoseMatrices);
    }

    public SkeletalDebugFrameSnapshot CaptureBindPoseDebugFrame(int sampleVertexCount = 64)
    {
        return CaptureDebugFrameFromWorldMatrices(null, 0, _restPoseMatrices, sampleVertexCount);
    }

    public MeshData? BuildAnimatedMesh(
        string sequenceName,
        double elapsedSeconds,
        bool loop,
        out bool finished)
    {
        var sequence = _sequences.FirstOrDefault(x => x.Name.Is(sequenceName));
        if (sequence is null)
        {
            finished = true;
            return null;
        }

        var duration = Math.Max(0.001f, sequence.DurationSeconds);
        var rawElapsed = Math.Max(0f, (float)elapsedSeconds);
        var localTime = loop ? rawElapsed % duration : MathF.Min(rawElapsed, duration);
        finished = !loop && rawElapsed >= duration;

        var frameIndex = sequence.AnimRate > 0.0001f
            ? (int)MathF.Round(localTime * sequence.AnimRate)
            : (sequence.NumFrames <= 1 ? 0 : (int)MathF.Round((localTime / duration) * (sequence.NumFrames - 1)));
        frameIndex = Math.Clamp(frameIndex, 0, Math.Max(0, sequence.NumFrames - 1));

        var worldMatrices = BuildAnimatedWorldMatrices(sequence, frameIndex);
        return BuildVertexSkinnedMesh(
            BuildSkinMatrices(worldMatrices),
            $"ActorX animation preview: {sequence.Name} frame {frameIndex}");
    }

    public SkeletalDebugOverlay? BuildAnimatedOverlay(string sequenceName, double elapsedSeconds, bool loop, out bool finished)
    {
        var sequence = _sequences.FirstOrDefault(x => x.Name.Is(sequenceName));
        if (sequence is null)
        {
            finished = true;
            return null;
        }

        var duration = Math.Max(0.001f, sequence.DurationSeconds);
        var rawElapsed = Math.Max(0f, (float)elapsedSeconds);
        var localTime = loop ? rawElapsed % duration : MathF.Min(rawElapsed, duration);
        finished = !loop && rawElapsed >= duration;

        var frameIndex = sequence.AnimRate > 0.0001f
            ? (int)MathF.Round(localTime * sequence.AnimRate)
            : (sequence.NumFrames <= 1 ? 0 : (int)MathF.Round((localTime / duration) * (sequence.NumFrames - 1)));
        frameIndex = Math.Clamp(frameIndex, 0, Math.Max(0, sequence.NumFrames - 1));

        return BuildOverlay(BuildAnimatedWorldMatrices(sequence, frameIndex));
    }

    public MeshData? BuildAnimatedMeshFrame(
        string sequenceName,
        int frameIndex)
    {
        var sequence = _sequences.FirstOrDefault(x => x.Name.Is(sequenceName));
        if (sequence is null)
        {
            return null;
        }

        var clampedFrame = Math.Clamp(frameIndex, 0, Math.Max(0, sequence.NumFrames - 1));
        return BuildVertexSkinnedMesh(
            BuildSkinMatrices(BuildAnimatedWorldMatrices(sequence, clampedFrame)),
            $"ActorX animation preview: {sequence.Name} frame {clampedFrame}");
    }

    public SkeletalDebugOverlay? BuildAnimatedOverlayFrame(string sequenceName, int frameIndex)
    {
        var sequence = _sequences.FirstOrDefault(x => x.Name.Is(sequenceName));
        if (sequence is null)
        {
            return null;
        }

        var clampedFrame = Math.Clamp(frameIndex, 0, Math.Max(0, sequence.NumFrames - 1));
        return BuildOverlay(BuildAnimatedWorldMatrices(sequence, clampedFrame));
    }

    public SkeletalDebugFrameSnapshot? CaptureAnimatedDebugFrame(string sequenceName, int frameIndex, int sampleVertexCount = 64)
    {
        var sequence = _sequences.FirstOrDefault(x => x.Name.Is(sequenceName));
        if (sequence is null)
        {
            return null;
        }

        var clampedFrame = Math.Clamp(frameIndex, 0, Math.Max(0, sequence.NumFrames - 1));
        var worldMatrices = BuildAnimatedWorldMatrices(sequence, clampedFrame);
        return CaptureDebugFrameFromWorldMatrices(sequence.Name, clampedFrame, worldMatrices, sampleVertexCount);
    }

    public SkeletalDebugFrameSnapshot? CaptureAnimatedDebugFrameAtElapsed(string sequenceName, double elapsedSeconds, bool loop, int sampleVertexCount = 64)
    {
        var sequence = _sequences.FirstOrDefault(x => x.Name.Is(sequenceName));
        if (sequence is null)
        {
            return null;
        }

        var duration = Math.Max(0.001f, sequence.DurationSeconds);
        var rawElapsed = Math.Max(0f, (float)elapsedSeconds);
        var localTime = loop ? rawElapsed % duration : MathF.Min(rawElapsed, duration);

        var frameIndex = sequence.AnimRate > 0.0001f
            ? (int)MathF.Round(localTime * sequence.AnimRate)
            : (sequence.NumFrames <= 1 ? 0 : (int)MathF.Round((localTime / duration) * (sequence.NumFrames - 1)));
        frameIndex = Math.Clamp(frameIndex, 0, Math.Max(0, sequence.NumFrames - 1));

        var worldMatrices = BuildAnimatedWorldMatrices(sequence, frameIndex);
        return CaptureDebugFrameFromWorldMatrices(sequence.Name, frameIndex, worldMatrices, sampleVertexCount);
    }

    private Matrix4x4[] BuildSkinMatrices(Matrix4x4[] worldMatrices)
    {
        var skinMatrices = new Matrix4x4[worldMatrices.Length];
        for (var i = 0; i < worldMatrices.Length; i++)
        {
            skinMatrices[i] = _inverseRestPoseMatrices[i] * worldMatrices[i];
        }

        return skinMatrices;
    }

    private MeshData BuildVertexSkinnedMesh(Matrix4x4[] skinMatrices, string note)
    {
        var triangles = new List<Triangle>(_vertices.Length / 3);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i + 2 < _vertices.Length; i += 3)
        {
            var a = SkinVertex(_vertices[i], skinMatrices);
            var b = SkinVertex(_vertices[i + 1], skinMatrices);
            var c = SkinVertex(_vertices[i + 2], skinMatrices);
            var normal = ComputeFaceNormal(a.Position, b.Position, c.Position);
            a = a with { Normal = normal };
            b = b with { Normal = normal };
            c = c with { Normal = normal };
            triangles.Add(new Triangle(a, b, c, _vertices[i].MaterialId));
            UpdateBounds(a.Position, ref min, ref max);
            UpdateBounds(b.Position, ref min, ref max);
            UpdateBounds(c.Position, ref min, ref max);
        }

        if (triangles.Count == 0)
        {
            min = Vector3.Zero;
            max = Vector3.Zero;
        }

        return new MeshData(_meshName, triangles, min, max, note, _textureRef, _usedTextures);
    }

    private SkeletalDebugFrameSnapshot CaptureDebugFrameFromWorldMatrices(string? sequenceName, int frameIndex, Matrix4x4[] worldMatrices, int sampleVertexCount)
    {
        var skinMatrices = new Matrix4x4[worldMatrices.Length];
        for (var i = 0; i < worldMatrices.Length; i++)
        {
            skinMatrices[i] = _inverseRestPoseMatrices[i] * worldMatrices[i];
        }

        var vertices = SkinPoints(_bindPoints, _pointInfluences, skinMatrices);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var point in vertices)
        {
            UpdateBounds(point, ref min, ref max);
        }

        if (vertices.Length == 0)
        {
            min = Vector3.Zero;
            max = Vector3.Zero;
        }

        var bones = new SkeletalDebugBoneSnapshot[_model.Skeleton.Bones.Length];
        var poseSample = sequenceName is null
            ? null
            : ActorXBlenderCompat.SamplePose(_model.Skeleton, _animationSet, sequenceName, frameIndex);
        for (var i = 0; i < _model.Skeleton.Bones.Length; i++)
        {
            var bone = _model.Skeleton.Bones[i];
            var parentName = bone.ParentIndex >= 0 && bone.ParentIndex < _model.Skeleton.Bones.Length
                ? _model.Skeleton.Bones[bone.ParentIndex].Name
                : null;

            bones[i] = new SkeletalDebugBoneSnapshot(
                bone.Name,
                parentName,
                worldMatrices[i].Translation / 100f,
                Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(worldMatrices[i])),
                poseSample?.Bones[i].ChannelLocation ?? Vector3.Zero,
                poseSample?.Bones[i].ChannelRotation ?? Quaternion.Identity);
        }

        var count = Math.Min(sampleVertexCount, _bindPoints.Length);
        var samples = new SkeletalDebugVertexSnapshot[count];
        for (var i = 0; i < count; i++)
        {
            var influences = _pointInfluences[i]
                .Select(x => (_model.Skeleton.Bones[x.BoneIndex].Name, x.Weight))
                .Cast<(string BoneName, float Weight)>()
                .ToArray();
            samples[i] = new SkeletalDebugVertexSnapshot(
                i,
                _bindPoints[i] / 100f,
                vertices[i] / 100f,
                influences);
        }

        return new SkeletalDebugFrameSnapshot(frameIndex, bones, samples, min / 100f, max / 100f);
    }

    private static RuntimeTriangle[] BuildTriangles(BlenderCompatMesh mesh)
    {
        var triangles = new List<RuntimeTriangle>(mesh.Faces.Length);
        foreach (var face in mesh.Faces)
        {
            if ((uint)face.WedgeIndex0 >= mesh.Wedges.Length || (uint)face.WedgeIndex1 >= mesh.Wedges.Length || (uint)face.WedgeIndex2 >= mesh.Wedges.Length)
            {
                continue;
            }

            var w0 = mesh.Wedges[face.WedgeIndex0];
            var w1 = mesh.Wedges[face.WedgeIndex1];
            var w2 = mesh.Wedges[face.WedgeIndex2];
            if ((uint)w0.PointIndex >= mesh.Points.Length || (uint)w1.PointIndex >= mesh.Points.Length || (uint)w2.PointIndex >= mesh.Points.Length)
            {
                continue;
            }

            triangles.Add(new RuntimeTriangle(
                w1.PointIndex,
                w0.PointIndex,
                w2.PointIndex,
                w1.UV,
                w0.UV,
                w2.UV,
                face.MaterialIndex));
        }

        return triangles.ToArray();
    }

    private static RuntimeVertex[] BuildVertices(BlenderCompatMesh mesh, RuntimeInfluence[][] influencesByPoint)
    {
        var vertices = new List<RuntimeVertex>(mesh.Faces.Length * 3);
        foreach (var face in mesh.Faces)
        {
            if ((uint)face.WedgeIndex0 >= mesh.Wedges.Length || (uint)face.WedgeIndex1 >= mesh.Wedges.Length || (uint)face.WedgeIndex2 >= mesh.Wedges.Length)
            {
                continue;
            }

            var w0 = mesh.Wedges[face.WedgeIndex0];
            var w1 = mesh.Wedges[face.WedgeIndex1];
            var w2 = mesh.Wedges[face.WedgeIndex2];
            if ((uint)w0.PointIndex >= mesh.Points.Length || (uint)w1.PointIndex >= mesh.Points.Length || (uint)w2.PointIndex >= mesh.Points.Length)
            {
                continue;
            }

            vertices.Add(CreateVertex(mesh.Points[w1.PointIndex] * 100f, w1.UV, face.MaterialIndex, influencesByPoint, w1.PointIndex));
            vertices.Add(CreateVertex(mesh.Points[w0.PointIndex] * 100f, w0.UV, face.MaterialIndex, influencesByPoint, w0.PointIndex));
            vertices.Add(CreateVertex(mesh.Points[w2.PointIndex] * 100f, w2.UV, face.MaterialIndex, influencesByPoint, w2.PointIndex));
        }

        return vertices.ToArray();
    }

    private static RuntimeVertex CreateVertex(Vector3 position, Vector2 uv, int materialIndex, RuntimeInfluence[][] influencesByPoint, int pointIndex)
    {
        var influences = pointIndex >= 0 && pointIndex < influencesByPoint.Length ? influencesByPoint[pointIndex] : [];
        return new RuntimeVertex(position, Vector3.UnitZ, uv, materialIndex, influences);
    }

    private static RuntimeInfluence[][] BuildInfluenceMap(int pointCount, BlenderCompatMeshWeight[] influences)
    {
        var lists = new List<RuntimeInfluence>[pointCount];
        foreach (var influence in influences)
        {
            if (influence.Weight <= 0f || influence.PointIndex < 0 || influence.PointIndex >= pointCount)
            {
                continue;
            }

            lists[influence.PointIndex] ??= [];
            lists[influence.PointIndex].Add(new RuntimeInfluence(influence.BoneIndex, influence.Weight));
        }

        var result = new RuntimeInfluence[pointCount][];
        for (var i = 0; i < pointCount; i++)
        {
            result[i] = lists[i] is null ? [] : NormalizeWeights(lists[i]!);
        }

        return result;
    }

    private static RuntimeInfluence[] NormalizeWeights(List<RuntimeInfluence> influences)
    {
        if (influences.Count == 0)
        {
            return [];
        }

        var totalWeight = influences.Sum(x => x.Weight);
        if (totalWeight <= 0.000001f)
        {
            return [];
        }

        return influences.Where(x => x.Weight > 0f).Select(x => new RuntimeInfluence(x.BoneIndex, x.Weight / totalWeight)).ToArray();
    }

    private SkeletalDebugOverlay BuildOverlay(Matrix4x4[] worldMatrices)
    {
        var positions = worldMatrices.Select(x => x.Translation).ToArray();
        return BuildOverlayFromPositions(positions, _parentIndices);
    }

    private static SkeletalDebugOverlay BuildOverlayFromPositions(Vector3[] positions, int[] parentIndices)
    {
        var segments = new List<SkeletalBoneSegment>(positions.Length * 4);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var point in positions)
        {
            UpdateBounds(point, ref min, ref max);
        }

        if (positions.Length == 0)
        {
            return new SkeletalDebugOverlay([], Vector3.Zero, Vector3.Zero);
        }

        var extent = max - min;
        var markerRadius = MathF.Max(1.25f, MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)) * 0.03f);

        for (var i = 0; i < positions.Length; i++)
        {
            var point = positions[i];
            if (i < parentIndices.Length && parentIndices[i] >= 0 && parentIndices[i] < positions.Length)
            {
                segments.Add(new SkeletalBoneSegment(positions[parentIndices[i]], point));
            }

            segments.Add(new SkeletalBoneSegment(point + new Vector3(-markerRadius, 0f, 0f), point + new Vector3(markerRadius, 0f, 0f)));
            segments.Add(new SkeletalBoneSegment(point + new Vector3(0f, -markerRadius, 0f), point + new Vector3(0f, markerRadius, 0f)));
            segments.Add(new SkeletalBoneSegment(point + new Vector3(0f, 0f, -markerRadius), point + new Vector3(0f, 0f, markerRadius)));
        }

        return new SkeletalDebugOverlay(segments, min, max);
    }

    private Matrix4x4[] BuildAnimatedWorldMatrices(RuntimeSequence sequence, int frameIndex)
    {
        return ActorXBlenderCompat.BuildPoseMatrices(_model.Skeleton, _compatRestPoseMatrices, _animationSet, sequence.Name, frameIndex)
            .Select(ScaleTranslationBy100)
            .ToArray();
    }

    private static TriangleVertex SkinVertex(RuntimeVertex vertex, Matrix4x4[] skinMatrices)
    {
        if (vertex.Influences.Length == 0)
        {
            return new TriangleVertex(vertex.BindPosition, vertex.UV, vertex.BindNormal);
        }

        var position = Vector3.Zero;
        var normal = Vector3.Zero;
        foreach (var influence in vertex.Influences)
        {
            if ((uint)influence.BoneIndex >= skinMatrices.Length || influence.Weight <= 0f)
            {
                continue;
            }

            position += Vector3.Transform(vertex.BindPosition, skinMatrices[influence.BoneIndex]) * influence.Weight;
            normal += Vector3.TransformNormal(vertex.BindNormal, skinMatrices[influence.BoneIndex]) * influence.Weight;
        }

        if (normal.LengthSquared() > 0.000001f)
        {
            normal = Vector3.Normalize(normal);
        }
        else
        {
            normal = vertex.BindNormal.LengthSquared() > 0.000001f ? Vector3.Normalize(vertex.BindNormal) : Vector3.UnitZ;
        }

        return new TriangleVertex(position, vertex.UV, normal);
    }

    private static Vector3[] SkinPoints(Vector3[] bindPoints, RuntimeInfluence[][] pointInfluences, Matrix4x4[] skinMatrices)
    {
        var result = new Vector3[bindPoints.Length];
        for (var pointIndex = 0; pointIndex < bindPoints.Length; pointIndex++)
        {
            var influences = pointInfluences[pointIndex];
            if (influences.Length == 0)
            {
                result[pointIndex] = bindPoints[pointIndex];
                continue;
            }

            var position = Vector3.Zero;
            foreach (var influence in influences)
            {
                if ((uint)influence.BoneIndex >= skinMatrices.Length || influence.Weight <= 0f)
                {
                    continue;
                }

                position += Vector3.Transform(bindPoints[pointIndex], skinMatrices[influence.BoneIndex]) * influence.Weight;
            }

            result[pointIndex] = position;
        }

        return result;
    }

    private static Matrix4x4 ScaleTranslationBy100(Matrix4x4 matrix)
    {
        matrix.Translation *= 100f;
        return matrix;
    }

    private static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Cross(b - a, c - a);
        return normal.LengthSquared() > 0.000001f ? Vector3.Normalize(normal) : Vector3.UnitZ;
    }

    private static void UpdateBounds(Vector3 point, ref Vector3 min, ref Vector3 max)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }
}
