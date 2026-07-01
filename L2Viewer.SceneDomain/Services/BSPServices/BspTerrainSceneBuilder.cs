using L2Viewer.SceneDomain.Models;
using L2Viewer.PackageCore;
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
        var quadVisibility = BuildQuadVisibilityMask(terrain, heightTexture);
        var edgeTurn = BuildEdgeTurnMask(terrain, heightTexture);
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
        var geometry = BuildTerrainGeometry(heightTexture, heightField, terrainScale);
        var chunks = new List<MeshData>();

        for (var chunkY = 0; chunkY < geometry.Height - 1; chunkY += ChunkSampleSize)
        {
            for (var chunkX = 0; chunkX < geometry.Width - 1; chunkX += ChunkSampleSize)
            {
                var endX = Math.Min(chunkX + ChunkSampleSize, geometry.Width - 1);
                var endY = Math.Min(chunkY + ChunkSampleSize, geometry.Height - 1);
                var chunkTexture = BuildChunkTexture(heightTexture, layers, chunkX, chunkY, endX, endY);
                var chunkMesh = BuildChunkMesh(geometry, chunkX, chunkY, endX, endY, chunkTexture, quadVisibility, edgeTurn);
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

    private static MeshData? BuildChunkMesh(
        TerrainGeometry geometry,
        int startX,
        int startY,
        int endX,
        int endY,
        TextureData chunkTexture,
        TerrainBitGrid? quadVisibility,
        TerrainBitGrid? edgeTurn)
    {
        var triangles = new List<Triangle>();
        var localWidth = Math.Max(1, endX - startX);
        var localHeight = Math.Max(1, endY - startY);

        for (var y = startY; y < endY; y++)
        {
            for (var x = startX; x < endX; x++)
            {
                if (!IsQuadVisible(quadVisibility, geometry, x, y))
                {
                    continue;
                }

                var a = geometry.GetPosition(x, y);
                var b = geometry.GetPosition(x + 1, y);
                var c = geometry.GetPosition(x + 1, y + 1);
                var d = geometry.GetPosition(x, y + 1);

                var ua = new Vector2((float)(x - startX) / localWidth, (float)(y - startY) / localHeight);
                var ub = new Vector2((float)(x + 1 - startX) / localWidth, (float)(y - startY) / localHeight);
                var uc = new Vector2((float)(x + 1 - startX) / localWidth, (float)(y + 1 - startY) / localHeight);
                var ud = new Vector2((float)(x - startX) / localWidth, (float)(y + 1 - startY) / localHeight);
                var n = Vector3.UnitZ;

                if (!IsBitSet(edgeTurn, geometry, x, y))
                {
                    triangles.Add(new Triangle(
                        new TriangleVertex(a, ua, n),
                        new TriangleVertex(b, ub, n),
                        new TriangleVertex(c, uc, n),
                        0));
                    triangles.Add(new Triangle(
                        new TriangleVertex(a, ua, n),
                        new TriangleVertex(c, uc, n),
                        new TriangleVertex(d, ud, n),
                        0));
                }
                else
                {
                    triangles.Add(new Triangle(
                        new TriangleVertex(a, ua, n),
                        new TriangleVertex(b, ub, n),
                        new TriangleVertex(d, ud, n),
                        0));
                    triangles.Add(new Triangle(
                        new TriangleVertex(b, ub, n),
                        new TriangleVertex(c, uc, n),
                        new TriangleVertex(d, ud, n),
                        0));
                }
            }
        }

        if (triangles.Count == 0)
        {
            return null;
        }

        var bboxMin = new Vector3(
            triangles.SelectMany(t => new[] { t.A.Position.X, t.B.Position.X, t.C.Position.X }).Min(),
            triangles.SelectMany(t => new[] { t.A.Position.Y, t.B.Position.Y, t.C.Position.Y }).Min(),
            triangles.SelectMany(t => new[] { t.A.Position.Z, t.B.Position.Z, t.C.Position.Z }).Min());
        var bboxMax = new Vector3(
            triangles.SelectMany(t => new[] { t.A.Position.X, t.B.Position.X, t.C.Position.X }).Max(),
            triangles.SelectMany(t => new[] { t.A.Position.Y, t.B.Position.Y, t.C.Position.Y }).Max(),
            triangles.SelectMany(t => new[] { t.A.Position.Z, t.B.Position.Z, t.C.Position.Z }).Max());

        return new MeshData(
            $"TerrainChunk[{startX},{startY}]",
            triangles,
            bboxMin,
            bboxMax,
            "terrain chunk",
            chunkTexture.Name,
            [new MaterialTextureInfo(chunkTexture.Name, chunkTexture.SourcePackage)]);
    }

    private static TerrainBitGrid? BuildQuadVisibilityMask(UnrTerrainInfoObject terrain, TextureData heightTexture)
    {
        return BuildBitGrid(terrain.QuadVisibilityBitmap, terrain.MapX, terrain.MapY, heightTexture);
    }

    private static TerrainBitGrid? BuildEdgeTurnMask(UnrTerrainInfoObject terrain, TextureData heightTexture)
    {
        return BuildBitGrid(terrain.EdgeTurnBitmap, terrain.MapX, terrain.MapY, heightTexture);
    }

    private static TerrainBitGrid? BuildBitGrid(uint[] words, int? mapX, int? mapY, TextureData heightTexture)
    {
        if (words.Length == 0)
        {
            return null;
        }

        var bitCount = checked(words.Length * 32);
        var mapWidth = mapX.GetValueOrDefault();
        var mapHeight = mapY.GetValueOrDefault();
        var expectedQuadWidth = Math.Max(1, heightTexture.Width - 1);
        var expectedQuadHeight = Math.Max(1, heightTexture.Height - 1);
        var mappedArea = mapWidth > 0 && mapHeight > 0 ? checked(mapWidth * mapHeight) : 0;
        var expectedArea = checked(expectedQuadWidth * expectedQuadHeight);

        if (mapWidth <= 0 ||
            mapHeight <= 0 ||
            mappedArea > bitCount ||
            mappedArea < expectedArea / 4)
        {
            var inferred = InferBitmapDimensions(bitCount, heightTexture);
            mapWidth = inferred.Width;
            mapHeight = inferred.Height;
        }

        return new TerrainBitGrid(mapWidth, mapHeight, words);
    }

    private static (int Width, int Height) InferBitmapDimensions(int bitCount, TextureData heightTexture)
    {
        var quadWidth = Math.Max(1, heightTexture.Width - 1);
        var quadHeight = Math.Max(1, heightTexture.Height - 1);
        if (quadWidth * quadHeight <= bitCount)
        {
            var paddedWidth = quadWidth + 1;
            var paddedHeight = quadHeight + 1;
            if (paddedWidth * paddedHeight == bitCount)
            {
                return (paddedWidth, paddedHeight);
            }

            return (quadWidth, quadHeight);
        }

        var side = (int)Math.Sqrt(bitCount);
        if (side > 0 && side * side == bitCount)
        {
            return (side, side);
        }

        return (quadWidth, quadHeight);
    }

    private static bool IsQuadVisible(TerrainBitGrid? visibility, TerrainGeometry geometry, int quadX, int quadY)
    {
        if (visibility is null)
        {
            return true;
        }

        return IsBitSet(visibility, geometry, quadX, quadY);
    }

    private static bool IsBitSet(TerrainBitGrid? bitGrid, TerrainGeometry geometry, int quadX, int quadY)
    {
        if (bitGrid is null)
        {
            return false;
        }

        var sampleX = Math.Clamp((int)Math.Floor(quadX * (bitGrid.Width / (double)Math.Max(1, geometry.Width - 1))), 0, bitGrid.Width - 1);
        var sampleY = Math.Clamp((int)Math.Floor(quadY * (bitGrid.Height / (double)Math.Max(1, geometry.Height - 1))), 0, bitGrid.Height - 1);
        var bitIndex = checked(sampleY * bitGrid.Width + sampleX);
        var wordIndex = bitIndex >> 5;
        var bitMask = 1u << (bitIndex & 31);
        return wordIndex >= 0 &&
               wordIndex < bitGrid.Words.Length &&
               (bitGrid.Words[wordIndex] & bitMask) != 0;
    }

    private static TerrainGeometry BuildTerrainGeometry(
        TextureData heightTexture,
        IReadOnlyList<float> heightField,
        Vector3? terrainScale)
    {
        var width = Math.Max(2, heightTexture.Width);
        var height = Math.Max(2, heightTexture.Height);
        var sx = terrainScale?.X ?? 4f;
        var syHeight = terrainScale?.Y ?? 240f;
        if (terrainScale is not null)
        {
            syHeight *= 256f;
        }

        var sz = terrainScale?.Z ?? 4f;
        var cx = 0.5f * (width - 1);
        var cy = 0.5f * (height - 1);
        var positions = new Vector3[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var heightValue = heightField[y * width + x];
                positions[y * width + x] = new Vector3((x - cx) * sx, (y - cy) * sz, (heightValue - 0.5f) * syHeight);
            }
        }

        return new TerrainGeometry(width, height, positions);
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
    private sealed record TerrainBitGrid(int Width, int Height, uint[] Words);

    private sealed class TerrainGeometry
    {
        private readonly Vector3[] _positions;

        public TerrainGeometry(int width, int height, Vector3[] positions)
        {
            Width = width;
            Height = height;
            _positions = positions;
        }

        public int Width { get; }
        public int Height { get; }

        public Vector3 GetPosition(int x, int y) => _positions[y * Width + x];
    }
}
