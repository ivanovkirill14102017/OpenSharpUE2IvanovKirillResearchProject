using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDTO;

namespace L2Viewer.SceneDomain.Services.BSPServices;

public sealed class BspDiagnosticSceneBuilder
{
    public BspDiagnosticScene Load(string path)
    {
        var unr = UnrFileReader.Read(path);
        return Build(unr);
    }

    public BspDiagnosticScene Build(UnrFile.UnrFile unr)
    {
        var worldModelExports = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrLevelObject>()
            .Where(x => x.ModelReference?.ExportIndex is not null)
            .Select(x => x.ModelReference!.ExportIndex!.Value)
            .ToHashSet();

        var builtModels = new List<BspDiagnosticModel>();
        var sceneMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var sceneMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var entry in unr.ExportObjects)
        {
            if (entry.Object is not UnrModelObject model)
            {
                continue;
            }

            var brushPolys = BspUvResolver.ResolveBrushPolys(unr, model);
            var built = BuildModel(model, brushPolys, worldModelExports.Contains(model.ExportIndex));
            if (built is null)
            {
                continue;
            }

            builtModels.Add(built);
            sceneMin = Vector3.Min(sceneMin, built.BoundsMin);
            sceneMax = Vector3.Max(sceneMax, built.BoundsMax);
        }

        if (builtModels.Count == 0)
        {
            sceneMin = Vector3.Zero;
            sceneMax = Vector3.Zero;
        }

