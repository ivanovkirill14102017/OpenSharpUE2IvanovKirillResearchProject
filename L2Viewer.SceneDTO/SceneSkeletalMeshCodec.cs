using System.Numerics;
using L2Viewer.PackageCore;
using L2Viewer.UkxFile;

namespace L2Viewer.SceneDTO;

public static class SceneSkeletalMeshCodec
{
    public static MeshData? LoadSkeletalMeshNamed(
        string packagePath,
        string meshName)
    {
        var ukx = UkxFileReader.Read(packagePath);
        var mesh = ukx.ExportObjects
            .Select(x => x.Object)
            .OfType<UkxSkeletalMeshObject>()
            .FirstOrDefault(x => x.ObjectName.Is(meshName));
        return mesh is null ? null : DecodeSkeletalMesh(mesh, packagePath);
    }

    public static MeshData? DecodeSkeletalMeshExport(
        PackageData package,
        int exportIndex)
    {
        var ukx = UkxFileReader.Read(package.Path);
        var mesh = ukx.ExportObjects
            .FirstOrDefault(x => x.Export.Index == exportIndex)
            ?.Object as UkxSkeletalMeshObject;
        return mesh is null ? null : DecodeSkeletalMesh(mesh, package.Path);
    }

    public static MeshData? DecodeSkeletalMesh(UkxSkeletalMeshObject mesh, string packagePath)
    {
        var triangles = BuildTriangles(mesh);
        if (triangles.Count == 0)
        {
            return null;
        }

        var bboxMin = new Vector3(float.MaxValue);
        var bboxMax = new Vector3(float.MinValue);
        foreach (var triangle in triangles)
        {
            UpdateBounds(triangle.A.Position, ref bboxMin, ref bboxMax);
            UpdateBounds(triangle.B.Position, ref bboxMin, ref bboxMax);
            UpdateBounds(triangle.C.Position, ref bboxMin, ref bboxMax);
        }

        var (primaryTextureRef, usedTextures) = ResolveTextureReferences(mesh, packagePath);

        return new MeshData(
            mesh.ObjectName,
            triangles,
            bboxMin,
            bboxMax,
            string.Empty,
            primaryTextureRef,
            usedTextures);
    }

    private static IReadOnlyList<Triangle> BuildTriangles(UkxSkeletalMeshObject mesh)
    {
        if (mesh.Version <= 1)
        {
            return BuildTrianglesFromLegacy(mesh.BasePoints, mesh.LegacyWedges, mesh.LegacyFaces, mesh.MeshScale, mesh.MeshOrigin);
        }

        var triangles = new List<Triangle>();
        if (mesh.LodModels.Length > 0)
        {
            triangles.AddRange(BuildTrianglesFromLod(mesh.LodModels[0], mesh.MeshScale, mesh.MeshOrigin));
        }

        if (triangles.Count > 0)
        {
            return triangles;
        }

        if (mesh.BaseLodPoints.Length > 0 && mesh.BaseLodWedges.Length > 0 && mesh.BaseLodTriangles.Length > 0)
        {
            triangles.AddRange(BuildTrianglesFromBaseLod(mesh.BaseLodPoints, mesh.BaseLodWedges, mesh.BaseLodTriangles, mesh.MeshScale, mesh.MeshOrigin));
        }

        return triangles;
    }

    private static IReadOnlyList<Triangle> BuildTrianglesFromLod(UkxSkeletalLodModel lod, Vector3 meshScale, Vector3 meshOrigin)
    {
        var result = new List<Triangle>();

        if (lod.LineageWedges.Length > 0)
        {
            AppendSectionTriangles(result, lod.SoftSections, lod.SoftIndices.Indices, lod.LineageWedges, meshScale, meshOrigin);
        }

        if (lod.VertexStream.Vertices.Length > 0)
        {
            AppendSectionTriangles(result, lod.RigidSections, lod.RigidIndices.Indices, lod.VertexStream.Vertices, meshScale, meshOrigin);
        }

        if (result.Count > 0)
        {
            return result;
        }

        return BuildTrianglesFromFaces(lod.Points, lod.Wedges, lod.Faces, meshScale, meshOrigin);
    }

