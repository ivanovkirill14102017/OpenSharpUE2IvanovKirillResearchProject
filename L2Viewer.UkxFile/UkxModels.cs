using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.UkxFile;

public sealed record UkxFileHeader(
    ushort Version,
    ushort LicenseeVersion,
    uint Flags,
    uint NameCount,
    uint NameOffset,
    uint ExportCount,
    uint ExportOffset,
    uint ImportCount,
    uint ImportOffset);

public sealed record UkxFileNameEntry(int Index, string Name);

public sealed record UkxFileImportEntry(
    int Index,
    int ClassPackage,
    int ClassName,
    uint PackageIndex,
    int ObjectName);

public sealed record UkxFileExportEntry(
    int Index,
    int ClassIndex,
    int SuperIndex,
    uint PackageIndex,
    int ObjectName,
    uint ObjectFlags,
    int SerialSize,
    int? SerialOffset);

public abstract class UkxFileObject
{
    public required int ExportIndex { get; init; }
    public required string ClassName { get; init; }
    public required string ObjectName { get; init; }
    public UkxUnknownProperty[] UnknownProperties { get; init; } = [];
    public byte[] UnknownNativePayloadBytes { get; init; } = [];
}

public sealed class UkxGenericObject : UkxFileObject
{
}

public sealed class UkxSkeletalMeshObject : UkxFileObject
{
    public BoundingBox PrimitiveBoundingBox { get; init; } = new(Vector3.Zero, Vector3.Zero, null);
    public BoundingSphere PrimitiveBoundingSphere { get; init; } = new(Vector3.Zero, null);
    public int Version { get; init; }
    public int VertexCount { get; init; }
    public int[] PackedVertices { get; init; } = [];
    public UkxLegacyMeshTri2[] LegacyTriangles2 { get; init; } = [];
    public UkxObjectReference?[] TextureReferences { get; init; } = [];
    public Vector3 MeshScale { get; init; }
    public Vector3 MeshOrigin { get; init; }
    public UkxRotator MeshRotation { get; init; } = new(0, 0, 0);
    public ushort[] LegacyFaceLevels { get; init; } = [];
    public ushort[] FaceLevels { get; init; } = [];
    public UkxMeshFace[] LegacyFaces { get; init; } = [];
    public ushort[] LegacyCollapseWedges { get; init; } = [];
    public UkxMeshWedge[] LegacyWedges { get; init; } = [];
    public UkxMeshMaterial[] Materials { get; init; } = [];
    public float MeshScaleMax { get; init; }
    public float LODHysteresis { get; init; }
    public float LODStrength { get; init; }
    public int LODMinVerts { get; init; }
    public float LODMorph { get; init; }
    public float LODZDisplace { get; init; }
    public int? HasImpostor { get; init; }
    public UkxObjectReference? SpriteMaterialReference { get; init; }
    public Vector3? SpriteScale { get; init; }
    public UkxRotator? SpriteRotation { get; init; }
    public Vector3? SpriteOffset { get; init; }
    public UkxColor? SpriteColor { get; init; }
    public int? SpriteValue0 { get; init; }
    public int? SpriteValue1 { get; init; }
    public int? SpriteValue2 { get; init; }
    public float? Version4Value { get; init; }
    public int? LegacyVersion5Value { get; init; }
    public Vector3[] BasePoints { get; init; } = [];
    public UkxMeshBone[] RefSkeleton { get; init; } = [];
    public UkxObjectReference? AnimationReference { get; init; }
    public int SkeletalDepth { get; init; }
    public UkxWeightIndex[] WeightIndices { get; init; } = [];
    public UkxBoneInfluence[] BoneInfluences { get; init; } = [];
    public string[] AttachAliases { get; init; } = [];
    public string[] AttachBoneNames { get; init; } = [];
    public UkxCoords[] AttachCoords { get; init; } = [];
    public UkxSkeletalLodModel[] LodModels { get; init; } = [];
    public UkxObjectReference? BaseMeshReference { get; init; }
    public Vector3[] BaseLodPoints { get; init; } = [];
    public UkxMeshWedge[] BaseLodWedges { get; init; } = [];
    public UkxTriangle[] BaseLodTriangles { get; init; } = [];
    public UkxVertInfluence[] BaseLodVertInfluences { get; init; } = [];
    public ushort[] CollapseWedges { get; init; } = [];
    public ushort[] TrianglePointIndices { get; init; } = [];
    public int? LineageValue118_3 { get; init; }
    public float[] LineageValues123_18 { get; init; } = [];
    public int? LineageValue120 { get; init; }
    public int? LineageValueLicensee23 { get; init; }
    public UkxLegacyLodSection[] LegacySoftLodSections { get; init; } = [];
    public UkxLegacyLodSection[] LegacyRigidLodSections { get; init; } = [];
    public ushort[] LegacyLodIndices { get; init; } = [];
}