        return new BspDiagnosticScene
        {
            SourcePath = unr.FilePath,
            Models = builtModels
                .OrderByDescending(x => x.IsWorldModel)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            BoundsMin = sceneMin,
            BoundsMax = sceneMax
        };
    }

    private static BspDiagnosticModel? BuildModel(UnrModelObject model, UnrPolysObject? brushPolys, bool isWorldModel)
    {
        var polygons = new List<BspChunkPartitioner.BspPolygonFragment>();
        var modelMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var modelMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        var polygonCount = 0;
        var triangleCount = 0;

        for (var nodeIndex = 0; nodeIndex < model.Nodes.Length; nodeIndex++)
        {
            var node = model.Nodes[nodeIndex];
            if (node.SurfaceIndex < 0 || node.SurfaceIndex >= model.Surfaces.Length)
            {
                continue;
            }

            var vertexCount = node.VertexCount;
            if (vertexCount < 3)
            {
                continue;
            }

            var polygon = new List<Vector3>(vertexCount);
            var validPolygon = true;
            for (var vertexOffset = 0; vertexOffset < vertexCount; vertexOffset++)
            {
                var vertexIndex = node.VertexPoolIndex + vertexOffset;
                if (vertexIndex < 0 || vertexIndex >= model.Vertices.Length)
                {
                    validPolygon = false;
                    break;
                }

                var pointIndex = model.Vertices[vertexIndex].PointIndex;
                if (pointIndex < 0 || pointIndex >= model.Points.Length)
                {
                    validPolygon = false;
                    break;
                }

                polygon.Add(model.Points[pointIndex]);
            }

            if (!validPolygon)
            {
                continue;
            }

            polygon = RemoveSequentialDuplicates(polygon);
            if (polygon.Count < 3)
            {
                continue;
            }

            var surface = model.Surfaces[node.SurfaceIndex];
            var normal = SafeNormalize(new Vector3(node.Plane.X, node.Plane.Y, node.Plane.Z));
            if (normal.LengthSquared() < 0.000001f)
            {
                normal = ComputePolygonNormal(polygon);
            }

            var polygonMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var polygonMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var point in polygon)
            {
                polygonMin = Vector3.Min(polygonMin, point);
                polygonMax = Vector3.Max(polygonMax, point);
                modelMin = Vector3.Min(modelMin, point);
                modelMax = Vector3.Max(modelMax, point);
            }

            var builtTriangles = new List<BspChunkPartitioner.BspTriangleFragment>(Math.Max(1, polygon.Count - 2));
            var first = polygon[0];
            for (var i = 1; i < polygon.Count - 1; i++)
            {
                var a = first;
                var b = polygon[i];
                var c = polygon[i + 1];

                if (IsDegenerateTriangle(a, b, c))
                {
                    continue;
                }

                builtTriangles.Add(new BspChunkPartitioner.BspTriangleFragment(
                    a,
                    b,
                    c,
                    BspUvResolver.ComputeRawUv(a, surface, model, brushPolys),
                    BspUvResolver.ComputeRawUv(b, surface, model, brushPolys),
                    BspUvResolver.ComputeRawUv(c, surface, model, brushPolys)));
                triangleCount++;
            }

            if (builtTriangles.Count == 0)
            {
                continue;
            }

            polygons.Add(new BspChunkPartitioner.BspPolygonFragment(
                nodeIndex,
                surface.MaterialRawReference,
                surface.MaterialReference?.PackageName,
                surface.MaterialReference?.ObjectName,
                surface.PolyFlags,
                node.Zone0,
                node.Zone1,
                node.LeafIndex0,
                node.LeafIndex1,
                CreateColor(surface.MaterialRawReference, polygons.Count),
                polygon.ToArray(),
                normal,
                polygonMin,
                polygonMax,
                builtTriangles.ToArray()));

            polygonCount++;
        }

        if (polygons.Count == 0)
        {
            return null;
        }

        if (polygonCount == 0)
        {
            modelMin = model.BoundsMin;
            modelMax = model.BoundsMax;
        }

        var chunks = BspChunkPartitioner.BuildChunks(model.ObjectName, polygons, BuildMeshParts);

        return new BspDiagnosticModel
        {
            ExportIndex = model.ExportIndex,
            Name = model.ObjectName,
            IsWorldModel = isWorldModel,
            NodeCount = model.Nodes.Length,
            PolygonCount = polygonCount,
            TriangleCount = triangleCount,
            BoundsMin = modelMin,
            BoundsMax = modelMax,
            Chunks = chunks
        };
    }

    private static BspDiagnosticMeshPart[] BuildMeshParts(IEnumerable<BspChunkPartitioner.BspPolygonFragment> polygons)
    {
        var parts = new Dictionary<(int MaterialRawReference, uint PolyFlags), MeshAccumulator>();
        foreach (var polygon in polygons)
        {
            var key = (polygon.MaterialRawReference, polygon.PolyFlags);
            if (!parts.TryGetValue(key, out var accumulator))
            {
                accumulator = new MeshAccumulator(
                    polygon.MaterialRawReference,
                    polygon.MaterialPackageName,
                    polygon.MaterialObjectName,
                    polygon.PolyFlags,
                    polygon.ColorArgb);
                parts.Add(key, accumulator);
            }

            accumulator.AddPolygon(polygon);
        }

        return parts
            .Select(x => x.Value.ToDto())
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static uint CreateColor(int materialRawReference, int colorSeed)
    {
        var seed = materialRawReference == 0 ? colorSeed + 1 : Math.Abs(materialRawReference);
        var hue = seed * 0.61803398875f % 1f;
        var rgb = HsvToRgb(hue, 0.62f, 0.92f);
        return 0xFF000000u | (uint)rgb.r << 16 | (uint)rgb.g << 8 | rgb.b;
    }

    private static (byte r, byte g, byte b) HsvToRgb(float h, float s, float v)
    {
        var i = (int)MathF.Floor(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        var (r, g, b) = (i % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };

        return ((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f));
    }

    private static List<Vector3> RemoveSequentialDuplicates(List<Vector3> polygon)
    {
        var result = new List<Vector3>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            if (i > 0 && Vector3.DistanceSquared(polygon[i - 1], polygon[i]) < 0.0001f)
            {
                continue;
            }

            result.Add(polygon[i]);
        }

        if (result.Count > 1 && Vector3.DistanceSquared(result[0], result[^1]) < 0.0001f)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    private static bool IsDegenerateTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        var cross = Vector3.Cross(b - a, c - a);
        return cross.LengthSquared() < 0.0001f;
    }

    private static Vector3 ComputePolygonNormal(List<Vector3> polygon)
    {
        var normal = Vector3.Zero;
        for (var i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Count];
            normal.X += (current.Y - next.Y) * (current.Z + next.Z);
            normal.Y += (current.Z - next.Z) * (current.X + next.X);
            normal.Z += (current.X - next.X) * (current.Y + next.Y);
        }

        return SafeNormalize(normal);
    }

    private static Vector3 SafeNormalize(Vector3 value)
    {
        if (value.LengthSquared() < 0.000001f)
        {
            return Vector3.UnitZ;
        }

        return Vector3.Normalize(value);
    }

    private sealed class MeshAccumulator
    {
        private readonly List<Vector3> _positions = [];
        private readonly List<Vector3> _normals = [];
        private readonly List<Vector2> _textureCoordinates = [];
        private readonly List<int> _indices = [];

        public MeshAccumulator(
            int materialRawReference,
            string? materialPackageName,
            string? materialObjectName,
            uint polyFlags,
            uint colorArgb)
        {
            MaterialRawReference = materialRawReference;
            MaterialPackageName = materialPackageName;
            MaterialObjectName = materialObjectName;
            PolyFlags = polyFlags;
            ColorArgb = colorArgb;
        }

        public int MaterialRawReference { get; }
        public string? MaterialPackageName { get; }
        public string? MaterialObjectName { get; }
        public uint PolyFlags { get; }
        public uint ColorArgb { get; }
        public int SurfaceCount { get; private set; }

        public void AddPolygon(BspChunkPartitioner.BspPolygonFragment polygon)
        {
            foreach (var triangle in polygon.Triangles)
            {
                var baseIndex = _positions.Count;
                _positions.Add(triangle.A);
                _positions.Add(triangle.B);
                _positions.Add(triangle.C);
                _normals.Add(polygon.Normal);
                _normals.Add(polygon.Normal);
                _normals.Add(polygon.Normal);
                _textureCoordinates.Add(triangle.UvA);
                _textureCoordinates.Add(triangle.UvB);
                _textureCoordinates.Add(triangle.UvC);
                _indices.Add(baseIndex);
                _indices.Add(baseIndex + 1);
                _indices.Add(baseIndex + 2);
            }

            SurfaceCount++;
        }

        public BspDiagnosticMeshPart ToDto()
        {
            return new BspDiagnosticMeshPart
            {
                Name = MaterialRawReference == 0 ? $"SurfaceGroup_{PolyFlags:X8}" : $"Material_{MaterialRawReference}",
                SurfaceCount = SurfaceCount,
                MaterialRawReference = MaterialRawReference,
                MaterialPackageName = MaterialPackageName,
                MaterialObjectName = MaterialObjectName,
                PolyFlags = PolyFlags,
                KnownPolyFlags = UnrPolyFlagsInfo.GetKnownFlags(PolyFlags),
                UnknownPolyFlagsMask = UnrPolyFlagsInfo.GetUnknownBits(PolyFlags),
                PolyFlagNames = UnrPolyFlagsInfo.GetNames(PolyFlags),
                ColorArgb = ColorArgb,
                Positions = _positions.ToArray(),
                Normals = _normals.ToArray(),
                TextureCoordinates = _textureCoordinates.ToArray(),
                Indices = _indices.ToArray()
            };
        }
    }

}
