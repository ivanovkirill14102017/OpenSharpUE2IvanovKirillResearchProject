using System.Numerics;
using L2Viewer.SceneDTO;
using L2Viewer.SceneDomain.Models;
using L2Viewer.UkxFile;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

[ForExternalUse]
public sealed class SceneSkeletalAssetBuilder
{
    private readonly object _sync = new();
    private readonly Dictionary<string, SceneSkeletalAsset> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SceneSkeletalAsset BuildExport(string packagePath, int exportIndex)
    {
        var key = $"{Path.GetFullPath(packagePath)}#{exportIndex}";
        lock (_sync)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var built = BuildAsset(packagePath, exportIndex);
        lock (_sync)
        {
            _cache[key] = built;
        }

        return built;
    }

    public SceneSkeletalAsset BuildNamed(string packagePath, string meshName)
    {
        var ukx = UkxFileReader.Read(packagePath);
        var exportIndex = ukx.ExportObjects
            .FirstOrDefault(x => x.Object is UkxSkeletalMeshObject mesh && mesh.ObjectName.Is(meshName))
            ?.Export.Index
            ?? throw new InvalidOperationException($"Skeletal mesh '{meshName}' was not found in '{packagePath}'.");

        return BuildExport(packagePath, exportIndex);
    }

    private static SceneSkeletalAsset BuildAsset(string packagePath, int exportIndex)
    {
        var ukx = UkxFileReader.Read(packagePath);
        var mesh = ukx.ExportObjects
            .FirstOrDefault(x => x.Export.Index == exportIndex)
            ?.Object as UkxSkeletalMeshObject
            ?? throw new InvalidOperationException($"Export {exportIndex} in '{packagePath}' is not a skeletal mesh.");

        if (mesh.AnimationReference is null)
        {
            throw new InvalidOperationException($"Skeletal mesh '{mesh.ObjectName}' does not reference an animation object.");
        }

        var animation = ukx.ExportObjects
            .Select(x => x.Object)
            .OfType<UkxMeshAnimationObject>()
            .FirstOrDefault(x => x.ObjectName.Is(mesh.AnimationReference.ObjectName))
            ?? throw new InvalidOperationException(
                $"Animation '{mesh.AnimationReference.ObjectName}' referenced by skeletal mesh '{mesh.ObjectName}' was not found in '{packagePath}'.");

        var materialSource = SceneSkeletalMeshCodec.DecodeSkeletalMesh(mesh, packagePath);
        var actorX = UkxActorXCompatBuilder.Build(mesh, animation);
        var (boundsMin, boundsMax) = ComputeBounds(actorX.Model.Mesh.Points);
        var animationSet = SceneSkeletalAnimationSemantics.BuildAnimationSet(ukx, animation, actorX.AnimationSet);
        var routingMetadata = SceneSkeletalAnimationRoutingResolver.BuildRoutingMetadata(packagePath, mesh.ObjectName, animationSet.Sequences);

        return new SceneSkeletalAsset
        {
            PackagePath = packagePath,
            MeshExportIndex = exportIndex,
            MeshObjectName = mesh.ObjectName,
            AnimationObjectName = animation.ObjectName,
            Source = "Native ActorX-compatible skeletal asset",
            Details = $"Mesh={mesh.ObjectName}\r\nAnimation={animation.ObjectName}",
            Skeleton = MapSkeleton(actorX.Model.Skeleton),
            Mesh = MapMesh(mesh.ObjectName, actorX.Model.Mesh, boundsMin, boundsMax),
            AnimationSet = animationSet,
            MaterialBindings = BuildMaterialBindings(mesh, packagePath),
            PrimaryTextureReference = materialSource?.TextureRef,
            UsedTextures = materialSource?.UsedTextures ?? [],
            RoutingProfiles = routingMetadata.Profiles,
            ConsumerWarnings = routingMetadata.Warnings,
            RequiresExplicitConsumerRouting = routingMetadata.RequiresExplicitConsumerRouting
        };
    }

