using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services;

namespace L2Viewer.Wpf;

internal sealed class MapLayerLoader
{
    private readonly MainWindow _owner;
    private readonly ScenePreviewRenderer _renderer;
    private readonly CameraPresetService _cameraPresetService;

    public MapLayerLoader(MainWindow owner, ScenePreviewRenderer renderer, CameraPresetService cameraPresetService)
    {
        _owner = owner;
        _renderer = renderer;
        _cameraPresetService = cameraPresetService;
    }

    public async Task LoadSelectedMapLayersAsync()
    {
        if (_owner.CurrentDocument is null ||
            !string.Equals(Path.GetExtension(_owner.CurrentDocument.Package.Path), ".unr", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var preserveCamera = _renderer.HasLoadedMapSceneContent();
        var cameraState = preserveCamera ? _cameraPresetService.CaptureCurrentCamera(_owner.Viewport) : null;
        var op = _owner.BeginOperation("Loading selected map layers");
        try
        {
            var clientRoot = (_owner.FindName("ClientRootTextBox") as System.Windows.Controls.TextBox)?.Text.Trim() ?? string.Empty;
            var loadTerrain = _owner.ShowTerrainCheckBox.IsChecked == true;
            var loadWater = _owner.ShowWaterCheckBox.IsChecked == true;
            var loadBrushPolys = _owner.LoadPolysCheckBox.IsChecked == true;
            var loadWorldBsp = _owner.LoadBspCheckBox.IsChecked == true;
            var loadProps = _owner.LoadPropsCheckBox.IsChecked == true;

            _owner.StatusTextBlock.Text = "Loading selected map layers...";
            var loaded = await Task.Run(() =>
            {
                TerrainImportData? terrain = null;
                SceneInstancedMeshResult? placedActors = null;
                IReadOnlyList<SceneVolumeData>? waterVolumes = null;
                SceneBrushPolyScene? brushPolys = null;
                SceneBspScene? worldBsp = null;
                BspTextureManager? textureManager = null;
                L2Viewer.UnrFile.UnrFile? unr = null;

                if (loadTerrain && string.IsNullOrWhiteSpace(clientRoot))
                {
                    throw new InvalidOperationException("Client root is required for terrain preview.");
                }

                var requiresSceneDomain = (loadTerrain || loadProps || loadWorldBsp || loadBrushPolys) && !string.IsNullOrWhiteSpace(clientRoot);
                if (requiresSceneDomain)
                {
                    unr = UnrFileReader.Read(_owner.CurrentDocument.Package.Path);
                    textureManager = new BspTextureManager(clientRoot);
                    var staticMeshResolver = new SceneStaticMeshResolver(clientRoot, textureManager);
                    var materialResolver = new SceneMaterialResolver(clientRoot, textureManager);

                    if (loadTerrain)
                    {
                        terrain = new TerrainImportBuilder(textureManager).Build(unr).FirstOrDefault();
                    }

                    if (loadProps)
                    {
                        placedActors = new SceneInstancedMeshBuilder(staticMeshResolver, textureManager).Build(unr);
                    }

                    if (loadWorldBsp)
                    {
                        worldBsp = new SceneBspBuilder(clientRoot, materialResolver, textureManager).Build(unr);
                    }

                    if (loadBrushPolys)
                    {
                        brushPolys = new SceneBrushPolyBuilder(clientRoot, materialResolver, textureManager).Build(unr);
                    }
                }

                if (loadWater)
                {
                    unr ??= UnrFileReader.Read(_owner.CurrentDocument.Package.Path);
                    waterVolumes = new SceneVolumeBuilder().Build(unr, 1024);
                }

                return (terrain, placedActors, waterVolumes, brushPolys, worldBsp, textureManager);
            });

            _renderer.ShowTerrain = loadTerrain;
            _renderer.ShowWater = loadWater;
            _renderer.CurrentTerrainPreview = loadTerrain && loaded.terrain is not null
                ? _renderer.BuildTerrainPreview(loaded.terrain)
                : null;
            _renderer.UpdateMapInfoPanel(_owner.CurrentDocument, _renderer.CurrentTerrainPreview);
            _renderer.SceneTextureManager = loaded.textureManager;
            _renderer.CurrentPropScene = loadProps && loaded.placedActors is not null
                ? _renderer.PrepareSceneProps(loaded.placedActors)
                : null;
            var water = loadWater ? loaded.waterVolumes?.OfType<SceneWaterVolumeData>().ToList() : null;
            var volumes = loadWater ? loaded.waterVolumes?.Where(v => v is not SceneWaterVolumeData).ToList() : null;
            _renderer.CurrentWaterScene = water is { Count: > 0 } ? _renderer.PrepareWaterScene(water) : null;
            _renderer.CurrentVolumesScene = volumes is { Count: > 0 } ? _renderer.PrepareVolumesScene(volumes) : null;
            _renderer.CurrentBrushPolyScene = loadBrushPolys && loaded.brushPolys is not null
                ? _renderer.PrepareBrushPolyScene(loaded.brushPolys)
                : null;
            _renderer.CurrentWorldBspScene = loadWorldBsp && loaded.worldBsp is not null
                ? _renderer.PrepareBspScene(loaded.worldBsp)
                : null;

            _renderer.ShowPreviewForCurrentMode();
            _cameraPresetService.RestoreCameraState(_owner.Viewport, cameraState);

            var terrainText = loadTerrain ? (_renderer.CurrentTerrainPreview is null ? "terrain=0" : "terrain=1") : "terrain=off";
            var propsText = loadProps ? $"props={_renderer.CurrentPropScene?.ActorCount ?? 0}" : "props=off";
            var waterText = loadWater ? $"water={_renderer.CurrentWaterScene?.ActorCount ?? 0}" : "water=off";
            var brushPolyText = loadBrushPolys ? $"brushPolys={_renderer.CurrentBrushPolyScene?.ActorCount ?? 0}" : "brushPolys=off";
            var bspText = loadWorldBsp ? $"worldBsp={_renderer.CurrentWorldBspScene?.ActorCount ?? 0}" : "worldBsp=off";
            _owner.StatusTextBlock.Text = $"Loaded selected layers | {terrainText} | {propsText} | {waterText} | {brushPolyText} | {bspText}";
        }
        catch (Exception ex)
        {
            _owner.StatusTextBlock.Text = $"Layer load failed: {ex.Message}";
        }
        finally
        {
            _owner.EndOperation(op);
        }
    }
}
