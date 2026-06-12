using L2Viewer.SceneDomain.Models;
using L2Viewer.PackageCore;

namespace L2Viewer.SceneDomain.Services;

[ForExternalUse]
public sealed class BspPropSceneBuilder
{
    private readonly BspStaticMeshManager _staticMeshManager;

    public BspPropSceneBuilder(BspStaticMeshManager staticMeshManager)
    {
        _staticMeshManager = staticMeshManager;
    }

    public BspDiagnosticPropPlacement[] Build(L2Viewer.UnrFile.UnrFile unr)
    {
        var actors = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrStaticMeshActorObject>()
            .Where(x => x.Location is not null && x.StaticMeshReference is not null)
            .ToList();
        var meshes = _staticMeshManager.ResolveMany(unr.FilePath, actors.Select(x => x.StaticMeshReference!));
        var placements = new List<BspDiagnosticPropPlacement>();
        foreach (var actor in actors)
        {
            var mesh = meshes[SceneReferenceUtilities.BuildReference(unr.FilePath, actor.StaticMeshReference!)];
            if (mesh is null || mesh.Triangles.Count == 0)
            {
                continue;
            }

            var transformed = TransformMesh(mesh, actor);
            var meshRef = actor.StaticMeshReference!;
            placements.Add(new BspDiagnosticPropPlacement
            {
                ExportIndex = actor.ExportIndex,
                ActorName = actor.ObjectName,
                ClassName = actor.ClassName,
                StaticMeshReference = $"{meshRef.PackageName ?? "<map>"}.{meshRef.ObjectName}",
                PrimaryTextureReference = mesh.TextureRef,
                Mesh = transformed,
                BoundsMin = transformed.BBoxMin,
                BoundsMax = transformed.BBoxMax
            });
        }

        return placements
            .OrderBy(x => x.ActorName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static MeshData TransformMesh(MeshData mesh, UnrStaticMeshActorObject actor)
    {
        var triangles = new List<Triangle>(mesh.Triangles.Count);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var tri in mesh.Triangles)
        {
            var a = TransformVertex(tri.A, actor);
            var b = TransformVertex(tri.B, actor);
            var c = TransformVertex(tri.C, actor);
            triangles.Add(new Triangle(a, b, c, tri.MaterialId));
            UpdateBounds(a.Position, ref min, ref max);
            UpdateBounds(b.Position, ref min, ref max);
            UpdateBounds(c.Position, ref min, ref max);
        }

        return new MeshData(
            $"{actor.ObjectName}:{mesh.Name}",
            triangles,
            min,
            max,
            $"{mesh.SourceNote}; placed",
            mesh.TextureRef,
            mesh.UsedTextures);
    }

    private static TriangleVertex TransformVertex(TriangleVertex vertex, UnrStaticMeshActorObject actor)
    {
        var position = TransformActorPoint(vertex.Position, actor);
        var normal = TransformActorNormal(vertex.Normal, actor);
        return new TriangleVertex(position, vertex.UV, normal);
    }

    private static Vector3 TransformActorPoint(Vector3 point, UnrStaticMeshActorObject actor)
    {
        var scale = actor.DrawScale3D * actor.DrawScale;
        var p = new Vector3(
            point.X * scale.X - actor.PrePivot.X,
            point.Y * scale.Y - actor.PrePivot.Y,
            point.Z * scale.Z - actor.PrePivot.Z);
        var rotated = RotateByUnrealRotator(p, actor.Rotation ?? Vector3.Zero);
        return rotated + actor.Location!.Value;
    }

    private static Vector3 TransformActorNormal(Vector3 normal, UnrStaticMeshActorObject actor)
    {
        var scale = actor.DrawScale3D * actor.DrawScale;
        var safeScale = new Vector3(
            Math.Abs(scale.X) < 0.0001f ? 1f : scale.X,
            Math.Abs(scale.Y) < 0.0001f ? 1f : scale.Y,
            Math.Abs(scale.Z) < 0.0001f ? 1f : scale.Z);
        var scaled = Vector3.Normalize(new Vector3(
            normal.X / safeScale.X,
            normal.Y / safeScale.Y,
            normal.Z / safeScale.Z));
        return Vector3.Normalize(RotateByUnrealRotator(scaled, actor.Rotation ?? Vector3.Zero));
    }

    private static Vector3 RotateByUnrealRotator(Vector3 vector, Vector3 rot)
    {
        var pitch = rot.X * (float)(Math.PI / 32768.0);
        var yaw = rot.Y * (float)(Math.PI / 32768.0);
        var roll = rot.Z * (float)(Math.PI / 32768.0);

        var cp = MathF.Cos(pitch);
        var sp = MathF.Sin(pitch);
        var cy = MathF.Cos(yaw);
        var sy = MathF.Sin(yaw);
        var cr = MathF.Cos(roll);
        var sr = MathF.Sin(roll);

        var forward = new Vector3(cp * cy, cp * sy, -sp);
        var right = new Vector3(-sr * sp * cy + cr * sy, -sr * sp * sy - cr * cy, -sr * cp);
        var up = new Vector3(cr * sp * cy + sr * sy, cr * sp * sy - sr * cy, cr * cp);
        return new Vector3(
            forward.X * vector.X - right.X * vector.Y + up.X * vector.Z,
            forward.Y * vector.X - right.Y * vector.Y + up.Y * vector.Z,
            forward.Z * vector.X - right.Z * vector.Y + up.Z * vector.Z);
    }

    private static void UpdateBounds(Vector3 point, ref Vector3 min, ref Vector3 max)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }
}
