using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.UkxFile;

internal static class UkxSkeletalMeshObjectReader
{
    internal static UkxSkeletalMeshObject Read(
        PackageData package,
        ExportEntry exportEntry,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, exportEntry);
        UkxExportReaderHelpers.SkipObjectStackIfPresent(reader, exportEntry);
        var unknownProperties = UkxPropertyReader.ReadUnknownProperties(package, reader, className, exportIndex, objectName);

        var primitiveBoundingBox = ReadBoundingBox(reader, package);
        var primitiveBoundingSphere = ReadBoundingSphere(reader, package);
        var version = reader.ReadInt32();
        var vertexCount = reader.ReadInt32();
        var packedVertices = ReadPackedVertices(reader);
        var legacyTriangles2 = Array.Empty<UkxLegacyMeshTri2>();
        if (version <= 1)
        {
            legacyTriangles2 = ReadLegacyMeshTri2Array(reader);
        }

        var textureReferences = ReadObjectRefArray(reader, package);
        var meshScale = ReadVector3(reader);
        var meshOrigin = ReadVector3(reader);
        var meshRotation = ReadRotator(reader);
        var legacyFaceLevels = Array.Empty<ushort>();
        if (version <= 1)
        {
            legacyFaceLevels = ReadUInt16Array(reader);
        }

        var faceLevels = ReadUInt16Array(reader);
        var legacyFaces = ReadArray(reader, ReadMeshFace);
        var legacyCollapseWedges = ReadUInt16Array(reader);
        var legacyWedges = ReadArray(reader, ReadMeshWedge);
        var materials = ReadArray(reader, ReadMeshMaterial);
        var meshScaleMax = reader.ReadSingle();
        var lodHysteresis = reader.ReadSingle();
        var lodStrength = reader.ReadSingle();
        var lodMinVerts = reader.ReadInt32();
        var lodMorph = reader.ReadSingle();
        var lodZDisplace = reader.ReadSingle();

        int? hasImpostor = null;
        UkxObjectReference? spriteMaterialReference = null;
        Vector3? spriteScale = null;
        UkxRotator? spriteRotation = null;
        Vector3? spriteOffset = null;
        UkxColor? spriteColor = null;
        int? spriteValue0 = null;
        int? spriteValue1 = null;
        int? spriteValue2 = null;
        if (version >= 3)
        {
            hasImpostor = reader.ReadInt32();
            spriteMaterialReference = UkxObjectReferenceDecoder.ReadObjectReference(reader, package);
            spriteScale = ReadVector3(reader);
            spriteRotation = ReadRotator(reader);
            spriteOffset = ReadVector3(reader);
            spriteColor = ReadColor(reader);
            spriteValue0 = reader.ReadInt32();
            spriteValue1 = reader.ReadInt32();
            spriteValue2 = reader.ReadInt32();
        }

        float? version4Value = null;
        if (version >= 4)
        {
            version4Value = reader.ReadSingle();
        }

        int? legacyVersion5Value = null;
        if (package.Header.Version < 224 && version >= 5)
        {
            legacyVersion5Value = reader.ReadInt32();
        }

        var points2 = ReadArray(reader, ReadVector3);
        var refSkeleton = ReadArray(reader, x => ReadMeshBone(x, package));
        var animationReference = UkxObjectReferenceDecoder.ReadObjectReference(reader, package);
        var skeletalDepth = reader.ReadInt32();
        var weightIndices = ReadArray(reader, ReadWeightIndex);
        var boneInfluences = ReadArray(reader, ReadBoneInfluence);
        var attachAliases = ReadArray(reader, x => ReadName(x, package.Names));
        var attachBoneNames = ReadArray(reader, x => ReadName(x, package.Names));
        var attachCoords = ReadArray(reader, ReadCoords);

        var lodModels = Array.Empty<UkxSkeletalLodModel>();
        UkxObjectReference? baseMeshReference = null;
        var baseLodPoints = Array.Empty<Vector3>();
        var baseLodWedges = Array.Empty<UkxMeshWedge>();
        var baseLodTriangles = Array.Empty<UkxTriangle>();
        var baseLodVertInfluences = Array.Empty<UkxVertInfluence>();
        var collapseWedges = Array.Empty<ushort>();
        var trianglePointIndices = Array.Empty<ushort>();
        int? lineageValue118_3 = null;
        var lineageValues123_18 = Array.Empty<float>();
        int? lineageValue120 = null;
        int? lineageValueLicensee23 = null;
        var legacySoftLodSections = Array.Empty<UkxLegacyLodSection>();
        var legacyRigidLodSections = Array.Empty<UkxLegacyLodSection>();
        var legacyLodIndices = Array.Empty<ushort>();

