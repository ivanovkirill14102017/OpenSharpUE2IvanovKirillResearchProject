using System.Numerics;
using L2Viewer.SceneDomain.Models;
using L2Viewer.UnrFile;
using L2Viewer.UsxFile;

namespace L2Viewer.SceneDomain.Services;

[ForExternalUse]
public sealed class SceneMoverBuilder
{
    private readonly string? _clientRoot;
    private readonly SceneMaterialResolver? _materialResolver;
    private readonly BspTextureManager? _textureManager;

    public SceneMoverBuilder()
    {
    }

    public SceneMoverBuilder(string clientRoot, SceneMaterialResolver materialResolver, BspTextureManager textureManager)
    {
        _clientRoot = clientRoot;
        _materialResolver = materialResolver;
        _textureManager = textureManager;
    }

    public SceneMoverScene Build(L2Viewer.UnrFile.UnrFile unr)
    {
        var movers = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrMoverObject>()
            .Where(x => x.BrushReference?.ExportIndex is not null)
            .ToList();
        var materialRequests = new List<SceneMaterialRequest>();
        var textureRequests = new List<SceneTextureRequest>();
        foreach (var mover in movers)
        {
            var brushExportIndex = mover.BrushReference!.ExportIndex!.Value;
            var modelObj = unr.ExportObjects.FirstOrDefault(x => x.Export.Index == brushExportIndex)?.Object as UnrModelObject;
            if (modelObj is null)
            {
                continue;
            }

            foreach (var surface in modelObj.Surfaces)
            {
                if (string.IsNullOrWhiteSpace(surface.MaterialReference?.ObjectName))
                {
                    continue;
                }

                materialRequests.Add(new SceneMaterialRequest(surface.MaterialReference.PackageName, surface.MaterialReference.ObjectName));
                if (!string.IsNullOrWhiteSpace(surface.MaterialReference.PackageName))
                {
                    textureRequests.Add(new SceneTextureRequest(surface.MaterialReference.PackageName, surface.MaterialReference.ObjectName));
                }
            }
        }
        var materialLookup = _materialResolver is null
            ? new Dictionary<string, ResolvedMaterialGraph?>(StringComparer.OrdinalIgnoreCase)
            : _materialResolver.ResolveMany(unr.FilePath, materialRequests);
        var directTextureLookup = _textureManager is null
            ? new Dictionary<string, BspTextureManager.ResolvedTexture>(StringComparer.OrdinalIgnoreCase)
            : _textureManager.ResolveMany(textureRequests);

        var builtMovers = new List<SceneMover>(movers.Count);
        var sceneMin = new Vector3(float.MaxValue);
        var sceneMax = new Vector3(float.MinValue);

        foreach (var mover in movers)
        {
            var brushExportIndex = mover.BrushReference!.ExportIndex!.Value;
            var modelObj = unr.ExportObjects.FirstOrDefault(x => x.Export.Index == brushExportIndex)?.Object as UnrModelObject;
            if (modelObj is null)
            {
                continue;
            }

            var transform = Matrix4x4.CreateScale(mover.DrawScale3D * mover.DrawScale) *
                            Matrix4x4.CreateFromYawPitchRoll(
                                (mover.Rotation?.Y ?? 0) * (MathF.PI / 32768f),
                                (mover.Rotation?.X ?? 0) * (MathF.PI / 32768f),
                                (mover.Rotation?.Z ?? 0) * (MathF.PI / 32768f)) *
                            Matrix4x4.CreateTranslation(mover.Location ?? Vector3.Zero);
            var prePivot = mover.PrePivot;
            var built = BuildMover(unr.FilePath, mover.ObjectName, mover.ExportIndex, modelObj, transform, prePivot, materialLookup, directTextureLookup);
            if (built is null)
            {
                continue;
            }

            builtMovers.Add(built);
            sceneMin = Vector3.Min(sceneMin, built.WorldBoundsMin);
            sceneMax = Vector3.Max(sceneMax, built.WorldBoundsMax);
        }

        if (builtMovers.Count == 0)
        {
            sceneMin = Vector3.Zero;
            sceneMax = Vector3.Zero;
        }

        return new SceneMoverScene
        {
            SourcePath = unr.FilePath,
            WorldBoundsMin = sceneMin,
            WorldBoundsMax = sceneMax,
            Movers = builtMovers.ToArray()
        };
    }

