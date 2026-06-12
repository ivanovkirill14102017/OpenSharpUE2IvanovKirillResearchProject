using System.Numerics;

namespace L2Viewer.UkxFile;

public static class UkxActorXCompatBuilder
{
    public static (BlenderCompatModel Model, BlenderCompatAnimationSet AnimationSet) Build(
        UkxSkeletalMeshObject mesh,
        UkxMeshAnimationObject animation)
    {
        if (mesh is null || animation is null)
        {
            throw new ArgumentNullException(mesh is null ? nameof(mesh) : nameof(animation));
        }

        if (mesh.RefSkeleton.Length == 0)
        {
            throw new InvalidOperationException($"Skeletal mesh '{mesh.ObjectName}' does not contain a reference skeleton.");
        }

        var compatMesh = BuildMesh(mesh);
        if (compatMesh.Faces.Length == 0 || compatMesh.Wedges.Length == 0 || compatMesh.Points.Length == 0)
        {
            throw new InvalidOperationException($"Skeletal mesh '{mesh.ObjectName}' did not produce ActorX-compatible geometry.");
        }

        var compatSkeleton = BuildSkeleton(mesh);
        var compatAnimation = BuildAnimationSet(mesh, animation);
        return (new BlenderCompatModel(mesh.ObjectName, compatSkeleton, compatMesh), compatAnimation);
    }

    private sealed record RuntimeInfluence(int BoneIndex, float Weight);
    private sealed record RuntimeVertex(Vector3 Position, Vector3 Normal, Vector2 UV, int MaterialId, RuntimeInfluence[] Influences);

    private static BlenderCompatSkeleton BuildSkeleton(UkxSkeletalMeshObject mesh)
    {
        var bones = new BlenderCompatSkeletonBone[mesh.RefSkeleton.Length];
        for (var i = 0; i < mesh.RefSkeleton.Length; i++)
        {
            var source = mesh.RefSkeleton[i];
            var parentIndex = source.ParentIndex >= 0 && source.ParentIndex < mesh.RefSkeleton.Length && source.ParentIndex != i
                ? source.ParentIndex
                : -1;
            var isRoot = parentIndex < 0;
            var rawBindPosition = ToActorXBonePosition(source.JointPosition.Position);
            var rawBindRotation = ToActorXOrientation(source.JointPosition.Orientation);
            var storedOrigQuaternion = isRoot ? Quaternion.Conjugate(rawBindRotation) : rawBindRotation;
            var postQuaternion = Quaternion.Conjugate(storedOrigQuaternion);

            bones[i] = new BlenderCompatSkeletonBone(
                source.Name,
                parentIndex,
                rawBindPosition,
                rawBindRotation,
                rawBindPosition,
                storedOrigQuaternion,
                postQuaternion,
                isRoot,
                true);
        }

        return new BlenderCompatSkeleton(mesh.ObjectName, bones);
    }

    private static BlenderCompatMesh BuildMesh(UkxSkeletalMeshObject mesh)
    {
        var runtimeVertices = BuildVertices(mesh);
        var points = new Vector3[runtimeVertices.Length];
        var wedges = new BlenderCompatMeshWedge[runtimeVertices.Length];
        var faces = new BlenderCompatMeshFace[runtimeVertices.Length / 3];
        var weights = new List<BlenderCompatMeshWeight>(runtimeVertices.Sum(x => x.Influences.Length));

        for (var i = 0; i < runtimeVertices.Length; i++)
        {
            points[i] = ToActorXPosition(runtimeVertices[i].Position);
            wedges[i] = new BlenderCompatMeshWedge(i, runtimeVertices[i].UV, (byte)Math.Clamp(runtimeVertices[i].MaterialId, byte.MinValue, byte.MaxValue));
            foreach (var influence in runtimeVertices[i].Influences)
            {
                if (influence.Weight <= 0f || influence.BoneIndex < 0)
                {
                    continue;
                }

                weights.Add(new BlenderCompatMeshWeight(influence.Weight, i, influence.BoneIndex));
            }
        }

        for (var faceIndex = 0; faceIndex < faces.Length; faceIndex++)
        {
            var baseIndex = faceIndex * 3;
            faces[faceIndex] = new BlenderCompatMeshFace(baseIndex + 0, baseIndex + 1, baseIndex + 2, wedges[baseIndex].MaterialIndex);
        }

        return new BlenderCompatMesh(points, wedges, faces, weights.ToArray());
    }

