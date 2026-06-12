using System.IO;
using System.Windows.Media.Imaging;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.Wpf;

internal sealed class ScenePreviewGeometry
{
    public BitmapSource CreateBitmap(TextureData texture)
    {
        return BitmapSource.Create(
            texture.Width,
            texture.Height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            ConvertRgbaToBgra(texture.RgbaBytes),
            texture.Width * 4);
    }

    public PreparedMeshScene PrepareMeshScene(MeshData mesh)
    {
        var geometry = BuildGeometry(mesh);
        var preparedBounds = PrepareBoundsScene(mesh.BBoxMin, mesh.BBoxMax);
        return new PreparedMeshScene(geometry, preparedBounds.Bounds, preparedBounds.ViewerBoundsMin, preparedBounds.ViewerBoundsMax);
    }

    public PreparedMeshScene PrepareMeshScene(SceneTriangleMeshData mesh)
    {
        var geometry = BuildGeometry(mesh);
        var preparedBounds = PrepareBoundsScene(mesh.BoundsMin, mesh.BoundsMax);
        return new PreparedMeshScene(geometry, preparedBounds.Bounds, preparedBounds.ViewerBoundsMin, preparedBounds.ViewerBoundsMax);
    }

    public PreparedBoundsScene PrepareBoundsScene(System.Numerics.Vector3 min, System.Numerics.Vector3 max)
    {
        var viewerCorners = new[]
        {
            ToViewerVector(new System.Numerics.Vector3(min.X, min.Y, min.Z)),
            ToViewerVector(new System.Numerics.Vector3(max.X, min.Y, min.Z)),
            ToViewerVector(new System.Numerics.Vector3(max.X, max.Y, min.Z)),
            ToViewerVector(new System.Numerics.Vector3(min.X, max.Y, min.Z)),
            ToViewerVector(new System.Numerics.Vector3(min.X, min.Y, max.Z)),
            ToViewerVector(new System.Numerics.Vector3(max.X, min.Y, max.Z)),
            ToViewerVector(new System.Numerics.Vector3(max.X, max.Y, max.Z)),
            ToViewerVector(new System.Numerics.Vector3(min.X, max.Y, max.Z))
        };

        var viewerMin = new System.Numerics.Vector3(
            viewerCorners.Min(v => v.X),
            viewerCorners.Min(v => v.Y),
            viewerCorners.Min(v => v.Z));
        var viewerMax = new System.Numerics.Vector3(
            viewerCorners.Max(v => v.X),
            viewerCorners.Max(v => v.Y),
            viewerCorners.Max(v => v.Z));
        return new PreparedBoundsScene(BuildBoundsGeometry(viewerMin, viewerMax), viewerMin, viewerMax);
    }

    public LineGeometry3D BuildBoneOverlayGeometry(SkeletalDebugOverlay overlay)
    {
        var positions = new HelixToolkit.Vector3Collection();
        var indices = new HelixToolkit.IntCollection();

        foreach (var segment in overlay.Segments)
        {
            var baseIndex = positions.Count;
            positions.Add(ToViewerVector(segment.Start));
            positions.Add(ToViewerVector(segment.End));
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
        }

        return new LineGeometry3D
        {
            Positions = positions,
            Indices = indices
        };
    }

    public PointGeometry3D BuildVertexDebugGeometry(SkeletalDebugFrameSnapshot frame)
    {
        var positions = new HelixToolkit.Vector3Collection(frame.Vertices.Count);
        foreach (var vertex in frame.Vertices)
        {
            positions.Add(ToViewerVector(vertex.DeformedWorld));
        }

        return new PointGeometry3D
        {
            Positions = positions
        };
    }

    public static System.Numerics.Vector3 ToViewerVector(System.Numerics.Vector3 unrealVector)
    {
        return new System.Numerics.Vector3(unrealVector.X, unrealVector.Z, unrealVector.Y);
    }

