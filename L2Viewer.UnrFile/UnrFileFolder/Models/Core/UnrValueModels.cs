namespace L2Viewer.UnrFile;

public sealed class UnrFileScale
{
    public required Vector3 Scale { get; init; }
    public required float SheerRate { get; init; }
    public required byte SheerAxis { get; init; }
}

public sealed class UnrPointRegion
{
    public UnrFileObjectReference? ZoneReference { get; init; }
    public int? LeafIndex { get; init; }
    public byte? ZoneNumber { get; init; }
}

public sealed class UnrTextureModifyInfo
{
    public bool? UseModify { get; init; }
    public bool? TwoSide { get; init; }
    public bool? AlphaBlend { get; init; }
    public bool? Dummy { get; init; }
    public UnrFileColor? Color { get; init; }
    public int? AlphaOp { get; init; }
    public int? ColorOp { get; init; }
}

public sealed class UnrParticleColorScale
{
    public required int ArrayIndex { get; init; }
    public int Type { get; init; }
    public string? StructName { get; init; }
    public float? RelativeTime { get; init; }
    public UnrFileColor? Color { get; init; }
    public string? RawHex { get; init; }
}

public sealed class UnrParticleSizeScale
{
    public required int ArrayIndex { get; init; }
    public int Type { get; init; }
    public string? StructName { get; init; }
    public float? RelativeTime { get; init; }
    public float? RelativeSize { get; init; }
    public string? RawHex { get; init; }
}

public sealed class UnrAccessoryTypeEntry
{
    public int? Depth { get; init; }
    public UnrFileObjectReference? MeshReference { get; init; }
}

public sealed class UnrFileColor
{
    public required byte R { get; init; }
    public required byte G { get; init; }
    public required byte B { get; init; }
    public required byte A { get; init; }
}

public enum UnrFileReferenceKind
{
    Import = 1,
    Export = 2
}

public sealed class UnrFileObjectReference
{
    public required int RawReference { get; init; }
    public required UnrFileReferenceKind Kind { get; init; }
    public int? ImportIndex { get; init; }
    public int? ExportIndex { get; init; }
    public required string ClassName { get; init; }
    public required string ObjectName { get; init; }
    public string? PackageName { get; init; }
}

public sealed class UnrFileUnknownProperty
{
    public required int Order { get; init; }
    public required string Name { get; init; }
    public required int Type { get; init; }
    public required int DataSize { get; init; }
    public int ArrayIndex { get; init; }
    public string? StructName { get; init; }
    public bool BoolValue { get; init; }
    public byte? ByteValue { get; init; }
    public int? IntValue { get; init; }
    public float? FloatValue { get; init; }
    public string? NameValue { get; init; }
    public UnrFileObjectReference? ObjectReference { get; init; }
    public UnrFileScale? ScaleValue { get; init; }
    public string? RawHex { get; init; }
}

public sealed class UnrFileMaterialTextureRef
{
    public required string Reference { get; init; }
    public string? ResolvedPackagePath { get; init; }
    public bool HasDecodedTexture { get; init; }
}

public sealed class UnrFilePoly
{
    public required int Index { get; init; }
    public required int NumVertices { get; init; }
    public required Vector3 Base { get; init; }
    public required Vector3 Normal { get; init; }
    public required Vector3 TextureU { get; init; }
    public required Vector3 TextureV { get; init; }
    public required Vector3[] Vertices { get; init; }
    public required uint PolyFlags { get; init; }
    public UnrPolyFlags KnownPolyFlags => UnrPolyFlagsInfo.GetKnownFlags(PolyFlags);
    public uint UnknownPolyFlagsMask => UnrPolyFlagsInfo.GetUnknownBits(PolyFlags);
    public string[] PolyFlagNames => UnrPolyFlagsInfo.GetNames(PolyFlags);
    public UnrFileObjectReference? ActorReference { get; init; }
    public UnrFileObjectReference? TextureReference { get; init; }
    public string? ItemName { get; init; }
    public required int ILink { get; init; }
    public required int IBrushPoly { get; init; }
    public required float LightMapScale { get; init; }
    public required int? SavePolyIndex { get; init; }
}