        if (version <= 1)
        {
            legacySoftLodSections = ReadLegacyLodSectionArray(reader);
            legacyRigidLodSections = ReadLegacyLodSectionArray(reader);
            legacyLodIndices = ReadUInt16Array(reader);
        }
        else
        {
            lodModels = ReadArray(reader, x => ReadSkeletalLodModel(x, package));
            baseMeshReference = UkxObjectReferenceDecoder.ReadObjectReference(reader, package);
            baseLodPoints = ReadLazyVector3Array(reader, package);
            baseLodWedges = ReadLazyArray(reader, package, ReadMeshWedge);
            baseLodTriangles = ReadLazyArray(reader, package, ReadTriangle);
            baseLodVertInfluences = ReadLazyArray(reader, package, ReadVertInfluence);
            collapseWedges = ReadLazyArray(reader, package, x => x.ReadUInt16());
            trianglePointIndices = ReadLazyArray(reader, package, x => x.ReadUInt16());

            if (package.Header.Version >= 118 && package.Header.LicenseeVersion >= 3)
            {
                lineageValue118_3 = reader.ReadInt32();
            }

            if (package.Header.Version >= 123 && package.Header.LicenseeVersion >= 0x12)
            {
                lineageValues123_18 = ReadArray(reader, x => x.ReadSingle());
            }

            if (package.Header.Version >= 120)
            {
                lineageValue120 = reader.ReadInt32();
            }

            if (package.Header.LicenseeVersion >= 0x23)
            {
                lineageValueLicensee23 = reader.ReadInt32();
            }
        }

