using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.Wpf;

internal sealed class ScenePreviewRenderer
{
    private readonly MainWindow _owner;
    private readonly ObservableCollection<TerrainTextureCard> _terrainTextureCards;
    private readonly ScenePreviewGeometry _geometry = new();
    private readonly SceneMaterialFactory _materials = new();
    private readonly ScenePreviewPreparationService _preparer;
    private readonly ObservableElement3DCollection _propSceneModels = [];
    private readonly ObservableElement3DCollection _waterSceneModels = [];
    private readonly ObservableElement3DCollection _volumeSceneModels = [];
    private readonly ObservableElement3DCollection _polySceneModels = [];

    public ScenePreviewRenderer(MainWindow owner, ObservableCollection<TerrainTextureCard> terrainTextureCards)
    {
        _owner = owner;
        _terrainTextureCards = terrainTextureCards;
        _preparer = new ScenePreviewPreparationService(_geometry, _materials);
    }

    internal ObservableElement3DCollection PropSceneModels => _propSceneModels;
    internal ObservableElement3DCollection WaterSceneModels => _waterSceneModels;
    internal ObservableElement3DCollection VolumeSceneModels => _volumeSceneModels;
    internal ObservableElement3DCollection PolySceneModels => _polySceneModels;
    internal PhongMaterial DefaultMaterial => _materials.DefaultMaterial;
    internal PhongMaterial CollisionMaterial => _materials.CollisionMaterial;
    internal BspTextureManager? SceneTextureManager
    {
        get => _materials.SceneTextureManager;
        set => _materials.SceneTextureManager = value;
    }

    internal PreparedTerrainPreview? CurrentTerrainPreview { get; set; }
    internal PreparedSceneDomainLayer? CurrentPropScene { get; set; }
    internal PreparedSceneDomainLayer? CurrentWaterScene { get; set; }
    internal PreparedSceneDomainLayer? CurrentVolumesScene { get; set; }
    internal PreparedSceneDomainLayer? CurrentBrushPolyScene { get; set; }
    internal PreparedSceneDomainLayer? CurrentWorldBspScene { get; set; }
    internal PreparedMapSceneInfo? CurrentMapScene { get; set; }
    internal SceneStaticMeshDefinition? CurrentStaticMeshDefinition { get; set; }
    internal bool ShowTerrain { get; set; } = true;
    internal bool ShowWater { get; set; } = true;
    internal bool ShowStaticMeshCollision { get; set; }

    private MeshGeometryModel3D MeshModel => _owner.MeshModel;
    private MeshGeometryModel3D CollisionMeshModel => _owner.CollisionMeshModel;
    private LineGeometryModel3D BoundsModel => _owner.BoundsModel;
    private LineGeometryModel3D BoneOverlayModel => _owner.BoneOverlayModel;
    private PointGeometryModel3D VertexDebugModel => _owner.VertexDebugModel;
    private GroupModel3D MapLightsGroup => _owner.MapLightsGroup;
    private GroupModel3D MapSunLightsGroup => _owner.MapSunLightsGroup;
    private GroupModel3D MapMoonLightsGroup => _owner.MapMoonLightsGroup;
    private Image TextureImage => _owner.TextureImage;
    private Border EmptyPreviewBorder => _owner.EmptyPreviewBorder;
    private Border TexturePreviewBorder => _owner.TexturePreviewBorder;
    private Border MeshPreviewBorder => _owner.MeshPreviewBorder;
    private StackPanel SceneLightingPanel => _owner.SceneLightingPanel;
    private HelixToolkit.Wpf.SharpDX.PerspectiveCamera Camera => _owner.Camera;
    private CheckBox LoadPropsCheckBox => _owner.LoadPropsCheckBox;
    private CheckBox LoadPolysCheckBox => _owner.LoadPolysCheckBox;
    private CheckBox LoadBspCheckBox => _owner.LoadBspCheckBox;
    private CheckBox ShowLocalLightsCheckBox => _owner.ShowLocalLightsCheckBox;
    private CheckBox ShowShadowsCheckBox => _owner.ShowShadowsCheckBox;
    private CheckBox ShowSunCheckBox => _owner.ShowSunCheckBox;
    private CheckBox ShowMoonCheckBox => _owner.ShowMoonCheckBox;
    private TextBlock PreviewSubtitleTextBlock => _owner.PreviewSubtitleTextBlockControl;

