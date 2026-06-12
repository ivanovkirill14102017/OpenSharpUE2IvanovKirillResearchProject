using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneInstancedMeshMetadataBuilder
{
    public SceneInstancedMeshMetadataResult Build(L2Viewer.UnrFile.UnrFile unr)
    {
        var instances = new List<SceneStaticMeshInstanceMetadata>();
        var terrainDecorations = new List<SceneTerrainDecorationMetadata>();

        foreach (var entry in unr.ExportObjects)
        {
            if (entry.Object is UnrTerrainInfoObject terrain)
            {
                AddTerrainDecorationMetadata(unr.FilePath, terrain, terrainDecorations);
                continue;
            }

            if (entry.Object is not UnrActorBaseObject actor || !IsMeshInstanceActor(actor))
            {
                continue;
            }

            if (ShouldSkipMeshInstanceActor(actor))
            {
                continue;
            }

            var rotationRaw = actor.Rotation;
            instances.Add(new SceneStaticMeshInstanceMetadata
            {
                ExportIndex = actor.ExportIndex,
                ActorName = actor.ObjectName,
                ClassName = actor.ClassName,
                MeshReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, actor.StaticMeshReference),
                TextureReference = SceneReferenceUtilities.BuildOptionalReference(unr.FilePath, actor.TextureReference),
                SkinReferences = GetSkinReferences(unr.FilePath, actor),
                WorldLocation = actor.Location,
                UnrealRotationRaw = rotationRaw,
                RotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
                Scale = SceneTransformUtilities.ComputeActorScale(actor),
                PrePivot = actor.PrePivot
            });
        }

        return new SceneInstancedMeshMetadataResult
        {
            Instances = instances
                .OrderBy(x => x.ActorName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            TerrainDecorations = terrainDecorations
                .OrderBy(x => x.TerrainActorName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.LayerIndex)
                .ToArray()
        };
    }

    private static void AddTerrainDecorationMetadata(
        string mapPath,
        UnrTerrainInfoObject terrain,
        List<SceneTerrainDecorationMetadata> terrainDecorations)
    {
        var heightMapReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, terrain.TerrainMapReference);
        var terrainScale = NormalizeTerrainScale(terrain.TerrainScale);

        for (var layerIndex = 0; layerIndex < terrain.DecoLayers.Length; layerIndex++)
        {
            var layer = terrain.DecoLayers[layerIndex];
            if (layer.ShowOnTerrain == 0)
            {
                continue;
            }

            terrainDecorations.Add(new SceneTerrainDecorationMetadata
            {
                TerrainExportIndex = terrain.ExportIndex,
                TerrainActorName = terrain.ObjectName,
                LayerIndex = layerIndex,
                HeightMapReference = heightMapReference,
                MeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, layer.StaticMeshReference),
                DensityMapReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, layer.DensityMapReference),
                ScaleMapReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, layer.ScaleMapReference),
                ColorMapReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, layer.ColorMapReference),
                MaxPerQuad = Math.Max(1, layer.MaxPerQuad),
                Seed = layer.Seed,
                RandomYaw = layer.RandomYaw != 0,
                TerrainLocation = terrain.Location,
                TerrainScale = terrainScale,
                DensityMultiplier = layer.DensityMultiplier,
                ScaleMultiplier = layer.ScaleMultiplier
            });
        }
    }

    private static bool IsMeshInstanceActor(UnrActorBaseObject actor)
    {
        return actor is UnrStaticMeshActorObject or UnrMovableStaticMeshActorObject;
    }

    private static bool ShouldSkipMeshInstanceActor(UnrActorBaseObject actor)
    {
        return actor switch
        {
            UnrStaticMeshActorObject staticMeshActor => staticMeshActor.DeleteMe || staticMeshActor.PendingDelete,
            _ => false
        };
    }

    private static IReadOnlyList<string> GetSkinReferences(string mapPath, UnrActorBaseObject actor)
    {
        if (actor is UnrStaticMeshActorObject staticActor)
        {
            return staticActor.Skins.Select(x => SceneReferenceUtilities.BuildReference(mapPath, x)).ToArray();
        }

        return Array.Empty<string>();
    }

    private static Vector3? NormalizeTerrainScale(Vector3? terrainScale)
    {
        if (terrainScale is null)
        {
            return null;
        }

        var scale = terrainScale.Value;
        return new Vector3(
            MathF.Abs(scale.X),
            MathF.Abs(scale.Z),
            MathF.Abs(scale.Y));
    }
}
