using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Services;
using UtxTextureCodec = L2Viewer.UtxFile.TextureCodec;

namespace L2Viewer.Wpf;

internal sealed class ExportPreviewService
{
    private readonly MainWindow _owner;
    private readonly ScenePreviewRenderer _renderer;
    private readonly SkeletalAnimationPreviewController _skeletalAnimationController;
    private CancellationTokenSource? _previewCts;

    public ExportPreviewService(MainWindow owner, ScenePreviewRenderer renderer, SkeletalAnimationPreviewController skeletalAnimationPreviewController)
    {
        _owner = owner;
        _renderer = renderer;
        _skeletalAnimationController = skeletalAnimationPreviewController;
    }

    public async Task PreviewSelectionAsync(object? selectedItem)
    {
        if (selectedItem is ExportTreeNode node && node.ExportInfo is not null)
        {
            await PreviewExportNodeAsync(node);
        }
    }

    public async Task ShowCollisionChangedAsync()
    {
        _renderer.ShowStaticMeshCollision = _owner.ShowCollisionCheckBox.IsChecked == true;
        if (_renderer.CurrentStaticMeshDefinition is null)
        {
            _owner.CollisionMeshModel.Geometry = null;
            return;
        }

        if (_owner.ExportsTreeView.SelectedItem is ExportTreeNode node && node.ExportInfo is not null &&
            node.ExportInfo.ClassName.Is(UnrealClassNames.StaticMesh))
        {
            await PreviewExportNodeAsync(node);
        }
    }

    public async Task ShowTerrainChangedAsync()
    {
        _renderer.ShowTerrain = _owner.ShowTerrainCheckBox.IsChecked != false;
        if (!Path.GetExtension(_owner.CurrentPackagePath).Is(".unr") &&
            _owner.ExportsTreeView.SelectedItem is ExportTreeNode node && node.ExportInfo is not null)
        {
            await PreviewExportNodeAsync(node);
        }
    }

    public async Task ShowWaterChangedAsync()
    {
        _renderer.ShowWater = _owner.ShowWaterCheckBox.IsChecked != false;
        if (!Path.GetExtension(_owner.CurrentPackagePath).Is(".unr") &&
            _owner.ExportsTreeView.SelectedItem is ExportTreeNode node && node.ExportInfo is not null)
        {
            await PreviewExportNodeAsync(node);
        }
    }

