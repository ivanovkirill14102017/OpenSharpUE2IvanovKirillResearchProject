using System.Numerics;
using System.Text;

namespace L2Viewer.UkxFile;

public static class ActorXBlenderCompat
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    public static BlenderCompatSkeleton LoadSkeletonFromPsk(string pskPath)
    {
        return LoadModelFromPsk(pskPath).Skeleton;
    }

    public static BlenderCompatMesh LoadMeshFromPsk(string pskPath)
    {
        return LoadModelFromPsk(pskPath).Mesh;
    }

    public static BlenderCompatModel LoadModelFromPsk(string pskPath)
    {
        using var stream = File.OpenRead(pskPath);
        using var reader = new BinaryReader(stream);
        ValidateMainHeader(reader, "ACTRHEAD");

        var contents = ReadPskContents(reader);
        if (contents.Bones.Length == 0)
        {
            throw new InvalidOperationException($"PSK '{pskPath}' does not contain REFSKELT.");
        }

        var name = Path.GetFileNameWithoutExtension(pskPath);
        return new BlenderCompatModel(
            name,
            new BlenderCompatSkeleton(name, contents.Bones),
            new BlenderCompatMesh(
                contents.Points,
                contents.Wedges,
                contents.Faces,
                contents.Weights));
    }

    public static BlenderCompatAnimationSet LoadAnimationSetFromPsa(string psaPath)
    {
        using var stream = File.OpenRead(psaPath);
        using var reader = new BinaryReader(stream);
        ValidateMainHeader(reader, "ANIMHEAD");

        var bones = Array.Empty<BlenderCompatAnimationBone>();
        var sequences = Array.Empty<BlenderCompatAnimationSequence>();
        var keys = Array.Empty<BlenderCompatAnimationKey>();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var chunk = ReadChunkHeader(reader);
            switch (chunk.Id)
            {
                case "BONENAMES":
                    bones = ReadPsaBones(reader, chunk);
                    break;
                case "ANIMINFO":
                    sequences = ReadPsaSequences(reader, chunk);
                    break;
                case "ANIMKEYS":
                    keys = ReadPsaKeys(reader, chunk);
                    break;
                default:
                    SkipChunk(reader, chunk);
                    break;
            }
        }

        if (bones.Length == 0)
        {
            throw new InvalidOperationException($"PSA '{psaPath}' does not contain BONENAMES.");
        }

        if (sequences.Length == 0)
        {
            throw new InvalidOperationException($"PSA '{psaPath}' does not contain ANIMINFO.");
        }

        return new BlenderCompatAnimationSet(Path.GetFileNameWithoutExtension(psaPath), bones, sequences, keys);
    }

    public static BlenderCompatPoseSample SamplePose(
        BlenderCompatSkeleton skeleton,
        BlenderCompatAnimationSet animationSet,
        string sequenceName,
        int frameIndex)
    {
        if (skeleton is null)
        {
            throw new ArgumentNullException(nameof(skeleton));
        }

        if (animationSet is null)
        {
            throw new ArgumentNullException(nameof(animationSet));
        }

        var restMatrices = BuildImportedRestPoseMatrices(skeleton);
        var frame = SampleFrame(skeleton, animationSet, sequenceName, frameIndex);
        var worldMatrices = BuildPoseMatrices(skeleton, restMatrices, frame.ChannelLocations, frame.ChannelRotations);

        var bones = skeleton.Bones
            .Select((bone, index) => new BlenderCompatBonePose(
                bone.Name,
                bone.ParentIndex >= 0 && bone.ParentIndex < skeleton.Bones.Length ? skeleton.Bones[bone.ParentIndex].Name : null,
                frame.RawLocalPositions[index],
                frame.RawLocalRotations[index],
                frame.ChannelLocations[index],
                frame.ChannelRotations[index],
                worldMatrices[index].Translation,
                Quaternion.CreateFromRotationMatrix(worldMatrices[index])))
            .ToArray();

        return new BlenderCompatPoseSample(
            frame.Sequence.Name,
            frame.ClampedFrame,
            frame.Sequence.NumRawFrames,
            frame.Sequence.TrackTime,
            frame.Sequence.AnimRate,
            bones);
    }

    public static Matrix4x4[] BuildImportedRestPoseMatrices(BlenderCompatSkeleton skeleton)
    {
        if (skeleton is null)
        {
            throw new ArgumentNullException(nameof(skeleton));
        }

        return BuildImportRestPoseMatrices(skeleton);
    }

    public static Matrix4x4[] BuildPoseMatrices(
        BlenderCompatSkeleton skeleton,
        Matrix4x4[] restPoseMatrices,
        BlenderCompatAnimationSet animationSet,
        string sequenceName,
        int frameIndex)
    {
        if (restPoseMatrices is null)
        {
            throw new ArgumentNullException(nameof(restPoseMatrices));
        }

        var frame = SampleFrame(skeleton, animationSet, sequenceName, frameIndex);
        return BuildPoseMatrices(skeleton, restPoseMatrices, frame.ChannelLocations, frame.ChannelRotations);
    }

    public static Dictionary<int, int> BuildAnimationNameMap(
        BlenderCompatSkeleton skeleton,
        BlenderCompatAnimationSet animationSet)
    {
        if (skeleton is null)
        {
            throw new ArgumentNullException(nameof(skeleton));
        }

        if (animationSet is null)
        {
            throw new ArgumentNullException(nameof(animationSet));
        }

        if (skeleton.Bones.Length == animationSet.Bones.Length &&
            skeleton.Bones.Zip(animationSet.Bones, (s, a) => s.Name.Is(a.Name)).All(x => x))
        {
            return Enumerable.Range(0, skeleton.Bones.Length).ToDictionary(x => x, x => x);
        }

        var buckets = new Dictionary<string, Queue<int>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < skeleton.Bones.Length; i++)
        {
            var key = skeleton.Bones[i].Name;
            if (!buckets.TryGetValue(key, out var queue))
            {
                queue = new Queue<int>();
                buckets[key] = queue;
            }

            queue.Enqueue(i);
        }

        var result = new Dictionary<int, int>();
        for (var i = 0; i < animationSet.Bones.Length; i++)
        {
            if (buckets.TryGetValue(animationSet.Bones[i].Name, out var queue) && queue.Count > 0)
            {
                result[i] = queue.Dequeue();
            }
        }

        return result;
    }

    private static BlenderCompatSkeletonBone[] ReadPskBones(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new BlenderCompatSkeletonBone[chunk.DataCount];
        var staged = new (string Name, int ParentIndex, Vector3 Position, Quaternion Rotation)[chunk.DataCount];

        for (var i = 0; i < chunk.DataCount; i++)
        {
            var name = ReadFixedString(reader, 64);
            _ = reader.ReadUInt32();
            _ = reader.ReadInt32();
            var parentIndex = reader.ReadInt32();
            var rawRotation = NormalizeSafe(ReadQuaternionXyzw(reader));
            var rawPosition = ReadVector3(reader) * 0.01f;
            _ = reader.ReadSingle();
            _ = ReadVector3(reader);

            if (parentIndex < 0)
            {
                parentIndex = 0;
            }

            staged[i] = (name, parentIndex, rawPosition, rawRotation);
        }

        for (var i = 0; i < staged.Length; i++)
        {
            var entry = staged[i];
            var isRoot = entry.ParentIndex == 0 && i == 0;
            var storedOrigQuaternion = isRoot ? Quaternion.Conjugate(entry.Rotation) : entry.Rotation;
            var postQuaternion = Quaternion.Conjugate(storedOrigQuaternion);
            result[i] = new BlenderCompatSkeletonBone(
                entry.Name,
                isRoot ? -1 : entry.ParentIndex,
                entry.Position,
                entry.Rotation,
                entry.Position,
                storedOrigQuaternion,
                postQuaternion,
                isRoot,
                true);
        }

        return result;
    }

    private static Vector3[] ReadPskPoints(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new Vector3[chunk.DataCount];
        for (var i = 0; i < chunk.DataCount; i++)
        {
            result[i] = ReadVector3(reader) * 0.01f;
        }

        return result;
    }

    private static BlenderCompatMeshWedge[] ReadPskWedges(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new BlenderCompatMeshWedge[chunk.DataCount];
        for (var i = 0; i < chunk.DataCount; i++)
        {
            var pointIndex = reader.ReadInt32();
            var u = reader.ReadSingle();
            var v = reader.ReadSingle();
            var materialIndex = reader.ReadByte();
            reader.BaseStream.Seek(3, SeekOrigin.Current);
            result[i] = new BlenderCompatMeshWedge(pointIndex, new Vector2(u, v), materialIndex);
        }

        return result;
    }

    private static BlenderCompatMeshFace[] ReadPskFaces(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new BlenderCompatMeshFace[chunk.DataCount];
        var use32BitIndices = chunk.Id.Contains("3200", StringComparison.Ordinal);
        for (var i = 0; i < chunk.DataCount; i++)
        {
            var wedgeIndex0 = use32BitIndices ? reader.ReadInt32() : reader.ReadUInt16();
            var wedgeIndex1 = use32BitIndices ? reader.ReadInt32() : reader.ReadUInt16();
            var wedgeIndex2 = use32BitIndices ? reader.ReadInt32() : reader.ReadUInt16();
            var materialIndex = reader.ReadByte();
            _ = reader.ReadByte();
            _ = reader.ReadInt32();
            result[i] = new BlenderCompatMeshFace(wedgeIndex0, wedgeIndex1, wedgeIndex2, materialIndex);
        }

        return result;
    }

    private static BlenderCompatMeshWeight[] ReadPskWeights(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new BlenderCompatMeshWeight[chunk.DataCount];
        for (var i = 0; i < chunk.DataCount; i++)
        {
            var weight = reader.ReadSingle();
            var pointIndex = reader.ReadInt32();
            var boneIndex = reader.ReadInt32();
            result[i] = new BlenderCompatMeshWeight(weight, pointIndex, boneIndex);
        }

        return result;
    }

    private static BlenderCompatAnimationBone[] ReadPsaBones(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new BlenderCompatAnimationBone[chunk.DataCount];
        for (var i = 0; i < chunk.DataCount; i++)
        {
            var name = ReadFixedString(reader, 64);
            _ = reader.ReadUInt32();
            _ = reader.ReadInt32();
            var parentIndex = reader.ReadInt32();
            _ = ReadQuaternionXyzw(reader);
            _ = ReadVector3(reader);
            _ = reader.ReadSingle();
            _ = ReadVector3(reader);
            result[i] = new BlenderCompatAnimationBone(name, parentIndex);
        }

        return result;
    }

    private static BlenderCompatAnimationSequence[] ReadPsaSequences(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new BlenderCompatAnimationSequence[chunk.DataCount];
        for (var i = 0; i < chunk.DataCount; i++)
        {
            var name = ReadFixedString(reader, 64);
            _ = ReadFixedString(reader, 64);
            var totalBones = reader.ReadInt32();
            _ = reader.ReadInt32();
            _ = reader.ReadInt32();
            _ = reader.ReadInt32();
            _ = reader.ReadSingle();
            var trackTime = reader.ReadSingle();
            var animRate = reader.ReadSingle();
            _ = reader.ReadInt32();
            var firstRawFrame = reader.ReadInt32();
            var numRawFrames = reader.ReadInt32();

            result[i] = new BlenderCompatAnimationSequence(name, totalBones, trackTime, animRate, firstRawFrame, numRawFrames);
        }

        return result;
    }

    private static BlenderCompatAnimationKey[] ReadPsaKeys(BinaryReader reader, ChunkHeader chunk)
    {
        var result = new BlenderCompatAnimationKey[chunk.DataCount];
        for (var i = 0; i < chunk.DataCount; i++)
        {
            var position = ReadVector3(reader) * 0.01f;
            var orientation = NormalizeSafe(ReadQuaternionXyzw(reader));
            var time = reader.ReadSingle();
            result[i] = new BlenderCompatAnimationKey(position, orientation, time);
        }

        return result;
    }

    private static PskContents ReadPskContents(BinaryReader reader)
    {
        var points = Array.Empty<Vector3>();
        var wedges = Array.Empty<BlenderCompatMeshWedge>();
        var faces = Array.Empty<BlenderCompatMeshFace>();
        var bones = Array.Empty<BlenderCompatSkeletonBone>();
        var weights = Array.Empty<BlenderCompatMeshWeight>();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var chunk = ReadChunkHeader(reader);
            switch (chunk.Id)
            {
                case "PNTS0000":
                    points = ReadPskPoints(reader, chunk);
                    break;
                case "VTXW0000":
                case "VTXW3200":
                    wedges = ReadPskWedges(reader, chunk);
                    break;
                case "FACE0000":
                case "FACE3200":
                    faces = ReadPskFaces(reader, chunk);
                    break;
                case "REFSKELT":
                case "REFSKEL0":
                    bones = ReadPskBones(reader, chunk);
                    break;
                case "RAWW0000":
                case "RAWWEIGH":
                case "RAWWEIGHTS":
                    weights = ReadPskWeights(reader, chunk);
                    break;
                default:
                    SkipChunk(reader, chunk);
                    break;
            }
        }

        return new PskContents(points, wedges, faces, bones, weights);
    }

    private static void ValidateMainHeader(BinaryReader reader, string expectedId)
    {
        var header = ReadChunkHeader(reader);
        if (!string.Equals(header.Id, expectedId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected main chunk '{expectedId}', found '{header.Id}'.");
        }
    }

    private static ChunkHeader ReadChunkHeader(BinaryReader reader)
    {
        if (reader.BaseStream.Position + 32 > reader.BaseStream.Length)
        {
            throw new EndOfStreamException("Unexpected EOF while reading ActorX chunk header.");
        }

        var id = ReadFixedString(reader, 20);
        var typeFlag = reader.ReadInt32();
        var dataSize = reader.ReadInt32();
        var dataCount = reader.ReadInt32();
        return new ChunkHeader(id, typeFlag, dataSize, dataCount);
    }

    private static void SkipChunk(BinaryReader reader, ChunkHeader chunk)
    {
        reader.BaseStream.Seek((long)chunk.DataSize * chunk.DataCount, SeekOrigin.Current);
    }

    private static string ReadFixedString(BinaryReader reader, int byteCount)
    {
        var bytes = reader.ReadBytes(byteCount);
        var end = Array.IndexOf(bytes, (byte)0);
        if (end < 0)
        {
            end = bytes.Length;
        }

        return Latin1.GetString(bytes, 0, end).TrimEnd();
    }

    private static Quaternion ReadQuaternionXyzw(BinaryReader reader)
    {
        return new Quaternion(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static Quaternion NormalizeSafe(Quaternion value)
    {
        return value.LengthSquared() > 0.000001f ? Quaternion.Normalize(value) : Quaternion.Identity;
    }

    private static Quaternion RotateLikeBlender(Quaternion value, Quaternion rotation)
    {
        return NormalizeSafe(rotation * value);
    }

    private static FrameSample SampleFrame(
        BlenderCompatSkeleton skeleton,
        BlenderCompatAnimationSet animationSet,
        string sequenceName,
        int frameIndex)
    {
        var sequence = animationSet.Sequences.FirstOrDefault(x => x.Name.Is(sequenceName));
        if (sequence is null)
        {
            throw new InvalidOperationException($"Animation '{sequenceName}' was not found in PSA '{animationSet.Name}'.");
        }

        if (sequence.NumRawFrames <= 0)
        {
            throw new InvalidOperationException($"Animation '{sequence.Name}' has no frames.");
        }

        var clampedFrame = Math.Clamp(frameIndex, 0, sequence.NumRawFrames - 1);
        var nameMap = BuildAnimationNameMap(skeleton, animationSet);

        var rawLocalPositions = skeleton.Bones.Select(x => x.RawBindPosition).ToArray();
        var rawLocalRotations = skeleton.Bones.Select(x => x.RawBindRotation).ToArray();
        var channelLocations = new Vector3[skeleton.Bones.Length];
        var channelRotations = Enumerable.Repeat(Quaternion.Identity, skeleton.Bones.Length).ToArray();

        var animatedBoneCount = Math.Min(sequence.TotalBones, animationSet.Bones.Length);
        for (var animationBoneIndex = 0; animationBoneIndex < animatedBoneCount; animationBoneIndex++)
        {
            if (!nameMap.TryGetValue(animationBoneIndex, out var skeletonBoneIndex) ||
                skeletonBoneIndex < 0 ||
                skeletonBoneIndex >= skeleton.Bones.Length)
            {
                continue;
            }

            var keyIndex = ((sequence.FirstRawFrame + clampedFrame) * sequence.TotalBones) + animationBoneIndex;
            if ((uint)keyIndex >= animationSet.Keys.Length)
            {
                continue;
            }

            var skeletonBone = skeleton.Bones[skeletonBoneIndex];
            var key = animationSet.Keys[keyIndex];

            rawLocalPositions[skeletonBoneIndex] = key.Position;
            rawLocalRotations[skeletonBoneIndex] = NormalizeSafe(key.Orientation);

            var q0 = RotateLikeBlender(skeletonBone.PostQuaternion, skeletonBone.StoredOrigQuaternion);
            var q1 = skeletonBone.IsRoot && skeletonBone.DontInvertRoot
                ? RotateLikeBlender(skeletonBone.PostQuaternion, Quaternion.Conjugate(key.Orientation))
                : RotateLikeBlender(skeletonBone.PostQuaternion, key.Orientation);
            channelRotations[skeletonBoneIndex] = NormalizeSafe(Quaternion.Conjugate(q1) * q0);

            var delta = key.Position - skeletonBone.StoredOrigLocation;
            channelLocations[skeletonBoneIndex] = Vector3.Transform(delta, Quaternion.Conjugate(skeletonBone.PostQuaternion));
        }

        return new FrameSample(sequence, clampedFrame, rawLocalPositions, rawLocalRotations, channelLocations, channelRotations);
    }

    private static Matrix4x4[] BuildPoseMatrices(
        BlenderCompatSkeleton skeleton,
        Matrix4x4[] restPoseMatrices,
        Vector3[] channelLocations,
        Quaternion[] channelRotations)
    {
        if (restPoseMatrices.Length != skeleton.Bones.Length)
        {
            throw new ArgumentException("Rest pose matrix count does not match skeleton bone count.", nameof(restPoseMatrices));
        }

        var localRestMatrices = BuildLocalMatricesFromArmatureMatrices(skeleton, restPoseMatrices);
        var localPoseMatrices = new Matrix4x4[skeleton.Bones.Length];
        for (var i = 0; i < skeleton.Bones.Length; i++)
        {
            localPoseMatrices[i] = CreateBlenderBasisMatrix(channelLocations[i], channelRotations[i]) * localRestMatrices[i];
        }

        return BuildArmatureMatrices(skeleton, localPoseMatrices);
    }

    private static Matrix4x4[] BuildImportRestPoseMatrices(BlenderCompatSkeleton skeleton)
    {
        var result = new Matrix4x4[skeleton.Bones.Length];
        var worldRotations = new Quaternion[skeleton.Bones.Length];
        for (var i = 0; i < skeleton.Bones.Length; i++)
        {
            var bone = skeleton.Bones[i];
            var rawRotation = NormalizeSafe(bone.RawBindRotation);
            if (bone.ParentIndex < 0 || bone.ParentIndex >= i)
            {
                var rootRotation = bone.DontInvertRoot ? rawRotation : Quaternion.Conjugate(rawRotation);
                worldRotations[i] = NormalizeSafe(rootRotation);
                result[i] = Matrix4x4.CreateFromQuaternion(worldRotations[i]) * Matrix4x4.CreateTranslation(bone.RawBindPosition);
                continue;
            }

            var parentRotation = worldRotations[bone.ParentIndex];
            var worldRotation = NormalizeSafe(parentRotation * Quaternion.Conjugate(rawRotation));
            var worldPosition = result[bone.ParentIndex].Translation + Vector3.Transform(bone.RawBindPosition, parentRotation);
            worldRotations[i] = worldRotation;
            result[i] = Matrix4x4.CreateFromQuaternion(worldRotation) * Matrix4x4.CreateTranslation(worldPosition);
        }

        return result;
    }

    private static Matrix4x4[] BuildArmatureMatrices(BlenderCompatSkeleton skeleton, Matrix4x4[] localMatrices)
    {
        var result = new Matrix4x4[skeleton.Bones.Length];
        for (var i = 0; i < skeleton.Bones.Length; i++)
        {
            var parentIndex = skeleton.Bones[i].ParentIndex;
            result[i] = parentIndex >= 0 && parentIndex < i
                ? localMatrices[i] * result[parentIndex]
                : localMatrices[i];
        }

        return result;
    }

    private static Matrix4x4[] BuildLocalMatricesFromArmatureMatrices(BlenderCompatSkeleton skeleton, Matrix4x4[] armatureMatrices)
    {
        var result = new Matrix4x4[skeleton.Bones.Length];
        for (var i = 0; i < skeleton.Bones.Length; i++)
        {
            var parentIndex = skeleton.Bones[i].ParentIndex;
            if (parentIndex >= 0 && parentIndex < i && Matrix4x4.Invert(armatureMatrices[parentIndex], out var inverseParent))
            {
                result[i] = armatureMatrices[i] * inverseParent;
            }
            else
            {
                result[i] = armatureMatrices[i];
            }
        }

        return result;
    }

    private static Matrix4x4 CreateBlenderBasisMatrix(Vector3 location, Quaternion rotation)
    {
        return Matrix4x4.CreateFromQuaternion(NormalizeSafe(rotation)) * Matrix4x4.CreateTranslation(location);
    }

    private sealed record ChunkHeader(string Id, int TypeFlag, int DataSize, int DataCount);
    private sealed record PskContents(
        Vector3[] Points,
        BlenderCompatMeshWedge[] Wedges,
        BlenderCompatMeshFace[] Faces,
        BlenderCompatSkeletonBone[] Bones,
        BlenderCompatMeshWeight[] Weights);
    private sealed record FrameSample(
        BlenderCompatAnimationSequence Sequence,
        int ClampedFrame,
        Vector3[] RawLocalPositions,
        Quaternion[] RawLocalRotations,
        Vector3[] ChannelLocations,
        Quaternion[] ChannelRotations);
}

public sealed record BlenderCompatModel(string Name, BlenderCompatSkeleton Skeleton, BlenderCompatMesh Mesh);
public sealed record BlenderCompatSkeleton(string Name, BlenderCompatSkeletonBone[] Bones);
public sealed record BlenderCompatMesh(
    Vector3[] Points,
    BlenderCompatMeshWedge[] Wedges,
    BlenderCompatMeshFace[] Faces,
    BlenderCompatMeshWeight[] Weights);
public sealed record BlenderCompatMeshWedge(int PointIndex, Vector2 UV, byte MaterialIndex);
public sealed record BlenderCompatMeshFace(int WedgeIndex0, int WedgeIndex1, int WedgeIndex2, byte MaterialIndex);
public sealed record BlenderCompatMeshWeight(float Weight, int PointIndex, int BoneIndex);

public sealed record BlenderCompatSkeletonBone(
    string Name,
    int ParentIndex,
    Vector3 RawBindPosition,
    Quaternion RawBindRotation,
    Vector3 StoredOrigLocation,
    Quaternion StoredOrigQuaternion,
    Quaternion PostQuaternion,
    bool IsRoot,
    bool DontInvertRoot);

public sealed record BlenderCompatAnimationSet(
    string Name,
    BlenderCompatAnimationBone[] Bones,
    BlenderCompatAnimationSequence[] Sequences,
    BlenderCompatAnimationKey[] Keys);

public sealed record BlenderCompatAnimationBone(string Name, int ParentIndex);

public sealed record BlenderCompatAnimationSequence(
    string Name,
    int TotalBones,
    float TrackTime,
    float AnimRate,
    int FirstRawFrame,
    int NumRawFrames);

public sealed record BlenderCompatAnimationKey(Vector3 Position, Quaternion Orientation, float Time);

public sealed record BlenderCompatBonePose(
    string Name,
    string? ParentName,
    Vector3 RawLocalPosition,
    Quaternion RawLocalRotation,
    Vector3 ChannelLocation,
    Quaternion ChannelRotation,
    Vector3 WorldPosition,
    Quaternion WorldRotation);

public sealed record BlenderCompatPoseSample(
    string AnimationName,
    int FrameIndex,
    int NumFrames,
    float TrackTime,
    float AnimRate,
    BlenderCompatBonePose[] Bones);
