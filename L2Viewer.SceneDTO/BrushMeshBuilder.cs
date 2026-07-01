using System.Numerics;
using L2Viewer.PackageCore;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDTO;

public static class BrushMeshBuilder
{
    public static MeshData? BuildBrushActorMesh(UnrActorBaseObject actor, UnrPolysObject polys, string geometryName)
    {
        var triangles = new List<Triangle>();
        var materials = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var boundsMin = new Vector3(float.MaxValue);
        var boundsMax = new Vector3(float.MinValue);
        var transform = BuildTransform(actor);
        var prePivot = actor.PrePivot;

        foreach (var poly in polys.Polys)
        {
            if (poly.NumVertices < 3)
            {
                continue;
            }

            var materialKey = poly.TextureReference is null
                ? "null"
                : $"{poly.TextureReference.PackageName}.{poly.TextureReference.ObjectName}";
            if (!materials.TryGetValue(materialKey, out var materialId))
            {
                materialId = materials.Count;
                materials.Add(materialKey, materialId);
            }

            for (var i = 2; i < poly.NumVertices; i++)
            {
                var p0 = Vector3.Transform(poly.Vertices[0] - prePivot, transform);
                var p1 = Vector3.Transform(poly.Vertices[i - 1] - prePivot, transform);
                var p2 = Vector3.Transform(poly.Vertices[i] - prePivot, transform);
                var normal = Vector3.Normalize(Vector3.TransformNormal(poly.Normal, transform));

                triangles.Add(new Triangle(
                    new TriangleVertex(p0, Vector2.Zero, normal),
                    new TriangleVertex(p1, Vector2.Zero, normal),
                    new TriangleVertex(p2, Vector2.Zero, normal),
                    materialId));

                boundsMin = Vector3.Min(boundsMin, p0);
                boundsMin = Vector3.Min(boundsMin, p1);
                boundsMin = Vector3.Min(boundsMin, p2);
                boundsMax = Vector3.Max(boundsMax, p0);
                boundsMax = Vector3.Max(boundsMax, p1);
                boundsMax = Vector3.Max(boundsMax, p2);
            }
        }

        return triangles.Count == 0
            ? null
            : new MeshData(geometryName, triangles, boundsMin, boundsMax, "Brush Polys", null, []);
    }

    public static BrushPolyMeshBuildResult? BuildBrushPolyMesh(
        UnrPolysObject polys,
        Matrix4x4 transform,
        Vector3 prePivot,
        string name)
    {
        var triangles = new List<Triangle>();
        var materials = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var materialCounts = new Dictionary<int, int>();

        var boundsMin = new Vector3(float.MaxValue);
        var boundsMax = new Vector3(float.MinValue);

        foreach (var poly in polys.Polys)
        {
            if (poly.NumVertices < 3)
            {
                continue;
            }

            var materialKey = poly.TextureReference is null
                ? "null"
                : $"{poly.TextureReference.PackageName}.{poly.TextureReference.ObjectName}";
            if (!materials.TryGetValue(materialKey, out var materialId))
            {
                materialId = materials.Count;
                materials.Add(materialKey, materialId);
            }

            for (var v = 2; v < poly.NumVertices; v++)
            {
                var p0 = poly.Vertices[0];
                var p1 = poly.Vertices[v - 1];
                var p2 = poly.Vertices[v];

                var t0 = new Vector2(Vector3.Dot(p0 - poly.Base, poly.TextureU), Vector3.Dot(p0 - poly.Base, poly.TextureV));
                var t1 = new Vector2(Vector3.Dot(p1 - poly.Base, poly.TextureU), Vector3.Dot(p1 - poly.Base, poly.TextureV));
                var t2 = new Vector2(Vector3.Dot(p2 - poly.Base, poly.TextureU), Vector3.Dot(p2 - poly.Base, poly.TextureV));

                var tp0 = Vector3.Transform(p0 - prePivot, transform);
                var tp1 = Vector3.Transform(p1 - prePivot, transform);
                var tp2 = Vector3.Transform(p2 - prePivot, transform);
                var normal = Vector3.Normalize(Vector3.TransformNormal(poly.Normal, transform));

                triangles.Add(new Triangle(
                    new TriangleVertex(tp0, t0, normal),
                    new TriangleVertex(tp1, t1, normal),
                    new TriangleVertex(tp2, t2, normal),
                    materialId));
                materialCounts[materialId] = materialCounts.GetValueOrDefault(materialId) + 1;

                boundsMin = Vector3.Min(boundsMin, tp0);
                boundsMin = Vector3.Min(boundsMin, tp1);
                boundsMin = Vector3.Min(boundsMin, tp2);
                boundsMax = Vector3.Max(boundsMax, tp0);
                boundsMax = Vector3.Max(boundsMax, tp1);
                boundsMax = Vector3.Max(boundsMax, tp2);
            }
        }

        if (triangles.Count == 0)
        {
            return null;
        }

        var mesh = new MeshData(name, triangles.ToArray(), boundsMin, boundsMax, "Brush Polys", null, []);
        var slots = materials
            .OrderBy(x => x.Value)
            .Select(x => new BrushPolyMaterialSlot(x.Value, x.Key, materialCounts.GetValueOrDefault(x.Value)))
            .ToArray();
        return new BrushPolyMeshBuildResult(mesh, slots);
    }

    private static Matrix4x4 BuildTransform(UnrActorBaseObject actor)
    {
        return Matrix4x4.CreateScale(actor.DrawScale3D * actor.DrawScale) *
               Matrix4x4.CreateFromYawPitchRoll(
                   (actor.Rotation?.Y ?? 0f) * (MathF.PI / 32768f),
                   (actor.Rotation?.X ?? 0f) * (MathF.PI / 32768f),
                   (actor.Rotation?.Z ?? 0f) * (MathF.PI / 32768f)) *
               Matrix4x4.CreateTranslation(actor.Location ?? Vector3.Zero);
    }
}

public sealed record BrushPolyMaterialSlot(int MaterialId, string MaterialReferenceKey, int TriangleCount);

public sealed record BrushPolyMeshBuildResult(MeshData Mesh, IReadOnlyList<BrushPolyMaterialSlot> MaterialSlots);
