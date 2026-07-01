using System.Numerics;
using L2Viewer.PackageCore;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDTO;

public static class MoverMeshBuilder
{
    public static MoverMeshBuildResult? BuildMoverMesh(
        string moverName,
        UnrModelObject model,
        UnrPolysObject? brushPolys,
        Matrix4x4 transform,
        Vector3 prePivot)
    {
        var triangles = new List<Triangle>();
        var materials = new Dictionary<(int RawReference, uint PolyFlags), int>();
        var materialSlots = new Dictionary<int, MoverMaterialSlot>();

        var modelMin = new Vector3(float.MaxValue);
        var modelMax = new Vector3(float.MinValue);

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
            const uint PF_Invisible = 0x00000001;
            if ((surface.PolyFlags & PF_Invisible) != 0)
            {
                continue;
            }

            var materialKey = (surface.MaterialRawReference, surface.PolyFlags);
            if (!materials.TryGetValue(materialKey, out var materialId))
            {
                materialId = materials.Count;
                materials.Add(materialKey, materialId);
                materialSlots[materialId] = new MoverMaterialSlot(
                    materialId,
                    surface.MaterialRawReference,
                    surface.PolyFlags,
                    surface.MaterialReference?.PackageName,
                    surface.MaterialReference?.ObjectName,
                    0);
            }

            var normal = SafeNormalize(new Vector3(node.Plane.X, node.Plane.Y, node.Plane.Z));
            if (normal.LengthSquared() < 0.000001f)
            {
                normal = ComputePolygonNormal(polygon);
            }

            var worldNormal = Vector3.Normalize(Vector3.TransformNormal(normal, transform));

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

                var uvA = BspUvResolver.ComputeRawUv(a, surface, model, brushPolys);
                var uvB = BspUvResolver.ComputeRawUv(b, surface, model, brushPolys);
                var uvC = BspUvResolver.ComputeRawUv(c, surface, model, brushPolys);

                var worldA = Vector3.Transform(a - prePivot, transform);
                var worldB = Vector3.Transform(b - prePivot, transform);
                var worldC = Vector3.Transform(c - prePivot, transform);

                triangles.Add(new Triangle(
                    new TriangleVertex(worldA, uvA, worldNormal),
                    new TriangleVertex(worldB, uvB, worldNormal),
                    new TriangleVertex(worldC, uvC, worldNormal),
                    materialId));

                materialSlots[materialId] = materialSlots[materialId] with
                {
                    TriangleCount = materialSlots[materialId].TriangleCount + 1
                };

                modelMin = Vector3.Min(modelMin, worldA);
                modelMin = Vector3.Min(modelMin, worldB);
                modelMin = Vector3.Min(modelMin, worldC);
                modelMax = Vector3.Max(modelMax, worldA);
                modelMax = Vector3.Max(modelMax, worldB);
                modelMax = Vector3.Max(modelMax, worldC);
            }
        }

        if (triangles.Count == 0)
        {
            return null;
        }

        var mesh = new MeshData(moverName, triangles.ToArray(), modelMin, modelMax, "Mover BSP", null, []);
        return new MoverMeshBuildResult(mesh, materialSlots.Values.OrderBy(x => x.MaterialId).ToArray());
    }

    private static List<Vector3> RemoveSequentialDuplicates(List<Vector3> points)
    {
        if (points.Count == 0)
        {
            return points;
        }

        var result = new List<Vector3>(points.Count) { points[0] };
        for (var i = 1; i < points.Count; i++)
        {
            if (Vector3.DistanceSquared(points[i], result[^1]) > 0.0001f)
            {
                result.Add(points[i]);
            }
        }

        if (result.Count > 1 && Vector3.DistanceSquared(result[0], result[^1]) <= 0.0001f)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    private static Vector3 SafeNormalize(Vector3 value)
    {
        var lenSq = value.LengthSquared();
        if (lenSq < 0.000001f)
        {
            return Vector3.Zero;
        }

        return value / MathF.Sqrt(lenSq);
    }

    private static Vector3 ComputePolygonNormal(List<Vector3> polygon)
    {
        var normal = Vector3.Zero;
        for (var i = 0; i < polygon.Count; i++)
        {
            var j = (i + 1) % polygon.Count;
            normal += Vector3.Cross(polygon[i], polygon[j]);
        }

        return SafeNormalize(normal);
    }

    private static bool IsDegenerateTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        var ab = b - a;
        var ac = c - a;
        return Vector3.Cross(ab, ac).LengthSquared() < 0.0001f;
    }
}

public sealed record MoverMaterialSlot(
    int MaterialId,
    int MaterialRawReference,
    uint PolyFlags,
    string? PackageName,
    string? ObjectName,
    int TriangleCount);

public sealed record MoverMeshBuildResult(MeshData Mesh, IReadOnlyList<MoverMaterialSlot> MaterialSlots);
