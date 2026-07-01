using L2Viewer.SceneDomain.Models;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Services.MaterialServices;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.BSPServices;

[ForExternalUse]
public sealed class BspTerrainDecoSceneBuilder
{
    private const int MaxSampleResolution = 128;
    private readonly BspTextureManager _textureManager;
    private readonly BspStaticMeshManager _staticMeshManager;

    public BspTerrainDecoSceneBuilder(BspTextureManager textureManager, BspStaticMeshManager staticMeshManager)
    {
        _textureManager = textureManager;
        _staticMeshManager = staticMeshManager;
    }

    public BspDiagnosticPropPlacement[] Build(UnrFile.UnrFile unr)
    {
        var terrains = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrTerrainInfoObject>()
            .ToList();
        var textures = TerrainShared.ResolveLayerAndHeightTextures(_textureManager, terrains);
        var meshes = _staticMeshManager.ResolveMany(
            unr.FilePath,
            terrains.SelectMany(x => x.DecoLayers)
                .Where(x => x.StaticMeshReference is not null)
                .Select(x => x.StaticMeshReference!));
        var placements = new List<BspDiagnosticPropPlacement>();
        foreach (var terrain in terrains)
        {
            placements.AddRange(BuildTerrainDeco(unr.FilePath, terrain, textures, meshes));
        }

        return placements
            .OrderBy(x => x.ActorName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IEnumerable<BspDiagnosticPropPlacement> BuildTerrainDeco(
        string mapPath,
        UnrTerrainInfoObject terrain,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> textures,
        IReadOnlyDictionary<string, MeshData> meshes)
    {
        if (terrain.TerrainMapReference?.PackageName is null || terrain.Location is null)
        {
            yield break;
        }

        var heightResolved = TerrainShared.GetResolvedTexture(terrain.TerrainMapReference, textures);
        if (heightResolved?.Texture is null)
        {
            yield break;
        }

        var heightTexture = heightResolved.Texture;
        var heightField = TerrainShared.BuildHeightField(heightTexture, out _);
        var terrainScale = TerrainShared.NormalizeTerrainScale(terrain.TerrainScale);

        for (var layerIndex = 0; layerIndex < terrain.DecoLayers.Length; layerIndex++)
        {
            var layer = terrain.DecoLayers[layerIndex];
            if (layer.ShowOnTerrain == 0 ||
                layer.StaticMeshReference is null ||
                layer.DensityMapReference?.PackageName is null)
            {
                continue;
            }

            var densityResolved = TerrainShared.GetResolvedTexture(layer.DensityMapReference, textures);
            if (densityResolved?.Texture is null)
            {
                continue;
            }

            var scaleResolved = layer.ScaleMapReference?.PackageName is null
                ? null
                : TerrainShared.GetResolvedTexture(layer.ScaleMapReference, textures);
            var meshReference = SceneReferenceUtilities.BuildReference(mapPath, layer.StaticMeshReference);
            var staticMesh = meshes[meshReference];
            if (staticMesh is null || staticMesh.Triangles.Count == 0)
            {
                continue;
            }

            var instances = BuildLayerInstances(
                terrain,
                layer,
                layerIndex,
                heightTexture,
                heightField,
                terrainScale,
                densityResolved.Texture,
                scaleResolved?.Texture,
                staticMesh);

            foreach (var instance in instances)
            {
                yield return instance;
            }
        }
    }

    private IEnumerable<BspDiagnosticPropPlacement> BuildLayerInstances(
        UnrTerrainInfoObject terrain,
        UnrTerrainDecoLayer layer,
        int layerIndex,
        TextureData heightTexture,
        IReadOnlyList<float> heightField,
        Vector3? terrainScale,
        TextureData densityTexture,
        TextureData? scaleTexture,
        MeshData staticMesh)
    {
        var staticMeshReference = layer.StaticMeshReference
            ?? throw new PackageReadException("Terrain deco layer has no static mesh reference.");
        var width = densityTexture.Width;
        var height = densityTexture.Height;
        if (width <= 0 || height <= 0)
        {
            yield break;
        }

        var stepX = Math.Max(1, (int)Math.Ceiling(width / (double)MaxSampleResolution));
        var stepY = Math.Max(1, (int)Math.Ceiling(height / (double)MaxSampleResolution));
        var rng = new Random(layer.Seed ^ terrain.ExportIndex * 397 ^ layerIndex * 7919);
        var densityScale = layer.DensityMultiplier?.Max ?? 100f;
        var maxPerQuad = Math.Max(1, layer.MaxPerQuad);
        var emitted = 0;

        for (var y = 0; y < height; y += stepY)
        {
            for (var x = 0; x < width; x += stepX)
            {
                var density = SampleGray01(densityTexture, x, y);
                if (density <= 0.05f)
                {
                    continue;
                }

                var desired = density * maxPerQuad * (densityScale / 100f);
                var count = Math.Clamp((int)MathF.Round(desired), 0, 3);
                if (count == 0 && density > 0.35f)
                {
                    count = 1;
                }

                for (var i = 0; i < count; i++)
                {
                    var jitterU = (x + (float)rng.NextDouble() * stepX) / Math.Max(1, width - 1);
                    var jitterV = (y + (float)rng.NextDouble() * stepY) / Math.Max(1, height - 1);
                    jitterU = Math.Clamp(jitterU, 0f, 1f);
                    jitterV = Math.Clamp(jitterV, 0f, 1f);

                    var position = SampleTerrainWorldPosition(heightTexture, heightField, terrainScale, terrain.Location!.Value, jitterU, jitterV);
                    var scale = ResolveScale(layer, scaleTexture, jitterU, jitterV, rng);
                    var yaw = layer.RandomYaw != 0
                        ? (float)(rng.NextDouble() * Math.PI * 2.0)
                        : 0f;
                    var mesh = TransformMesh(staticMesh, position, scale, yaw);

                    emitted++;
                    yield return new BspDiagnosticPropPlacement
                    {
                        ExportIndex = terrain.ExportIndex,
                        ActorName = $"{terrain.ObjectName}:Deco{layerIndex:D2}:{emitted:D5}",
                        ClassName = "TerrainDecoLayer",
                        StaticMeshReference = $"{staticMeshReference.PackageName ?? "<map>"}.{staticMeshReference.ObjectName}",
                        PrimaryTextureReference = staticMesh.TextureRef,
                        Mesh = mesh,
                        BoundsMin = mesh.BBoxMin,
                        BoundsMax = mesh.BBoxMax
                    };
                }
            }
        }
    }

    private static MeshData TransformMesh(MeshData mesh, Vector3 position, Vector3 scale, float yawRadians)
    {
        var triangles = new List<Triangle>(mesh.Triangles.Count);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var tri in mesh.Triangles)
        {
            var a = TransformVertex(tri.A, position, scale, yawRadians);
            var b = TransformVertex(tri.B, position, scale, yawRadians);
            var c = TransformVertex(tri.C, position, scale, yawRadians);
            triangles.Add(new Triangle(a, b, c, tri.MaterialId));
            min = Vector3.Min(min, a.Position);
            min = Vector3.Min(min, b.Position);
            min = Vector3.Min(min, c.Position);
            max = Vector3.Max(max, a.Position);
            max = Vector3.Max(max, b.Position);
            max = Vector3.Max(max, c.Position);
        }

        return mesh with
        {
            Triangles = triangles,
            BBoxMin = min,
            BBoxMax = max,
            SourceNote = $"{mesh.SourceNote}; terrain deco"
        };
    }

    private static TriangleVertex TransformVertex(TriangleVertex vertex, Vector3 position, Vector3 scale, float yawRadians)
    {
        var scaled = new Vector3(vertex.Position.X * scale.X, vertex.Position.Y * scale.Y, vertex.Position.Z * scale.Z);
        var rotatedPosition = RotateYaw(scaled, yawRadians) + position;
        var rotatedNormal = Vector3.Normalize(RotateYaw(vertex.Normal, yawRadians));
        return new TriangleVertex(rotatedPosition, vertex.UV, rotatedNormal);
    }

    private static Vector3 RotateYaw(Vector3 value, float yawRadians)
    {
        var cos = MathF.Cos(yawRadians);
        var sin = MathF.Sin(yawRadians);
        return new Vector3(
            value.X * cos - value.Y * sin,
            value.X * sin + value.Y * cos,
            value.Z);
    }

    private static Vector3 SampleTerrainWorldPosition(
        TextureData heightTexture,
        IReadOnlyList<float> heightField,
        Vector3? terrainScale,
        Vector3 terrainLocation,
        float u,
        float v)
    {
        var sampleX = u * Math.Max(1, heightTexture.Width - 1);
        var sampleY = v * Math.Max(1, heightTexture.Height - 1);
        var scaleX = terrainScale?.X ?? 4f;
        var scaleHeight = terrainScale is null ? 240f : terrainScale.Value.Y * 256f;
        var scaleY = terrainScale?.Z ?? 4f;
        var cx = 0.5f * (heightTexture.Width - 1);
        var cy = 0.5f * (heightTexture.Height - 1);
        var heightValue = SampleHeight(heightField, heightTexture.Width, heightTexture.Height, sampleX, sampleY);

        return new Vector3(
            (sampleX - cx) * scaleX + terrainLocation.X,
            (sampleY - cy) * scaleY + terrainLocation.Y,
            (heightValue - 0.5f) * scaleHeight + terrainLocation.Z);
    }

    private static float SampleHeight(IReadOnlyList<float> heightField, int width, int height, float sampleX, float sampleY)
    {
        var x0 = Math.Clamp((int)MathF.Floor(sampleX), 0, width - 1);
        var y0 = Math.Clamp((int)MathF.Floor(sampleY), 0, height - 1);
        var x1 = Math.Clamp(x0 + 1, 0, width - 1);
        var y1 = Math.Clamp(y0 + 1, 0, height - 1);
        var tx = sampleX - x0;
        var ty = sampleY - y0;

        var h00 = heightField[y0 * width + x0];
        var h10 = heightField[y0 * width + x1];
        var h01 = heightField[y1 * width + x0];
        var h11 = heightField[y1 * width + x1];
        var hx0 = Lerp(h00, h10, tx);
        var hx1 = Lerp(h01, h11, tx);
        return Lerp(hx0, hx1, ty);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static Vector3 ResolveScale(UnrTerrainDecoLayer layer, TextureData? scaleTexture, float u, float v, Random rng)
    {
        var amount = scaleTexture is null ? (float)rng.NextDouble() : SampleGray01(scaleTexture, u, v);
        if (layer.ScaleMultiplier is null)
        {
            return Vector3.One;
        }

        return new Vector3(
            Lerp(layer.ScaleMultiplier.X.Min, layer.ScaleMultiplier.X.Max, amount),
            Lerp(layer.ScaleMultiplier.Y.Min, layer.ScaleMultiplier.Y.Max, amount),
            Lerp(layer.ScaleMultiplier.Z.Min, layer.ScaleMultiplier.Z.Max, amount));
    }

    private static float SampleGray01(TextureData texture, int x, int y)
    {
        var clampedX = Math.Clamp(x, 0, texture.Width - 1);
        var clampedY = Math.Clamp(y, 0, texture.Height - 1);
        var src = (clampedY * texture.Width + clampedX) * 4;
        return (0.299f * texture.RgbaBytes[src + 0] +
                0.587f * texture.RgbaBytes[src + 1] +
                0.114f * texture.RgbaBytes[src + 2]) / 255f;
    }

    private static float SampleGray01(TextureData texture, float u, float v)
    {
        var x = Math.Clamp((int)MathF.Round(u * Math.Max(1, texture.Width - 1)), 0, texture.Width - 1);
        var y = Math.Clamp((int)MathF.Round(v * Math.Max(1, texture.Height - 1)), 0, texture.Height - 1);
        return SampleGray01(texture, x, y);
    }

}
