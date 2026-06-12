using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using L2Viewer.PackageCore;

namespace L2Viewer.Wpf;

public partial class MainWindow : Window
{
    private static readonly string[] SupportedExtensions = [".unr", ".utx", ".usx", ".ukx", ".u"];

    private readonly ObservableCollection<ExportTreeNode> _exportNodes = [];
    private readonly ObservableCollection<FileTreeItem> _rootItems = [];
    private readonly ObservableCollection<OperationItem> _activeOperations = [];
    private readonly ObservableCollection<TerrainTextureCard> _terrainTextureCards = [];
    private readonly ObservableCollection<SkeletalAnimationListItem> _skeletalAnimationItems = [];
    private readonly System.Windows.Threading.DispatcherTimer _inputTimer;

    private readonly ScenePreviewRenderer _sceneRenderer;
    private readonly SkeletalAnimationPreviewController _skeletalAnimationController;
    private readonly CameraPresetService _cameraPresetService;
    private readonly MainWindowLoadingController _loadingController;
    private readonly ExportPreviewService _exportPreviewService;
    private readonly MapLayerLoader _mapLayerLoader;

    private bool _isUpdatingLighting;
    private AppSettings _appSettings = AppSettings.Load();

    public MainWindow()
    {
        InitializeComponent();
        EffectsManager = new DefaultEffectsManager();
        DataContext = this;

        ExportsTreeView.ItemsSource = _exportNodes;
        FilesTreeView.ItemsSource = _rootItems;
        ActiveOperationsItemsControl.ItemsSource = _activeOperations;
        TerrainTexturesItemsControl.ItemsSource = _terrainTextureCards;
        AnimationListBox.ItemsSource = _skeletalAnimationItems;

        _sceneRenderer = new ScenePreviewRenderer(this, _terrainTextureCards);
        _cameraPresetService = new CameraPresetService(_appSettings);
        _skeletalAnimationController = new SkeletalAnimationPreviewController(this, _sceneRenderer, _skeletalAnimationItems);
        _loadingController = new MainWindowLoadingController(this, _sceneRenderer, _cameraPresetService);
        _exportPreviewService = new ExportPreviewService(this, _sceneRenderer, _skeletalAnimationController);
        _mapLayerLoader = new MapLayerLoader(this, _sceneRenderer, _cameraPresetService);

        PropSceneGroup.ItemsSource = _sceneRenderer.PropSceneModels;
        WaterSceneGroup.ItemsSource = _sceneRenderer.WaterSceneModels;
        VolumeSceneGroup.ItemsSource = _sceneRenderer.VolumeSceneModels;
        PolySceneGroup.ItemsSource = _sceneRenderer.PolySceneModels;
        MeshModel.Material = _sceneRenderer.DefaultMaterial;
        CollisionMeshModel.Material = _sceneRenderer.CollisionMaterial;
        UpdateEmptyState();

        Loaded += MainWindow_Loaded;

        _inputTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _inputTimer.Tick += InputTimer_Tick;
        _inputTimer.Start();
    }

    internal PackageDocument? CurrentDocument { get; set; }
    internal string? CurrentPackagePath { get; set; }
    internal static IReadOnlyList<string> SupportedFileExtensions => SupportedExtensions;
    public IEffectsManager EffectsManager { get; }

    internal ObservableCollection<ExportTreeNode> ExportNodes => _exportNodes;
    internal ObservableCollection<FileTreeItem> RootItems => _rootItems;
    internal AppSettings AppSettings => _appSettings;
    internal ScenePreviewRenderer SceneRenderer => _sceneRenderer;
    internal CameraPresetService CameraPresetService => _cameraPresetService;