    private static void AppendSectionTriangles(
        List<Triangle> target,
        IReadOnlyList<UkxSkelMeshSection> sections,
        IReadOnlyList<ushort> indices,
        IReadOnlyList<UkxLineageWedge> wedges,
        Vector3 meshScale,
        Vector3 meshOrigin)
    {
        foreach (var section in sections)
        {
            for (var faceIndex = 0; faceIndex < section.NumFaces; faceIndex++)
            {
                var baseIndex = (section.FirstFace + faceIndex) * 3;
                if (baseIndex + 2 >= indices.Count)
                {
                    continue;
                }

                var a = indices[baseIndex];
                var b = indices[baseIndex + 1];
                var c = indices[baseIndex + 2];
                if (a >= wedges.Count || b >= wedges.Count || c >= wedges.Count)
                {
                    continue;
                }

                var va = CreateVertex(wedges[a], meshScale, meshOrigin);
                var vb = CreateVertex(wedges[b], meshScale, meshOrigin);
                var vc = CreateVertex(wedges[c], meshScale, meshOrigin);
                var normal = ComputeFaceNormal(va.Position, vb.Position, vc.Position);
                target.Add(new Triangle(
                    va with { Normal = normal },
                    vb with { Normal = normal },
                    vc with { Normal = normal },
                    section.MaterialIndex));
            }
        }
    }

    private static void AppendSectionTriangles(
        List<Triangle> target,
        IReadOnlyList<UkxSkelMeshSection> sections,
        IReadOnlyList<ushort> indices,
        IReadOnlyList<UkxAnimMeshVertex> vertices,
        Vector3 meshScale,
        Vector3 meshOrigin)
    {
        foreach (var section in sections)
        {
            for (var faceIndex = 0; faceIndex < section.NumFaces; faceIndex++)
            {
                var baseIndex = (section.FirstFace + faceIndex) * 3;
                if (baseIndex + 2 >= indices.Count)
                {
                    continue;
                }

                var a = indices[baseIndex];
                var b = indices[baseIndex + 1];
                var c = indices[baseIndex + 2];
                if (a >= vertices.Count || b >= vertices.Count || c >= vertices.Count)
                {
                    continue;
                }

                var va = CreateVertex(vertices[a], meshScale, meshOrigin);
                var vb = CreateVertex(vertices[b], meshScale, meshOrigin);
                var vc = CreateVertex(vertices[c], meshScale, meshOrigin);
                var normal = ComputeFaceNormal(va.Position, vb.Position, vc.Position);
                target.Add(new Triangle(
                    va with { Normal = normal },
                    vb with { Normal = normal },
                    vc with { Normal = normal },
                    section.MaterialIndex));
            }
        }
    }

    private static IReadOnlyList<Triangle> BuildTrianglesFromLegacy(
        IReadOnlyList<Vector3> points,
        IReadOnlyList<UkxMeshWedge> wedges,
        IReadOnlyList<UkxMeshFace> faces,
        Vector3 meshScale,
        Vector3 meshOrigin)
    {
        return BuildTrianglesFromFaces(points, wedges, faces, meshScale, meshOrigin);
    }

    private static IReadOnlyList<Triangle> BuildTrianglesFromFaces(
        IReadOnlyList<Vector3> points,
        IReadOnlyList<UkxMeshWedge> wedges,
        IReadOnlyList<UkxMeshFace> faces,
        Vector3 meshScale,
        Vector3 meshOrigin)
    {
        var result = new List<Triangle>(faces.Count);
        foreach (var face in faces)
        {
            if (face.WedgeIndex0 >= wedges.Count || face.WedgeIndex1 >= wedges.Count || face.WedgeIndex2 >= wedges.Count)
            {
                continue;
            }

            var wa = wedges[face.WedgeIndex0];
            var wb = wedges[face.WedgeIndex1];
            var wc = wedges[face.WedgeIndex2];
            if (wa.VertexIndex >= points.Count || wb.VertexIndex >= points.Count || wc.VertexIndex >= points.Count)
            {
                continue;
            }

            var pa = TransformPosition(points[wa.VertexIndex], meshScale, meshOrigin);
            var pb = TransformPosition(points[wb.VertexIndex], meshScale, meshOrigin);
            var pc = TransformPosition(points[wc.VertexIndex], meshScale, meshOrigin);
            var normal = ComputeFaceNormal(pa, pb, pc);
            result.Add(new Triangle(
                new TriangleVertex(pa, wa.UV, normal),
                new TriangleVertex(pb, wb.UV, normal),
                new TriangleVertex(pc, wc.UV, normal),
                face.MaterialIndex));
        }

        return result;
    }