public sealed class UkxMeshAnimationObject : UkxFileObject
{
    public int Version { get; init; }
    public UkxNamedBone[] RefBones { get; init; } = [];
    public int? RoughArrayEndPosition { get; init; }
    public UkxMotionChunk[] Moves { get; init; } = [];
    public UkxMeshAnimSequence[] AnimSequences { get; init; } = [];
}

public sealed record UkxJointPosition(
    Quaternion Orientation,
    Vector3 Position,
    float? Length,
    Vector3? Size);

public sealed record UkxMeshBone(
    string Name,
    uint Flags,
    UkxJointPosition JointPosition,
    int ParentIndex,
    int NumChildren);

public sealed record UkxWeightIndex(
    ushort[] BoneInfluenceIndices,
    int StartBoneInfluence);

public sealed record UkxBoneInfluence(
    ushort BoneWeight,
    ushort BoneIndex);

public sealed record UkxSkinPoint(
    Vector3 Point,
    uint PackedNormal);

public sealed record UkxSkelMeshSection(
    ushort MaterialIndex,
    ushort MinStreamIndex,
    ushort MinWedgeIndex,
    ushort MaxWedgeIndex,
    ushort NumStreamIndices,
    ushort BoneIndex,
    ushort Value0E,
    ushort FirstFace,
    ushort NumFaces,
    int[] LineageBoneMap);

public sealed record UkxRawIndexBuffer(
    ushort[] Indices,
    int Revision);

public sealed record UkxAnimMeshVertex(
    Vector3 Position,
    Vector3 Normal,
    Vector2 UV);

public sealed record UkxSkinVertexStream(
    int Revision,
    int Value18,
    int Value1C,
    UkxAnimMeshVertex[] Vertices);

public sealed record UkxVertInfluence(
    float Weight,
    ushort PointIndex,
    ushort BoneIndex);

public sealed record UkxMeshWedge(
    ushort VertexIndex,
    Vector2 UV);

public sealed record UkxMeshFace(
    ushort WedgeIndex0,
    ushort WedgeIndex1,
    ushort WedgeIndex2,
    ushort MaterialIndex);

public sealed record UkxMeshMaterial(
    uint PolyFlags,
    int TextureIndex);

public sealed record UkxLegacyMeshTri2(byte[] RawBytes);

public sealed record UkxLegacyLodSection(byte[] RawBytes);

public sealed record UkxTriangle(
    ushort WedgeIndex0,
    ushort WedgeIndex1,
    ushort WedgeIndex2,
    byte MaterialIndex,
    byte AuxMaterialIndex,
    uint SmoothingGroups);

public sealed record UkxLineageWedge(
    Vector3 Position,
    Vector3 Normal,
    Vector2 UV,
    byte Bone0,
    byte Bone1,
    byte Bone2,
    byte Bone3,
    float Weight0,
    float Weight1,
    float Weight2,
    float Weight3);

public sealed record UkxSkeletalLodModel(
    uint[] SkinningData,
    UkxSkinPoint[] SkinPoints,
    int NumSoftWedges,
    UkxSkelMeshSection[] SoftSections,
    UkxSkelMeshSection[] RigidSections,
    UkxRawIndexBuffer SoftIndices,
    UkxRawIndexBuffer RigidIndices,
    UkxSkinVertexStream VertexStream,
    UkxVertInfluence[] VertInfluences,
    UkxMeshWedge[] Wedges,
    UkxMeshFace[] Faces,
    Vector3[] Points,
    float LODDistanceFactor,
    float LODHysteresis,
    int NumSharedVerts,
    int LODMaxInfluences,
    int Value114,
    int Value118,
    int? UseNewWedges,
    UkxLineageWedge[] LineageWedges);