    private SceneMover? BuildMover(
        string mapPath,
        string moverName,
        int exportIndex,
        UnrModelObject model,
        Matrix4x4 transform,
        Vector3 prePivot,
        IReadOnlyDictionary<string, ResolvedMaterialGraph?> materialLookup,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> directTextureLookup)
    {
        var triangles = new List<Triangle>();
        var materials = new Dictionary<(int RawReference, uint PolyFlags), int>();
        var subMeshes = new List<SceneMoverSubMesh>();

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

            var matKey = (surface.MaterialRawReference, surface.PolyFlags);
            if (!materials.TryGetValue(matKey, out var materialId))
            {
                materialId = materials.Count;
                materials.Add(matKey, materialId);
                
                var materialReference = string.IsNullOrWhiteSpace(surface.MaterialReference?.ObjectName)
                    ? $"<null>.MaterialRaw_{surface.MaterialRawReference}"
                    : SceneReferenceUtilities.BuildReference(mapPath, surface.MaterialReference.PackageName, surface.MaterialReference.ObjectName);
                var material = string.IsNullOrWhiteSpace(surface.MaterialReference?.ObjectName) || _materialResolver is null
                    ? null
                    : materialLookup.GetValueOrDefault(materialReference);
                var materialResource = material is null ? null : BuildMaterialResource(material);
                var directTexture = _textureManager is not null &&
                                    material is null &&
                                    !string.IsNullOrWhiteSpace(surface.MaterialReference?.PackageName) &&
                                    !string.IsNullOrWhiteSpace(surface.MaterialReference?.ObjectName)
                    ? directTextureLookup.GetValueOrDefault($"{surface.MaterialReference.PackageName}.{surface.MaterialReference.ObjectName}")
                    : null;
                var orderedTextureSlots = material is null
                    ? []
                    : MaterialTextureSlotOrdering.OrderTextureSlots(material.TextureSlots);
                var primaryTextureReference = orderedTextureSlots.FirstOrDefault()?.Reference ?? (directTexture is null ? null : SceneReferenceUtilities.BuildReference(mapPath, surface.MaterialReference!.PackageName, surface.MaterialReference!.ObjectName));
                var primaryTextureResource = orderedTextureSlots.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.PackagePath)) is { } slot
                    ? SceneReferenceUtilities.BuildResourceLocation(_clientRoot!, slot.PackagePath!, slot.PackageName, slot.ObjectName, slot.ClassName)
                    : (directTexture?.Resource);

                subMeshes.Add(new SceneMoverSubMesh
                {
                    MaterialId = materialId,
                    TriangleCount = 0, // We will update this later
                    MaterialReference = materialReference,
                    MaterialResource = materialResource,
                    Material = material,
                    PrimaryTextureReference = primaryTextureReference,
                    PrimaryTextureResource = primaryTextureResource,
                    PolyFlags = surface.PolyFlags,
                    ColorArgb = CreateColor(surface.MaterialRawReference, materialId)
                });
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

                var uvA = ComputeRawUv(a, surface, model);
                var uvB = ComputeRawUv(b, surface, model);
                var uvC = ComputeRawUv(c, surface, model);

                var worldA = Vector3.Transform(a - prePivot, transform);
                var worldB = Vector3.Transform(b - prePivot, transform);
                var worldC = Vector3.Transform(c - prePivot, transform);

                triangles.Add(new Triangle(
                    new TriangleVertex(worldA, uvA, worldNormal),
                    new TriangleVertex(worldB, uvB, worldNormal),
                    new TriangleVertex(worldC, uvC, worldNormal),
                    materialId));

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