    private static BlenderCompatAnimationSet BuildAnimationSet(UkxSkeletalMeshObject mesh, UkxMeshAnimationObject animation)
    {
        var compatBones = mesh.RefSkeleton
            .Select((x, i) =>
            {
                var parentIndex = x.ParentIndex >= 0 && x.ParentIndex < mesh.RefSkeleton.Length && x.ParentIndex != i
                    ? x.ParentIndex
                    : -1;
                return new BlenderCompatAnimationBone(x.Name, parentIndex);
            })
            .ToArray();

        var sequenceCount = Math.Min(animation.AnimSequences.Length, animation.Moves.Length);
        var sequences = new BlenderCompatAnimationSequence[sequenceCount];
        var keys = new List<BlenderCompatAnimationKey>();
        for (var sequenceIndex = 0; sequenceIndex < sequenceCount; sequenceIndex++)
        {
            var sequence = animation.AnimSequences[sequenceIndex];
            var move = animation.Moves[sequenceIndex];
            var totalBones = compatBones.Length;
            sequences[sequenceIndex] = new BlenderCompatAnimationSequence(
                sequence.Name,
                totalBones,
                move.TrackTime,
                sequence.Rate,
                keys.Count / Math.Max(1, totalBones),
                Math.Max(1, sequence.NumFrames));

            AppendSequenceKeys(mesh, animation, sequence, move, compatBones, keys);
        }

        return new BlenderCompatAnimationSet(animation.ObjectName, compatBones, sequences, keys.ToArray());
    }