    private static SceneSkeletalSkeleton MapSkeleton(BlenderCompatSkeleton skeleton)
    {
        return new SceneSkeletalSkeleton
        {
            Name = skeleton.Name,
            Bones = skeleton.Bones
                .Select((bone, index) => new SceneSkeletalBone
                {
                    Index = index,
                    Name = bone.Name,
                    ParentIndex = bone.ParentIndex,
                    RawBindPosition = bone.RawBindPosition,
                    RawBindRotation = bone.RawBindRotation,
                    StoredOrigLocation = bone.StoredOrigLocation,
                    StoredOrigQuaternion = bone.StoredOrigQuaternion,
                    PostQuaternion = bone.PostQuaternion,
                    IsRoot = bone.IsRoot,
                    DontInvertRoot = bone.DontInvertRoot
                })
                .ToArray()
        };
    }

    private static SceneSkeletalGeometry MapMesh(string name, BlenderCompatMesh mesh, Vector3 boundsMin, Vector3 boundsMax)
    {
        return new SceneSkeletalGeometry
        {
            Name = name,
            Points = mesh.Points.Select(x => new SceneSkeletalPoint
            {
                Position = x
            }).ToArray(),
            Wedges = mesh.Wedges.Select(x => new SceneSkeletalWedge
            {
                PointIndex = x.PointIndex,
                UV = x.UV,
                MaterialIndex = x.MaterialIndex
            }).ToArray(),
            Faces = mesh.Faces.Select(x => new SceneSkeletalFace
            {
                WedgeIndex0 = x.WedgeIndex0,
                WedgeIndex1 = x.WedgeIndex1,
                WedgeIndex2 = x.WedgeIndex2,
                MaterialIndex = x.MaterialIndex
            }).ToArray(),
            Weights = mesh.Weights.Select(x => new SceneSkeletalWeight
            {
                Weight = x.Weight,
                PointIndex = x.PointIndex,
                BoneIndex = x.BoneIndex
            }).ToArray(),
            SubMeshes = mesh.Faces
                .GroupBy(x => (int)x.MaterialIndex)
                .OrderBy(x => x.Key)
                .Select(x => new SceneSkeletalSubMesh
                {
                    MaterialId = x.Key,
                    FaceCount = x.Count()
                })
                .ToArray(),
            BoundsMin = boundsMin,
            BoundsMax = boundsMax
        };
    }

    private static IReadOnlyList<SceneSkeletalMaterialBinding> BuildMaterialBindings(UkxSkeletalMeshObject mesh, string packagePath)
    {
        var bindings = new List<SceneSkeletalMaterialBinding>(mesh.Materials.Length);
        for (var materialId = 0; materialId < mesh.Materials.Length; materialId++)
        {
            var material = mesh.Materials[materialId];
            string? packageName = null;
            string? objectName = null;
            string? textureReference = null;
            string? resolvedPackagePath = null;

            if (material.TextureIndex >= 0 && material.TextureIndex < mesh.TextureReferences.Length)
            {
                var reference = mesh.TextureReferences[material.TextureIndex];
                if (reference is not null)
                {
                    packageName = reference.PackageName ?? Path.GetFileNameWithoutExtension(packagePath);
                    objectName = reference.ObjectName;
                    resolvedPackagePath = reference.ExportIndex is not null ? packagePath : null;

                    if (reference.ClassName.EndsWith(UnrealClassNames.Texture, StringComparison.OrdinalIgnoreCase) ||
                        reference.ClassName.Is(UnrealClassNames.Texture))
                    {
                        textureReference = $"{packageName}.{objectName}";
                    }
                }
            }

            bindings.Add(new SceneSkeletalMaterialBinding
            {
                MaterialId = materialId,
                PackageName = packageName,
                ObjectName = objectName,
                TextureReference = textureReference,
                ResolvedPackagePath = resolvedPackagePath
            });
        }

        return bindings;
    }

    private static (Vector3 Min, Vector3 Max) ComputeBounds(IReadOnlyList<Vector3> points)
    {
        if (points.Count == 0)
        {
            return (Vector3.Zero, Vector3.Zero);
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        return (min, max);
    }
}
