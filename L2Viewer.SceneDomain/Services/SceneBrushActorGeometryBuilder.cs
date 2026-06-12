using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

internal static class SceneBrushActorGeometryBuilder
{
    public static SceneBrushActorGeometryData Build(
        string mapPath,
        IReadOnlyDictionary<int, UnrModelObject> modelsByExportIndex,
        IReadOnlyDictionary<int, UnrPolysObject> polysByExportIndex,
        UnrActorBaseObject actor,
        string geometryName)
    {
        if (actor.BrushReference?.ExportIndex is not int brushExportIndex ||
            !modelsByExportIndex.TryGetValue(brushExportIndex, out var model))
        {
            return new SceneBrushActorGeometryData();
        }

        var polysReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, model.PolysReference);
        if (model.PolysReference?.ExportIndex is not int polysExportIndex ||
            !polysByExportIndex.TryGetValue(polysExportIndex, out var polys))
        {
            return new SceneBrushActorGeometryData
            {
                BrushPolysReference = polysReference,
                BrushModelBoundsMin = model.BoundsValid ? model.BoundsMin : null,
                BrushModelBoundsMax = model.BoundsValid ? model.BoundsMax : null,
                BrushModelBoundsValid = model.BoundsValid
            };
        }

        var geometry = BuildGeometry(actor, polys, geometryName);
        return new SceneBrushActorGeometryData
        {
            BrushPolysReference = polysReference,
            BrushModelBoundsMin = model.BoundsValid ? model.BoundsMin : null,
            BrushModelBoundsMax = model.BoundsValid ? model.BoundsMax : null,
            BrushModelBoundsValid = model.BoundsValid,
            RenderGeometry = geometry,
            WorldBoundsMin = geometry?.BoundsMin,
            WorldBoundsMax = geometry?.BoundsMax
        };
    }

    private static SceneTriangleMeshData? BuildGeometry(UnrActorBaseObject actor, UnrPolysObject polys, string geometryName)
    {
        var triangles = new List<Triangle>();
        var materialIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
            if (!materialIds.TryGetValue(materialKey, out var materialId))
            {
                materialId = materialIds.Count;
                materialIds.Add(materialKey, materialId);
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

        if (triangles.Count == 0)
        {
            return null;
        }

        return new SceneTriangleMeshData
        {
            Name = geometryName,
            Triangles = triangles,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            SourceNote = "Brush Polys"
        };
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

internal sealed class SceneBrushActorGeometryData
{
    public string? BrushPolysReference { get; init; }
    public Vector3? BrushModelBoundsMin { get; init; }
    public Vector3? BrushModelBoundsMax { get; init; }
    public bool BrushModelBoundsValid { get; init; }
    public SceneTriangleMeshData? RenderGeometry { get; init; }
    public Vector3? WorldBoundsMin { get; init; }
    public Vector3? WorldBoundsMax { get; init; }
}
