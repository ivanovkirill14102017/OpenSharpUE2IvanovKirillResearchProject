using System.Collections.ObjectModel;

namespace L2Viewer.PackageCore;

public sealed record PackageHeader(
    ushort Version,
    ushort LicenseeVersion,
    uint Flags,
    uint NameCount,
    uint NameOffset,
    uint ExportCount,
    uint ExportOffset,
    uint ImportCount,
    uint ImportOffset);

public sealed record ImportEntry(int ClassPackage, int ClassName, uint PackageIndex, int ObjectName);

public sealed record ExportEntry(
    int ClassIndex,
    int SuperIndex,
    uint PackageIndex,
    int ObjectName,
    uint ObjectFlags,
    int SerialSize,
    int? SerialOffset);

public sealed record PackageData(
    string Path,
    string Wrapper,
    PackageHeader Header,
    IReadOnlyList<string> Names,
    IReadOnlyList<ImportEntry> Imports,
    IReadOnlyList<ExportEntry> Exports);

public sealed record PackageExportInfo(
    int Index,
    string ObjectName,
    string ClassName,
    int SerialSize,
    int? SerialOffset);

public sealed record TextureData(
    string Name,
    int Width,
    int Height,
    byte[] RgbaBytes,
    string SourcePackage,
    string DecodeNote);

public sealed record MaterialTextureInfo(
    string Reference,
    string? ResolvedPackagePath);

public sealed record NativeObjectReferenceInfo(
    int RawReference,
    int? ExportIndex,
    string ClassName,
    string ObjectName,
    string? PackageName);

public sealed record ModelNativeData(
    string Name,
    bool HasLeadingNone,
    Vector3 BoundsMin,
    Vector3 BoundsMax,
    bool BoundsValid,
    int ObservedHeaderSize,
    int InlinePayloadOffset,
    int InlinePayloadSize,
    IReadOnlyList<NativeObjectReferenceInfo> References,
    IReadOnlyList<NativeObjectReferenceInfo> PolyReferences);

public sealed record PolyNativeData(
    int Index,
    int NumVertices,
    Vector3 Base,
    Vector3 Normal,
    Vector3 TextureU,
    Vector3 TextureV,
    IReadOnlyList<Vector3> Vertices,
    uint PolyFlags,
    NativeObjectReferenceInfo? ActorReference,
    NativeObjectReferenceInfo? TextureReference,
    string? ItemName,
    int ILink,
    int IBrushPoly,
    float LightMapScale,
    int? SavePolyIndex);

public sealed record PolysNativeData(
    string Name,
    IReadOnlyList<PolyNativeData> Polys);

public sealed record StaticMeshCollisionInfo(
    bool UseSimpleLineCollision,
    bool UseSimpleBoxCollision,
    bool UseSimpleKarmaCollision,
    IReadOnlyList<bool> MaterialCollisionFlags);

public sealed record Lineage2CollisionBasis(
    Vector3 Min,
    Vector3 Max);

public sealed record Lineage2CollisionFaceGeometry(
    Vector3 A,
    Vector3 B,
    Vector3 C,
    IReadOnlyList<Lineage2CollisionBasis> Bases,
    Vector3 UnknownVector,
    int Unknown1,
    int Unknown2);

public sealed record StaticMeshCollisionData(
    MeshData Mesh,
    int NodeCount,
    int FaceCount,
    BoundingBox PrimitiveBoundingBox,
    BoundingSphere PrimitiveBoundingSphere,
    byte UnknownFlag,
    IReadOnlyList<Lineage2CollisionFaceGeometry> FaceGeometries,
    string SourceNote);

public readonly record struct UnrealPropertyTag(
    string Name,
    int Type,
    int DataSize,
    int ArrayIndex,
    string? StructName,
    bool BoolValue)
{
    public bool IsEnd => string.Equals(Name, "None", StringComparison.Ordinal);
}

public sealed record IndexedResourceRef(
    string PackagePath,
    string PackageName,
    string ObjectName,
    string ClassName,
    int ExportIndex);

public sealed record TriangleVertex(Vector3 Position, Vector2 UV, Vector3 Normal);

public sealed record Triangle(TriangleVertex A, TriangleVertex B, TriangleVertex C, int MaterialId);

public sealed record BoundingBox(Vector3 Min, Vector3 Max, byte? IsValid);

public sealed record BoundingSphere(Vector3 Center, float? Radius);

public sealed record MeshData(
    string Name,
    IReadOnlyList<Triangle> Triangles,
    Vector3 BBoxMin,
    Vector3 BBoxMax,
    string SourceNote,
    string? TextureRef,
    IReadOnlyList<MaterialTextureInfo> UsedTextures);

public sealed record StaticMeshSection(int Unused4, int FirstIndex, int FirstVertex, int LastVertex, int UnusedE, int NumFaces);

public sealed record StaticMeshVertexStream(
    IReadOnlyList<Vector3> Vertices,
    IReadOnlyList<Vector3> Normals,
    int Revision);