public sealed record UkxCoords(
    Vector3 Origin,
    Vector3 XAxis,
    Vector3 YAxis,
    Vector3 ZAxis);

public sealed record UkxRotator(
    int Pitch,
    int Yaw,
    int Roll);

public sealed record UkxColor(
    byte R,
    byte G,
    byte B,
    byte A);

public sealed record UkxNamedBone(
    string Name,
    uint Flags,
    int ParentIndex);

public sealed record UkxAnalogTrack(
    uint Flags,
    Quaternion[] KeyQuat,
    Vector3[] KeyPos,
    float[] KeyTime);

public sealed record UkxMotionChunk(
    int? RoughArrayItemEndPosition,
    Vector3 RootSpeed3D,
    float TrackTime,
    int StartBone,
    uint Flags,
    int[] BoneIndices,
    UkxAnalogTrack[] AnimTracks,
    UkxAnalogTrack RootTrack);

public sealed record UkxMeshAnimNotify(
    float Time,
    string FunctionName,
    UkxObjectReference? NotifyObjectReference,
    string? LineageExtraText);

public sealed record UkxMeshAnimSequenceLineageData(
    int Value2C,
    int Value30,
    int? Value34,
    UkxObjectReference? ObjectReference38,
    int? Value3C,
    int? Value40,
    byte[] Unknown44Bytes);

public sealed record UkxMeshAnimSequence(
    string Name,
    string[] Groups,
    int StartFrame,
    int NumFrames,
    float Rate,
    UkxMeshAnimNotify[] Notifies,
    float? Value28,
    UkxMeshAnimSequenceLineageData? LineageData);

internal static class UkxObjectReferenceDecoder
{
    internal static UkxObjectReference? ReadObjectReference(BinaryReader reader, L2Viewer.PackageCore.PackageData package)
    {
        var rawReference = L2Viewer.PackageCore.PackageReader.ReadCompactIndex(reader);
        return FromRawReference(rawReference, package);
    }

    internal static UkxObjectReference? FromRawReference(int rawReference, L2Viewer.PackageCore.PackageData package)
    {
        if (rawReference == 0)
        {
            return null;
        }

        var resolved = L2Viewer.PackageCore.UnrPackageHelpers.ResolveObjectRef(package, rawReference);
        if (resolved is null)
        {
            return new UkxObjectReference
            {
                RawReference = rawReference,
                ClassName = "<unknown>",
                ObjectName = "<unknown>"
            };
        }

        return new UkxObjectReference
        {
            RawReference = rawReference,
            ClassName = resolved.Value.ClassName,
            ObjectName = resolved.Value.ObjectName,
            PackageName = resolved.Value.PackageName,
            ImportIndex = rawReference < 0 ? -rawReference - 1 : null,
            ExportIndex = rawReference > 0 ? rawReference - 1 : null
        };
    }
}

public sealed class UkxUnknownProperty
{
    public required int Order { get; init; }
    public required string Name { get; init; }
    public required int Type { get; init; }
    public required int DataSize { get; init; }
    public string? StructName { get; init; }
    public bool BoolValue { get; init; }
    public byte? ByteValue { get; init; }
    public int? IntValue { get; init; }
    public float? FloatValue { get; init; }
    public string? NameValue { get; init; }
    public string? StringValue { get; init; }
    public UkxObjectReference? ObjectReference { get; init; }
    public string? RawHex { get; init; }
}

public sealed class UkxObjectReference
{
    public required int RawReference { get; init; }
    public required string ClassName { get; init; }
    public required string ObjectName { get; init; }
    public string? PackageName { get; init; }
    public int? ImportIndex { get; init; }
    public int? ExportIndex { get; init; }
}

public sealed record UkxFileExportObjectEntry(UkxFileExportEntry Export, UkxFileObject Object);

public sealed record UkxFile(
    string FilePath,
    string Wrapper,
    UkxFileHeader Header,
    IReadOnlyList<UkxFileNameEntry> Names,
    IReadOnlyList<UkxFileImportEntry> Imports,
    IReadOnlyList<UkxFileExportObjectEntry> ExportObjects);
