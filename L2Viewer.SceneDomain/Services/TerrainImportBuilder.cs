using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class TerrainImportBuilder
{
    private readonly BspTextureManager _textureManager;

    public TerrainImportBuilder(BspTextureManager textureManager)
    {
        _textureManager = textureManager;
    }

    public TerrainImportData[] Build(L2Viewer.UnrFile.UnrFile unr)
    {
        var textures = ResolveTextures(
            unr.ExportObjects
                .Select(x => x.Object)
                .OfType<UnrTerrainInfoObject>());

        return unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrTerrainInfoObject>()
            .Select(x => Build(x, textures))
            .Where(x => x is not null)
            .Cast<TerrainImportData>()
            .OrderBy(x => x.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public TerrainImportData? Build(UnrTerrainInfoObject terrain)
    {
        var textures = ResolveTextures([terrain]);
        return Build(terrain, textures);
    }

    private TerrainImportData? Build(
        UnrTerrainInfoObject terrain,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        if (terrain.TerrainMapReference?.PackageName is null)
        {
            return null;
        }

        var heightResolved = GetResolvedTexture(terrain.TerrainMapReference, textures);
        if (heightResolved?.Texture is null)
        {
            return null;
        }

        var heightTexture = heightResolved.Texture;
        var heightSamples = BuildHeightField(heightTexture, out var heightChannel);
        var normalizedScale = NormalizeTerrainScale(terrain.TerrainScale);
        var sampleSpacingX = normalizedScale?.X ?? 4f;
        var sampleSpacingY = normalizedScale?.Z ?? 4f;
        var heightValueScale = normalizedScale is null ? 240f : normalizedScale.Value.Y * 256f;
        var worldCenter = terrain.Location;
        var worldMinCorner = ComputeWorldMinCorner(worldCenter, heightTexture.Width, heightTexture.Height, sampleSpacingX, sampleSpacingY);
        var worldMaxCorner = ComputeWorldMaxCorner(worldCenter, heightTexture.Width, heightTexture.Height, sampleSpacingX, sampleSpacingY);
        var layers = terrain.Layers
            .OrderBy(x => x.ArrayIndex)
            .Select(x => BuildLayerImportData(x, heightTexture, textures))
            .ToArray();

        return new TerrainImportData
        {
            ExportIndex = terrain.ExportIndex,
            ObjectName = terrain.ObjectName,
            WorldCenter = worldCenter,
            WorldMinCorner = worldMinCorner,
            WorldMaxCorner = worldMaxCorner,
            RawTerrainScale = terrain.TerrainScale,
            SampleSpacingX = sampleSpacingX,
            SampleSpacingY = sampleSpacingY,
            HeightValueScale = heightValueScale,
            MapX = terrain.MapX,
            MapY = terrain.MapY,
            HeightReference = $"{terrain.TerrainMapReference.PackageName}.{terrain.TerrainMapReference.ObjectName}",
            HeightTexture = heightTexture,
            HeightChannel = heightChannel,
            HeightWidth = heightTexture.Width,
            HeightHeight = heightTexture.Height,
            HeightSamples = heightSamples,
            QuadVisibilityMask = BuildBitMask(terrain.QuadVisibilityBitmap, terrain.MapX, terrain.MapY, heightTexture),
            EdgeTurnMask = BuildBitMask(terrain.EdgeTurnBitmap, terrain.MapX, terrain.MapY, heightTexture),
            Layers = layers
        };
    }

    private TerrainLayerImportData BuildLayerImportData(
        UnrTerrainLayer layer,
        TextureData heightTexture,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        var texture = Resolve(layer.TextureReference, textures);
        var alphaMap = Resolve(layer.AlphaMapReference, textures);

        return new TerrainLayerImportData
        {
            ArrayIndex = layer.ArrayIndex,
            TextureReference = ToReferenceText(layer.TextureReference),
            Texture = texture,
            AlphaMapReference = ToReferenceText(layer.AlphaMapReference),
            AlphaMapTexture = alphaMap,
            MaskWidth = heightTexture.Width,
            MaskHeight = heightTexture.Height,
            MaskSamples = alphaMap is null
                ? []
                : BuildAlignedMask(alphaMap, heightTexture.Width, heightTexture.Height),
            TileSampleSize = 32
        };
    }

    private static TextureData? Resolve(
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        if (reference?.PackageName is null)
        {
            return null;
        }

        return GetResolvedTexture(reference, textures)?.Texture;
    }

    private IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> ResolveTextures(IEnumerable<UnrTerrainInfoObject> terrains)
    {
        var requests = terrains
            .SelectMany(terrain => terrain.Layers.SelectMany(layer => new[] { layer.TextureReference, layer.AlphaMapReference })
                .Append(terrain.TerrainMapReference))
            .Where(reference => !string.IsNullOrWhiteSpace(reference?.PackageName))
            .Select(reference => new SceneTextureRequest(reference!.PackageName!, reference.ObjectName))
            .ToArray();
        return _textureManager.ResolveMany(requests);
    }

    private static BspTextureManager.ResolvedTexture? GetResolvedTexture(
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        if (reference?.PackageName is null)
        {
            return null;
        }

        return textures.GetValueOrDefault($"{reference.PackageName}.{reference.ObjectName}");
    }

    private static string? ToReferenceText(UnrFileObjectReference? reference)
    {
        if (reference?.PackageName is null)
        {
            return null;
        }

        return $"{reference.PackageName}.{reference.ObjectName}";
    }

    private static Vector3? NormalizeTerrainScale(Vector3? terrainScale)
    {
        if (terrainScale is null)
        {
            return null;
        }

        var scale = terrainScale.Value;
        return new Vector3(
            MathF.Abs(scale.X),
            MathF.Abs(scale.Z),
            MathF.Abs(scale.Y));
    }

    private static Vector3? ComputeWorldMinCorner(
        Vector3? worldCenter,
        int heightWidth,
        int heightHeight,
        float sampleSpacingX,
        float sampleSpacingY)
    {
        if (worldCenter is null)
        {
            return null;
        }

        var cx = 0.5f * Math.Max(0, heightWidth - 1);
        var cy = 0.5f * Math.Max(0, heightHeight - 1);
        return new Vector3(
            worldCenter.Value.X - (cx * sampleSpacingX),
            worldCenter.Value.Y - (cy * sampleSpacingY),
            worldCenter.Value.Z);
    }

    private static Vector3? ComputeWorldMaxCorner(
        Vector3? worldCenter,
        int heightWidth,
        int heightHeight,
        float sampleSpacingX,
        float sampleSpacingY)
    {
        if (worldCenter is null)
        {
            return null;
        }

        var cx = 0.5f * Math.Max(0, heightWidth - 1);
        var cy = 0.5f * Math.Max(0, heightHeight - 1);
        return new Vector3(
            worldCenter.Value.X + (cx * sampleSpacingX),
            worldCenter.Value.Y + (cy * sampleSpacingY),
            worldCenter.Value.Z);
    }

    private static float[] BuildHeightField(TextureData texture, out string channelName)
    {
        var pixels = ToPixels(texture);
        var channels = new[] { "a", "r", "g", "b", "luma" };
        channelName = "luma";
        var bestScore = float.MaxValue;
        foreach (var channel in channels)
        {
            var (rough, lo, hi) = AnalyzeChannel(pixels, texture.Width, texture.Height, channel);
            var range = hi - lo;
            if (range < 0.03f)
            {
                continue;
            }

            var score = rough - 0.04f * range;
            if (score < bestScore)
            {
                bestScore = score;
                channelName = channel;
            }
        }

        var selectedChannel = channelName;
        return pixels
            .Select(x => ChannelValue(x, selectedChannel))
            .ToArray();
    }

    private static float[] BuildAlignedMask(TextureData texture, int targetWidth, int targetHeight)
    {
        var result = new float[targetWidth * targetHeight];
        for (var y = 0; y < targetHeight; y++)
        {
            var v = targetHeight <= 1 ? 0f : y / (float)(targetHeight - 1);
            for (var x = 0; x < targetWidth; x++)
            {
                var u = targetWidth <= 1 ? 0f : x / (float)(targetWidth - 1);
                var pixel = SampleTextureClamp(texture, u, v);
                result[(y * targetWidth) + x] = pixel.R / 255f;
            }
        }

        return result;
    }

    private static TerrainBitMaskData? BuildBitMask(uint[] words, int? mapX, int? mapY, TextureData heightTexture)
    {
        if (words.Length == 0)
        {
            return null;
        }

        var (width, height) = ResolveBitGridDimensions(words, mapX, mapY, heightTexture);
        var bits = new bool[width * height];
        for (var i = 0; i < bits.Length; i++)
        {
            var wordIndex = i >> 5;
            var bitMask = 1u << (i & 31);
            bits[i] = wordIndex >= 0 &&
                      wordIndex < words.Length &&
                      (words[wordIndex] & bitMask) != 0;
        }

        return new TerrainBitMaskData
        {
            Width = width,
            Height = height,
            Bits = bits,
            RawWords = words.ToArray()
        };
    }

    private static (int Width, int Height) ResolveBitGridDimensions(uint[] words, int? mapX, int? mapY, TextureData heightTexture)
    {
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
            return InferBitmapDimensions(bitCount, heightTexture);
        }

        return (mapWidth, mapHeight);
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

    private static List<(byte R, byte G, byte B, byte A)> ToPixels(TextureData texture)
    {
        var pixels = new List<(byte R, byte G, byte B, byte A)>(texture.Width * texture.Height);
        for (var i = 0; i < texture.RgbaBytes.Length; i += 4)
        {
            pixels.Add((texture.RgbaBytes[i], texture.RgbaBytes[i + 1], texture.RgbaBytes[i + 2], texture.RgbaBytes[i + 3]));
        }

        return pixels;
    }

    private static float ChannelValue((byte R, byte G, byte B, byte A) pixel, string mode)
    {
        return mode switch
        {
            "r" => pixel.R / 255f,
            "g" => pixel.G / 255f,
            "b" => pixel.B / 255f,
            "a" => pixel.A / 255f,
            _ => (0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B) / 255f
        };
    }

    private static (float Rough, float Lo, float Hi) AnalyzeChannel(
        List<(byte R, byte G, byte B, byte A)> pixels,
        int width,
        int height,
        string mode)
    {
        var lo = 1f;
        var hi = 0f;
        var rough = 0f;
        var count = 0;
        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            for (var x = 0; x < width; x++)
            {
                var value = ChannelValue(pixels[row + x], mode);
                lo = MathF.Min(lo, value);
                hi = MathF.Max(hi, value);
                if (x + 1 < width)
                {
                    rough += MathF.Abs(ChannelValue(pixels[row + x + 1], mode) - value);
                    count++;
                }

                if (y + 1 < height)
                {
                    rough += MathF.Abs(ChannelValue(pixels[row + x + width], mode) - value);
                    count++;
                }
            }
        }

        return (rough / Math.Max(1, count), lo, hi);
    }

    private static (byte R, byte G, byte B, byte A) SampleTextureClamp(TextureData texture, float u, float v)
    {
        var x = Math.Clamp((int)MathF.Round(u * Math.Max(0, texture.Width - 1)), 0, Math.Max(0, texture.Width - 1));
        var y = Math.Clamp((int)MathF.Round(v * Math.Max(0, texture.Height - 1)), 0, Math.Max(0, texture.Height - 1));
        var src = (y * texture.Width + x) * 4;
        return (
            texture.RgbaBytes[src + 0],
            texture.RgbaBytes[src + 1],
            texture.RgbaBytes[src + 2],
            texture.RgbaBytes[src + 3]);
    }
}