    private static void AppendSequenceKeys(
        UkxSkeletalMeshObject mesh,
        UkxMeshAnimationObject animation,
        UkxMeshAnimSequence sequence,
        UkxMotionChunk move,
        BlenderCompatAnimationBone[] compatBones,
        List<BlenderCompatAnimationKey> keys)
    {
        var nameToSkeletonIndex = mesh.RefSkeleton
            .Select((bone, index) => (bone.Name, index))
            .ToDictionary(x => x.Name, x => x.index, StringComparer.OrdinalIgnoreCase);
        var animationBoneToSkeleton = new int[compatBones.Length];
        for (var i = 0; i < compatBones.Length; i++)
        {
            animationBoneToSkeleton[i] = nameToSkeletonIndex.TryGetValue(compatBones[i].Name, out var skeletonIndex) ? skeletonIndex : -1;
        }

        var frameCount = Math.Max(1, sequence.NumFrames);
        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            var localPositions = mesh.RefSkeleton.Select(x => x.JointPosition.Position).ToArray();
            var localRotations = mesh.RefSkeleton.Select(x => NormalizeSafe(x.JointPosition.Orientation)).ToArray();
            ApplyMotion(sequence, move, frameIndex, localRotations, localPositions);

            for (var animationBoneIndex = 0; animationBoneIndex < compatBones.Length; animationBoneIndex++)
            {
                var skeletonIndex = animationBoneToSkeleton[animationBoneIndex];
                if (skeletonIndex < 0 || skeletonIndex >= localRotations.Length)
                {
                    keys.Add(new BlenderCompatAnimationKey(Vector3.Zero, Quaternion.Identity, 0f));
                    continue;
                }

                keys.Add(new BlenderCompatAnimationKey(
                    ToActorXBonePosition(localPositions[skeletonIndex]),
                    ToActorXOrientation(localRotations[skeletonIndex]),
                    0f));
            }
        }
    }

    private static RuntimeVertex[] BuildVertices(UkxSkeletalMeshObject mesh)
    {
        var lod = mesh.LodModels[0];

        if (HasModernSectionVertices(lod))
        {
            var modernVertices = new List<RuntimeVertex>();
            BuildLineageSoftVertices(lod, modernVertices);
            BuildRigidVertices(lod, modernVertices);
            return modernVertices.ToArray();
        }

        if (HasStandardLodVertices(lod))
        {
            var standardVertices = new List<RuntimeVertex>();
            BuildStandardVertices(lod.Points, lod.Wedges, lod.Faces, lod.VertInfluences, standardVertices);
            return standardVertices.ToArray();
        }

        if (HasBaseLodVertices(mesh))
        {
            var baseVertices = new List<RuntimeVertex>();
            BuildStandardBaseVertices(mesh, baseVertices);
            return baseVertices.ToArray();
        }

        return [];
    }

    private static void BuildLineageSoftVertices(UkxSkeletalLodModel lod, List<RuntimeVertex> vertices)
    {
        if (lod.LineageWedges.Length == 0 || lod.SoftIndices.Indices.Length == 0)
        {
            return;
        }

        foreach (var section in lod.SoftSections)
        {
            for (var faceIndex = 0; faceIndex < section.NumFaces; faceIndex++)
            {
                var baseIndex = (section.FirstFace + faceIndex) * 3;
                if (baseIndex + 2 >= lod.SoftIndices.Indices.Length)
                {
                    continue;
                }

                var wedge0 = lod.SoftIndices.Indices[baseIndex];
                var wedge1 = lod.SoftIndices.Indices[baseIndex + 1];
                var wedge2 = lod.SoftIndices.Indices[baseIndex + 2];
                if ((uint)wedge0 >= lod.LineageWedges.Length || (uint)wedge1 >= lod.LineageWedges.Length || (uint)wedge2 >= lod.LineageWedges.Length)
                {
                    continue;
                }

                vertices.Add(CreateLineageVertex(lod.LineageWedges[wedge0], section));
                vertices.Add(CreateLineageVertex(lod.LineageWedges[wedge1], section));
                vertices.Add(CreateLineageVertex(lod.LineageWedges[wedge2], section));
            }
        }
    }

    private static void BuildRigidVertices(UkxSkeletalLodModel lod, List<RuntimeVertex> vertices)
    {
        if (lod.VertexStream.Vertices.Length == 0 || lod.RigidIndices.Indices.Length == 0)
        {
            return;
        }

        foreach (var section in lod.RigidSections)
        {
            for (var faceIndex = 0; faceIndex < section.NumFaces; faceIndex++)
            {
                var baseIndex = (section.FirstFace + faceIndex) * 3;
                if (baseIndex + 2 >= lod.RigidIndices.Indices.Length)
                {
                    continue;
                }

                var wedge0 = lod.RigidIndices.Indices[baseIndex];
                var wedge1 = lod.RigidIndices.Indices[baseIndex + 1];
                var wedge2 = lod.RigidIndices.Indices[baseIndex + 2];
                if ((uint)wedge0 >= lod.VertexStream.Vertices.Length || (uint)wedge1 >= lod.VertexStream.Vertices.Length || (uint)wedge2 >= lod.VertexStream.Vertices.Length)
                {
                    continue;
                }

                vertices.Add(CreateRigidVertex(lod.VertexStream.Vertices[wedge0], section.MaterialIndex, section.BoneIndex));
                vertices.Add(CreateRigidVertex(lod.VertexStream.Vertices[wedge1], section.MaterialIndex, section.BoneIndex));
                vertices.Add(CreateRigidVertex(lod.VertexStream.Vertices[wedge2], section.MaterialIndex, section.BoneIndex));
            }
        }
    }

    private static void BuildStandardVertices(Vector3[] points, UkxMeshWedge[] wedges, UkxMeshFace[] faces, UkxVertInfluence[] influences, List<RuntimeVertex> vertices)
    {
        if (points.Length == 0 || wedges.Length == 0 || faces.Length == 0)
        {
            return;
        }

        var influencesByPoint = BuildInfluenceMap(points.Length, influences);
        foreach (var face in faces)
        {
            if ((uint)face.WedgeIndex0 >= wedges.Length || (uint)face.WedgeIndex1 >= wedges.Length || (uint)face.WedgeIndex2 >= wedges.Length)
            {
                continue;
            }

            var wedge0 = wedges[face.WedgeIndex0];
            var wedge1 = wedges[face.WedgeIndex1];
            var wedge2 = wedges[face.WedgeIndex2];
            if ((uint)wedge0.VertexIndex >= points.Length || (uint)wedge1.VertexIndex >= points.Length || (uint)wedge2.VertexIndex >= points.Length)
            {
                continue;
            }

            var normal = ComputeFaceNormal(points[wedge0.VertexIndex], points[wedge1.VertexIndex], points[wedge2.VertexIndex]);
            vertices.Add(CreateStandardVertex(points[wedge0.VertexIndex], wedge0.UV, normal, face.MaterialIndex, influencesByPoint, wedge0.VertexIndex));
            vertices.Add(CreateStandardVertex(points[wedge1.VertexIndex], wedge1.UV, normal, face.MaterialIndex, influencesByPoint, wedge1.VertexIndex));
            vertices.Add(CreateStandardVertex(points[wedge2.VertexIndex], wedge2.UV, normal, face.MaterialIndex, influencesByPoint, wedge2.VertexIndex));
        }
    }

    private static void BuildStandardBaseVertices(UkxSkeletalMeshObject mesh, List<RuntimeVertex> vertices)
    {
        if (mesh.BaseLodPoints.Length > 0 &&
            mesh.BaseLodWedges.Length > 0 &&
            mesh.BaseLodTriangles.Length > 0)
        {
            BuildStandardTriangles(mesh.BaseLodPoints, mesh.BaseLodWedges, mesh.BaseLodTriangles, mesh.BaseLodVertInfluences, vertices);
            return;
        }

        if (mesh.BasePoints.Length == 0 ||
            mesh.BaseLodWedges.Length == 0 ||
            mesh.BaseLodTriangles.Length == 0 ||
            mesh.WeightIndices.Length == 0 ||
            mesh.BoneInfluences.Length == 0)
        {
            return;
        }

        BuildStandardTriangles(mesh.BasePoints, mesh.BaseLodWedges, mesh.BaseLodTriangles, ConvertLegacyInfluences(mesh.WeightIndices, mesh.BoneInfluences), vertices);
    }

    private static bool HasModernSectionVertices(UkxSkeletalLodModel lod)
    {
        return (lod.SoftSections.Length > 0 && lod.LineageWedges.Length > 0 && lod.SoftIndices.Indices.Length > 0) ||
               (lod.RigidSections.Length > 0 && lod.VertexStream.Vertices.Length > 0 && lod.RigidIndices.Indices.Length > 0);
    }

    private static bool HasStandardLodVertices(UkxSkeletalLodModel lod)
    {
        return lod.Points.Length > 0 &&
               lod.Wedges.Length > 0 &&
               lod.Faces.Length > 0;
    }

    private static bool HasBaseLodVertices(UkxSkeletalMeshObject mesh)
    {
        return (mesh.BaseLodPoints.Length > 0 && mesh.BaseLodWedges.Length > 0 && mesh.BaseLodTriangles.Length > 0) ||
               (mesh.BasePoints.Length > 0 &&
                mesh.BaseLodWedges.Length > 0 &&
                mesh.BaseLodTriangles.Length > 0 &&
                mesh.WeightIndices.Length > 0 &&
                mesh.BoneInfluences.Length > 0);
    }

    private static void BuildStandardTriangles(Vector3[] points, UkxMeshWedge[] wedges, UkxTriangle[] triangles, UkxVertInfluence[] influences, List<RuntimeVertex> vertices)
    {
        if (points.Length == 0 || wedges.Length == 0 || triangles.Length == 0)
        {
            return;
        }

        var influencesByPoint = BuildInfluenceMap(points.Length, influences);
        foreach (var triangle in triangles)
        {
            if ((uint)triangle.WedgeIndex0 >= wedges.Length || (uint)triangle.WedgeIndex1 >= wedges.Length || (uint)triangle.WedgeIndex2 >= wedges.Length)
            {
                continue;
            }

            var wedge0 = wedges[triangle.WedgeIndex0];
            var wedge1 = wedges[triangle.WedgeIndex1];
            var wedge2 = wedges[triangle.WedgeIndex2];
            if ((uint)wedge0.VertexIndex >= points.Length || (uint)wedge1.VertexIndex >= points.Length || (uint)wedge2.VertexIndex >= points.Length)
            {
                continue;
            }

            var normal = ComputeFaceNormal(points[wedge0.VertexIndex], points[wedge1.VertexIndex], points[wedge2.VertexIndex]);
            vertices.Add(CreateStandardVertex(points[wedge0.VertexIndex], wedge0.UV, normal, triangle.MaterialIndex, influencesByPoint, wedge0.VertexIndex));
            vertices.Add(CreateStandardVertex(points[wedge1.VertexIndex], wedge1.UV, normal, triangle.MaterialIndex, influencesByPoint, wedge1.VertexIndex));
            vertices.Add(CreateStandardVertex(points[wedge2.VertexIndex], wedge2.UV, normal, triangle.MaterialIndex, influencesByPoint, wedge2.VertexIndex));
        }
    }

    private static RuntimeVertex CreateLineageVertex(UkxLineageWedge wedge, UkxSkelMeshSection section)
    {
        var influences = new List<RuntimeInfluence>(4);
        AppendLineageInfluence(influences, section.LineageBoneMap, wedge.Bone0, wedge.Weight0);
        AppendLineageInfluence(influences, section.LineageBoneMap, wedge.Bone1, wedge.Weight1);
        AppendLineageInfluence(influences, section.LineageBoneMap, wedge.Bone2, wedge.Weight2);
        AppendLineageInfluence(influences, section.LineageBoneMap, wedge.Bone3, wedge.Weight3);
        return new RuntimeVertex(wedge.Position, NormalizeSafe(wedge.Normal), wedge.UV, section.MaterialIndex, NormalizeWeights(influences));
    }

    private static RuntimeVertex CreateRigidVertex(UkxAnimMeshVertex vertex, int materialIndex, int boneIndex)
    {
        return new RuntimeVertex(vertex.Position, NormalizeSafe(vertex.Normal), vertex.UV, materialIndex, [new RuntimeInfluence(boneIndex, 1f)]);
    }

    private static RuntimeVertex CreateStandardVertex(Vector3 position, Vector2 uv, Vector3 normal, int materialIndex, RuntimeInfluence[][] influencesByPoint, int pointIndex)
    {
        var influences = (uint)pointIndex < influencesByPoint.Length ? influencesByPoint[pointIndex] : [];
        return new RuntimeVertex(position, NormalizeSafe(normal), uv, materialIndex, influences);
    }

    private static RuntimeInfluence[][] BuildInfluenceMap(int pointCount, UkxVertInfluence[] influences)
    {
        var lists = new List<RuntimeInfluence>[pointCount];
        foreach (var influence in influences)
        {
            if (influence.Weight <= 0f || influence.PointIndex >= pointCount)
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

    private static UkxVertInfluence[] ConvertLegacyInfluences(UkxWeightIndex[] weightIndices, UkxBoneInfluence[] boneInfluences)
    {
        var result = new List<UkxVertInfluence>();
        for (var influenceCountMinusOne = 0; influenceCountMinusOne < weightIndices.Length; influenceCountMinusOne++)
        {
            var weightIndex = weightIndices[influenceCountMinusOne];
            var sourceIndex = weightIndex.StartBoneInfluence;
            foreach (var pointIndex in weightIndex.BoneInfluenceIndices)
            {
                for (var influenceOffset = 0; influenceOffset <= influenceCountMinusOne; influenceOffset++)
                {
                    if ((uint)sourceIndex >= boneInfluences.Length)
                    {
                        break;
                    }

                    var boneInfluence = boneInfluences[sourceIndex++];
                    result.Add(new UkxVertInfluence(boneInfluence.BoneWeight / 65535.0f, pointIndex, boneInfluence.BoneIndex));
                }
            }
        }

        return result.ToArray();
    }

    private static void ApplyMotion(UkxMeshAnimSequence sequence, UkxMotionChunk move, int frameIndex, Quaternion[] rotations, Vector3[] positions)
    {
        var trackCount = Math.Min(move.BoneIndices.Length, move.AnimTracks.Length);
        for (var i = 0; i < trackCount; i++)
        {
            var boneIndex = move.BoneIndices[i];
            if ((uint)boneIndex >= rotations.Length)
            {
                continue;
            }

            var track = move.AnimTracks[i];
            if (track.KeyQuat.Length > 0)
            {
                rotations[boneIndex] = SampleQuaternion(track, frameIndex, sequence.NumFrames, move.TrackTime);
            }

            if (track.KeyPos.Length > 0)
            {
                positions[boneIndex] = SampleVector(track, frameIndex, sequence.NumFrames, move.TrackTime);
            }
        }
    }

    private static Quaternion SampleQuaternion(UkxAnalogTrack track, float frameTime, int numFrames, float trackTime)
    {
        if (track.KeyQuat.Length == 1 || numFrames <= 1 || frameTime <= 0f)
        {
            return NormalizeSafe(track.KeyQuat[0]);
        }

        var (index0, index1, alpha) = SampleIndices(track.KeyQuat.Length, track.KeyTime, frameTime, numFrames, trackTime);
        return NormalizeSafe(Quaternion.Slerp(track.KeyQuat[index0], track.KeyQuat[index1], alpha));
    }

    private static Vector3 SampleVector(UkxAnalogTrack track, float frameTime, int numFrames, float trackTime)
    {
        if (track.KeyPos.Length == 1 || numFrames <= 1 || frameTime <= 0f)
        {
            return track.KeyPos[0];
        }

        var (index0, index1, alpha) = SampleIndices(track.KeyPos.Length, track.KeyTime, frameTime, numFrames, trackTime);
        return Vector3.Lerp(track.KeyPos[index0], track.KeyPos[index1], alpha);
    }

    private static (int Index0, int Index1, float Alpha) SampleIndices(int keyCount, float[] rawTimes, float frameTime, int numFrames, float trackTime)
    {
        if (keyCount <= 1)
        {
            return (0, 0, 0f);
        }

        var times = BuildSampleTimes(keyCount, rawTimes, numFrames, trackTime);
        if (times.Length == 0 || frameTime <= times[0])
        {
            return (0, 0, 0f);
        }

        for (var i = 1; i < times.Length; i++)
        {
            if (frameTime > times[i])
            {
                continue;
            }

            var span = MathF.Max(0.0001f, times[i] - times[i - 1]);
            var alpha = Math.Clamp((frameTime - times[i - 1]) / span, 0f, 1f);
            return (i - 1, i, alpha);
        }

        var last = times.Length - 1;
        return (last, last, 0f);
    }

    private static float[] BuildSampleTimes(int keyCount, float[] rawTimes, int numFrames, float trackTime)
    {
        if (rawTimes.Length == keyCount && rawTimes.Length > 0)
        {
            var isMonotonic = true;
            for (var i = 1; i < rawTimes.Length; i++)
            {
                if (rawTimes[i] < rawTimes[i - 1])
                {
                    isMonotonic = false;
                    break;
                }
            }

            if (isMonotonic)
            {
                var scale = trackTime > 0.0001f ? numFrames / trackTime : 1f;
                return rawTimes.Select(x => x * scale).ToArray();
            }
        }

        var times = new float[keyCount];
        for (var i = 0; i < keyCount; i++)
        {
            times[i] = keyCount > 0 ? (numFrames * i) / (float)keyCount : 0f;
        }

        return times;
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

    private static void AppendLineageInfluence(List<RuntimeInfluence> influences, int[] lineageBoneMap, byte localBoneIndex, float weight)
    {
        if (localBoneIndex == byte.MaxValue || weight <= 0f || localBoneIndex >= lineageBoneMap.Length)
        {
            return;
        }

        influences.Add(new RuntimeInfluence(lineageBoneMap[localBoneIndex], weight));
    }

    private static Vector3 ToActorXPosition(Vector3 value)
    {
        return new Vector3(value.X, -value.Y, value.Z) * 0.01f;
    }

    private static Vector3 ToActorXBonePosition(Vector3 value)
    {
        return new Vector3(value.X, -value.Y, value.Z) * 0.01f;
    }

    private static Quaternion ToActorXOrientation(Quaternion value)
    {
        return NormalizeSafe(new Quaternion(value.X, -value.Y, value.Z, -value.W));
    }

    private static Quaternion NormalizeSafe(Quaternion value) => value.LengthSquared() > 0.000001f ? Quaternion.Normalize(value) : Quaternion.Identity;
    private static Vector3 NormalizeSafe(Vector3 value) => value.LengthSquared() > 0.000001f ? Vector3.Normalize(value) : Vector3.UnitZ;

    private static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Cross(b - a, c - a);
        return normal.LengthSquared() > 0.000001f ? Vector3.Normalize(normal) : Vector3.UnitZ;
    }
}
