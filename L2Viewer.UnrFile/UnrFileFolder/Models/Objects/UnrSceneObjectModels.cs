namespace L2Viewer.UnrFile;

public sealed class UnrModelObject : UnrFileObject
{
    public Vector3 BoundsMin { get; init; }
    public Vector3 BoundsMax { get; init; }
    public bool BoundsValid { get; init; }
    public Vector3 BoundingSphereCenter { get; init; }
    public float BoundingSphereRadius { get; init; }
    public int? UnknownTailHeaderInt0 { get; init; }
    public int? UnknownTailHeaderInt1 { get; init; }
    public UnrFileObjectReference? PolysReference { get; init; }
    public Vector3[] Vectors { get; init; } = [];
    public Vector3[] Points { get; init; } = [];
    public UnrModelNode[] Nodes { get; init; } = [];
    public UnrModelSurface[] Surfaces { get; init; } = [];
    public UnrModelVertex[] Vertices { get; init; } = [];
    public int NativeTailSize { get; init; }
}

public sealed class UnrModelNode
{
    public required Vector4 Plane { get; init; }
    public required ulong ZoneMask { get; init; }
    public required byte NodeFlags { get; init; }
    public required int VertexPoolIndex { get; init; }
    public required int SurfaceIndex { get; init; }
    public required int BackNodeIndex { get; init; }
    public required int FrontNodeIndex { get; init; }
    public required int PlaneNodeIndex { get; init; }
    public required int CollisionBoundIndex { get; init; }
    public required int RenderBoundIndex { get; init; }
    public required UnrUnknownVector3FloatStruct UnknownStruct0 { get; init; }
    public required UnrUnknownVector3FloatStruct UnknownStruct1 { get; init; }
    public required byte Zone0 { get; init; }
    public required byte Zone1 { get; init; }
    public required byte VertexCount { get; init; }
    public required int LeafIndex0 { get; init; }
    public required int LeafIndex1 { get; init; }
    public required int UnknownInt1 { get; init; }
    public required int UnknownInt2 { get; init; }
    public required int UnknownInt3 { get; init; }
}

public sealed class UnrUnknownVector3FloatStruct
{
    public required Vector3 Center { get; init; }
    public required float Radius { get; init; }
}

public sealed class UnrModelSurface
{
    public required int MaterialRawReference { get; init; }
    public UnrFileObjectReference? MaterialReference { get; init; }
    public required uint PolyFlags { get; init; }
    public UnrPolyFlags KnownPolyFlags => UnrPolyFlagsInfo.GetKnownFlags(PolyFlags);
    public uint UnknownPolyFlagsMask => UnrPolyFlagsInfo.GetUnknownBits(PolyFlags);
    public string[] PolyFlagNames => UnrPolyFlagsInfo.GetNames(PolyFlags);
    public required int BaseIndex { get; init; }
    public required int NormalIndex { get; init; }
    public required int TextureUIndex { get; init; }
    public required int TextureVIndex { get; init; }
    public required int BrushPolyIndex { get; init; }
    public required int ActorIndex { get; init; }
    public required Vector4 Plane { get; init; }
    public required float LightMapScale { get; init; }
    public required int LightMapIndex { get; init; }
}

public sealed class UnrModelVertex
{
    public required int PointIndex { get; init; }
    public required int SideIndex { get; init; }
}

public sealed class UnrPolysObject : UnrFileObject
{
    public UnrFilePoly[] Polys { get; init; } = [];
}

public sealed class UnrLevelObject : UnrFileObject
{
    public UnrFileObjectReference? ModelReference { get; init; }
    public UnrFileObjectReference? BrushReference { get; init; }
    public required int NativeTailSize { get; init; }
    public string? UrlProtocol { get; init; }
    public string? UrlHost { get; init; }
    public string? UrlMap { get; init; }
    public string? UrlPortal { get; init; }
    public int? UrlPort { get; init; }
    public bool? UrlValid { get; init; }
}

public sealed class UnrTerrainInfoObject : UnrFileObject
{
    public Vector3? Location { get; init; }
    public Vector3? TerrainScale { get; init; }
    public int? MapX { get; init; }
    public int? MapY { get; init; }
    public UnrFileObjectReference? TerrainMapReference { get; init; }
    public uint[] QuadVisibilityBitmap { get; init; } = [];
    public uint[] QuadVisibilityBitmapOriginal { get; init; } = [];
    public uint[] EdgeTurnBitmap { get; init; } = [];
    public uint[] EdgeTurnBitmapOriginal { get; init; } = [];
    public UnrTerrainLayer[] Layers { get; init; } = [];
    public UnrTerrainDecoLayer[] DecoLayers { get; init; } = [];
    public UnrFileUnknownProperty[] UnknownProperties { get; init; } = [];
    public required int NativeTailSize { get; init; }
}

public sealed class UnrTerrainLayer
{
    public int ArrayIndex { get; init; }
    public UnrFileObjectReference? TextureReference { get; init; }
    public UnrFileObjectReference? AlphaMapReference { get; init; }
}

public sealed class UnrTerrainDecoLayer
{
    public int Order { get; init; }
    public int ShowOnTerrain { get; init; }
    public UnrFileObjectReference? ScaleMapReference { get; init; }
    public UnrFileObjectReference? DensityMapReference { get; init; }
    public UnrFileObjectReference? ColorMapReference { get; init; }
    public UnrFileObjectReference? StaticMeshReference { get; init; }
    public UnrRangeVector? ScaleMultiplier { get; init; }
    public UnrFloatRange? FadeoutRadius { get; init; }
    public UnrFloatRange? DensityMultiplier { get; init; }
    public int MaxPerQuad { get; init; }
    public int Seed { get; init; }
    public int AlignToTerrain { get; init; }
    public byte DrawOrder { get; init; }
    public int ShowOnInvisibleTerrain { get; init; }
    public int LitDirectional { get; init; }
    public int DisregardTerrainLighting { get; init; }
    public int RandomYaw { get; init; }
    public int ForceRender { get; init; }
}

public sealed class UnrFloatRange
{
    public required float Min { get; init; }
    public required float Max { get; init; }
}

public sealed class UnrRangeVector
{
    public required UnrFloatRange X { get; init; }
    public required UnrFloatRange Y { get; init; }
    public required UnrFloatRange Z { get; init; }
}

public sealed class UnrStaticMeshObject : UnrFileObject
{
    public required int PayloadSize { get; init; }
}

public sealed class UnrTextureObject : UnrFileObject
{
    public required int FormatValue { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public UnrFileObjectReference? PaletteReference { get; init; }
}

public sealed class UnrVolumeObject : UnrFileObject
{
    public required int PayloadSize { get; init; }
}
