using Microsoft.Win32;
using HelixToolkit.Wpf.SharpDX;
using L2Viewer.DatFile;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services;
using System.Windows.Controls;

namespace L2Viewer.Wpf;

internal sealed class MainWindowLoadingController
{
    private readonly MainWindow _owner;
    private readonly ScenePreviewRenderer _renderer;
    private readonly CameraPresetService _cameraPresetService;
    private CancellationTokenSource? _packageLoadCts;
    private readonly List<MapInitialCameraTarget> _mapCameraTargets = [];
    private int _mapCameraTargetIndex = -1;
    private sealed record MapInitialCameraTarget(System.Numerics.Vector3 BoundsMin, System.Numerics.Vector3 BoundsMax, string Label);

    public MainWindowLoadingController(MainWindow owner, ScenePreviewRenderer renderer, CameraPresetService cameraPresetService)
    {
        _owner = owner;
        _renderer = renderer;
        _cameraPresetService = cameraPresetService;
    }

    private TreeView FilesTreeView => _owner.FilesTreeView;
    private TreeView ExportsTreeView => _owner.ExportsTreeView;
    private TextBox ClientRootTextBox => _owner.FindName("ClientRootTextBox") as TextBox
        ?? throw new InvalidOperationException("ClientRootTextBox not initialized.");
    private TextBlock StatusTextBlock => _owner.StatusTextBlock;
    private TextBox DetailsTextBox => _owner.DetailsTextBox;
    private Button ReloadTreeButton => _owner.FindName("ReloadTreeButton") as Button
        ?? throw new InvalidOperationException("ReloadTreeButton not initialized.");
    private Button OpenPackageButton => _owner.FindName("OpenPackageButton") as Button
        ?? throw new InvalidOperationException("OpenPackageButton not initialized.");
    private Button NextStaticMeshButton => _owner.NextStaticMeshButton;
    private CheckBox ShowCollisionCheckBox => _owner.ShowCollisionCheckBox;
    private ComboBox CamerasComboBox => _owner.CamerasComboBox;
    private TextBlock PackageTitleTextBlock => _owner.PackageTitleTextBlockControl;
    private TextBlock PackageMetaTextBlock => _owner.PackageMetaTextBlockControl;
    private TextBlock PreviewTitleTextBlock => _owner.PreviewTitleTextBlockControl;
    private TextBlock PreviewSubtitleTextBlock => _owner.PreviewSubtitleTextBlockControl;
    private TabControl InfoTabControl => _owner.InfoTabControl;
    private StackPanel UnrOptionsPanel => _owner.UnrOptionsPanel;
    private StackPanel StaticMeshOptionsPanel => _owner.StaticMeshOptionsPanel;
    private Viewport3DX Viewport => _owner.Viewport;

    public async Task HandleLoadedAsync()
    {
        FilesTreeView.SelectedItemChanged += FilesTreeView_SelectedItemChanged;
        FilesTreeView.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(FilesTreeViewItem_Expanded));
        ExportsTreeView.SelectedItemChanged += _owner.ExportsTreeView_SelectedItemChanged;
        _owner.ShowTerrainCheckBox.Checked += _owner.ShowTerrainCheckBox_Changed;
        _owner.ShowTerrainCheckBox.Unchecked += _owner.ShowTerrainCheckBox_Changed;
        _owner.ShowWaterCheckBox.Checked += _owner.ShowWaterCheckBox_Changed;
        _owner.ShowWaterCheckBox.Unchecked += _owner.ShowWaterCheckBox_Changed;
        _owner.LoadPolysCheckBox.Checked += MapLayerVisibilityCheckBox_Changed;
        _owner.LoadPolysCheckBox.Unchecked += MapLayerVisibilityCheckBox_Changed;
        _owner.LoadBspCheckBox.Checked += MapLayerVisibilityCheckBox_Changed;
        _owner.LoadBspCheckBox.Unchecked += MapLayerVisibilityCheckBox_Changed;
        _owner.LoadPropsCheckBox.Checked += MapLayerVisibilityCheckBox_Changed;
        _owner.LoadPropsCheckBox.Unchecked += MapLayerVisibilityCheckBox_Changed;
        _owner.ShowCollisionCheckBox.Checked += _owner.ShowCollisionCheckBox_Changed;
        _owner.ShowCollisionCheckBox.Unchecked += _owner.ShowCollisionCheckBox_Changed;
        _owner.LoadSelectedButton.Click += _owner.LoadSelectedMapLayersButton_Click;
        _owner.NextStaticMeshButton.Click += _owner.NextStaticMeshButton_Click;
        _owner.SaveCameraButton.Click += _owner.SaveCameraButton_Click;
        _owner.CamerasComboBox.SelectionChanged += _owner.CamerasComboBox_SelectionChanged;
        _owner.CameraSettingsButton.Click += _owner.CameraSettings_Click;
        _owner.ShowAmbientCheckBox.Checked += _owner.SceneLighting_Changed;
        _owner.ShowAmbientCheckBox.Unchecked += _owner.SceneLighting_Changed;
        _owner.ShowLocalLightsCheckBox.Checked += _owner.SceneLighting_Changed;
        _owner.ShowLocalLightsCheckBox.Unchecked += _owner.SceneLighting_Changed;
        _owner.ShowShadowsCheckBox.Checked += _owner.SceneLighting_Changed;
        _owner.ShowShadowsCheckBox.Unchecked += _owner.SceneLighting_Changed;
        _owner.ShowSunCheckBox.Checked += _owner.SceneLighting_Changed;
        _owner.ShowSunCheckBox.Unchecked += _owner.SceneLighting_Changed;
        _owner.ShowMoonCheckBox.Checked += _owner.SceneLighting_Changed;
        _owner.ShowMoonCheckBox.Unchecked += _owner.SceneLighting_Changed;

