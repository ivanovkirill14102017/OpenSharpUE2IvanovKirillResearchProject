using System.Text;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services.Utility;

public sealed class SceneActorPreviewDiagnosticBuilder
{
    private readonly SceneStaticMeshResolver _staticMeshResolver;

    public SceneActorPreviewDiagnosticBuilder(SceneStaticMeshResolver staticMeshResolver)
    {
        _staticMeshResolver = staticMeshResolver;
    }

    public SceneActorPreviewDiagnosticData? Build(string packagePath, UnrFile.UnrFile unr, int exportIndex)
    {
        var actor = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrActorBaseObject>()
            .FirstOrDefault(x => x.ExportIndex == exportIndex);
        if (actor is null)
        {
            return null;
        }

        var rotationRaw = actor.Rotation;
        Vector3? rotationEuler = rotationRaw is null
            ? null
            : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value);
        var bounds = ResolveBounds(packagePath, unr, actor, rotationEuler);

        return new SceneActorPreviewDiagnosticData
        {
            ExportIndex = actor.ExportIndex,
            Name = actor.ObjectName,
            ClassName = actor.ClassName,
            WorldLocation = actor.Location,
            UnrealRotationRaw = rotationRaw,
            RotationEulerDegrees = rotationEuler,
            BoundsMin = bounds?.Min,
            BoundsMax = bounds?.Max,
            PropertiesText = BuildPropertiesText(packagePath, actor, rotationEuler, bounds)
        };
    }

    private string BuildPropertiesText(
        string packagePath,
        UnrActorBaseObject actor,
        Vector3? rotationEuler,
        (Vector3 Min, Vector3 Max)? bounds)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ObjectName = {actor.ObjectName}");
        sb.AppendLine($"Class = {actor.ClassName}");
        if (actor.Location is not null)
        {
            sb.AppendLine($"Location = {actor.Location.Value}");
        }

        if (actor.Rotation is not null)
        {
            sb.AppendLine($"RotationRaw = {actor.Rotation.Value}");
        }

        if (rotationEuler is not null)
        {
            sb.AppendLine($"RotationEulerDegrees = {rotationEuler.Value}");
        }

        sb.AppendLine($"DrawScale = {actor.DrawScale}");
        sb.AppendLine($"DrawScale3D = {actor.DrawScale3D}");
        sb.AppendLine($"PrePivot = {actor.PrePivot}");

        AppendReference(sb, "StaticMesh", packagePath, actor.StaticMeshReference);
        AppendReference(sb, "Brush", packagePath, actor.BrushReference);
        AppendReference(sb, "Mesh", packagePath, actor.MeshReference);
        AppendReference(sb, "Texture", packagePath, actor.TextureReference);
        AppendReference(sb, "Base", packagePath, actor.BaseReference);
        AppendReference(sb, "Owner", packagePath, actor.OwnerReference);
        AppendReference(sb, "PhysicsVolume", packagePath, actor.PhysicsVolumeReference);

        if (!string.IsNullOrWhiteSpace(actor.Tag))
        {
            sb.AppendLine($"Tag = {actor.Tag}");
        }

        if (!string.IsNullOrWhiteSpace(actor.Event))
        {
            sb.AppendLine($"Event = {actor.Event}");
        }

        if (bounds is not null)
        {
            sb.AppendLine($"BoundsMin = {bounds.Value.Min}");
            sb.AppendLine($"BoundsMax = {bounds.Value.Max}");
        }

        if (actor.UnknownProperties.Length > 0)
        {
            sb.AppendLine("UnknownProperties =");
            foreach (var property in actor.UnknownProperties.OrderBy(x => x.Order))
            {
                sb.AppendLine($"  - [{property.Order}] {property.Name} type={property.Type} size={property.DataSize}");
            }
        }

        return sb.ToString();
    }

    private static void AppendReference(
        StringBuilder sb,
        string label,
        string packagePath,
        UnrFileObjectReference? reference)
    {
        if (reference is null)
        {
            return;
        }

        sb.AppendLine($"{label} = {SceneReferenceUtilities.BuildReference(packagePath, reference)}");
    }

    private (Vector3 Min, Vector3 Max)? ResolveBounds(
        string packagePath,
        UnrFile.UnrFile unr,
        UnrActorBaseObject actor,
        Vector3? rotationEuler)
    {
        if (actor.StaticMeshReference is not null)
        {
            var meshReference = SceneReferenceUtilities.BuildReference(packagePath, actor.StaticMeshReference);
            var mesh = _staticMeshResolver.ResolveMany(packagePath, [actor.StaticMeshReference])[meshReference];
            return TransformBounds(
                mesh.RenderGeometry.BoundsMin,
                mesh.RenderGeometry.BoundsMax,
                actor.Location,
                rotationEuler,
                actor.DrawScale3D * actor.DrawScale,
                actor.PrePivot);
        }

        if (actor.BrushReference?.ExportIndex is not null)
        {
            var brush = unr.ExportObjects
                .Select(x => x.Object)
                .OfType<UnrModelObject>()
                .FirstOrDefault(x => x.ExportIndex == actor.BrushReference.ExportIndex.Value);
            if (brush is not null && brush.BoundsValid)
            {
                return TransformBounds(
                    brush.BoundsMin,
                    brush.BoundsMax,
                    actor.Location,
                    rotationEuler,
                    actor.DrawScale3D * actor.DrawScale,
                    actor.PrePivot);
            }
        }

        if (actor is UnrTerrainSectorObject terrainSector)
        {
            return (terrainSector.BoundsMin, terrainSector.BoundsMax);
        }

        if (actor.Location is not null)
        {
            return (actor.Location.Value, actor.Location.Value);
        }

        return null;
    }

    private static (Vector3 Min, Vector3 Max)? TransformBounds(
        Vector3 localMin,
        Vector3 localMax,
        Vector3? location,
        Vector3? rotationEuler,
        Vector3 scale,
        Vector3 prePivot)
    {
        if (location is null)
        {
            return null;
        }

        var points = new[]
        {
            new Vector3(localMin.X, localMin.Y, localMin.Z),
            new Vector3(localMax.X, localMin.Y, localMin.Z),
            new Vector3(localMin.X, localMax.Y, localMin.Z),
            new Vector3(localMax.X, localMax.Y, localMin.Z),
            new Vector3(localMin.X, localMin.Y, localMax.Z),
            new Vector3(localMax.X, localMin.Y, localMax.Z),
            new Vector3(localMin.X, localMax.Y, localMax.Z),
            new Vector3(localMax.X, localMax.Y, localMax.Z)
        };

        var transformed = points
            .Select(point => TransformPoint(point, location.Value, rotationEuler, scale, prePivot))
            .ToArray();
        return (
            new Vector3(
                transformed.Min(x => x.X),
                transformed.Min(x => x.Y),
                transformed.Min(x => x.Z)),
            new Vector3(
                transformed.Max(x => x.X),
                transformed.Max(x => x.Y),
                transformed.Max(x => x.Z)));
    }

    private static Vector3 TransformPoint(
        Vector3 point,
        Vector3 location,
        Vector3? rotationEuler,
        Vector3 scale,
        Vector3 prePivot)
    {
        var scaled = point * scale;
        var translated = scaled - prePivot;
        var rotated = rotationEuler is null
            ? translated
            : RotateByEulerDegrees(translated, rotationEuler.Value);
        return rotated + location;
    }

    private static Vector3 RotateByEulerDegrees(Vector3 value, Vector3 degrees)
    {
        var radians = degrees * (MathF.PI / 180f);
        var rotation = Quaternion.CreateFromYawPitchRoll(radians.Z, radians.Y, radians.X);
        return Vector3.Transform(value, rotation);
    }
}
