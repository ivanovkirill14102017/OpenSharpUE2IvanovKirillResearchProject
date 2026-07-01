using L2Viewer.SceneDomain.Models;
using L2Viewer.PackageCore;
using L2Viewer.SceneDTO;
using L2Viewer.SceneDomain.Services.MaterialServices;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.BSPServices;

[ForExternalUse]
public sealed class BspTerrainSceneBuilder
{
    private const int TerrainTileSize = 32;
    private const int ChunkSampleSize = 64;
    private const int ChunkTextureSize = 512;
    private readonly BspTextureManager _textureManager;

    public BspTerrainSceneBuilder(BspTextureManager textureManager)
    {
        _textureManager = textureManager;
    }

    public BspDiagnosticTerrainPlacement[] Build(UnrFile.UnrFile unr)
    {
        var textures = TerrainShared.ResolveLayerAndHeightTextures(
            _textureManager,
            unr.ExportObjects
                .Select(x => x.Object)
                .OfType<UnrTerrainInfoObject>());
        var placements = new List<BspDiagnosticTerrainPlacement>();
        foreach (var entry in unr.ExportObjects)
        {
            if (entry.Object is not UnrTerrainInfoObject terrain)
            {
                continue;
            }

            var built = BuildTerrain(terrain, textures);
            if (built is not null)
            {
                placements.Add(built);
            }
        }

        return placements
            .OrderBy(x => x.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private BspDiagnosticTerrainPlacement? BuildTerrain(
        UnrTerrainInfoObject terrain,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        if (terrain.TerrainMapReference?.PackageName is null)
        {
            return null;
        }

        var heightResolved = TerrainShared.GetResolvedTexture(terrain.TerrainMapReference, textures);
        if (heightResolved?.Texture is null)
        {
            return null;
        }

        var heightTexture = heightResolved.Texture;
        var heightField = TerrainShared.BuildHeightField(heightTexture, out var heightChannel);
        var terrainScale = TerrainShared.NormalizeTerrainScale(terrain.TerrainScale);
        var loadedLayers = LoadLayers(terrain, textures);
        var quadVisibility = TerrainMeshBuilder.BuildQuadVisibilityMask(terrain, heightTexture);
        var edgeTurn = TerrainMeshBuilder.BuildEdgeTurnMask(terrain, heightTexture);
        var chunkMeshes = BuildChunkMeshes(heightTexture, heightField, terrainScale, loadedLayers, quadVisibility, edgeTurn);

        if (terrain.Location is not null)
        {
            chunkMeshes = chunkMeshes
                .Select(x => TranslateMesh(x, terrain.Location.Value))
                .ToArray();
        }

        if (chunkMeshes.Length == 0)
        {
            return null;
        }

        var boundsMin = new Vector3(
            chunkMeshes.Min(x => x.BBoxMin.X),
            chunkMeshes.Min(x => x.BBoxMin.Y),
            chunkMeshes.Min(x => x.BBoxMin.Z));
        var boundsMax = new Vector3(
            chunkMeshes.Max(x => x.BBoxMax.X),
            chunkMeshes.Max(x => x.BBoxMax.Y),
            chunkMeshes.Max(x => x.BBoxMax.Z));

        return new BspDiagnosticTerrainPlacement
        {
            ExportIndex = terrain.ExportIndex,
            ObjectName = terrain.ObjectName,
            Meshes = chunkMeshes,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            HeightReference = $"{terrain.TerrainMapReference.PackageName}.{terrain.TerrainMapReference.ObjectName}",
            ColorReference = loadedLayers.Count == 0
                ? $"{heightTexture.SourcePackage}.{heightTexture.Name}"
                : $"chunks={chunkMeshes.Length}, layers={loadedLayers.Count}",
            HeightChannel = heightChannel
        };
    }

    private List<LoadedTerrainLayer> LoadLayers(
        UnrTerrainInfoObject terrain,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        return terrain.Layers
            .Select(layer => (
                Texture: TerrainShared.ResolveTexture(layer.TextureReference, textures),
                AlphaMap: TerrainShared.ResolveTexture(layer.AlphaMapReference, textures),
                layer.TextureReference))
            .Where(x => x.Texture is not null && x.AlphaMap is not null)
            .Select(x => new LoadedTerrainLayer(
                x.Texture!,
                x.AlphaMap!,
                x.TextureReference?.PackageName is null
                    ? x.Texture!.Name
                    : $"{x.TextureReference.PackageName}.{x.TextureReference.ObjectName}"))
            .ToList();
    }

    private static MeshData[] BuildChunkMeshes(
        TextureData heightTexture,
        IReadOnlyList<float> heightField,
        Vector3? terrainScale,
        IReadOnlyList<LoadedTerrainLayer> layers,
        TerrainBitGrid? quadVisibility,
        TerrainBitGrid? edgeTurn)
    {
        var geometry = TerrainMeshBuilder.BuildTerrainGeometry(heightTexture, heightField, terrainScale);
        var chunks = new List<MeshData>();

        for (var chunkY = 0; chunkY < geometry.Height - 1; chunkY += ChunkSampleSize)
        {
            for (var chunkX = 0; chunkX < geometry.Width - 1; chunkX += ChunkSampleSize)
            {
                var endX = Math.Min(chunkX + ChunkSampleSize, geometry.Width - 1);
                var endY = Math.Min(chunkY + ChunkSampleSize, geometry.Height - 1);
                var chunkTexture = BuildChunkTexture(heightTexture, layers, chunkX, chunkY, endX, endY);
                var chunkMesh = TerrainMeshBuilder.BuildChunkMesh(geometry, chunkX, chunkY, endX, endY, chunkTexture, quadVisibility, edgeTurn);
                if (chunkMesh is not null)
                {
                    chunks.Add(chunkMesh);
                }
            }
        }

        return chunks.ToArray();
    }

    private static TextureData BuildChunkTexture(
        TextureData heightTexture,
        IReadOnlyList<LoadedTerrainLayer> layers,
        int startX,
        int startY,
        int endX,
        int endY)
    {
        if (layers.Count == 0)
        {
            return BuildChunkBaseTexture(heightTexture, startX, startY, endX, endY);
        }

        var output = new byte[ChunkTextureSize * ChunkTextureSize * 4];

        for (var y = 0; y < ChunkTextureSize; y++)
        {
            for (var x = 0; x < ChunkTextureSize; x++)
            {
                var chunkU = (float)x / Math.Max(1, ChunkTextureSize - 1);
                var chunkV = (float)y / Math.Max(1, ChunkTextureSize - 1);
                var globalU = LerpSample(startX, endX, chunkU, heightTexture.Width);
                var globalV = LerpSample(startY, endY, chunkV, heightTexture.Height);
                var terrainX = globalU * Math.Max(1, heightTexture.Width - 1);
                var terrainY = globalV * Math.Max(1, heightTexture.Height - 1);

                var baseSample = SampleTextureWrap(heightTexture, globalU, globalV);
                var finalR = (float)baseSample.R;
                var finalG = (float)baseSample.G;
                var finalB = (float)baseSample.B;

                foreach (var layer in layers)
                {
                    var alphaSample = SampleTextureWrap(layer.AlphaMap, globalU, globalV);
                    var alpha = alphaSample.R / 255f;
                    if (alpha <= 0f)
                    {
                        continue;
                    }

                    var colorSample = SampleTextureWrap(layer.Texture, terrainX / TerrainTileSize, terrainY / TerrainTileSize);
                    finalR = finalR * (1f - alpha) + colorSample.R * alpha;
                    finalG = finalG * (1f - alpha) + colorSample.G * alpha;
                    finalB = finalB * (1f - alpha) + colorSample.B * alpha;
                }

                var dst = (y * ChunkTextureSize + x) * 4;
                output[dst + 0] = (byte)Math.Clamp(finalR, 0f, 255f);
                output[dst + 1] = (byte)Math.Clamp(finalG, 0f, 255f);
                output[dst + 2] = (byte)Math.Clamp(finalB, 0f, 255f);
                output[dst + 3] = 255;
            }
        }

        return new RgbaTextureData(
            $"terrain_chunk_{startX}_{startY}",
            ChunkTextureSize,
            ChunkTextureSize,
            output,
            heightTexture.SourcePackage,
            $"terrain chunk blend {startX},{startY}");
    }

    private static TextureData BuildChunkBaseTexture(
        TextureData baseTexture,
        int startX,
        int startY,
        int endX,
        int endY)
    {
        var output = new byte[ChunkTextureSize * ChunkTextureSize * 4];
        for (var y = 0; y < ChunkTextureSize; y++)
        {
            for (var x = 0; x < ChunkTextureSize; x++)
            {
                var globalU = LerpSample(startX, endX, (float)x / Math.Max(1, ChunkTextureSize - 1), baseTexture.Width);
                var globalV = LerpSample(startY, endY, (float)y / Math.Max(1, ChunkTextureSize - 1), baseTexture.Height);
                var sample = SampleTextureWrap(baseTexture, globalU, globalV);
                var dst = (y * ChunkTextureSize + x) * 4;
                output[dst + 0] = sample.R;
                output[dst + 1] = sample.G;
                output[dst + 2] = sample.B;
                output[dst + 3] = 255;
            }
        }

        return new RgbaTextureData(
            $"terrain_chunk_base_{startX}_{startY}",
            ChunkTextureSize,
            ChunkTextureSize,
            output,
            baseTexture.SourcePackage,
            $"terrain chunk base {startX},{startY}");
    }

    private static float LerpSample(int start, int end, float t, int size)
    {
        var sample = start + (end - start) * t;
        return sample / Math.Max(1, size - 1);
    }


    private static (byte R, byte G, byte B, byte A) SampleTextureWrap(TextureData texture, float u, float v)
    {
        var x = (int)MathF.Floor(u * texture.Width) % texture.Width;
        if (x < 0) x += texture.Width;
        var y = (int)MathF.Floor(v * texture.Height) % texture.Height;
        if (y < 0) y += texture.Height;
        var src = (y * texture.Width + x) * 4;
        return (
            texture.RgbaBytes[src + 0],
            texture.RgbaBytes[src + 1],
            texture.RgbaBytes[src + 2],
            texture.RgbaBytes[src + 3]);
    }

    private static MeshData TranslateMesh(MeshData mesh, Vector3 offset)
    {
        var triangles = mesh.Triangles
            .Select(t => new Triangle(
                new TriangleVertex(t.A.Position + offset, t.A.UV, t.A.Normal),
                new TriangleVertex(t.B.Position + offset, t.B.UV, t.B.Normal),
                new TriangleVertex(t.C.Position + offset, t.C.UV, t.C.Normal),
                t.MaterialId))
            .ToList();
        return mesh with
        {
            Triangles = triangles,
            BBoxMin = mesh.BBoxMin + offset,
            BBoxMax = mesh.BBoxMax + offset
        };
    }

    private sealed record LoadedTerrainLayer(TextureData Texture, TextureData AlphaMap, string ReferenceText);
}