    private static IReadOnlyList<Triangle> BuildTrianglesFromBaseLod(
        IReadOnlyList<Vector3> points,
        IReadOnlyList<UkxMeshWedge> wedges,
        IReadOnlyList<UkxTriangle> triangles,
        Vector3 meshScale,
        Vector3 meshOrigin)
    {
        var result = new List<Triangle>(triangles.Count);
        foreach (var triangle in triangles)
        {
            if (triangle.WedgeIndex0 >= wedges.Count || triangle.WedgeIndex1 >= wedges.Count || triangle.WedgeIndex2 >= wedges.Count)
            {
                continue;
            }

            var wa = wedges[triangle.WedgeIndex0];
            var wb = wedges[triangle.WedgeIndex1];
            var wc = wedges[triangle.WedgeIndex2];
            if (wa.VertexIndex >= points.Count || wb.VertexIndex >= points.Count || wc.VertexIndex >= points.Count)
            {
                continue;
            }

            var pa = TransformPosition(points[wa.VertexIndex], meshScale, meshOrigin);
            var pb = TransformPosition(points[wb.VertexIndex], meshScale, meshOrigin);
            var pc = TransformPosition(points[wc.VertexIndex], meshScale, meshOrigin);
            var normal = ComputeFaceNormal(pa, pb, pc);
            result.Add(new Triangle(
                new TriangleVertex(pa, wa.UV, normal),
                new TriangleVertex(pb, wb.UV, normal),
                new TriangleVertex(pc, wc.UV, normal),
                triangle.MaterialIndex));
        }

        return result;
    }

    private static TriangleVertex CreateVertex(UkxLineageWedge wedge, Vector3 meshScale, Vector3 meshOrigin)
    {
        return new TriangleVertex(
            TransformPosition(wedge.Position, meshScale, meshOrigin),
            wedge.UV,
            NormalizeSafe(wedge.Normal));
    }

    private static TriangleVertex CreateVertex(UkxAnimMeshVertex vertex, Vector3 meshScale, Vector3 meshOrigin)
    {
        return new TriangleVertex(
            TransformPosition(vertex.Position, meshScale, meshOrigin),
            vertex.UV,
            NormalizeSafe(vertex.Normal));
    }

    private static (string? TextureRef, IReadOnlyList<MaterialTextureInfo> UsedTextures) ResolveTextureReferences(
        UkxSkeletalMeshObject mesh,
        string packagePath)
    {
        var usedTextures = new List<MaterialTextureInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? primaryTextureRef = null;

        foreach (var material in mesh.Materials)
        {
            if (material.TextureIndex < 0 || material.TextureIndex >= mesh.TextureReferences.Length)
            {
                continue;
            }

            var reference = mesh.TextureReferences[material.TextureIndex];
            if (reference is null)
            {
                continue;
            }

            if (!reference.ClassName.EndsWith(UnrealClassNames.Texture, StringComparison.OrdinalIgnoreCase) &&
                !reference.ClassName.Is(UnrealClassNames.Texture))
            {
                continue;
            }

            var packageName = reference.PackageName ?? Path.GetFileNameWithoutExtension(packagePath);
            var resolvedPath = reference.ExportIndex is not null ? packagePath : null;
            var resourceRef = $"{packageName}.{reference.ObjectName}";
            if (!seen.Add(resourceRef))
            {
                continue;
            }

            usedTextures.Add(new MaterialTextureInfo(resourceRef, resolvedPath));
            primaryTextureRef ??= resourceRef;
        }

        return (primaryTextureRef, usedTextures);
    }

    private static Vector3 TransformPosition(Vector3 value, Vector3 meshScale, Vector3 meshOrigin)
    {
        return new Vector3(
            value.X * meshScale.X + meshOrigin.X,
            value.Y * meshScale.Y + meshOrigin.Y,
            value.Z * meshScale.Z + meshOrigin.Z);
    }

    private static Vector3 NormalizeSafe(Vector3 normal)
    {
        var lengthSq = normal.LengthSquared();
        return lengthSq > 1e-12f ? Vector3.Normalize(normal) : Vector3.UnitZ;
    }

    private static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Cross(b - a, c - a);
        return NormalizeSafe(normal);
    }

    private static void UpdateBounds(Vector3 point, ref Vector3 min, ref Vector3 max)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }
}
