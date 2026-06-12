using L2Viewer.PackageCore;

namespace L2Viewer.SceneDomain.Models;

public sealed class SceneTerrainDecorationLayer
{
    public required int TerrainExportIndex { get; init; }
    public required string TerrainActorName { get; init; }
    public required int LayerIndex { get; init; }
    public required string HeightMapReference { get; init; }
    public required SceneResourceLocation HeightMapResource { get; init; }
    public required string MeshReference { get; init; }
    public required SceneResourceLocation MeshResource { get; init; }
    public required string DensityMapReference { get; init; }
    public required SceneResourceLocation DensityMapResource { get; init; }
    public string? ScaleMapReference { get; init; }
    public SceneResourceLocation? ScaleMapResource { get; init; }
    public string? ColorMapReference { get; init; }
    public SceneResourceLocation? ColorMapResource { get; init; }
    public required TextureData DensityMapTexture { get; init; }
    public TextureData? ScaleMapTexture { get; init; }
    public TextureData? ColorMapTexture { get; init; }
    public required int MaxPerQuad { get; init; }
    public required int Seed { get; init; }
    public required bool RandomYaw { get; init; }
    public required Vector3 TerrainLocation { get; init; }
    public Vector3? TerrainScale { get; init; }
    public UnrFloatRange? DensityMultiplier { get; init; }
    public UnrRangeVector? ScaleMultiplier { get; init; }
}