public sealed record RawColorStream(
    IReadOnlyList<uint> Colors,
    int Revision);

public sealed record StaticMeshUVStream(
    IReadOnlyList<Vector2> Uv,
    int Revision,
    int Unused14);

public sealed record RawIndexBuffer(
    IReadOnlyList<int> Indices,
    int Revision);

public sealed record StaticMeshRaw(
    BoundingBox? BoundingBox,
    BoundingSphere? BoundingSphere,
    IReadOnlyList<StaticMeshSection> Sections,
    BoundingBox? Box,
    StaticMeshVertexStream VertexStream,
    RawColorStream ColorStream1,
    RawColorStream ColorStream2,
    IReadOnlyList<StaticMeshUVStream> UvSets,
    RawIndexBuffer IndexStream1,
    RawIndexBuffer IndexStream2,
    int WireframeIndexCount,
    string SourceNote);

public sealed record TextureMeta(int? FormatValue, int? Width, int? Height);

public sealed record PackageDocument(
    PackageData Package,
    IReadOnlyList<PackageExportInfo> Exports,
    IReadOnlyList<string> Names)
{
    public static PackageDocument Load(string path)
    {
        var package = PackageReader.LoadPackage(path);
        var exports = package.Exports
            .Select((x, i) => new PackageExportInfo(
                i,
                PackageReader.SafeName(package.Names, x.ObjectName),
                PackageReader.ExportClassName(package, x),
                x.SerialSize,
                x.SerialOffset))
            .ToList();
        return new PackageDocument(package, exports, package.Names.ToList());
    }
}

public sealed record ClientAssetIndex(
    IReadOnlyDictionary<string, IndexedResourceRef> Textures,
    IReadOnlyDictionary<string, IndexedResourceRef> StaticMeshes);

public sealed record TerrainTextureRef(string PackageName, string ObjectName);

public sealed record TerrainLayerData(string TextureReference, string AlphaMapReference);

public sealed record TerrainPreviewData(
    MeshData Mesh,
    TextureData HeightTexture,
    TextureData HeightPreviewTexture,
    TextureData? DiffuseTexture,
    TextureData RenderTexture,
    string HeightReference,
    string HeightChannel,
    string? DiffuseReference,
    string RenderReference,
    IReadOnlyList<MaterialTextureInfo> TextureEntries,
    IReadOnlyList<TerrainLayerData> Layers,
    IReadOnlyList<string> AllReferences,
    string PropertiesText);

public sealed record ActorPreviewData(
    string ActorName,
    string ClassName,
    Vector3 BBoxMin,
    Vector3 BBoxMax,
    string PropertiesText);

public sealed record ActorReference(
    int Reference,
    int? ExportIndex,
    string ClassName,
    string ObjectName,
    string? PackageName);

public sealed record MapActorSceneData(
    int ExportIndex,
    string Name,
    string ClassName,
    Vector3? Location,
    Vector3? Rotation,
    float DrawScale,
    Vector3 DrawScale3D,
    Vector3 PrePivot,
    ActorReference? StaticMeshRef,
    ActorReference? BrushRef,
    ActorReference? BaseRef,
    ActorReference? OwnerRef,
    ActorReference? MeshRef,
    ActorReference? TextureRef,
    ActorReference? PhysicsVolumeRef,
    string? Tag,
    string? Event,
    IReadOnlyDictionary<string, string> DecodedProperties,
    IReadOnlyDictionary<string, ActorReference> ObjectReferences,
    IReadOnlyCollection<string> PropertyNames);

public sealed record MapSceneData(
    string MapPath,
    IReadOnlyList<MapActorSceneData> Actors,
    IReadOnlyDictionary<int, IReadOnlyList<int>> HierarchyChildren,
    IReadOnlyList<int> HierarchyRoots,
    IReadOnlyDictionary<string, int> PropertyUsage,
    IReadOnlyDictionary<string, int> ObjectReferenceUsage);

public sealed record PlacedStaticMeshActorData(
    int ExportIndex,
    string ActorName,
    string ClassName,
    string StaticMeshReference,
    MeshData Mesh,
    Vector3 WorldBoundsMin,
    Vector3 WorldBoundsMax);

public sealed record PlacedWaterVolumeData(
    int ExportIndex,
    string ActorName,
    string ClassName,
    string BrushReference,
    MeshData Mesh,
    Vector3 WorldBoundsMin,
    Vector3 WorldBoundsMax);

public sealed record PlacedPolyData(
    int ExportIndex,
    string PolyName,
    MeshData Mesh,
    Vector3 WorldBoundsMin,
    Vector3 WorldBoundsMax);

public sealed record MeshDecodeDiagnostic(
    MeshData? Mesh,
    StaticMeshCollisionData? Collision,
    StaticMeshCollisionInfo? CollisionInfo,
    bool CacheHit);