    internal BitmapSource CreateBitmap(TextureData texture) => _geometry.CreateBitmap(texture);
    internal PreparedMeshScene PrepareMeshScene(MeshData mesh) => _geometry.PrepareMeshScene(mesh);
    internal PreparedMeshScene PrepareMeshScene(SceneTriangleMeshData mesh) => _geometry.PrepareMeshScene(mesh);
    internal PreparedBoundsScene PrepareBoundsScene(System.Numerics.Vector3 min, System.Numerics.Vector3 max) => _geometry.PrepareBoundsScene(min, max);
    internal LineGeometry3D BuildBoneOverlayGeometry(SkeletalDebugOverlay overlay) => _geometry.BuildBoneOverlayGeometry(overlay);
    internal PointGeometry3D BuildVertexDebugGeometry(SkeletalDebugFrameSnapshot frame) => _geometry.BuildVertexDebugGeometry(frame);
    internal PhongMaterial CreateGhostedMaterial(MeshData mesh) => _materials.CreateGhostedMaterial(mesh);
    internal PhongMaterial CreateSkeletalDiagnosticMaterial() => _materials.CreateSkeletalDiagnosticMaterial();
    internal PreparedSceneDomainLayer PrepareWaterScene(IReadOnlyList<SceneWaterVolumeData> volumes) => _preparer.PrepareWaterScene(volumes);
    internal PreparedSceneDomainLayer PrepareVolumesScene(IReadOnlyList<SceneVolumeData> volumes) => _preparer.PrepareVolumesScene(volumes);
    internal PreparedSceneDomainLayer PrepareBrushPolyScene(SceneBrushPolyScene scene) => _preparer.PrepareBrushPolyScene(scene);
    internal PreparedSceneDomainLayer PrepareBspScene(SceneBspScene scene) => _preparer.PrepareBspScene(scene);
    internal PreparedSceneDomainLayer PrepareSceneProps(SceneInstancedMeshResult props) => _preparer.PrepareSceneProps(props);
    internal PreparedTerrainPreview BuildTerrainPreview(TerrainImportData terrain) => _preparer.BuildTerrainPreview(terrain);
    internal PreparedMapSceneInfo BuildMapSceneInfo(L2Viewer.UnrFile.UnrFile unr) => _preparer.BuildMapSceneInfo(unr);

    internal void ApplyPreparedMesh(MeshData mesh, PreparedMeshScene scene, bool fitCamera = true)
    {
        MeshModel.Geometry = scene.Geometry;
        MeshModel.Material = _materials.CreateMaterial(mesh);
        MeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.Back;
        CollisionMeshModel.Geometry = null;
        CollisionMeshModel.Material = _materials.CollisionMaterial;
        CollisionMeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.Back;
        ClearSceneGroups();
        BoundsModel.Geometry = scene.Bounds;
        BoneOverlayModel.Geometry = null;
        VertexDebugModel.Geometry = null;
        if (fitCamera)
        {
            FitCamera(scene.ViewerBoundsMin, scene.ViewerBoundsMax);
        }
    }

    internal void ApplyPreparedStaticMesh(SceneStaticMeshDefinition mesh, PreparedStaticMeshScene scene)
    {
        MeshModel.Geometry = scene.RenderScene.Geometry;
        MeshModel.Material = _materials.CreateSceneStaticMeshMaterial(mesh);
        MeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.Back;
        CollisionMeshModel.Geometry = ShowStaticMeshCollision ? scene.CollisionScene?.Geometry : null;
        CollisionMeshModel.Material = _materials.CollisionMaterial;
        CollisionMeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.Back;
        ClearSceneGroups();
        BoundsModel.Geometry = scene.RenderScene.Bounds;
        BoneOverlayModel.Geometry = null;
        VertexDebugModel.Geometry = null;

        var min = scene.RenderScene.ViewerBoundsMin;
        var max = scene.RenderScene.ViewerBoundsMax;
        if (ShowStaticMeshCollision && scene.CollisionScene is not null)
        {
            min = System.Numerics.Vector3.Min(min, scene.CollisionScene.ViewerBoundsMin);
            max = System.Numerics.Vector3.Max(max, scene.CollisionScene.ViewerBoundsMax);
        }

        FitCamera(min, max);
    }

