using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.Wpf;

internal sealed class ScenePreviewPreparationService
{
    private readonly ScenePreviewGeometry _geometry;
    private readonly SceneMaterialFactory _materials;

    public ScenePreviewPreparationService(ScenePreviewGeometry geometry, SceneMaterialFactory materials)
    {
        _geometry = geometry;
        _materials = materials;
    }

    public PreparedStaticMeshScene PrepareStaticMeshScene(MeshDecodeDiagnostic diagnostic)
    {
        var renderScene = _geometry.PrepareMeshScene(diagnostic.Mesh!);
        var collisionScene = diagnostic.Collision is null ? null : _geometry.PrepareMeshScene(diagnostic.Collision.Mesh);
        return new PreparedStaticMeshScene(renderScene, collisionScene);
    }

    public PreparedSceneDomainLayer PrepareWaterScene(IReadOnlyList<SceneWaterVolumeData> volumes)
    {
        return PrepareVolumeLayer(volumes, _materials.WaterMaterial);
    }

    public PreparedSceneDomainLayer PrepareVolumesScene(IReadOnlyList<SceneVolumeData> volumes)
    {
        return PrepareVolumeLayer(volumes, _materials.VolumeMaterial);
    }

    public PreparedSceneDomainLayer PrepareBrushPolyScene(SceneBrushPolyScene scene)
    {
        var visuals = new List<PreparedRenderableMesh>();
        foreach (var actor in scene.Actors)
        {
            foreach (var subMesh in actor.SubMeshes)
            {
                var previewMesh = ScenePreviewGeometry.BuildPreviewMesh(actor.RenderGeometry, null, subMesh.MaterialId);
                if (previewMesh.Triangles.Count == 0)
                {
                    continue;
                }

                visuals.Add(new PreparedRenderableMesh(
                    _geometry.PrepareMeshScene(previewMesh).Geometry,
                    _materials.CreateBrushPolyMaterial(subMesh)));
            }
        }

        var bounds = _geometry.PrepareBoundsScene(scene.WorldBoundsMin, scene.WorldBoundsMax);
        return new PreparedSceneDomainLayer(visuals, bounds.Bounds, bounds.ViewerBoundsMin, bounds.ViewerBoundsMax, scene.Actors.Count);
    }

    public PreparedSceneDomainLayer PrepareBspScene(SceneBspScene scene)
    {
        var visuals = new List<PreparedRenderableMesh>();
        foreach (var model in scene.Models)
        {
            foreach (var chunk in model.Chunks)
            {
                foreach (var section in chunk.MeshSections)
                {
                    visuals.Add(new PreparedRenderableMesh(
                        _geometry.BuildGeometry(section, _materials.ResolvePreviewTexture(section)),
                        _materials.CreateBspMaterial(section)));
                }
            }
        }

        var bounds = _geometry.PrepareBoundsScene(scene.WorldBoundsMin, scene.WorldBoundsMax);
        return new PreparedSceneDomainLayer(visuals, bounds.Bounds, bounds.ViewerBoundsMin, bounds.ViewerBoundsMax, visuals.Count);
    }

    public PreparedSceneDomainLayer PrepareSceneProps(SceneInstancedMeshResult props)
    {
        var visuals = new List<PreparedRenderableMesh>();
        var sceneMin = new System.Numerics.Vector3(float.MaxValue);
        var sceneMax = new System.Numerics.Vector3(float.MinValue);

        foreach (var instance in props.Instances)
        {
            if (!props.UniqueMeshes.TryGetValue(instance.MeshReference, out var definition))
            {
                throw new InvalidOperationException($"Missing mesh definition for instance '{instance.ActorName}' ({instance.MeshReference}).");
            }

            foreach (var subMesh in definition.SubMeshes)
            {
                var previewMesh = ScenePreviewGeometry.BuildPreviewMesh(definition.RenderGeometry, instance, subMesh.MaterialId);
                if (previewMesh.Triangles.Count == 0)
                {
                    continue;
                }

                visuals.Add(new PreparedRenderableMesh(
                    _geometry.PrepareMeshScene(previewMesh).Geometry,
                    _materials.CreateScenePropMaterial(subMesh)));

                sceneMin = System.Numerics.Vector3.Min(sceneMin, previewMesh.BBoxMin);
                sceneMax = System.Numerics.Vector3.Max(sceneMax, previewMesh.BBoxMax);
            }
        }

        if (visuals.Count == 0)
        {
            sceneMin = System.Numerics.Vector3.Zero;
            sceneMax = System.Numerics.Vector3.Zero;
        }

        var bounds = _geometry.PrepareBoundsScene(sceneMin, sceneMax);
        return new PreparedSceneDomainLayer(visuals, bounds.Bounds, bounds.ViewerBoundsMin, bounds.ViewerBoundsMax, props.Instances.Count);
    }

