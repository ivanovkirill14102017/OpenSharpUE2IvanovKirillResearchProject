using System.Numerics;
using L2Viewer.PackageCore;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDTO;

public static class TerrainMeshBuilder
{
    public static TerrainBitGrid? BuildQuadVisibilityMask(UnrTerrainInfoObject terrain, TextureData heightTexture)
    {
        return BuildBitGrid(terrain.QuadVisibilityBitmap, terrain.MapX, terrain.MapY, heightTexture);
    }

    public static TerrainBitGrid? BuildEdgeTurnMask(UnrTerrainInfoObject terrain, TextureData heightTexture)
    {
        return BuildBitGrid(terrain.EdgeTurnBitmap, terrain.MapX, terrain.MapY, heightTexture);
    }

    public static TerrainBitGrid? BuildBitGrid(uint[] words, int? mapX, int? mapY, TextureData heightTexture)
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

    public static TerrainGeometry BuildTerrainGeometry(
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

    public static MeshData? BuildChunkMesh(
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
                var normal = Vector3.UnitZ;

                if (!IsBitSet(edgeTurn, geometry, x, y))
                {
                    triangles.Add(new Triangle(
                        new TriangleVertex(a, ua, normal),
                        new TriangleVertex(b, ub, normal),
                        new TriangleVertex(c, uc, normal),
                        0));
                    triangles.Add(new Triangle(
                        new TriangleVertex(a, ua, normal),
                        new TriangleVertex(c, uc, normal),
                        new TriangleVertex(d, ud, normal),
                        0));
                }
                else
                {
                    triangles.Add(new Triangle(
                        new TriangleVertex(a, ua, normal),
                        new TriangleVertex(b, ub, normal),
                        new TriangleVertex(d, ud, normal),
                        0));
                    triangles.Add(new Triangle(
                        new TriangleVertex(b, ub, normal),
                        new TriangleVertex(c, uc, normal),
                        new TriangleVertex(d, ud, normal),
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
}

public sealed record TerrainBitGrid(int Width, int Height, uint[] Words);

public sealed class TerrainGeometry
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