        var finalSubMeshes = new List<SceneMoverSubMesh>(subMeshes.Count);
        foreach (var subMesh in subMeshes)
        {
            var count = triangles.Count(x => x.MaterialId == subMesh.MaterialId);
            if (count > 0)
            {
                finalSubMeshes.Add(new SceneMoverSubMesh
                {
                    MaterialId = subMesh.MaterialId,
                    TriangleCount = count,
                    MaterialReference = subMesh.MaterialReference,
                    MaterialResource = subMesh.MaterialResource,
                    Material = subMesh.Material,
                    PrimaryTextureReference = subMesh.PrimaryTextureReference,
                    PrimaryTextureResource = subMesh.PrimaryTextureResource,
                    PolyFlags = subMesh.PolyFlags,
                    ColorArgb = subMesh.ColorArgb
                });
            }
        }

        return new SceneMover
        {
            ExportIndex = exportIndex,
            Name = moverName,
            RenderGeometry = new SceneTriangleMeshData
            {
                Name = moverName,
                Triangles = triangles.ToArray(),
                BoundsMin = modelMin,
                BoundsMax = modelMax,
                SourceNote = "Mover BSP"
            },
            SubMeshes = finalSubMeshes.ToArray(),
            WorldBoundsMin = modelMin,
            WorldBoundsMax = modelMax
        };
    }

    private static Vector2 ComputeRawUv(Vector3 point, UnrModelSurface surface, UnrModelObject model)
    {
        var pBase = surface.BaseIndex >= 0 && surface.BaseIndex < model.Vectors.Length ? model.Vectors[surface.BaseIndex] : Vector3.Zero;
        var vTextureU = surface.TextureUIndex >= 0 && surface.TextureUIndex < model.Vectors.Length ? model.Vectors[surface.TextureUIndex] : Vector3.UnitX;
        var vTextureV = surface.TextureVIndex >= 0 && surface.TextureVIndex < model.Vectors.Length ? model.Vectors[surface.TextureVIndex] : Vector3.UnitY;
        return new Vector2(Vector3.Dot(point - pBase, vTextureU), Vector3.Dot(point - pBase, vTextureV));
    }

    private static List<Vector3> RemoveSequentialDuplicates(List<Vector3> points)
    {
        if (points.Count == 0) return points;
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

    private static Vector3 SafeNormalize(Vector3 v)
    {
        var lenSq = v.LengthSquared();
        if (lenSq < 0.000001f)
        {
            return Vector3.Zero;
        }
        return v / MathF.Sqrt(lenSq);
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

    private static uint CreateColor(int materialRawReference, int colorSeed)
    {
        var seed = materialRawReference == 0 ? colorSeed + 1 : Math.Abs(materialRawReference);
        var hue = (seed * 0.61803398875f) % 1f;
        var i = (int)MathF.Floor(hue * 6f);
        var f = hue * 6f - i;
        var p = 0.92f * (1f - 0.62f);
        var q = 0.92f * (1f - f * 0.62f);
        var t = 0.92f * (1f - (1f - f) * 0.62f);

        var (r, g, b) = (i % 6) switch
        {
            0 => ((byte)(0.92f * 255), (byte)(t * 255), (byte)(p * 255)),
            1 => ((byte)(q * 255), (byte)(0.92f * 255), (byte)(p * 255)),
            2 => ((byte)(p * 255), (byte)(0.92f * 255), (byte)(t * 255)),
            3 => ((byte)(p * 255), (byte)(q * 255), (byte)(0.92f * 255)),
            4 => ((byte)(t * 255), (byte)(p * 255), (byte)(0.92f * 255)),
            _ => ((byte)(0.92f * 255), (byte)(p * 255), (byte)(q * 255)),
        };
        return 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    private SceneResourceLocation BuildMaterialResource(ResolvedMaterialGraph material)
    {
        if (string.IsNullOrWhiteSpace(_clientRoot))
        {
            throw new PackageReadException("Material resource resolution requires a client root.");
        }

        var rootNode = material.Nodes.FirstOrDefault(x => string.Equals(x.Reference, material.RootReference, StringComparison.OrdinalIgnoreCase))
            ?? material.Nodes.FirstOrDefault()
            ?? throw new PackageReadException($"Material '{material.RootReference}' has no graph nodes.");
        if (string.IsNullOrWhiteSpace(rootNode.PackagePath))
        {
            throw new PackageReadException($"Material '{material.RootReference}' has no package path.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            _clientRoot,
            rootNode.PackagePath,
            material.RootPackageName,
            material.RootObjectName,
            rootNode.ClassName);
    }
}