    public PreparedTerrainPreview BuildTerrainPreview(TerrainImportData terrain)
    {
        var previewMesh = ScenePreviewGeometry.BuildTerrainMesh(terrain);
        var heightPreview = ScenePreviewGeometry.BuildHeightPreviewTexture(terrain);
        var cards = new List<TerrainTextureCard>
        {
            new(
                "Height",
                terrain.HeightReference,
                $"{terrain.HeightTexture.Width}x{terrain.HeightTexture.Height}",
                terrain.HeightTexture,
                null)
        };

        foreach (var layer in terrain.Layers.OrderBy(x => x.ArrayIndex))
        {
            cards.Add(new TerrainTextureCard(
                $"Layer {layer.ArrayIndex} Texture",
                layer.TextureReference ?? "<none>",
                layer.Texture is null ? "<not decoded>" : $"{layer.Texture.Width}x{layer.Texture.Height}",
                layer.Texture,
                null));
            cards.Add(new TerrainTextureCard(
                $"Layer {layer.ArrayIndex} Alpha",
                layer.AlphaMapReference ?? "<none>",
                layer.AlphaMapTexture is null ? "<not decoded>" : $"{layer.AlphaMapTexture.Width}x{layer.AlphaMapTexture.Height}",
                layer.AlphaMapTexture,
                null));
        }

        return new PreparedTerrainPreview(terrain, previewMesh, heightPreview, cards, ScenePreviewGeometry.BuildTerrainSummaryText(terrain));
    }

    public PreparedMapSceneInfo BuildMapSceneInfo(L2Viewer.UnrFile.UnrFile unr)
    {
        var lightingBuilder = new SceneLightingBuilder();
        var fogBuilder = new SceneFogBuilder();
        var particleBuilder = new SceneParticleBuilder();
        return new PreparedMapSceneInfo(
            lightingBuilder.BuildLights(unr),
            lightingBuilder.BuildSuns(unr),
            lightingBuilder.BuildMoons(unr),
            lightingBuilder.BuildSkyZones(unr),
            fogBuilder.BuildFogZones(unr),
            particleBuilder.BuildEmitters(unr),
            unr.ExportObjects.Select(x => x.Object).OfType<UnrActorBaseObject>().Count());
    }

    private PreparedSceneDomainLayer PrepareVolumeLayer(IReadOnlyList<SceneVolumeData> volumes, HelixToolkit.Wpf.SharpDX.PhongMaterial material)
    {
        var visuals = new List<PreparedRenderableMesh>(volumes.Count);
        var sceneMin = new System.Numerics.Vector3(float.MaxValue);
        var sceneMax = new System.Numerics.Vector3(float.MinValue);
        var renderableCount = 0;

        foreach (var volume in volumes)
        {
            if (volume.RenderGeometry is null ||
                volume.WorldBoundsMin is not System.Numerics.Vector3 boundsMin ||
                volume.WorldBoundsMax is not System.Numerics.Vector3 boundsMax)
            {
                continue;
            }

            visuals.Add(new PreparedRenderableMesh(_geometry.PrepareMeshScene(volume.RenderGeometry).Geometry, material));
            sceneMin = System.Numerics.Vector3.Min(sceneMin, boundsMin);
            sceneMax = System.Numerics.Vector3.Max(sceneMax, boundsMax);
            renderableCount++;
        }

        if (visuals.Count == 0)
        {
            sceneMin = System.Numerics.Vector3.Zero;
            sceneMax = System.Numerics.Vector3.Zero;
        }

        var bounds = _geometry.PrepareBoundsScene(sceneMin, sceneMax);
        return new PreparedSceneDomainLayer(visuals, bounds.Bounds, bounds.ViewerBoundsMin, bounds.ViewerBoundsMax, renderableCount);
    }
}