    public static MeshData BuildPreviewMesh(SceneTriangleMeshData source, SceneStaticMeshInstance? instance, int materialId)
    {
        var triangles = new List<Triangle>(source.Triangles.Count);
        var min = new System.Numerics.Vector3(float.MaxValue);
        var max = new System.Numerics.Vector3(float.MinValue);

        foreach (var tri in source.Triangles)
        {
            if (tri.MaterialId != materialId)
            {
                continue;
            }

            var a = instance is null ? tri.A : TransformPreviewVertex(tri.A, instance);
            var b = instance is null ? tri.B : TransformPreviewVertex(tri.B, instance);
            var c = instance is null ? tri.C : TransformPreviewVertex(tri.C, instance);
            triangles.Add(new Triangle(a, b, c, tri.MaterialId));
            UpdateBounds(a.Position, ref min, ref max);
            UpdateBounds(b.Position, ref min, ref max);
            UpdateBounds(c.Position, ref min, ref max);
        }

        var namePrefix = instance?.ActorName is null ? "" : $"{instance.ActorName}:";
        return new MeshData(
            $"{namePrefix}{source.Name}",
            triangles,
            min,
            max,
            $"{source.SourceNote}; preview instance",
            null,
            []);
    }

    public static MeshData BuildTerrainMesh(TerrainImportData terrain)
    {
        var width = terrain.HeightWidth;
        var height = terrain.HeightHeight;
        var cx = 0.5f * Math.Max(0, width - 1);
        var cy = 0.5f * Math.Max(0, height - 1);
        var worldCenter = terrain.WorldCenter ?? System.Numerics.Vector3.Zero;
        var triangles = new List<Triangle>((width - 1) * (height - 1) * 2);

        System.Numerics.Vector3 PositionAt(int x, int y)
        {
            var sample = terrain.HeightSamples[(y * width) + x];
            return new System.Numerics.Vector3(
                worldCenter.X + ((x - cx) * terrain.SampleSpacingX),
                worldCenter.Y + ((y - cy) * terrain.SampleSpacingY),
                worldCenter.Z + ((sample - 0.5f) * terrain.HeightValueScale));
        }

        System.Numerics.Vector2 UvAt(int x, int y)
        {
            return new System.Numerics.Vector2(
                width <= 1 ? 0f : x / (float)(width - 1),
                height <= 1 ? 0f : y / (float)(height - 1));
        }

        for (var y = 0; y < height - 1; y++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                var a = PositionAt(x, y);
                var b = PositionAt(x + 1, y);
                var c = PositionAt(x + 1, y + 1);
                var d = PositionAt(x, y + 1);
                var n = System.Numerics.Vector3.UnitZ;
                triangles.Add(new Triangle(new TriangleVertex(a, UvAt(x, y), n), new TriangleVertex(b, UvAt(x + 1, y), n), new TriangleVertex(c, UvAt(x + 1, y + 1), n), 0));
                triangles.Add(new Triangle(new TriangleVertex(a, UvAt(x, y), n), new TriangleVertex(c, UvAt(x + 1, y + 1), n), new TriangleVertex(d, UvAt(x, y + 1), n), 0));
            }
        }