    internal async Task PreviewExportNodeAsync(ExportTreeNode node)
    {
        if (_owner.CurrentDocument is null || node.ExportInfo is null)
        {
            return;
        }

        var exportInfo = node.ExportInfo;
        var (token, op) = BeginPreviewOperation(exportInfo);
        InitializePreviewState(exportInfo);
        try
        {
            await PreviewExportByClassAsync(exportInfo, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _owner.StatusTextBlock.Text = $"Preview failed: {ex.Message}";
            _owner.DetailsTextBox.Text = ex.ToString();
            _renderer.ShowEmptyPreview();
        }
        finally
        {
            _owner.EndOperation(op);
        }
    }

    private (CancellationToken Token, OperationItem Operation) BeginPreviewOperation(PackageExportInfo exportInfo)
    {
        _previewCts?.Cancel();
        _previewCts?.Dispose();
        _previewCts = new CancellationTokenSource();
        return (_previewCts.Token, _owner.BeginOperation($"Preparing preview {exportInfo.ObjectName}"));
    }

    private void InitializePreviewState(PackageExportInfo exportInfo)
    {
        var isStaticMeshExport = exportInfo.ClassName.Is(UnrealClassNames.StaticMesh);
        _owner.StaticMeshOptionsPanel.Visibility = isStaticMeshExport ? Visibility.Visible : Visibility.Collapsed;
        _owner.PreviewTitleTextBlockControl.Text = exportInfo.ObjectName;
        _owner.PreviewSubtitleTextBlockControl.Text = string.Empty;
        _renderer.ShowPreviewForCurrentMode();
        _owner.StatusTextBlock.Text = $"Loading {exportInfo.ObjectName}...";
        _owner.DetailsTextBox.Text = PackageSummaryBuilder.BuildExportSummary(_owner.CurrentDocument!, exportInfo);
        _skeletalAnimationController.Reset();

        if (!isStaticMeshExport)
        {
            _renderer.CurrentStaticMeshDefinition = null;
            _owner.CollisionMeshModel.Geometry = null;
            _owner.BoneOverlayModel.Geometry = null;
        }
    }

    private async Task PreviewExportByClassAsync(PackageExportInfo exportInfo, CancellationToken token)
    {
        if (exportInfo.ClassName.Is(UnrealClassNames.Texture))
        {
            await PreviewTextureExportAsync(exportInfo, token);
            return;
        }

        if (exportInfo.ClassName.Is("TerrainInfo"))
        {
            await PreviewTerrainExportAsync(exportInfo, token);
            return;
        }

        if (exportInfo.ClassName.Is(UnrealClassNames.StaticMesh))
        {
            await PreviewStaticMeshExportAsync(exportInfo, token);
            return;
        }

        if (exportInfo.ClassName.Is(UnrealClassNames.SkeletalMesh))
        {
            await PreviewSkeletalMeshExportAsync(exportInfo, token);
            return;
        }

        await PreviewActorOrMetadataAsync(exportInfo, token);
    }

    private async Task PreviewTextureExportAsync(PackageExportInfo exportInfo, CancellationToken token)
    {
        var texture = await Task.Run(() =>
        {
            var package = PackageReader.LoadPackage(_owner.CurrentDocument!.Package.Path);
            return UtxTextureCodec.DecodeTextureExport(package, exportInfo.Index);
        }, token);
        token.ThrowIfCancellationRequested();

        if (texture is null)
        {
            _owner.StatusTextBlock.Text = $"Texture export {exportInfo.ObjectName} could not be decoded.";
            _owner.DetailsTextBox.Text = $"Texture decode failed for {exportInfo.ObjectName}.";
            _renderer.ShowEmptyPreview();
            return;
        }

        _owner.TextureImage.Source = _renderer.CreateBitmap(texture);
        _owner.DetailsTextBox.Text = "unsupport debug show, maybe later";
        _owner.StatusTextBlock.Text = $"Decoded texture {texture.Name}: {texture.Width}x{texture.Height}.";
        if (!_renderer.TryRenderCurrentMapScene())
        {
            _renderer.ShowTexturePreview();
        }
    }

    private async Task PreviewTerrainExportAsync(PackageExportInfo exportInfo, CancellationToken token)
    {
        var clientRoot = (_owner.FindName("ClientRootTextBox") as System.Windows.Controls.TextBox)?.Text.Trim() ?? string.Empty;
        var terrain = await Task.Run<(PreparedTerrainPreview Preview, PreparedMeshScene Scene)?>(() =>
        {
            if (string.IsNullOrWhiteSpace(clientRoot))
            {
                return null;
            }

            var unr = UnrFileReader.Read(_owner.CurrentDocument!.Package.Path);
            var textureManager = new BspTextureManager(clientRoot);
            var terrainData = new TerrainImportBuilder(textureManager).Build(unr)
                .FirstOrDefault(x => x.ExportIndex == exportInfo.Index);
            if (terrainData is null)
            {
                return null;
            }

            var preview = _renderer.BuildTerrainPreview(terrainData);
            return (preview, _renderer.PrepareMeshScene(preview.Mesh));
        }, token);
        token.ThrowIfCancellationRequested();

        if (terrain is null)
        {
            _owner.StatusTextBlock.Text = $"Terrain preview for {exportInfo.ObjectName} could not be decoded.";
            _owner.DetailsTextBox.Text = $"Terrain decode failed for {exportInfo.ObjectName}.";
            _renderer.ShowPreviewForCurrentMode();
            return;
        }

        _renderer.CurrentTerrainPreview = terrain.Value.Preview;
        _renderer.UpdateMapInfoPanel(_owner.CurrentDocument!, _renderer.CurrentTerrainPreview);
        if (_renderer.ShowTerrain)
        {
            _renderer.ApplyPreparedMesh(terrain.Value.Preview.Mesh, terrain.Value.Scene);
            _renderer.ShowMeshPreview();
        }
        else
        {
            _renderer.ShowEmptyPreview();
        }

        _owner.DetailsTextBox.Text = terrain.Value.Preview.DetailsText;
        _owner.StatusTextBlock.Text = $"Decoded terrain {exportInfo.ObjectName}.";
    }

    private async Task PreviewStaticMeshExportAsync(PackageExportInfo exportInfo, CancellationToken token)
    {
        var clientRoot = (_owner.FindName("ClientRootTextBox") as System.Windows.Controls.TextBox)?.Text.Trim() ?? string.Empty;
        var mesh = await Task.Run<(SceneStaticMeshDefinition Mesh, PreparedStaticMeshScene Scene)?>(() =>
        {
            if (string.IsNullOrWhiteSpace(clientRoot))
            {
                return null;
            }

            var textureManager = new BspTextureManager(clientRoot);
            var resolver = new SceneStaticMeshResolver(clientRoot, textureManager);
            var meshReference = new UnrFileObjectReference
            {
                RawReference = exportInfo.Index + 1,
                Kind = UnrFileReferenceKind.Export,
                ExportIndex = exportInfo.Index,
                ImportIndex = null,
                ClassName = exportInfo.ClassName,
                ObjectName = exportInfo.ObjectName,
                PackageName = Path.GetFileNameWithoutExtension(_owner.CurrentDocument!.Package.Path)
            };
            var meshKey = SceneReferenceUtilities.BuildReference(_owner.CurrentDocument!.Package.Path, meshReference);
            var definition = resolver.ResolveMany(_owner.CurrentDocument!.Package.Path, [meshReference])[meshKey];
            var scene = new PreparedStaticMeshScene(
                _renderer.PrepareMeshScene(definition.RenderGeometry),
                definition.CollisionGeometry is null ? null : _renderer.PrepareMeshScene(definition.CollisionGeometry));
            return (definition, scene);
        }, token);
        token.ThrowIfCancellationRequested();

        if (mesh is null)
        {
            _owner.StatusTextBlock.Text = $"Static mesh {exportInfo.ObjectName} could not be decoded.";
            _owner.DetailsTextBox.Text = $"Static mesh decode failed for {exportInfo.ObjectName}.";
            _renderer.ShowPreviewForCurrentMode();
            return;
        }

        _renderer.CurrentStaticMeshDefinition = mesh.Value.Mesh;
        _renderer.ApplyPreparedStaticMesh(mesh.Value.Mesh, mesh.Value.Scene);
        _owner.DetailsTextBox.Text = "unsupport debug show, maybe later";
        _owner.StatusTextBlock.Text = $"Decoded static mesh {mesh.Value.Mesh.RenderGeometry.Name}: {mesh.Value.Mesh.RenderGeometry.Triangles.Count} triangles.";
        if (!_renderer.TryRenderCurrentMapScene())
        {
            _renderer.ShowMeshPreview();
        }
    }

    private async Task PreviewSkeletalMeshExportAsync(PackageExportInfo exportInfo, CancellationToken token)
    {
        var clientRoot = (_owner.FindName("ClientRootTextBox") as System.Windows.Controls.TextBox)?.Text.Trim() ?? string.Empty;
        var animatedPreview = await Task.Run(() =>
            new SceneSkeletalMeshResolver().ResolveExport(_owner.CurrentDocument!.Package.Path, exportInfo.Index), token);
        token.ThrowIfCancellationRequested();

        var session = animatedPreview.Session;
        _renderer.SceneTextureManager = string.IsNullOrWhiteSpace(clientRoot) ? null : new BspTextureManager(clientRoot);
        var bindPoseMesh = session.BuildBindPoseMesh();
        _renderer.ApplyPreparedMesh(bindPoseMesh, _renderer.PrepareMeshScene(bindPoseMesh));
        _owner.MeshModel.Material = _renderer.CreateSkeletalDiagnosticMaterial();
        _owner.MeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.None;
        _owner.BoneOverlayModel.Geometry = null;
        _owner.VertexDebugModel.Geometry = null;
        _skeletalAnimationController.Bind(session);
        _owner.DetailsTextBox.Text =
            $"{animatedPreview.Source} ready.\r\n" +
            $"Mesh={animatedPreview.MeshObjectName}\r\n" +
            $"Animation={animatedPreview.AnimationObjectName}\r\n" +
            $"Sequences={session.Animations.Count}" +
            (string.IsNullOrWhiteSpace(animatedPreview.Details) ? string.Empty : $"\r\n{animatedPreview.Details}");
        _owner.StatusTextBlock.Text = $"Animated skeletal preview ready for {animatedPreview.MeshObjectName}.";
        if (!_renderer.TryRenderCurrentMapScene())
        {
            _renderer.ShowMeshPreview();
        }
    }

    private async Task PreviewActorOrMetadataAsync(PackageExportInfo exportInfo, CancellationToken token)
    {
        var actorClientRoot = (_owner.FindName("ClientRootTextBox") as System.Windows.Controls.TextBox)?.Text.Trim() ?? string.Empty;
        var actorPreview = await Task.Run<(SceneActorPreviewData Preview, PreparedBoundsScene Scene)?>(() =>
        {
            if (string.IsNullOrWhiteSpace(actorClientRoot))
            {
                return null;
            }

            var unr = UnrFileReader.Read(_owner.CurrentDocument!.Package.Path);
            var textureManager = new BspTextureManager(actorClientRoot);
            var resolver = new SceneStaticMeshResolver(actorClientRoot, textureManager);
            var preview = new SceneActorPreviewBuilder(resolver).Build(_owner.CurrentDocument!.Package.Path, unr, exportInfo.Index);
            if (preview is null)
            {
                return null;
            }

            var boundsMin = preview.BoundsMin ?? preview.WorldLocation ?? System.Numerics.Vector3.Zero;
            var boundsMax = preview.BoundsMax ?? preview.WorldLocation ?? System.Numerics.Vector3.Zero;
            return (preview, _renderer.PrepareBoundsScene(boundsMin, boundsMax));
        }, token);
        token.ThrowIfCancellationRequested();

        if (actorPreview is not null)
        {
            _owner.DetailsTextBox.Text = actorPreview.Value.Preview.PropertiesText;
            _owner.StatusTextBlock.Text = $"Decoded actor preview {actorPreview.Value.Preview.Name}.";
            if (!_renderer.TryRenderCurrentMapScene())
            {
                _owner.BoundsModel.Geometry = actorPreview.Value.Scene.Bounds;
                _renderer.FitCamera(actorPreview.Value.Scene.ViewerBoundsMin, actorPreview.Value.Scene.ViewerBoundsMax);
                _renderer.ShowMeshPreview();
            }

            return;
        }

        _owner.DetailsTextBox.Text = PackageSummaryBuilder.BuildExportSummary(_owner.CurrentDocument!, exportInfo);
        _owner.StatusTextBlock.Text = $"Export {exportInfo.ObjectName} is loaded as metadata only.";
        _renderer.ShowPreviewForCurrentMode();
    }
}