        _cameraPresetService.LoadSavedCameraPresets();
        _owner.SceneLighting_Changed(_owner, new RoutedEventArgs());
        await ReloadTreeAsync();
    }

    public async Task OpenPackageAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Client Files|*.utx;*.usx;*.ukx;*.u;*.unr;*.dat|All Files|*.*",
            Title = "Open Unreal Package"
        };

        if (dialog.ShowDialog(_owner) == true)
        {
            await LoadPackageAsync(dialog.FileName, autoSelectInTree: true);
        }
    }

    public async Task ReloadTreeAsync()
    {
        var root = ClientRootTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            _owner.RootItems.Clear();
            StatusTextBlock.Text = "Client root folder was not found.";
            return;
        }

        var op = _owner.BeginOperation("Loading client file tree");
        StatusTextBlock.Text = "Loading file tree...";
        ReloadTreeButton.IsEnabled = false;
        OpenPackageButton.IsEnabled = false;
        try
        {
            var items = await Task.Run(() => FileTreeService.BuildRootTree(root));
            _owner.RootItems.Clear();
            foreach (var item in items)
            {
                _owner.RootItems.Add(item);
            }

            StatusTextBlock.Text = $"Loaded tree for {root}.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Tree load failed: {ex.Message}";
        }
        finally
        {
            ReloadTreeButton.IsEnabled = true;
            OpenPackageButton.IsEnabled = true;
            _owner.EndOperation(op);
        }
    }

    public void SaveCurrentCamera()
    {
        var savedName = _cameraPresetService.SaveCurrentCamera(_owner.CurrentPackagePath, Viewport, CamerasComboBox);
        if (!string.IsNullOrWhiteSpace(savedName))
        {
            StatusTextBlock.Text = $"Saved camera preset '{savedName}'.";
        }
    }

    public void ApplySelectedCamera(SelectionChangedEventArgs e)
    {
        _cameraPresetService.ApplySelectedCamera(e, CamerasComboBox, Viewport);
    }

    public void CycleToNextMapStaticMesh()
    {
        if (_mapCameraTargets.Count == 0)
        {
            StatusTextBlock.Text = "No static mesh camera targets were found for this map.";
            return;
        }

        _mapCameraTargetIndex = (_mapCameraTargetIndex + 1) % _mapCameraTargets.Count;
        var target = _mapCameraTargets[_mapCameraTargetIndex];
        _renderer.FitCamera(target.BoundsMin, target.BoundsMax);
        PreviewSubtitleTextBlock.Text = $"Map scene | target: {target.Label} ({_mapCameraTargetIndex + 1}/{_mapCameraTargets.Count})";
        StatusTextBlock.Text = $"Focused static mesh target {_mapCameraTargetIndex + 1}/{_mapCameraTargets.Count}: {target.Label}";
    }

    private void MapLayerVisibilityCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (string.Equals(Path.GetExtension(_owner.CurrentPackagePath), ".unr", StringComparison.OrdinalIgnoreCase))
        {
            _renderer.ShowPreviewForCurrentMode();
        }
    }

    private async void FilesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FileTreeItem item && !item.IsDirectory)
        {
            await LoadPackageAsync(item.FullPath, autoSelectInTree: false);
        }
    }

    private async void FilesTreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem tvi || tvi.DataContext is not FileTreeItem item || !item.IsDirectory || !item.HasDummyChild)
        {
            return;
        }

        try
        {
            var children = await Task.Run(() => FileTreeService.BuildChildren(item.FullPath));
            item.Children.Clear();
            foreach (var child in children)
            {
                item.Children.Add(child);
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Folder expand failed: {ex.Message}";
        }
    }

    internal async Task LoadPackageAsync(string path, bool autoSelectInTree)
    {
        if (!File.Exists(path))
        {
            StatusTextBlock.Text = $"File not found: {path}";
            return;
        }

        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".dat", StringComparison.OrdinalIgnoreCase))
        {
            await LoadDatDocumentAsync(path, autoSelectInTree);
            return;
        }

        _packageLoadCts?.Cancel();
        _packageLoadCts?.Dispose();
        var cts = new CancellationTokenSource();
        _packageLoadCts = cts;
        var token = cts.Token;

        _owner.SetBusyState(true);
        System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        var op = _owner.BeginOperation($"Opening package {Path.GetFileName(path)}");
        StatusTextBlock.Text = $"Opening {Path.GetFileName(path)}...";
        DetailsTextBox.Text = string.Empty;
        _renderer.CurrentStaticMeshDefinition = null;
        _renderer.ShowStaticMeshCollision = false;
        ShowCollisionCheckBox.IsChecked = false;
        _renderer.ClearPropSceneState();
        _renderer.ClearWaterSceneState();
        _renderer.ClearPolySceneState();
        _owner.VolumeSceneGroup.Children.Clear();
        _renderer.ResetScene();
        _renderer.ShowEmptyPreview();
        ClearExportSelection();
        _mapCameraTargets.Clear();
        _mapCameraTargetIndex = -1;

        try
        {
            var document = await Task.Run(() => PackageDocument.Load(path), token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _owner.CurrentDocument = document;
            _owner.CurrentPackagePath = path;
            _renderer.CurrentTerrainPreview = null;
            _renderer.ClearPropSceneState();
            _renderer.ClearWaterSceneState();
            _renderer.ClearPolySceneState();
            _owner.VolumeSceneGroup.Children.Clear();
            UpdateDocumentSpecificUi(path);
            NextStaticMeshButton.Visibility = string.Equals(Path.GetExtension(path), ".unr", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
            _owner.ExportNodes.Clear();
            foreach (var group in document.Exports
                         .OrderBy(x => x.ClassName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.ObjectName, StringComparer.OrdinalIgnoreCase)
                         .GroupBy(x => x.ClassName, StringComparer.OrdinalIgnoreCase))
            {
                _owner.ExportNodes.Add(ExportTreeNode.CreateGroup(group.Key, group));
            }

            PackageTitleTextBlock.Text = Path.GetFileName(document.Package.Path);
            PackageMetaTextBlock.Text = "unsupport debug show, maybe later";
            PreviewTitleTextBlock.Text = string.Empty;
            PreviewSubtitleTextBlock.Text = string.Empty;
            DetailsTextBox.Text = "unsupport debug show, maybe later";
            if (string.Equals(Path.GetExtension(path), ".unr", StringComparison.OrdinalIgnoreCase))
            {
                _renderer.UpdateMapInfoPanel(document, null);
            }
            else
            {
                _renderer.ClearMapInfoPanel();
            }

            StatusTextBlock.Text = $"Loaded {Path.GetFileName(document.Package.Path)}.";
            await UpdateMapInfoAndAutoPreviewAsync(document, path, token);
            if (autoSelectInTree)
            {
                FileTreeService.TrySelectPathInTree(_owner.RootItems, path);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _owner.CurrentDocument = null;
            _owner.CurrentPackagePath = null;
            _renderer.CurrentTerrainPreview = null;
            _renderer.CurrentStaticMeshDefinition = null;
            _renderer.ClearPropSceneState();
            _renderer.ClearWaterSceneState();
            _renderer.ClearPolySceneState();
            _owner.VolumeSceneGroup.Children.Clear();
            UpdateDocumentSpecificUi(null);
            NextStaticMeshButton.Visibility = Visibility.Collapsed;
            _owner.ExportNodes.Clear();
            PackageTitleTextBlock.Text = Path.GetFileName(path);
            PackageMetaTextBlock.Text = string.Empty;
            StatusTextBlock.Text = $"Open failed: {ex.Message}";
            DetailsTextBox.Text = ex.ToString();
            _renderer.ClearMapInfoPanel();
        }
        finally
        {
            _owner.SetBusyState(false);
            System.Windows.Input.Mouse.OverrideCursor = null;
            _owner.EndOperation(op);
        }
    }

    private async Task LoadDatDocumentAsync(string path, bool autoSelectInTree)
    {
        _owner.SetBusyState(true);
        System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        var op = _owner.BeginOperation($"Opening DAT {Path.GetFileName(path)}");
        StatusTextBlock.Text = $"Opening {Path.GetFileName(path)}...";
        DetailsTextBox.Text = string.Empty;
        _renderer.CurrentStaticMeshDefinition = null;
        _renderer.ShowStaticMeshCollision = false;
        ShowCollisionCheckBox.IsChecked = false;
        _renderer.ClearPropSceneState();
        _renderer.ClearWaterSceneState();
        _renderer.ClearPolySceneState();
        _owner.VolumeSceneGroup.Children.Clear();
        _renderer.ResetScene();
        _renderer.ShowEmptyPreview();
        ClearExportSelection();
        _mapCameraTargets.Clear();
        _mapCameraTargetIndex = -1;

        try
        {
            var document = await Task.Run(() => DatFileReader.ReadDocument(path));
            var json = await Task.Run(() => FileInspectionJsonSerializer.Serialize(document));

            _owner.CurrentDocument = null;
            _owner.CurrentPackagePath = path;
            _renderer.CurrentTerrainPreview = null;
            _renderer.ClearPropSceneState();
            _renderer.ClearWaterSceneState();
            _renderer.ClearPolySceneState();
            _owner.VolumeSceneGroup.Children.Clear();
            UpdateDocumentSpecificUi(path);
            NextStaticMeshButton.Visibility = Visibility.Collapsed;
            _owner.ExportNodes.Clear();
            PackageTitleTextBlock.Text = Path.GetFileName(path);
            PackageMetaTextBlock.Text = document.GetType().Name;
            PreviewTitleTextBlock.Text = string.Empty;
            PreviewSubtitleTextBlock.Text = string.Empty;
            DetailsTextBox.Text = json;
            _renderer.ClearMapInfoPanel();
            StatusTextBlock.Text = $"Loaded {Path.GetFileName(path)}.";

            if (autoSelectInTree)
            {
                FileTreeService.TrySelectPathInTree(_owner.RootItems, path);
            }
        }
        catch (Exception ex)
        {
            _owner.CurrentDocument = null;
            _owner.CurrentPackagePath = null;
            _renderer.CurrentTerrainPreview = null;
            _renderer.CurrentStaticMeshDefinition = null;
            _renderer.ClearPropSceneState();
            _renderer.ClearWaterSceneState();
            _renderer.ClearPolySceneState();
            _owner.VolumeSceneGroup.Children.Clear();
            UpdateDocumentSpecificUi(null);
            NextStaticMeshButton.Visibility = Visibility.Collapsed;
            _owner.ExportNodes.Clear();
            PackageTitleTextBlock.Text = Path.GetFileName(path);
            PackageMetaTextBlock.Text = string.Empty;
            StatusTextBlock.Text = $"Open failed: {ex.Message}";
            DetailsTextBox.Text = ex.ToString();
            _renderer.ClearMapInfoPanel();
        }
        finally
        {
            _owner.SetBusyState(false);
            System.Windows.Input.Mouse.OverrideCursor = null;
            _owner.EndOperation(op);
        }
    }

    private void ClearExportSelection()
    {
        if (ExportsTreeView.SelectedItem is TreeViewItem treeViewItem)
        {
            treeViewItem.IsSelected = false;
        }
    }

    private void UpdateDocumentSpecificUi(string? path)
    {
        var isUnr = !string.IsNullOrWhiteSpace(path) &&
                    string.Equals(Path.GetExtension(path), ".unr", StringComparison.OrdinalIgnoreCase);
        UnrOptionsPanel.Visibility = isUnr ? Visibility.Visible : Visibility.Collapsed;
        StaticMeshOptionsPanel.Visibility = Visibility.Collapsed;
        InfoTabControl.SelectedIndex = isUnr ? 0 : 1;

        if (isUnr)
        {
            Viewport.CameraMode = HelixToolkit.SharpDX.CameraMode.WalkAround;
            Viewport.CameraRotationMode = HelixToolkit.SharpDX.CameraRotationMode.Turntable;
            Viewport.IsMoveEnabled = false;
            _owner.ApplyCameraSettings();
            Viewport.InputBindings.Clear();
            Viewport.InputBindings.Add(new System.Windows.Input.KeyBinding { Key = System.Windows.Input.Key.D, Command = System.Windows.Input.ApplicationCommands.NotACommand });
            Viewport.InputBindings.Add(new System.Windows.Input.MouseBinding { Gesture = new System.Windows.Input.MouseGesture(System.Windows.Input.MouseAction.RightClick), Command = HelixToolkit.Wpf.SharpDX.ViewportCommands.Rotate });
            Viewport.InputBindings.Add(new System.Windows.Input.MouseBinding { Gesture = new System.Windows.Input.MouseGesture(System.Windows.Input.MouseAction.LeftClick), Command = System.Windows.Input.ApplicationCommands.NotACommand });
        }
        else
        {
            Viewport.CameraMode = HelixToolkit.SharpDX.CameraMode.Inspect;
            Viewport.CameraRotationMode = HelixToolkit.SharpDX.CameraRotationMode.Turnball;
            Viewport.IsMoveEnabled = true;
            Viewport.RotationSensitivity = 1.0;
            Viewport.LeftRightRotationSensitivity = 1.0;
            Viewport.UpDownRotationSensitivity = 1.0;
            Viewport.InputBindings.Clear();
            Viewport.InputBindings.Add(new System.Windows.Input.KeyBinding { Key = System.Windows.Input.Key.D, Command = System.Windows.Input.ApplicationCommands.NotACommand });
            Viewport.InputBindings.Add(new System.Windows.Input.MouseBinding { Gesture = new System.Windows.Input.MouseGesture(System.Windows.Input.MouseAction.LeftClick), Command = HelixToolkit.Wpf.SharpDX.ViewportCommands.Rotate });
            Viewport.InputBindings.Add(new System.Windows.Input.MouseBinding { Gesture = new System.Windows.Input.MouseGesture(System.Windows.Input.MouseAction.RightClick), Command = HelixToolkit.Wpf.SharpDX.ViewportCommands.Pan });
        }

        _owner.UpdateLightingState();
    }

    private async Task UpdateMapInfoAndAutoPreviewAsync(PackageDocument document, string path, CancellationToken token)
    {
        if (!string.Equals(Path.GetExtension(path), ".unr", StringComparison.OrdinalIgnoreCase))
        {
            _renderer.ClearMapInfoPanel();
            return;
        }

        var clientRoot = ClientRootTextBox.Text.Trim();
        var mapInfoResult = await Task.Run(() =>
        {
            var unr = UnrFileReader.Read(document.Package.Path);
            var mapInfo = _renderer.BuildMapSceneInfo(unr);
            var cameraTargets = string.IsNullOrWhiteSpace(clientRoot)
                ? null
                : BuildInitialCameraTargets(unr, clientRoot);
            return (mapInfo, cameraTargets);
        }, token);
        if (token.IsCancellationRequested)
        {
            return;
        }

        _renderer.CurrentTerrainPreview = null;
        _renderer.CurrentMapScene = mapInfoResult.mapInfo;
        _renderer.ClearPropSceneState();
        _renderer.ClearWaterSceneState();
        _renderer.ClearPolySceneState();
        _renderer.UpdateMapInfoPanel(document, null);
        DetailsTextBox.Text = "unsupport debug show, maybe later";
        _cameraPresetService.BindPackageCameras(path, CamerasComboBox);
        _mapCameraTargets.Clear();
        if (mapInfoResult.cameraTargets is not null)
        {
            _mapCameraTargets.AddRange(mapInfoResult.cameraTargets);
        }
        _mapCameraTargetIndex = -1;
        _renderer.ShowPreviewForCurrentMode();

        if (_renderer.IsDefaultCameraState() && _mapCameraTargets.Count > 0)
        {
            var target = _mapCameraTargets[0];
            _mapCameraTargetIndex = 0;
            _renderer.FitCamera(target.BoundsMin, target.BoundsMax);
            PreviewSubtitleTextBlock.Text = $"Map scene | start target: {target.Label}";
        }

        if (!token.IsCancellationRequested)
        {
            StatusTextBlock.Text = $"Loaded {Path.GetFileName(document.Package.Path)}. Select layers and click Load Selected.";
        }
    }

    private static IReadOnlyList<MapInitialCameraTarget> BuildInitialCameraTargets(L2Viewer.UnrFile.UnrFile unr, string clientRoot)
    {
        var textureManager = new BspTextureManager(clientRoot);
        var resolver = new SceneStaticMeshResolver(clientRoot, textureManager);
        var metadata = new SceneInstancedMeshMetadataBuilder().Build(unr);
        var candidates = metadata.Instances
            .Where(x => x.WorldLocation is not null && !string.IsNullOrWhiteSpace(x.MeshReference))
            .OrderByDescending(IsTreeLikeCandidate)
            .ThenBy(x => x.ActorName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var meshReferences = candidates
            .Select(x => ParseMeshReference(x.MeshReference!))
            .Where(x => x is not null)
            .Cast<UnrFileObjectReference>()
            .ToArray();
        if (meshReferences.Length == 0)
        {
            return [];
        }

        var resolved = resolver.ResolveMany(unr.FilePath, meshReferences);
        var result = new List<MapInitialCameraTarget>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (candidate.MeshReference is null || !resolved.TryGetValue(candidate.MeshReference, out var mesh))
            {
                continue;
            }

            var bounds = TransformBounds(mesh.RenderGeometry.BoundsMin, mesh.RenderGeometry.BoundsMax, candidate);
            result.Add(new MapInitialCameraTarget(bounds.Min, bounds.Max, candidate.ActorName));
        }

        return result;
    }

    private static int IsTreeLikeCandidate(SceneStaticMeshInstanceMetadata instance)
    {
        var actorName = instance.ActorName ?? string.Empty;
        var className = instance.ClassName ?? string.Empty;
        var meshReference = instance.MeshReference ?? string.Empty;
        return actorName.Contains("tree", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("tree", StringComparison.OrdinalIgnoreCase) ||
               meshReference.Contains("tree", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
    }

    private static UnrFileObjectReference? ParseMeshReference(string reference)
    {
        var parts = reference.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        return new UnrFileObjectReference
        {
            RawReference = 0,
            Kind = UnrFileReferenceKind.Import,
            ClassName = UnrealClassNames.StaticMesh,
            PackageName = parts[0],
            ObjectName = parts[1]
        };
    }

    private static (System.Numerics.Vector3 Min, System.Numerics.Vector3 Max) TransformBounds(
        System.Numerics.Vector3 boundsMin,
        System.Numerics.Vector3 boundsMax,
        SceneStaticMeshInstanceMetadata instance)
    {
        var corners = new[]
        {
            new System.Numerics.Vector3(boundsMin.X, boundsMin.Y, boundsMin.Z),
            new System.Numerics.Vector3(boundsMax.X, boundsMin.Y, boundsMin.Z),
            new System.Numerics.Vector3(boundsMax.X, boundsMax.Y, boundsMin.Z),
            new System.Numerics.Vector3(boundsMin.X, boundsMax.Y, boundsMin.Z),
            new System.Numerics.Vector3(boundsMin.X, boundsMin.Y, boundsMax.Z),
            new System.Numerics.Vector3(boundsMax.X, boundsMin.Y, boundsMax.Z),
            new System.Numerics.Vector3(boundsMax.X, boundsMax.Y, boundsMax.Z),
            new System.Numerics.Vector3(boundsMin.X, boundsMax.Y, boundsMax.Z)
        };

        var min = new System.Numerics.Vector3(float.MaxValue);
        var max = new System.Numerics.Vector3(float.MinValue);
        foreach (var corner in corners)
        {
            var transformed = TransformPoint(corner, instance);
            min = System.Numerics.Vector3.Min(min, transformed);
            max = System.Numerics.Vector3.Max(max, transformed);
        }

        return (min, max);
    }

    private static System.Numerics.Vector3 TransformPoint(System.Numerics.Vector3 point, SceneStaticMeshInstanceMetadata instance)
    {
        var scaled = point * instance.Scale;
        var translated = scaled - instance.PrePivot;
        var rotated = RotateByEulerDegrees(translated, instance.RotationEulerDegrees ?? System.Numerics.Vector3.Zero);
        return rotated + (instance.WorldLocation ?? System.Numerics.Vector3.Zero);
    }

    private static System.Numerics.Vector3 RotateByEulerDegrees(System.Numerics.Vector3 value, System.Numerics.Vector3 degrees)
    {
        var radians = degrees * (MathF.PI / 180f);
        var rotation = System.Numerics.Quaternion.CreateFromYawPitchRoll(radians.Z, radians.Y, radians.X);
        return System.Numerics.Vector3.Transform(value, rotation);
    }
}