    internal sealed record SkeletalAnimationListItem(
        string DisplayName,
        string SequenceName,
        float DurationSeconds,
        int NumFrames,
        int TrackCount);

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _loadingController.HandleLoadedAsync();
    }

    private async void OpenPackage_Click(object sender, RoutedEventArgs e)
    {
        await _loadingController.OpenPackageAsync();
    }

    private async void ReloadTreeButton_Click(object sender, RoutedEventArgs e)
    {
        await _loadingController.ReloadTreeAsync();
    }

    internal async void LoadSelectedMapLayersButton_Click(object sender, RoutedEventArgs e)
    {
        await _mapLayerLoader.LoadSelectedMapLayersAsync();
    }

    internal void NextStaticMeshButton_Click(object sender, RoutedEventArgs e)
    {
        _loadingController.CycleToNextMapStaticMesh();
    }

    internal async void ExportsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        await _exportPreviewService.PreviewSelectionAsync(e.NewValue);
    }

    internal async void ShowCollisionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        await _exportPreviewService.ShowCollisionChangedAsync();
    }

    internal async void ShowTerrainCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        await _exportPreviewService.ShowTerrainChangedAsync();
    }

    internal async void ShowWaterCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        await _exportPreviewService.ShowWaterChangedAsync();
    }

    private void AnimationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _skeletalAnimationController.AnimationSelectionChanged();
    }

    private void AnimateSkeletalPreviewCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _skeletalAnimationController.AnimateToggleChanged();
    }

    private void AnimationFrameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _skeletalAnimationController.FrameSliderChanged();
    }

    private void PrevAnimationFrameButton_Click(object sender, RoutedEventArgs e)
    {
        _skeletalAnimationController.StepFrame(-1);
    }

    private void NextAnimationFrameButton_Click(object sender, RoutedEventArgs e)
    {
        _skeletalAnimationController.StepFrame(1);
    }

    internal void SaveCameraButton_Click(object sender, RoutedEventArgs e)
    {
        _loadingController.SaveCurrentCamera();
    }

    internal void CamerasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _loadingController.ApplySelectedCamera(e);
    }

    internal void CameraSettings_Click(object sender, RoutedEventArgs e)
    {
        var wnd = new SettingsWindow(_appSettings) { Owner = this };
        if (wnd.ShowDialog() == true)
        {
            ApplyCameraSettings();
        }
    }

    internal void ApplyCameraSettings()
    {
        if (Viewport.CameraMode == HelixToolkit.SharpDX.CameraMode.WalkAround)
        {
            Viewport.RotationSensitivity = _appSettings.RotationSensitivity;
            Viewport.LeftRightRotationSensitivity = _appSettings.InvertX ? -_appSettings.RotationSensitivity : _appSettings.RotationSensitivity;
            Viewport.UpDownRotationSensitivity = _appSettings.InvertY ? -_appSettings.RotationSensitivity : _appSettings.RotationSensitivity;
        }
    }

    private void InputTimer_Tick(object? sender, EventArgs e)
    {
        _skeletalAnimationController.UpdateAnimationFrame();

        if (Viewport.CameraMode != HelixToolkit.SharpDX.CameraMode.WalkAround || !IsActive)
        {
            return;
        }

        if (System.Windows.Input.Keyboard.FocusedElement is TextBox)
        {
            return;
        }

        var moveSpeed = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift)
            ? _appSettings.CameraShiftSpeed
            : _appSettings.CameraMoveSpeed;
        var moveVector = new System.Windows.Media.Media3D.Vector3D();

        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.W)) moveVector.Z += 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.S)) moveVector.Z -= 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.A)) moveVector.X -= 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D)) moveVector.X += 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.E)) moveVector.Y += 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Q)) moveVector.Y -= 1;

        if (moveVector.LengthSquared > 0 && Viewport.Camera is HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera)
        {
            moveVector.Normalize();
            moveVector *= moveSpeed;

            var lookDir = camera.LookDirection;
            lookDir.Normalize();
            var upDir = camera.UpDirection;
            upDir.Normalize();
            var rightDir = System.Windows.Media.Media3D.Vector3D.CrossProduct(lookDir, upDir);
            rightDir.Normalize();

            var actualMove = (lookDir * moveVector.Z) + (rightDir * moveVector.X) + (upDir * moveVector.Y);
            var pos = camera.Position;
            camera.Position = new System.Windows.Media.Media3D.Point3D(pos.X + actualMove.X, pos.Y + actualMove.Y, pos.Z + actualMove.Z);
        }
    }

    private void Viewport_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Viewport.Focus();
    }

    internal void SetBusyState(bool isBusy)
    {
        OpenPackageButton.IsEnabled = !isBusy;
        ReloadTreeButton.IsEnabled = !isBusy;
        FilesTreeView.IsEnabled = !isBusy;
        ExportsTreeView.IsEnabled = !isBusy;
        UpdateEmptyState();
    }

    internal OperationItem BeginOperation(string description)
    {
        var item = new OperationItem(description);
        _activeOperations.Add(item);
        UpdateEmptyState();
        return item;
    }

    internal void EndOperation(OperationItem item)
    {
        _activeOperations.Remove(item);
        UpdateEmptyState();
    }

    internal void UpdateEmptyState()
    {
        EmptyPreviewBorder.Visibility =
            _activeOperations.Count == 0 &&
            TexturePreviewBorder.Visibility != Visibility.Visible &&
            MeshPreviewBorder.Visibility != Visibility.Visible
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    internal void SceneLighting_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingLighting)
        {
            return;
        }

        _isUpdatingLighting = true;

        if (sender == ShowSunCheckBox && ShowSunCheckBox.IsChecked == true)
        {
            ShowMoonCheckBox.IsChecked = false;
        }
        else if (sender == ShowMoonCheckBox && ShowMoonCheckBox.IsChecked == true)
        {
            ShowSunCheckBox.IsChecked = false;
        }

        AmbientLight.IsRendering = ShowAmbientCheckBox.IsChecked == true;
        UpdateLightingState();
        _isUpdatingLighting = false;
    }

    internal void UpdateLightingState()
    {
        var isUnr = string.Equals(Path.GetExtension(CurrentPackagePath), ".unr", StringComparison.OrdinalIgnoreCase);
        var showAmbient = ShowAmbientCheckBox.IsChecked == true;
        var showLocalLights = ShowLocalLightsCheckBox.IsChecked == true;
        var showSun = ShowSunCheckBox.IsChecked == true;
        var showMoon = ShowMoonCheckBox.IsChecked == true;
        var showShadows = ShowShadowsCheckBox.IsChecked == true;

        AmbientLight.IsRendering = showAmbient;
        PreviewKeyLight.IsRendering = !isUnr && showSun;
        PreviewFillLight.IsRendering = !isUnr && (showLocalLights || showMoon);
        MapLightsGroup.IsRendering = isUnr && showLocalLights;
        MapSunLightsGroup.IsRendering = isUnr && showSun;
        MapMoonLightsGroup.IsRendering = isUnr && showMoon;
        ShadowMap.IsRendering = isUnr && showShadows;

        _sceneRenderer.UpdateMaterialsShadowMap(isUnr && showShadows);
    }

    internal TreeView FilesTreeView => FileBrowser.FilesTreeView;
    internal TreeView ExportsTreeView => FileBrowser.ExportsTreeView;
    internal TabControl InfoTabControl => PackageInfoCtrl.InfoTabControl;
    internal TextBox DetailsTextBox => PackageInfoCtrl.DetailsTextBox;
    internal TextBlock MapNameTextBlock => PackageInfoCtrl.MapNameTextBlock;
    internal TextBlock MapVersionTextBlock => PackageInfoCtrl.MapVersionTextBlock;
    internal TextBlock TerrainStatusTextBlock => PackageInfoCtrl.TerrainStatusTextBlock;
    internal TextBlock HeightMapRefTextBlock => PackageInfoCtrl.HeightMapRefTextBlock;
    internal TextBlock HeightMapSizeTextBlock => PackageInfoCtrl.HeightMapSizeTextBlock;
    internal Image HeightMapPreviewImage => PackageInfoCtrl.HeightMapPreviewImage;
    internal TextBlock DiffuseTextureRefTextBlock => PackageInfoCtrl.DiffuseTextureRefTextBlock;
    internal TextBlock DiffuseTextureSizeTextBlock => PackageInfoCtrl.DiffuseTextureSizeTextBlock;
    internal Image DiffuseTexturePreviewImage => PackageInfoCtrl.DiffuseTexturePreviewImage;
    internal TextBlock RenderTextureRefTextBlock => PackageInfoCtrl.RenderTextureRefTextBlock;
    internal TextBlock RenderTextureSizeTextBlock => PackageInfoCtrl.RenderTextureSizeTextBlock;
    internal Image RenderTexturePreviewImage => PackageInfoCtrl.RenderTexturePreviewImage;
    internal ItemsControl TerrainTexturesItemsControl => PackageInfoCtrl.TerrainTexturesItemsControl;
    internal ItemsControl ActiveOperationsItemsControl => ActivityCtrl.ActiveOperationsHost;
    internal StackPanel ActivityPanel => ActivityCtrl.ActivityPanelHost;
    internal TextBlock StatusTextBlock => ActivityCtrl.StatusTextBlockHost;
    internal StackPanel UnrOptionsPanel => OptionsCtrl.UnrOptionsPanelHost;
    internal StackPanel StaticMeshOptionsPanel => OptionsCtrl.StaticMeshOptionsPanelHost;
    internal StackPanel SceneLightingPanel => OptionsCtrl.SceneLightingPanelHost;
    internal CheckBox ShowTerrainCheckBox => OptionsCtrl.ShowTerrainCheckBoxHost;
    internal CheckBox ShowWaterCheckBox => OptionsCtrl.ShowWaterCheckBoxHost;
    internal CheckBox LoadPolysCheckBox => OptionsCtrl.LoadPolysCheckBoxHost;
    internal CheckBox LoadBspCheckBox => OptionsCtrl.LoadBspCheckBoxHost;
    internal CheckBox LoadPropsCheckBox => OptionsCtrl.LoadPropsCheckBoxHost;
    internal Button LoadSelectedButton => OptionsCtrl.LoadSelectedButtonHost;
    internal Button NextStaticMeshButton => OptionsCtrl.NextStaticMeshButtonHost;
    internal ComboBox CamerasComboBox => OptionsCtrl.CamerasComboBoxHost;
    internal Button SaveCameraButton => OptionsCtrl.SaveCameraButtonHost;
    internal Button CameraSettingsButton => OptionsCtrl.CameraSettingsButtonHost;
    internal CheckBox ShowCollisionCheckBox => OptionsCtrl.ShowCollisionCheckBoxHost;
    internal CheckBox ShowAmbientCheckBox => OptionsCtrl.ShowAmbientCheckBoxHost;
    internal CheckBox ShowLocalLightsCheckBox => OptionsCtrl.ShowLocalLightsCheckBoxHost;
    internal CheckBox ShowShadowsCheckBox => OptionsCtrl.ShowShadowsCheckBoxHost;
    internal CheckBox ShowSunCheckBox => OptionsCtrl.ShowSunCheckBoxHost;
    internal CheckBox ShowMoonCheckBox => OptionsCtrl.ShowMoonCheckBoxHost;
    internal Border EmptyPreviewBorder => PreviewCtrl.EmptyPreviewBorderHost;
    internal Border TexturePreviewBorder => PreviewCtrl.TexturePreviewBorderHost;
    internal Border MeshPreviewBorder => PreviewCtrl.MeshPreviewBorderHost;
    internal TextBlock PackageTitleTextBlockControl => (TextBlock)FindName("PackageTitleTextBlock");
    internal TextBlock PackageMetaTextBlockControl => (TextBlock)FindName("PackageMetaTextBlock");
    internal TextBlock PreviewTitleTextBlockControl => (TextBlock)FindName("PreviewTitleTextBlock");
    internal TextBlock PreviewSubtitleTextBlockControl => (TextBlock)FindName("PreviewSubtitleTextBlock");
    internal Image TextureImage => PreviewCtrl.TextureImageHost;
    internal Viewport3DX Viewport => PreviewCtrl.ViewportHost;
    internal HelixToolkit.Wpf.SharpDX.PerspectiveCamera Camera => PreviewCtrl.CameraHost;
    internal AmbientLight3D AmbientLight => PreviewCtrl.AmbientLightHost;
    internal ShadowMap3D ShadowMap => PreviewCtrl.ShadowMapHost;
    internal DirectionalLight3D PreviewKeyLight => PreviewCtrl.PreviewKeyLightHost;
    internal DirectionalLight3D PreviewFillLight => PreviewCtrl.PreviewFillLightHost;
    internal GroupModel3D MapSunLightsGroup => PreviewCtrl.MapSunLightsGroupHost;
    internal GroupModel3D MapMoonLightsGroup => PreviewCtrl.MapMoonLightsGroupHost;
    internal GroupModel3D MapLightsGroup => PreviewCtrl.MapLightsGroupHost;
    internal MeshGeometryModel3D MeshModel => PreviewCtrl.MeshModelHost;
    internal MeshGeometryModel3D CollisionMeshModel => PreviewCtrl.CollisionMeshModelHost;
    internal GroupModel3D WaterSceneGroup => PreviewCtrl.WaterSceneGroupHost;
    internal GroupModel3D VolumeSceneGroup => PreviewCtrl.VolumeSceneGroupHost;
    internal GroupModel3D PropSceneGroup => PreviewCtrl.PropSceneGroupHost;
    internal GroupModel3D PolySceneGroup => PreviewCtrl.PolySceneGroupHost;
    internal LineGeometryModel3D BoundsModel => PreviewCtrl.BoundsModelHost;
    internal LineGeometryModel3D BoneOverlayModel => PreviewCtrl.BoneOverlayModelHost;
    internal PointGeometryModel3D VertexDebugModel => PreviewCtrl.VertexDebugModelHost;
    internal Border AnimationPanelBorderControl => AnimationPanelBorder;
    internal TextBlock AnimationSummaryTextBlockControl => AnimationSummaryTextBlock;
    internal ListBox AnimationListBoxControl => AnimationListBox;
    internal CheckBox AnimateSkeletalPreviewCheckBoxControl => AnimateSkeletalPreviewCheckBox;
    internal CheckBox LoopAnimationCheckBoxControl => LoopAnimationCheckBox;
    internal Slider AnimationFrameSliderControl => AnimationFrameSlider;
    internal TextBlock AnimationFrameTextBlockControl => AnimationFrameTextBlock;
    internal Button PrevAnimationFrameButtonControl => PrevAnimationFrameButton;
    internal Button NextAnimationFrameButtonControl => NextAnimationFrameButton;
}
