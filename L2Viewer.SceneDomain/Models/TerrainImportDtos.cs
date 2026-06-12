namespace L2Viewer.SceneDomain.Models;

public sealed class TerrainImportData
{
    public required int ExportIndex { get; init; }
    public required string ObjectName { get; init; }
    public Vector3? WorldCenter { get; init; }
    public Vector3? WorldMinCorner { get; init; }
    public Vector3? WorldMaxCorner { get; init; }
    public Vector3? RawTerrainScale { get; init; }
    public required float SampleSpacingX { get; init; }
    public required float SampleSpacingY { get; init; }
    public required float HeightValueScale { get; init; }
    public int? MapX { get; init; }
    public int? MapY { get; init; }
    public required string HeightReference { get; init; }
    public required TextureData HeightTexture { get; init; }
    public required string HeightChannel { get; init; }
    public required int HeightWidth { get; init; }
    public required int HeightHeight { get; init; }
    public required float[] HeightSamples { get; init; }
    public TerrainBitMaskData? QuadVisibilityMask { get; init; }
    public TerrainBitMaskData? EdgeTurnMask { get; init; }
    public required TerrainLayerImportData[] Layers { get; init; }
}

public sealed class TerrainLayerImportData
{
    public required int ArrayIndex { get; init; }
    public string? TextureReference { get; init; }
    public TextureData? Texture { get; init; }
    public string? AlphaMapReference { get; init; }
    public TextureData? AlphaMapTexture { get; init; }
    public required int MaskWidth { get; init; }
    public required int MaskHeight { get; init; }
    public required float[] MaskSamples { get; init; }
    public required int TileSampleSize { get; init; }
}

public sealed class TerrainBitMaskData
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required bool[] Bits { get; init; }
    public required uint[] RawWords { get; init; }
}
