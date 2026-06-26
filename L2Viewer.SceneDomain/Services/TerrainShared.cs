using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

internal static class TerrainShared
{
    public static IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> ResolveLayerAndHeightTextures(
        BspTextureManager textureManager,
        IEnumerable<UnrTerrainInfoObject> terrains)
    {
        var requests = terrains
            .SelectMany(terrain => terrain.Layers.SelectMany(layer => new[] { layer.TextureReference, layer.AlphaMapReference })
                .Append(terrain.TerrainMapReference))
            .Where(reference => !string.IsNullOrWhiteSpace(reference?.PackageName))
            .Select(reference => new SceneTextureRequest(reference!.PackageName!, reference.ObjectName))
            .ToArray();
        return textureManager.ResolveMany(requests);
    }

    public static BspTextureManager.ResolvedTexture? GetResolvedTexture(
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        if (reference?.PackageName is null)
        {
            return null;
        }

        return textures.GetValueOrDefault($"{reference.PackageName}.{reference.ObjectName}");
    }

    public static TextureData? ResolveTexture(
        UnrFileObjectReference? reference,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures)
    {
        return GetResolvedTexture(reference, textures)?.Texture;
    }

    public static string? ToReferenceText(UnrFileObjectReference? reference)
    {
        if (reference?.PackageName is null)
        {
            return null;
        }

        return $"{reference.PackageName}.{reference.ObjectName}";
    }

    public static Vector3? NormalizeTerrainScale(Vector3? terrainScale)
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

    public static float[] BuildHeightField(TextureData texture, out string channelName)
    {
        if (texture is Gray16TextureData gray16Texture)
        {
            channelName = "g16";
            var gray16Result = new float[gray16Texture.Gray16Samples.Length];
            for (var i = 0; i < gray16Result.Length; i++)
            {
                gray16Result[i] = gray16Texture.Gray16Samples[i] / 65535f;
            }

            return gray16Result;
        }

        channelName = "r";
        var result = new float[texture.Width * texture.Height];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = texture.RgbaBytes[i * 4] / 255f;
        }

        return result;
    }
}