        var nativePayload = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        return new UkxSkeletalMeshObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            UnknownProperties = unknownProperties,
            PrimitiveBoundingBox = primitiveBoundingBox,
            PrimitiveBoundingSphere = primitiveBoundingSphere,
            Version = version,
            VertexCount = vertexCount,
            PackedVertices = packedVertices,
            LegacyTriangles2 = legacyTriangles2,
            TextureReferences = textureReferences,
            MeshScale = meshScale,
            MeshOrigin = meshOrigin,
            MeshRotation = meshRotation,
            LegacyFaceLevels = legacyFaceLevels,
            FaceLevels = faceLevels,
            LegacyFaces = legacyFaces,
            LegacyCollapseWedges = legacyCollapseWedges,
            LegacyWedges = legacyWedges,
            Materials = materials,
            MeshScaleMax = meshScaleMax,
            LODHysteresis = lodHysteresis,
            LODStrength = lodStrength,
            LODMinVerts = lodMinVerts,
            LODMorph = lodMorph,
            LODZDisplace = lodZDisplace,
            HasImpostor = hasImpostor,
            SpriteMaterialReference = spriteMaterialReference,
            SpriteScale = spriteScale,
            SpriteRotation = spriteRotation,
            SpriteOffset = spriteOffset,
            SpriteColor = spriteColor,
            SpriteValue0 = spriteValue0,
            SpriteValue1 = spriteValue1,
            SpriteValue2 = spriteValue2,
            Version4Value = version4Value,
            LegacyVersion5Value = legacyVersion5Value,
            BasePoints = points2,
            RefSkeleton = refSkeleton,
            AnimationReference = animationReference,
            SkeletalDepth = skeletalDepth,
            WeightIndices = weightIndices,
            BoneInfluences = boneInfluences,
            AttachAliases = attachAliases,
            AttachBoneNames = attachBoneNames,
            AttachCoords = attachCoords,
            LodModels = lodModels,
            BaseMeshReference = baseMeshReference,
            BaseLodPoints = baseLodPoints,
            BaseLodWedges = baseLodWedges,
            BaseLodTriangles = baseLodTriangles,
            BaseLodVertInfluences = baseLodVertInfluences,
            CollapseWedges = collapseWedges,
            TrianglePointIndices = trianglePointIndices,
            LineageValue118_3 = lineageValue118_3,
            LineageValues123_18 = lineageValues123_18,
            LineageValue120 = lineageValue120,
            LineageValueLicensee23 = lineageValueLicensee23,
            LegacySoftLodSections = legacySoftLodSections,
            LegacyRigidLodSections = legacyRigidLodSections,
            LegacyLodIndices = legacyLodIndices,
            UnknownNativePayloadBytes = nativePayload
        };
    }

    private static UkxSkeletalLodModel ReadSkeletalLodModel(BinaryReader reader, PackageData package)
    {
        var skinningData = ReadArray(reader, x => x.ReadUInt32());
        var skinPoints = ReadArray(reader, ReadSkinPoint);
        var numSoftWedges = reader.ReadInt32();
        var softSections = ReadArray(reader, x => ReadSkelMeshSection(x, package));
        var rigidSections = ReadArray(reader, x => ReadSkelMeshSection(x, package));
        var softIndices = ReadRawIndexBuffer(reader);
        var rigidIndices = ReadRawIndexBuffer(reader);
        var vertexStream = ReadSkinVertexStream(reader);
        var vertInfluences = ReadLazyArray(reader, package, ReadVertInfluence);
        var wedges = ReadLazyArray(reader, package, ReadMeshWedge);
        var faces = ReadLazyArray(reader, package, ReadMeshFace);
        var points = ReadLazyVector3Array(reader, package);
        var lodDistanceFactor = reader.ReadSingle();
        var lodHysteresis = reader.ReadSingle();
        var numSharedVerts = reader.ReadInt32();
        var lodMaxInfluences = reader.ReadInt32();
        var value114 = reader.ReadInt32();
        var value118 = reader.ReadInt32();
        int? useNewWedges = null;
        var lineageWedges = Array.Empty<UkxLineageWedge>();
        if (package.Header.LicenseeVersion >= 0x1C)
        {
            useNewWedges = reader.ReadInt32();
            lineageWedges = ReadArray(reader, ReadLineageWedge);
        }

        return new UkxSkeletalLodModel(
            skinningData,
            skinPoints,
            numSoftWedges,
            softSections,
            rigidSections,
            softIndices,
            rigidIndices,
            vertexStream,
            vertInfluences,
            wedges,
            faces,
            points,
            lodDistanceFactor,
            lodHysteresis,
            numSharedVerts,
            lodMaxInfluences,
            value114,
            value118,
            useNewWedges,
            lineageWedges);
    }

    private static UkxMeshBone ReadMeshBone(BinaryReader reader, PackageData package)
    {
        var name = ReadName(reader, package.Names);
        var flags = reader.ReadUInt32();
        var jointPosition = ReadJointPosition(reader, package);
        var numChildren = reader.ReadInt32();
        var parentIndex = reader.ReadInt32();
        return new UkxMeshBone(
            name,
            flags,
            jointPosition,
            parentIndex,
            numChildren);
    }

    private static UkxJointPosition ReadJointPosition(BinaryReader reader, PackageData package)
    {
        var orientation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        var position = ReadVector3(reader);
        if (package.Header.Version < 224)
        {
            return new UkxJointPosition(
                orientation,
                position,
                reader.ReadSingle(),
                ReadVector3(reader));
        }

        return new UkxJointPosition(orientation, position, null, null);
    }

    private static UkxWeightIndex ReadWeightIndex(BinaryReader reader)
    {
        return new UkxWeightIndex(ReadArray(reader, x => x.ReadUInt16()), reader.ReadInt32());
    }

    private static UkxBoneInfluence ReadBoneInfluence(BinaryReader reader)
    {
        return new UkxBoneInfluence(reader.ReadUInt16(), reader.ReadUInt16());
    }

    private static UkxSkinPoint ReadSkinPoint(BinaryReader reader)
    {
        return new UkxSkinPoint(ReadVector3(reader), reader.ReadUInt32());
    }

    private static UkxSkelMeshSection ReadSkelMeshSection(BinaryReader reader, PackageData package)
    {
        var materialIndex = reader.ReadUInt16();
        var minStreamIndex = reader.ReadUInt16();
        var minWedgeIndex = reader.ReadUInt16();
        var maxWedgeIndex = reader.ReadUInt16();
        var numStreamIndices = reader.ReadUInt16();
        var boneIndex = reader.ReadUInt16();
        var value0E = reader.ReadUInt16();
        var firstFace = reader.ReadUInt16();
        var numFaces = reader.ReadUInt16();
        var lineageBoneMap = package.Header.LicenseeVersion >= 0x1C
            ? ReadArray(reader, x => x.ReadInt32())
            : [];
        return new UkxSkelMeshSection(
            materialIndex,
            minStreamIndex,
            minWedgeIndex,
            maxWedgeIndex,
            numStreamIndices,
            boneIndex,
            value0E,
            firstFace,
            numFaces,
            lineageBoneMap);
    }

    private static UkxRawIndexBuffer ReadRawIndexBuffer(BinaryReader reader)
    {
        return new UkxRawIndexBuffer(ReadArray(reader, x => x.ReadUInt16()), reader.ReadInt32());
    }

    private static UkxSkinVertexStream ReadSkinVertexStream(BinaryReader reader)
    {
        var revision = reader.ReadInt32();
        var value18 = reader.ReadInt32();
        var value1C = reader.ReadInt32();
        var vertices = ReadArray(reader, ReadAnimMeshVertex);
        return new UkxSkinVertexStream(revision, value18, value1C, vertices);
    }

    private static UkxAnimMeshVertex ReadAnimMeshVertex(BinaryReader reader)
    {
        return new UkxAnimMeshVertex(
            ReadVector3(reader),
            ReadVector3(reader),
            new Vector2(reader.ReadSingle(), reader.ReadSingle()));
    }

    private static UkxVertInfluence ReadVertInfluence(BinaryReader reader)
    {
        return new UkxVertInfluence(reader.ReadSingle(), reader.ReadUInt16(), reader.ReadUInt16());
    }

    private static UkxMeshWedge ReadMeshWedge(BinaryReader reader)
    {
        return new UkxMeshWedge(reader.ReadUInt16(), new Vector2(reader.ReadSingle(), reader.ReadSingle()));
    }

    private static UkxMeshFace ReadMeshFace(BinaryReader reader)
    {
        return new UkxMeshFace(
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16());
    }

    private static UkxTriangle ReadTriangle(BinaryReader reader)
    {
        return new UkxTriangle(
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadUInt32());
    }

    private static UkxLineageWedge ReadLineageWedge(BinaryReader reader)
    {
        return new UkxLineageWedge(
            ReadVector3(reader),
            ReadVector3(reader),
            new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }

    private static UkxCoords ReadCoords(BinaryReader reader)
    {
        return new UkxCoords(
            ReadVector3(reader),
            ReadVector3(reader),
            ReadVector3(reader),
            ReadVector3(reader));
    }

    private static UkxObjectReference?[] ReadObjectRefArray(BinaryReader reader, PackageData package)
    {
        return ReadArray(reader, x => UkxObjectReferenceDecoder.ReadObjectReference(x, package));
    }

    private static BoundingBox ReadBoundingBox(BinaryReader reader, PackageData package)
    {
        var min = ReadVector3(reader);
        var max = ReadVector3(reader);
        byte? isValid = package.Header.Version < 146 ? reader.ReadByte() : null;
        return new BoundingBox(min, max, isValid);
    }

    private static BoundingSphere ReadBoundingSphere(BinaryReader reader, PackageData package)
    {
        var center = ReadVector3(reader);
        var radius = package.Header.Version >= 61 ? reader.ReadSingle() : (float?)null;
        return new BoundingSphere(center, radius);
    }

    private static int[] ReadPackedVertices(BinaryReader reader)
    {
        return ReadArray(reader, x => x.ReadInt32());
    }

    private static UkxLegacyMeshTri2[] ReadLegacyMeshTri2Array(BinaryReader reader)
    {
        var count = PackageReader.ReadCompactIndex(reader);
        EnsureReasonableCount(count, "FMeshTri2");
        var result = new UkxLegacyMeshTri2[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = new UkxLegacyMeshTri2(reader.ReadBytes(36));
        }

        return result;
    }

    private static UkxMeshMaterial ReadMeshMaterial(BinaryReader reader)
    {
        return new UkxMeshMaterial(reader.ReadUInt32(), reader.ReadInt32());
    }

    private static UkxLegacyLodSection[] ReadLegacyLodSectionArray(BinaryReader reader)
    {
        var count = PackageReader.ReadCompactIndex(reader);
        EnsureReasonableCount(count, "FLODMeshSection");
        var result = new UkxLegacyLodSection[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = new UkxLegacyLodSection(reader.ReadBytes(18));
        }

        return result;
    }

    private static ushort[] ReadUInt16Array(BinaryReader reader)
    {
        return ReadArray(reader, x => x.ReadUInt16());
    }

    private static UkxRotator ReadRotator(BinaryReader reader)
    {
        return new UkxRotator(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32());
    }

    private static UkxColor ReadColor(BinaryReader reader)
    {
        return new UkxColor(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte());
    }

    private static T[] ReadLazyArray<T>(BinaryReader reader, PackageData package, Func<BinaryReader, T> itemReader)
    {
        SkipLazyArrayHeader(reader, package);
        return ReadArray(reader, itemReader);
    }

    private static Vector3[] ReadLazyVector3Array(BinaryReader reader, PackageData package)
    {
        return ReadLazyArray(reader, package, ReadVector3);
    }

    private static void SkipLazyArrayHeader(BinaryReader reader, PackageData package)
    {
        if (package.Header.Version > 61)
        {
            _ = reader.ReadInt32();
        }
    }

    private static T[] ReadArray<T>(BinaryReader reader, Func<BinaryReader, T> itemReader)
    {
        var count = PackageReader.ReadCompactIndex(reader);
        EnsureReasonableCount(count, typeof(T).Name);
        var result = new T[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = itemReader(reader);
        }

        return result;
    }

    private static void EnsureReasonableCount(int count, string name)
    {
        if (count < 0 || count > 1_000_000)
        {
            throw new PackageReadException($"Invalid {name} count: {count}");
        }
    }

    private static string ReadName(BinaryReader reader, IReadOnlyList<string> names)
    {
        var index = PackageReader.ReadCompactIndex(reader);
        if (index < 0 || index >= names.Count)
        {
            throw new PackageReadException($"Invalid name index: {index}");
        }

        return names[index];
    }

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