        var boundsMin = new System.Numerics.Vector3(
            triangles.SelectMany(t => new[] { t.A.Position, t.B.Position, t.C.Position }).Min(v => v.X),
            triangles.SelectMany(t => new[] { t.A.Position, t.B.Position, t.C.Position }).Min(v => v.Y),
            triangles.SelectMany(t => new[] { t.A.Position, t.B.Position, t.C.Position }).Min(v => v.Z));
        var boundsMax = new System.Numerics.Vector3(
            triangles.SelectMany(t => new[] { t.A.Position, t.B.Position, t.C.Position }).Max(v => v.X),
            triangles.SelectMany(t => new[] { t.A.Position, t.B.Position, t.C.Position }).Max(v => v.Y),
            triangles.SelectMany(t => new[] { t.A.Position, t.B.Position, t.C.Position }).Max(v => v.Z));
        return new MeshData(
            terrain.ObjectName,
            triangles,
            boundsMin,
            boundsMax,
            $"terrain import height_channel={terrain.HeightChannel}",
            null,
            []);
    }

    public static TextureData BuildHeightPreviewTexture(TerrainImportData terrain)
    {
        var min = terrain.HeightSamples.Min();
        var max = terrain.HeightSamples.Max();
        var range = MathF.Max(0.0001f, max - min);
        var rgba = new byte[terrain.HeightSamples.Length * 4];
        for (var i = 0; i < terrain.HeightSamples.Length; i++)
        {
            var normalized = (terrain.HeightSamples[i] - min) / range;
            var value = (byte)Math.Clamp(normalized * 255f, 0f, 255f);
            var dst = i * 4;
            rgba[dst + 0] = value;
            rgba[dst + 1] = value;
            rgba[dst + 2] = value;
            rgba[dst + 3] = 255;
        }

        return new TextureData(
            terrain.ObjectName,
            terrain.HeightWidth,
            terrain.HeightHeight,
            rgba,
            terrain.HeightTexture.SourcePackage,
            $"height preview ({terrain.HeightChannel})");
    }

    public static string BuildTerrainSummaryText(TerrainImportData terrain)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ObjectName = {terrain.ObjectName}");
        sb.AppendLine("Class = TerrainInfo");
        sb.AppendLine($"HeightMap = {terrain.HeightReference}");
        sb.AppendLine($"HeightTextureSize = {terrain.HeightWidth}x{terrain.HeightHeight}");
        sb.AppendLine($"HeightChannel = {terrain.HeightChannel}");
        sb.AppendLine($"SampleSpacingX = {terrain.SampleSpacingX}");
        sb.AppendLine($"SampleSpacingY = {terrain.SampleSpacingY}");
        sb.AppendLine($"HeightValueScale = {terrain.HeightValueScale}");
        if (terrain.WorldCenter is not null)
        {
            sb.AppendLine($"WorldCenter = {terrain.WorldCenter.Value}");
        }

        sb.AppendLine($"Layers = {terrain.Layers.Length}");
        foreach (var layer in terrain.Layers.OrderBy(x => x.ArrayIndex))
        {
            sb.AppendLine($"  - Layer[{layer.ArrayIndex}] Texture = {layer.TextureReference ?? "<none>"}");
            sb.AppendLine($"    AlphaMap = {layer.AlphaMapReference ?? "<none>"}");
        }

        return sb.ToString();
    }

    private HelixToolkit.SharpDX.MeshGeometry3D BuildGeometry(MeshData mesh)
    {
        var positions = new HelixToolkit.Vector3Collection();
        var normals = new HelixToolkit.Vector3Collection();
        var texCoords = new HelixToolkit.Vector2Collection();
        var indices = new HelixToolkit.IntCollection();

        foreach (var tri in mesh.Triangles)
        {
            var baseIndex = positions.Count;
            positions.Add(ToViewerVector(tri.A.Position));
            positions.Add(ToViewerVector(tri.C.Position));
            positions.Add(ToViewerVector(tri.B.Position));
            normals.Add(ToViewerNormal(tri.A.Normal));
            normals.Add(ToViewerNormal(tri.C.Normal));
            normals.Add(ToViewerNormal(tri.B.Normal));
            texCoords.Add(new System.Numerics.Vector2(tri.A.UV.X, 1f - tri.A.UV.Y));
            texCoords.Add(new System.Numerics.Vector2(tri.C.UV.X, 1f - tri.C.UV.Y));
            texCoords.Add(new System.Numerics.Vector2(tri.B.UV.X, 1f - tri.B.UV.Y));
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
        }

        return new HelixToolkit.SharpDX.MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = texCoords,
            Indices = indices
        };
    }

    private HelixToolkit.SharpDX.MeshGeometry3D BuildGeometry(SceneTriangleMeshData mesh)
    {
        var positions = new HelixToolkit.Vector3Collection();
        var normals = new HelixToolkit.Vector3Collection();
        var texCoords = new HelixToolkit.Vector2Collection();
        var indices = new HelixToolkit.IntCollection();

        foreach (var tri in mesh.Triangles)
        {
            var baseIndex = positions.Count;
            positions.Add(ToViewerVector(tri.A.Position));
            positions.Add(ToViewerVector(tri.C.Position));
            positions.Add(ToViewerVector(tri.B.Position));
            normals.Add(ToViewerNormal(tri.A.Normal));
            normals.Add(ToViewerNormal(tri.C.Normal));
            normals.Add(ToViewerNormal(tri.B.Normal));
            texCoords.Add(new System.Numerics.Vector2(tri.A.UV.X, 1f - tri.A.UV.Y));
            texCoords.Add(new System.Numerics.Vector2(tri.C.UV.X, 1f - tri.C.UV.Y));
            texCoords.Add(new System.Numerics.Vector2(tri.B.UV.X, 1f - tri.B.UV.Y));
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
        }

        return new HelixToolkit.SharpDX.MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = texCoords,
            Indices = indices
        };
    }

    public HelixToolkit.SharpDX.MeshGeometry3D BuildGeometry(SceneBspMeshSection part, TextureData? texture)
    {
        var positions = new HelixToolkit.Vector3Collection();
        var normals = new HelixToolkit.Vector3Collection();
        var texCoords = new HelixToolkit.Vector2Collection();
        var indices = new HelixToolkit.IntCollection();

        for (var i = 0; i < part.Positions.Length; i++)
        {
            positions.Add(ToViewerVector(part.Positions[i]));
            normals.Add(ToViewerNormal(part.Normals[i]));
            texCoords.Add(NormalizeTextureCoordinate(part.TextureCoordinates[i], texture));
        }

        for (var i = 0; i < part.Indices.Length; i++)
        {
            indices.Add(part.Indices[i]);
        }

        return new HelixToolkit.SharpDX.MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = texCoords,
            Indices = indices
        };
    }

    private static System.Numerics.Vector3 ToViewerNormal(System.Numerics.Vector3 unrealNormal)
    {
        return System.Numerics.Vector3.Normalize(ToViewerVector(unrealNormal));
    }

    private static System.Numerics.Vector2 NormalizeTextureCoordinate(System.Numerics.Vector2 rawUv, TextureData? texture)
    {
        if (texture is null || texture.Width <= 0 || texture.Height <= 0)
        {
            return rawUv;
        }

        return new System.Numerics.Vector2(rawUv.X / texture.Width, 1f - (rawUv.Y / texture.Height));
    }

    private static TriangleVertex TransformPreviewVertex(TriangleVertex vertex, SceneStaticMeshInstance instance)
    {
        var scaled = vertex.Position * instance.Scale;
        var translated = scaled - instance.PrePivot;
        var rotated = RotateByEulerDegrees(translated, instance.RotationEulerDegrees);
        var world = rotated + instance.WorldLocation;
        var normal = RotateNormalByEulerDegrees(vertex.Normal, instance.RotationEulerDegrees);
        return new TriangleVertex(world, vertex.UV, normal);
    }

    private static System.Numerics.Vector3 RotateByEulerDegrees(System.Numerics.Vector3 value, System.Numerics.Vector3 degrees)
    {
        var radians = degrees * (MathF.PI / 180f);
        var rotation = System.Numerics.Quaternion.CreateFromYawPitchRoll(radians.Z, radians.Y, radians.X);
        return System.Numerics.Vector3.Transform(value, rotation);
    }

    private static System.Numerics.Vector3 RotateNormalByEulerDegrees(System.Numerics.Vector3 normal, System.Numerics.Vector3 degrees)
    {
        var rotated = RotateByEulerDegrees(normal, degrees);
        return rotated.LengthSquared() < 0.000001f
            ? System.Numerics.Vector3.UnitZ
            : System.Numerics.Vector3.Normalize(rotated);
    }

    private static void UpdateBounds(System.Numerics.Vector3 point, ref System.Numerics.Vector3 min, ref System.Numerics.Vector3 max)
    {
        min = System.Numerics.Vector3.Min(min, point);
        max = System.Numerics.Vector3.Max(max, point);
    }

    private static byte[] ConvertRgbaToBgra(byte[] rgba)
    {
        var bgra = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            bgra[i + 0] = rgba[i + 2];
            bgra[i + 1] = rgba[i + 1];
            bgra[i + 2] = rgba[i + 0];
            bgra[i + 3] = rgba[i + 3];
        }

        return bgra;
    }

    public static byte[] EncodePng(TextureData texture)
    {
        var bitmap = BitmapSource.Create(
            texture.Width,
            texture.Height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            ConvertRgbaToBgra(texture.RgbaBytes),
            texture.Width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    public static LineGeometry3D BuildBoundsGeometry(System.Numerics.Vector3 min, System.Numerics.Vector3 max)
    {
        var pts =
            new HelixToolkit.Vector3Collection([
                new System.Numerics.Vector3(min.X, min.Y, min.Z),
                new System.Numerics.Vector3(max.X, min.Y, min.Z),
                new System.Numerics.Vector3(max.X, max.Y, min.Z),
                new System.Numerics.Vector3(min.X, max.Y, min.Z),
                new System.Numerics.Vector3(min.X, min.Y, max.Z),
                new System.Numerics.Vector3(max.X, min.Y, max.Z),
                new System.Numerics.Vector3(max.X, max.Y, max.Z),
                new System.Numerics.Vector3(min.X, max.Y, max.Z)
            ]);

        var indices = new HelixToolkit.IntCollection
        {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        };
        return new LineGeometry3D { Positions = pts, Indices = indices };
    }
}