    internal void UpdateMaterialsShadowMap(bool renderShadowMap)
    {
        if (!_owner.Dispatcher.CheckAccess())
        {
            _owner.Dispatcher.Invoke(() => UpdateMaterialsShadowMap(renderShadowMap));
            return;
        }

        _materials.UpdateSharedShadowMap(renderShadowMap);

        if (MeshModel.Material is PhongMaterial material)
        {
            material.RenderShadowMap = renderShadowMap;
        }

        UpdateGroupMaterials(_owner.PropSceneGroup, renderShadowMap);
        UpdateGroupMaterials(_owner.PolySceneGroup, renderShadowMap);
        UpdateGroupMaterials(_owner.WaterSceneGroup, renderShadowMap);
    }

    internal void FitCamera(System.Numerics.Vector3 min, System.Numerics.Vector3 max)
    {
        var center = (min + max) * 0.5f;
        var extent = max - min;
        var radius = Math.Max(20f, extent.Length() * 0.75f);
        var distance = radius * 2.8f;
        Camera.Position = new System.Windows.Media.Media3D.Point3D(center.X + radius * 1.8f, center.Y + radius * 1.1f, center.Z + radius * 1.8f);
        Camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-radius * 1.8f, -radius * 1.1f, -radius * 1.8f);
        Camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
        Camera.NearPlaneDistance = Math.Max(0.01, radius / 2000.0);
        Camera.FarPlaneDistance = Math.Max(5000.0, distance * 12.0);
    }

    internal bool IsDefaultCameraState()
    {
        static bool NearlyEqual(double a, double b) => Math.Abs(a - b) < 0.001;

        var position = Camera.Position;
        var lookDirection = Camera.LookDirection;
        var upDirection = Camera.UpDirection;

        return NearlyEqual(position.X, 0) &&
               NearlyEqual(position.Y, 0) &&
               NearlyEqual(position.Z, 220) &&
               NearlyEqual(lookDirection.X, 0) &&
               NearlyEqual(lookDirection.Y, 0) &&
               NearlyEqual(lookDirection.Z, -220) &&
               NearlyEqual(upDirection.X, 0) &&
               NearlyEqual(upDirection.Y, 1) &&
               NearlyEqual(upDirection.Z, 0);
    }

    internal void ResetScene()
    {
        MeshModel.Geometry = null;
        MeshModel.Material = _materials.DefaultMaterial;
        MeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.Back;
        CollisionMeshModel.Geometry = null;
        CollisionMeshModel.Material = _materials.CollisionMaterial;
        CollisionMeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.Back;
        ClearSceneGroups();
        BoundsModel.Geometry = null;
        BoneOverlayModel.Geometry = null;
        VertexDebugModel.Geometry = null;
        TextureImage.Source = null;
    }

    internal void ShowTexturePreview()
    {
        EmptyPreviewBorder.Visibility = Visibility.Collapsed;
        TexturePreviewBorder.Visibility = Visibility.Visible;
        MeshPreviewBorder.Visibility = Visibility.Collapsed;
        SceneLightingPanel.Visibility = Visibility.Collapsed;
        ClearMeshOnly();
    }

    internal void ShowMeshPreview()
    {
        EmptyPreviewBorder.Visibility = Visibility.Collapsed;
        TexturePreviewBorder.Visibility = Visibility.Collapsed;
        MeshPreviewBorder.Visibility = Visibility.Visible;
        SceneLightingPanel.Visibility = Visibility.Visible;
        TextureImage.Source = null;
    }

    internal void ShowEmptyPreview()
    {
        EmptyPreviewBorder.Visibility = Visibility.Visible;
        TexturePreviewBorder.Visibility = Visibility.Collapsed;
        MeshPreviewBorder.Visibility = Visibility.Collapsed;
        SceneLightingPanel.Visibility = Visibility.Collapsed;
        ResetScene();
    }

    internal void ClearPropSceneState() => CurrentPropScene = null;

    internal void ClearWaterSceneState()
    {
        CurrentWaterScene = null;
        CurrentVolumesScene = null;
    }

    internal void ClearPolySceneState()
    {
        CurrentBrushPolyScene = null;
        CurrentWorldBspScene = null;
        SceneTextureManager = null;
    }

    internal bool HasLoadedMapSceneContent()
    {
        return CurrentTerrainPreview is not null ||
               (CurrentPropScene?.ActorCount ?? 0) > 0 ||
               (CurrentWaterScene?.ActorCount ?? 0) > 0 ||
               (CurrentBrushPolyScene?.ActorCount ?? 0) > 0 ||
               (CurrentWorldBspScene?.ActorCount ?? 0) > 0;
    }

    internal void UpdateMapInfoPanel(PackageDocument? document, PreparedTerrainPreview? terrainPreview)
    {
        if (document is null)
        {
            ClearMapInfoPanel();
            return;
        }

        _owner.MapNameTextBlock.Text = Path.GetFileName(document.Package.Path);
        _owner.MapVersionTextBlock.Text =
            $"Wrapper={document.Package.Wrapper} | Version={document.Package.Header.Version} | Licensee={document.Package.Header.LicenseeVersion}";

        if (terrainPreview is null)
        {
            _owner.TerrainStatusTextBlock.Text = "Terrain preview is not decoded yet.";
            _owner.HeightMapRefTextBlock.Text = "<none>";
            _owner.HeightMapSizeTextBlock.Text = string.Empty;
            _owner.HeightMapPreviewImage.Source = null;
            _owner.DiffuseTextureRefTextBlock.Text = "<none>";
            _owner.DiffuseTextureSizeTextBlock.Text = string.Empty;
            _owner.DiffuseTexturePreviewImage.Source = null;
            _owner.RenderTextureRefTextBlock.Text = "<none>";
            _owner.RenderTextureSizeTextBlock.Text = string.Empty;
            _owner.RenderTexturePreviewImage.Source = null;
            _terrainTextureCards.Clear();
            return;
        }

        _owner.TerrainStatusTextBlock.Text = $"Terrain preview ready. Layers: {terrainPreview.Terrain.Layers.Length}";
        _owner.HeightMapRefTextBlock.Text = terrainPreview.Terrain.HeightReference;
        _owner.HeightMapSizeTextBlock.Text = $"{terrainPreview.Terrain.HeightWidth}x{terrainPreview.Terrain.HeightHeight} | channel={terrainPreview.Terrain.HeightChannel}";
        _owner.HeightMapPreviewImage.Source = _geometry.CreateBitmap(terrainPreview.HeightPreviewTexture);
        _owner.DiffuseTextureRefTextBlock.Text = "<none>";
        _owner.DiffuseTextureSizeTextBlock.Text = string.Empty;
        _owner.DiffuseTexturePreviewImage.Source = null;
        _owner.RenderTextureRefTextBlock.Text = "<none>";
        _owner.RenderTextureSizeTextBlock.Text = string.Empty;
        _owner.RenderTexturePreviewImage.Source = null;

        _terrainTextureCards.Clear();
        foreach (var item in terrainPreview.TextureCards)
        {
            _terrainTextureCards.Add(new TerrainTextureCard(
                item.Title,
                item.Reference,
                item.Size,
                item.PreviewTexture,
                item.PreviewTexture is null ? null : _geometry.CreateBitmap(item.PreviewTexture)));
        }
    }

    internal void ClearMapInfoPanel()
    {
        CurrentMapScene = null;
        _owner.MapNameTextBlock.Text = string.Empty;
        _owner.MapVersionTextBlock.Text = string.Empty;
        _owner.TerrainStatusTextBlock.Text = "Open a map (.unr) to see terrain information.";
        _owner.HeightMapRefTextBlock.Text = "<none>";
        _owner.HeightMapSizeTextBlock.Text = string.Empty;
        _owner.HeightMapPreviewImage.Source = null;
        _owner.DiffuseTextureRefTextBlock.Text = "<none>";
        _owner.DiffuseTextureSizeTextBlock.Text = string.Empty;
        _owner.DiffuseTexturePreviewImage.Source = null;
        _owner.RenderTextureRefTextBlock.Text = "<none>";
        _owner.RenderTextureSizeTextBlock.Text = string.Empty;
        _owner.RenderTexturePreviewImage.Source = null;
        _terrainTextureCards.Clear();
    }

    internal void ShowPreviewForCurrentMode()
    {
        if (!TryRenderCurrentMapScene())
        {
            ShowEmptyPreview();
        }
    }

    internal bool TryRenderCurrentMapScene()
    {
        if (!Path.GetExtension(_owner.CurrentPackagePath).Is(".unr"))
        {
            return false;
        }

        var hasTerrain = ShowTerrain && CurrentTerrainPreview is not null;
        var hasWater = ShowWater && CurrentWaterScene is not null && CurrentWaterScene.ActorCount > 0;
        var hasVolumes = ShowWater && CurrentVolumesScene is not null && CurrentVolumesScene.ActorCount > 0;
        var hasProps = LoadPropsCheckBox.IsChecked == true && CurrentPropScene is not null && CurrentPropScene.ActorCount > 0;
        var hasBrushPolys = LoadPolysCheckBox.IsChecked == true && CurrentBrushPolyScene is not null && CurrentBrushPolyScene.ActorCount > 0;
        var hasWorldBsp = LoadBspCheckBox.IsChecked == true && CurrentWorldBspScene is not null && CurrentWorldBspScene.ActorCount > 0;
        var hasAnyPolys = hasBrushPolys || hasWorldBsp;
        if (!hasTerrain && !hasProps && !hasWater && !hasVolumes && !hasAnyPolys)
        {
            return false;
        }

        if (hasTerrain)
        {
            var scene = _geometry.PrepareMeshScene(CurrentTerrainPreview!.Mesh);
            MeshModel.Geometry = scene.Geometry;
            MeshModel.Material = _materials.CreateMaterial(CurrentTerrainPreview.Mesh);
            BoundsModel.Geometry = scene.Bounds;
            if (!hasProps)
            {
                FitCamera(scene.ViewerBoundsMin, scene.ViewerBoundsMax);
            }

            PreviewSubtitleTextBlock.Text = BuildTerrainSubtitle(hasProps, hasWater, hasBrushPolys, hasWorldBsp, hasAnyPolys);
        }
        else
        {
            MeshModel.Geometry = null;
            MeshModel.Material = _materials.DefaultMaterial;
        }

        ApplyLights(CurrentMapScene);
        ApplySceneLayer(_waterSceneModels, ShowWater ? CurrentWaterScene : null, global::SharpDX.Direct3D11.CullMode.Back);
        ApplySceneLayer(_volumeSceneModels, ShowWater ? CurrentVolumesScene : null, global::SharpDX.Direct3D11.CullMode.Back);
        ApplySceneLayer(_propSceneModels, hasProps ? CurrentPropScene : null, global::SharpDX.Direct3D11.CullMode.Back);
        ApplySceneLayers(_polySceneModels, hasBrushPolys ? CurrentBrushPolyScene : null, hasWorldBsp ? CurrentWorldBspScene : null, global::SharpDX.Direct3D11.CullMode.None);

        UpdateBoundsAndCameraForActiveScene(hasTerrain, hasProps, hasWater, hasVolumes, hasBrushPolys, hasWorldBsp);
        ShowMeshPreview();
        return true;
    }

    private string BuildTerrainSubtitle(bool hasProps, bool hasWater, bool hasBrushPolys, bool hasWorldBsp, bool hasAnyPolys)
    {
        return hasProps
            ? $"Terrain scene | props loaded: {CurrentPropScene!.ActorCount}" +
              (hasWater ? $" | water loaded: {CurrentWaterScene!.ActorCount}" : string.Empty) +
              (hasBrushPolys ? $" | brush polys: {CurrentBrushPolyScene!.ActorCount}" : string.Empty) +
              (hasWorldBsp ? $" | world bsp: {CurrentWorldBspScene!.ActorCount}" : string.Empty)
            : hasWater
                ? $"Terrain scene | water loaded: {CurrentWaterScene!.ActorCount}" +
                  (hasBrushPolys ? $" | brush polys: {CurrentBrushPolyScene!.ActorCount}" : string.Empty) +
                  (hasWorldBsp ? $" | world bsp: {CurrentWorldBspScene!.ActorCount}" : string.Empty)
                : hasAnyPolys
                    ? $"Terrain scene" +
                      (hasBrushPolys ? $" | brush polys: {CurrentBrushPolyScene!.ActorCount}" : string.Empty) +
                      (hasWorldBsp ? $" | world bsp: {CurrentWorldBspScene!.ActorCount}" : string.Empty)
                    : "Terrain scene";
    }

    private void UpdateBoundsAndCameraForActiveScene(bool hasTerrain, bool hasProps, bool hasWater, bool hasVolumes, bool hasBrushPolys, bool hasWorldBsp)
    {
        if (ShowWater && CurrentWaterScene is not null)
        {
            BoundsModel.Geometry = CurrentWaterScene.Bounds;
            if (!hasTerrain && !hasProps && !hasVolumes && !hasBrushPolys && !hasWorldBsp)
            {
                FitCamera(CurrentWaterScene.ViewerBoundsMin, CurrentWaterScene.ViewerBoundsMax);
                PreviewSubtitleTextBlock.Text = $"Water scene | count: {CurrentWaterScene.ActorCount}";
            }
        }

        if (ShowWater && CurrentVolumesScene is not null)
        {
            BoundsModel.Geometry = CurrentVolumesScene.Bounds;
            if (!hasTerrain && !hasProps && !hasWater && !hasBrushPolys && !hasWorldBsp)
            {
                FitCamera(CurrentVolumesScene.ViewerBoundsMin, CurrentVolumesScene.ViewerBoundsMax);
                PreviewSubtitleTextBlock.Text = $"Volumes scene | count: {CurrentVolumesScene.ActorCount}";
            }
        }

        if (hasBrushPolys && CurrentBrushPolyScene is not null)
        {
            BoundsModel.Geometry = CurrentBrushPolyScene.Bounds;
            if (!hasTerrain && !hasProps && !hasWater && !hasVolumes && !hasWorldBsp)
            {
                FitCamera(CurrentBrushPolyScene.ViewerBoundsMin, CurrentBrushPolyScene.ViewerBoundsMax);
                PreviewSubtitleTextBlock.Text = $"Brush polys | count: {CurrentBrushPolyScene.ActorCount}";
            }
        }

        if (hasWorldBsp && CurrentWorldBspScene is not null)
        {
            BoundsModel.Geometry = CurrentWorldBspScene.Bounds;
            if (!hasTerrain && !hasProps && !hasWater && !hasVolumes)
            {
                FitCamera(CurrentWorldBspScene.ViewerBoundsMin, CurrentWorldBspScene.ViewerBoundsMax);
                PreviewSubtitleTextBlock.Text = hasBrushPolys
                    ? $"Polys scene | brush: {CurrentBrushPolyScene!.ActorCount} | bsp: {CurrentWorldBspScene.ActorCount}"
                    : $"World BSP | count: {CurrentWorldBspScene.ActorCount}";
            }
        }

        if (hasProps && CurrentPropScene is not null)
        {
            BoundsModel.Geometry = CurrentPropScene.Bounds;
            if (!hasTerrain && !hasWater && !hasVolumes)
            {
                FitCamera(CurrentPropScene.ViewerBoundsMin, CurrentPropScene.ViewerBoundsMax);
                PreviewSubtitleTextBlock.Text = $"Props scene | count: {CurrentPropScene.ActorCount}";
            }
        }
    }

    private void ApplyLights(PreparedMapSceneInfo? scene)
    {
        MapLightsGroup.Children.Clear();
        MapSunLightsGroup.Children.Clear();
        MapMoonLightsGroup.Children.Clear();

        if (scene is null)
        {
            return;
        }

        foreach (var light in scene.Lights)
        {
            if (light.WorldLocation is null)
            {
                continue;
            }

            var loc = light.WorldLocation.Value;
            var brightness = Math.Clamp((light.Brightness ?? 255f) / 255f, 0f, 1f);
            var hue = light.Hue.GetValueOrDefault();
            var saturation = light.Saturation.GetValueOrDefault(255);
            var radius = light.Radius.GetValueOrDefault(64f);
            float h = (hue / 255f) * 360f;
            float s = 1f - (saturation / 255f);
            float v = brightness;

            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;

            float r = 0, g = 0, b = 0;
            if (h >= 0 && h < 60) { r = c; g = x; }
            else if (h >= 60 && h < 120) { r = x; g = c; }
            else if (h >= 120 && h < 180) { g = c; b = x; }
            else if (h >= 180 && h < 240) { g = x; b = c; }
            else if (h >= 240 && h < 300) { r = x; b = c; }
            else if (h >= 300 && h <= 360) { r = c; b = x; }

            MapLightsGroup.Children.Add(new PointLight3D
            {
                Color = Color.FromRgb(
                    (byte)Math.Clamp((r + m) * 255, 0, 255),
                    (byte)Math.Clamp((g + m) * 255, 0, 255),
                    (byte)Math.Clamp((b + m) * 255, 0, 255)),
                Position = new System.Windows.Media.Media3D.Point3D(loc.X, loc.Z, loc.Y),
                Attenuation = new System.Windows.Media.Media3D.Vector3D(1.0, 0.001, 0.0),
                Range = radius * 64.0f
            });
        }

        AddDirectionalLights(scene.Suns, MapSunLightsGroup);
        AddDirectionalLights(scene.Moons, MapMoonLightsGroup);
        MapLightsGroup.IsRendering = ShowLocalLightsCheckBox.IsChecked == true;
        MapSunLightsGroup.IsRendering = ShowSunCheckBox.IsChecked == true;
        MapMoonLightsGroup.IsRendering = ShowMoonCheckBox.IsChecked == true;
        UpdateMaterialsShadowMap(ShowShadowsCheckBox.IsChecked == true);
    }

    private static void AddDirectionalLights<T>(IReadOnlyList<T> items, GroupModel3D group)
        where T : class
    {
        foreach (var item in items)
        {
            System.Numerics.Vector3? rotation = item switch
            {
                SceneSunData sun => sun.WorldRotationEulerDegrees,
                SceneMoonData moon => moon.WorldRotationEulerDegrees,
                _ => null
            };

            if (rotation is null)
            {
                continue;
            }

            var radians = rotation.Value * (MathF.PI / 180f);
            var direction = new System.Numerics.Vector3(
                MathF.Cos(radians.Y) * MathF.Cos(radians.Z),
                MathF.Sin(radians.Z),
                MathF.Sin(radians.Y) * MathF.Cos(radians.Z));
            var viewerDirection = ScenePreviewGeometry.ToViewerVector(direction);
            group.Children.Add(new DirectionalLight3D
            {
                Color = Color.FromRgb(255, 255, 255),
                Direction = new System.Windows.Media.Media3D.Vector3D(viewerDirection.X, viewerDirection.Y, viewerDirection.Z)
            });
        }
    }

    private void ApplySceneLayer(ObservableElement3DCollection target, PreparedSceneDomainLayer? scene, global::SharpDX.Direct3D11.CullMode cullMode)
    {
        target.Clear();
        if (scene is null)
        {
            return;
        }

        foreach (var visual in scene.Visuals)
        {
            target.Add(new MeshGeometryModel3D
            {
                Geometry = visual.Geometry,
                Material = _materials.CloneForScene(visual.Material),
                CullMode = cullMode
            });
        }
    }

    private void ApplySceneLayers(
        ObservableElement3DCollection target,
        PreparedSceneDomainLayer? first,
        PreparedSceneDomainLayer? second,
        global::SharpDX.Direct3D11.CullMode cullMode)
    {
        target.Clear();
        AddLayerVisuals(target, first, cullMode);
        AddLayerVisuals(target, second, cullMode);
    }

    private void AddLayerVisuals(
        ObservableElement3DCollection target,
        PreparedSceneDomainLayer? scene,
        global::SharpDX.Direct3D11.CullMode cullMode)
    {
        if (scene is null)
        {
            return;
        }

        foreach (var visual in scene.Visuals)
        {
            target.Add(new MeshGeometryModel3D
            {
                Geometry = visual.Geometry,
                Material = _materials.CloneForScene(visual.Material),
                CullMode = cullMode
            });
        }
    }

    private void ClearMeshOnly()
    {
        MeshModel.Geometry = null;
        MeshModel.Material = _materials.DefaultMaterial;
        CollisionMeshModel.Geometry = null;
        CollisionMeshModel.Material = _materials.CollisionMaterial;
        ClearSceneGroups();
        BoundsModel.Geometry = null;
        BoneOverlayModel.Geometry = null;
        VertexDebugModel.Geometry = null;
    }

    private void ClearSceneGroups()
    {
        _propSceneModels.Clear();
        _waterSceneModels.Clear();
        _volumeSceneModels.Clear();
        _polySceneModels.Clear();
    }

    private static void UpdateGroupMaterials(GroupModel3D group, bool renderShadowMap)
    {
        foreach (var child in group.Children)
        {
            if (child is MeshGeometryModel3D mesh && mesh.Material is PhongMaterial material)
            {
                material.RenderShadowMap = renderShadowMap;
            }
            else if (child is GroupModel3D subgroup)
            {
                UpdateGroupMaterials(subgroup, renderShadowMap);
            }
        }
    }
}
