using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.SharpDX;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.Wpf;

internal sealed record PreparedMeshScene(
    HelixToolkit.SharpDX.MeshGeometry3D Geometry,
    LineGeometry3D Bounds,
    System.Numerics.Vector3 ViewerBoundsMin,
    System.Numerics.Vector3 ViewerBoundsMax);

internal sealed record PreparedStaticMeshScene(
    PreparedMeshScene RenderScene,
    PreparedMeshScene? CollisionScene);

internal sealed record PreparedRenderableMesh(
    HelixToolkit.SharpDX.MeshGeometry3D Geometry,
    PhongMaterial Material);

internal sealed record PreparedBoundsScene(
    LineGeometry3D Bounds,
    System.Numerics.Vector3 ViewerBoundsMin,
    System.Numerics.Vector3 ViewerBoundsMax);

internal sealed record PreparedPropScene(
    IReadOnlyList<PreparedRenderableMesh> Visuals,
    LineGeometry3D Bounds,
    System.Numerics.Vector3 ViewerBoundsMin,
    System.Numerics.Vector3 ViewerBoundsMax,
    int ActorCount);

internal sealed record PreparedSceneDomainLayer(
    IReadOnlyList<PreparedRenderableMesh> Visuals,
    LineGeometry3D Bounds,
    System.Numerics.Vector3 ViewerBoundsMin,
    System.Numerics.Vector3 ViewerBoundsMax,
    int ActorCount);

internal sealed record PreparedTerrainPreview(
    TerrainImportData Terrain,
    MeshData Mesh,
    TextureData HeightPreviewTexture,
    IReadOnlyList<TerrainTextureCard> TextureCards,
    string DetailsText);

internal sealed record PreparedMapSceneInfo(
    IReadOnlyList<SceneLightData> Lights,
    IReadOnlyList<SceneSunData> Suns,
    IReadOnlyList<SceneMoonData> Moons,
    IReadOnlyList<SceneSkyZoneData> SkyZones,
    IReadOnlyList<SceneZoneInfoData> FogZones,
    IReadOnlyList<SceneParticleEmitterData> Emitters,
    int ActorCount);
