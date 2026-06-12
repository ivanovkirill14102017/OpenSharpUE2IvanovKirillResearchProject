using System.ComponentModel;
using System.Runtime.CompilerServices;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services;

namespace L2Viewer.BspDiagnostics.Wpf;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string DefaultClientRoot = @"C:\Users\User\Downloads\Lineage2_Interlude_windows10\Interlude";
    private const string DefaultMapRelativePath = @"Maps\20_20.unr";
    private const uint PF_Invisible = 0x00000001;
    private const uint PF_Portal = 0x04000000;

    private readonly ObservableCollection<ModelListItem> _modelEntries = [];
    private readonly ObservableCollection<PolyFlagFilterItem> _polyFlagFilters = [];
    private readonly ObservableElement3DCollection _sceneModels = [];
    private readonly Dictionary<string, List<SavedCameraState>> _savedCameras = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _inputTimer;
    private readonly AppSettings _appSettings = AppSettings.Load();
    private SceneBspScene? _currentScene;
    private SceneInstancedMeshResult? _currentInstancedMeshes;
    private TerrainImportData[] _currentTerrains = [];
    private string? _currentPath;
    private BspTextureManager? _textureManager;
    private bool _isPointCaptureArmed;
    private Vector3? _capturedPointUnr;

    public MainWindow()
    {
        InitializeComponent();
        EffectsManager = new DefaultEffectsManager();
        DataContext = this;
        SceneRoot.ItemsSource = _sceneModels;
        PathTextBox.Text = ResolveInitialPath();
        OpacitySlider.Value = _appSettings.GeometryOpacity;
        SpeedSlider.Value = _appSettings.CameraSpeedScale;
        InitializePolyFlagFilters();
        UpdateSliderLabels();
        UpdateCapturePointUi();
        LoadSavedCameraPresets();
        ApplyCameraSettings();

        _inputTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _inputTimer.Tick += InputTimer_Tick;
        _inputTimer.Start();

        Loaded += (_, _) => LoadScene(PathTextBox.Text, fitCamera: true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IEffectsManager EffectsManager { get; }

    public ObservableCollection<ModelListItem> ModelEntries => _modelEntries;
    public ObservableCollection<PolyFlagFilterItem> PolyFlagFilters => _polyFlagFilters;

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        LoadScene(PathTextBox.Text, fitCamera: true);
    }

    private void FitAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentScene is null)
        {
            return;
        }

        FitCamera(_currentScene.WorldBoundsMin, _currentScene.WorldBoundsMax);
    }

    private void ModelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectionSummary();
        RebuildViewport();
    }

    private void Viewport_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Viewport.Focus();

        if (!_isPointCaptureArmed || e.ChangedButton != System.Windows.Input.MouseButton.Left)
        {
            return;
        }

        _isPointCaptureArmed = false;
        UpdateCapturePointUi();

        var hit = TryFindHitPoint(e.GetPosition(Viewport));
        if (hit is null)
        {
            CapturedPointTextBox.Text = "No hit";
            RebuildViewport();
            return;
        }

        _capturedPointUnr = FromViewerVector(hit.Value);
        CapturedPointTextBox.Text = FormatVector(_capturedPointUnr.Value);
        RebuildViewport();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _appSettings.GeometryOpacity = OpacitySlider.Value;
        UpdateSliderLabels();
        RebuildViewport();
        _appSettings.Save();
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _appSettings.CameraSpeedScale = SpeedSlider.Value;
        UpdateSliderLabels();
        _appSettings.Save();
    }

    private void InputTimer_Tick(object? sender, EventArgs e)
    {
        if (Viewport.CameraMode != CameraMode.WalkAround || !IsActive)
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
        moveSpeed *= _appSettings.CameraSpeedScale;

        var moveVector = new Vector3D();
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.W)) moveVector.Z += 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.S)) moveVector.Z -= 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.A)) moveVector.X -= 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D)) moveVector.X += 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.E)) moveVector.Y += 1;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Q)) moveVector.Y -= 1;

        if (moveVector.LengthSquared <= 0 || Viewport.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera)
        {
            return;
        }

        moveVector.Normalize();
        moveVector *= moveSpeed;

        var lookDir = camera.LookDirection;
        lookDir.Normalize();
        var upDir = camera.UpDirection;
        upDir.Normalize();
        var rightDir = Vector3D.CrossProduct(lookDir, upDir);
        rightDir.Normalize();

        var actualMove = (lookDir * moveVector.Z) + (rightDir * moveVector.X) + (upDir * moveVector.Y);
        var pos = camera.Position;
        camera.Position = new Point3D(pos.X + actualMove.X, pos.Y + actualMove.Y, pos.Z + actualMove.Z);
    }

    private void SaveCameraButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentPath) || Viewport.Camera is not HelixToolkit.Wpf.SharpDX.ProjectionCamera projCamera)
        {
            return;
        }

        if (!_savedCameras.TryGetValue(_currentPath, out var saved))
        {
            saved = [];
            _savedCameras[_currentPath] = saved;
        }

        saved.Add(new SavedCameraState
        {
            Name = $"Pos {saved.Count + 1}",
            Position = projCamera.Position,
            LookDirection = projCamera.LookDirection,
            UpDirection = projCamera.UpDirection
        });

        CamerasComboBox.ItemsSource = null;
        CamerasComboBox.ItemsSource = saved;
        CamerasComboBox.SelectedIndex = saved.Count - 1;
        PersistSavedCameraPresets();
    }

    private void CapturePointButton_Click(object sender, RoutedEventArgs e)
    {
        _isPointCaptureArmed = true;
        CapturedPointTextBox.Text = "Click BSP surface...";
        UpdateCapturePointUi();
        Viewport.Focus();
    }

    private void CamerasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not SavedCameraState cameraState || Viewport.Camera is not HelixToolkit.Wpf.SharpDX.ProjectionCamera camera)
        {
            return;
        }

        camera.Position = cameraState.Position;
        camera.LookDirection = cameraState.LookDirection;
        camera.UpDirection = cameraState.UpDirection;
        Viewport.Focus();
    }

    private void LoadScene(string path, bool fitCamera)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var unr = UnrFileReader.Read(path);
            var clientRoot = ResolveClientRoot(path)
                ?? throw new InvalidOperationException("Unable to resolve client root for the selected map.");
            _textureManager = new BspTextureManager(clientRoot);
            var materialResolver = new SceneMaterialResolver(clientRoot, _textureManager);
            var staticMeshResolver = new SceneStaticMeshResolver(clientRoot, _textureManager);
            var scene = new SceneBspBuilder(clientRoot, materialResolver, _textureManager).Build(unr);
            _currentInstancedMeshes = new SceneInstancedMeshBuilder(staticMeshResolver, _textureManager).Build(unr);
            _currentTerrains = new TerrainImportBuilder(_textureManager).Build(unr);
            _currentScene = scene;
            _currentPath = scene.SourcePath;

            SceneTitleTextBlock.Text = Path.GetFileName(scene.SourcePath);
            var chunkCount = scene.Models.Sum(x => x.Chunks.Length);
            var propCount = _currentInstancedMeshes.Instances.Count;
            var decoCount = _currentInstancedMeshes.TerrainDecorations.Count;
            SceneSummaryTextBlock.Text = $"Models: {scene.Models.Length} | Chunks: {chunkCount} | Props: {propCount} | DecoMaps: {decoCount} | Terrains: {_currentTerrains.Length} | Bounds: {FormatVector(scene.WorldBoundsMin)} .. {FormatVector(scene.WorldBoundsMax)}";

            PopulateModelEntries(scene);
            BindSavedCameras(scene.SourcePath);
            UpdateSelectionSummary();
            RebuildViewport();

            if (fitCamera)
            {
                FitCamera(scene.WorldBoundsMin, scene.WorldBoundsMax);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Failed to load BSP diagnostics", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PopulateModelEntries(SceneBspScene scene)
    {
        _modelEntries.Clear();
        foreach (var model in scene.Models)
        {
            if (model.Chunks.Length == 0)
            {
                var fallbackEntry = new ModelListItem(model);
                fallbackEntry.PropertyChanged += ModelEntry_PropertyChanged;
                _modelEntries.Add(fallbackEntry);
                continue;
            }

            foreach (var chunk in model.Chunks)
            {
                var entry = new ModelListItem(model, chunk);
                entry.PropertyChanged += ModelEntry_PropertyChanged;
                _modelEntries.Add(entry);
            }
        }

        ModelsListBox.SelectedIndex = _modelEntries.Count > 0 ? 0 : -1;
    }

    private void ModelEntry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModelListItem.IsVisible))
        {
            RebuildViewport();
        }
    }

    private void RebuildViewport()
    {
        _sceneModels.Clear();
        if (_currentScene is null)
        {
            return;
        }

        var selected = ModelsListBox.SelectedItem as ModelListItem;

        foreach (var entry in _modelEntries)
        {
            if (!entry.IsVisible)
            {
                continue;
            }

            var opacity = selected is null
                ? 1f
                : ReferenceEquals(entry, selected)
                    ? 1f
                    : (float)_appSettings.GeometryOpacity;
            foreach (var part in entry.MeshParts)
            {
                if (IsFilteredByGlobalFlags(part))
                {
                    continue;
                }

                var previewTexture = ResolvePreviewTexture(part);

                _sceneModels.Add(new MeshGeometryModel3D
                {
                    CullMode = SharpDX.Direct3D11.CullMode.None,
                    Geometry = BuildGeometry(part, previewTexture),
                    Material = BuildMaterial(part, opacity, previewTexture)
                });
            }
        }

        if (_currentInstancedMeshes is not null)
        {
            foreach (var instance in _currentInstancedMeshes.Instances)
            {
                if (!_currentInstancedMeshes.UniqueMeshes.TryGetValue(instance.MeshReference, out var definition))
                {
                    throw new InvalidOperationException($"Missing mesh definition for instance '{instance.ActorName}' ({instance.MeshReference}).");
                }

                foreach (var subMesh in definition.SubMeshes)
                {
                    var previewMesh = BuildPreviewMesh(definition.RenderGeometry, instance, subMesh.MaterialId);
                    if (previewMesh.Triangles.Count == 0)
                    {
                        continue;
                    }

                    _sceneModels.Add(new MeshGeometryModel3D
                    {
                        CullMode = SharpDX.Direct3D11.CullMode.Back,
                        Geometry = BuildGeometry(previewMesh),
                        Material = BuildMaterial(subMesh, 1f)
                    });
                }
            }
        }

        foreach (var terrain in _currentTerrains)
        {
            var terrainMesh = BuildTerrainPreviewMesh(terrain);
            _sceneModels.Add(new MeshGeometryModel3D
            {
                CullMode = SharpDX.Direct3D11.CullMode.Back,
                Geometry = BuildGeometry(terrainMesh),
                Material = BuildTerrainMaterial(terrain, 1f)
            });
        }

        if (_capturedPointUnr is not null)
        {
            _sceneModels.Add(new MeshGeometryModel3D
            {
                CullMode = SharpDX.Direct3D11.CullMode.None,
                Geometry = BuildPointGeometry(_capturedPointUnr.Value),
                Material = BuildSolidColorMaterial(0xFFFF3344u, 1f)
            });
        }
    }

    private static HelixToolkit.SharpDX.MeshGeometry3D BuildGeometry(SceneBspMeshSection part, TextureData? texture)
    {
        var positions = new HelixToolkit.Vector3Collection();
        var normals = new HelixToolkit.Vector3Collection();
        var textureCoordinates = new HelixToolkit.Vector2Collection();
        var indices = new HelixToolkit.IntCollection();

        for (var i = 0; i < part.Positions.Length; i++)
        {
            positions.Add(ToViewerVector(part.Positions[i]));
            normals.Add(ToViewerVector(part.Normals[i]));
            textureCoordinates.Add(NormalizeTextureCoordinate(part.TextureCoordinates[i], texture));
        }

        for (var i = 0; i < part.Indices.Length; i++)
        {
            indices.Add(part.Indices[i]);
        }

        return new HelixToolkit.SharpDX.MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = textureCoordinates,
            Indices = indices
        };
    }

    private static HelixToolkit.SharpDX.MeshGeometry3D BuildGeometry(MeshData mesh)
    {
        var positions = new HelixToolkit.Vector3Collection();
        var normals = new HelixToolkit.Vector3Collection();
        var textureCoordinates = new HelixToolkit.Vector2Collection();
        var indices = new HelixToolkit.IntCollection();
        var flipVerticalUv = ShouldFlipVerticalUv(mesh);

        foreach (var tri in mesh.Triangles)
        {
            var baseIndex = positions.Count;
            positions.Add(ToViewerVector(tri.A.Position));
            positions.Add(ToViewerVector(tri.C.Position));
            positions.Add(ToViewerVector(tri.B.Position));
            normals.Add(ToViewerNormal(tri.A.Normal));
            normals.Add(ToViewerNormal(tri.C.Normal));
            normals.Add(ToViewerNormal(tri.B.Normal));
            textureCoordinates.Add(new Vector2(tri.A.UV.X, flipVerticalUv ? 1f - tri.A.UV.Y : tri.A.UV.Y));
            textureCoordinates.Add(new Vector2(tri.C.UV.X, flipVerticalUv ? 1f - tri.C.UV.Y : tri.C.UV.Y));
            textureCoordinates.Add(new Vector2(tri.B.UV.X, flipVerticalUv ? 1f - tri.B.UV.Y : tri.B.UV.Y));
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
        }

        return new HelixToolkit.SharpDX.MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = textureCoordinates,
            Indices = indices
        };
    }

    private static PhongMaterial BuildMaterial(
        SceneBspMeshSection part,
        float opacity,
        TextureData? previewTexture)
    {
        if (previewTexture is not null)
        {
            return new PhongMaterial
            {
                DiffuseMap = new MemoryStream(EncodePng(previewTexture), writable: false),
                DiffuseColor = new Color4(1f, 1f, 1f, opacity),
                AmbientColor = new Color4(0.22f, 0.22f, 0.22f, opacity),
                SpecularColor = new Color4(0.06f, 0.06f, 0.06f, opacity),
                RenderShadowMap = false
            };
        }

        var a = ((part.ColorArgb >> 24) & 0xFF) / 255f * opacity;
        var r = ((part.ColorArgb >> 16) & 0xFF) / 255f;
        var g = ((part.ColorArgb >> 8) & 0xFF) / 255f;
        var b = (part.ColorArgb & 0xFF) / 255f;

        return new PhongMaterial
        {
            DiffuseColor = new Color4(r, g, b, a),
            AmbientColor = new Color4(r * 0.28f, g * 0.28f, b * 0.28f, a),
            SpecularColor = new Color4(0.08f, 0.08f, 0.08f, a),
            RenderShadowMap = false
        };
    }

    private static PhongMaterial BuildSolidColorMaterial(uint argb, float opacity)
    {
        var a = ((argb >> 24) & 0xFF) / 255f * opacity;
        var r = ((argb >> 16) & 0xFF) / 255f;
        var g = ((argb >> 8) & 0xFF) / 255f;
        var b = (argb & 0xFF) / 255f;
        return new PhongMaterial
        {
            DiffuseColor = new Color4(r, g, b, a),
            AmbientColor = new Color4(r * 0.28f, g * 0.28f, b * 0.28f, a),
            SpecularColor = new Color4(0.08f, 0.08f, 0.08f, a),
            RenderShadowMap = false
        };
    }

    private PhongMaterial BuildMaterial(SceneStaticMeshSubMeshDefinition subMesh, float opacity)
    {
        var previewTexture = ResolvePreviewTexture(subMesh);
        if (previewTexture is null)
        {
            return new PhongMaterial
            {
                DiffuseColor = new Color4(0.76f, 0.78f, 0.82f, opacity),
                AmbientColor = new Color4(0.2f, 0.2f, 0.22f, opacity),
                SpecularColor = new Color4(0.08f, 0.08f, 0.08f, opacity),
                RenderShadowMap = false
            };
        }

        var encoded = EncodePng(previewTexture);
        return new PhongMaterial
        {
            DiffuseMap = new MemoryStream(encoded, writable: false),
            DiffuseColor = new Color4(1f, 1f, 1f, opacity),
            AmbientColor = new Color4(0.22f, 0.22f, 0.22f, opacity),
            SpecularColor = new Color4(0.06f, 0.06f, 0.06f, opacity),
            RenderShadowMap = false
        };
    }

    private TextureData? ResolvePreviewTexture(SceneStaticMeshSubMeshDefinition subMesh)
    {
        var graphTexture = subMesh.Material?.TextureSlots
            .FirstOrDefault(x => string.Equals(x.Reference, subMesh.PrimaryTextureReference, StringComparison.OrdinalIgnoreCase))?.Texture
            ?? subMesh.Material?.TextureSlots.FirstOrDefault(x => x.Texture is not null)?.Texture;
        if (graphTexture is not null)
        {
            return graphTexture;
        }

        if (_textureManager is null)
        {
            return null;
        }

        var primaryResource = subMesh.PrimaryTextureResource ?? subMesh.TextureResources.FirstOrDefault();
        if (primaryResource is null)
        {
            return null;
        }

        return ResolveTexture(primaryResource.PackageName, primaryResource.ObjectName);
    }

    private static PhongMaterial BuildTerrainMaterial(TerrainImportData terrain, float opacity)
    {
        var previewTexture = terrain.Layers.FirstOrDefault(x => x.Texture is not null)?.Texture
            ?? terrain.HeightTexture;
        if (previewTexture is null)
        {
            return new PhongMaterial
            {
                DiffuseColor = new Color4(0.76f, 0.78f, 0.82f, opacity),
                AmbientColor = new Color4(0.2f, 0.2f, 0.22f, opacity),
                SpecularColor = new Color4(0.08f, 0.08f, 0.08f, opacity),
                RenderShadowMap = false
            };
        }

        var encoded = EncodePng(previewTexture);
        return new PhongMaterial
        {
            DiffuseMap = new MemoryStream(encoded, writable: false),
            DiffuseColor = new Color4(1f, 1f, 1f, opacity),
            AmbientColor = new Color4(0.22f, 0.22f, 0.22f, opacity),
            SpecularColor = new Color4(0.06f, 0.06f, 0.06f, opacity),
            RenderShadowMap = false
        };
    }

    private TextureData? ResolvePreviewTexture(SceneBspMeshSection part)
    {
        var graphTexture = part.Material?.TextureSlots
            .FirstOrDefault(x => string.Equals(x.Reference, part.PrimaryTextureReference, StringComparison.OrdinalIgnoreCase))?.Texture
            ?? part.Material?.TextureSlots.FirstOrDefault(x => x.Texture is not null)?.Texture;
        if (graphTexture is not null)
        {
            return graphTexture;
        }

        if (_textureManager is null ||
            string.IsNullOrWhiteSpace(part.MaterialPackageName) ||
            string.IsNullOrWhiteSpace(part.MaterialObjectName))
        {
            return null;
        }

        return ResolveTexture(part.MaterialPackageName, part.MaterialObjectName);
    }

    private TextureData? ResolveTexture(string packageName, string objectName)
    {
        if (_textureManager is null)
        {
            return null;
        }

        return _textureManager.ResolveMany([new SceneTextureRequest(packageName, objectName)])
            .TryGetValue($"{packageName}.{objectName}", out var resolved)
            ? resolved.Texture
            : null;
    }

    private static MeshData BuildPreviewMesh(SceneTriangleMeshData source, SceneStaticMeshInstance instance, int materialId)
    {
        var triangles = new List<Triangle>(source.Triangles.Count);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var tri in source.Triangles)
        {
            if (tri.MaterialId != materialId)
            {
                continue;
            }

            var a = TransformPreviewVertex(tri.A, instance);
            var b = TransformPreviewVertex(tri.B, instance);
            var c = TransformPreviewVertex(tri.C, instance);
            triangles.Add(new Triangle(a, b, c, tri.MaterialId));
            UpdateBounds(a.Position, ref min, ref max);
            UpdateBounds(b.Position, ref min, ref max);
            UpdateBounds(c.Position, ref min, ref max);
        }

        return new MeshData(
            $"{instance.ActorName}:{source.Name}",
            triangles,
            min,
            max,
            $"{source.SourceNote}; preview instance",
            null,
            Array.Empty<MaterialTextureInfo>());
    }

    private static TriangleVertex TransformPreviewVertex(TriangleVertex vertex, SceneStaticMeshInstance instance)
    {
        var position = TransformPreviewPoint(vertex.Position, instance);
        var normal = TransformPreviewNormal(vertex.Normal, instance);
        return new TriangleVertex(position, vertex.UV, normal);
    }

    private static Vector3 TransformPreviewPoint(Vector3 point, SceneStaticMeshInstance instance)
    {
        var scale = instance.Scale;
        var local = new Vector3(
            point.X * scale.X - instance.PrePivot.X,
            point.Y * scale.Y - instance.PrePivot.Y,
            point.Z * scale.Z - instance.PrePivot.Z);
        var rotated = SceneTransformUtilities.RotateByUnrealRotator(local, instance.UnrealRotationRaw);
        return rotated + instance.WorldLocation;
    }

    private static Vector3 TransformPreviewNormal(Vector3 normal, SceneStaticMeshInstance instance)
    {
        var scale = instance.Scale;
        var safeScale = new Vector3(
            Math.Abs(scale.X) < 0.0001f ? 1f : scale.X,
            Math.Abs(scale.Y) < 0.0001f ? 1f : scale.Y,
            Math.Abs(scale.Z) < 0.0001f ? 1f : scale.Z);
        var scaled = Vector3.Normalize(new Vector3(
            normal.X / safeScale.X,
            normal.Y / safeScale.Y,
            normal.Z / safeScale.Z));
        return Vector3.Normalize(SceneTransformUtilities.RotateByUnrealRotator(scaled, instance.UnrealRotationRaw));
    }

    private static MeshData BuildTerrainPreviewMesh(TerrainImportData terrain)
    {
        if (terrain.HeightWidth < 2 || terrain.HeightHeight < 2)
        {
            throw new InvalidOperationException($"Terrain '{terrain.ObjectName}' has invalid height dimensions {terrain.HeightWidth}x{terrain.HeightHeight}.");
        }

        if (terrain.HeightSamples.Length != terrain.HeightWidth * terrain.HeightHeight)
        {
            throw new InvalidOperationException(
                $"Terrain '{terrain.ObjectName}' height sample count mismatch. Expected {terrain.HeightWidth * terrain.HeightHeight}, got {terrain.HeightSamples.Length}.");
        }

        var origin = terrain.WorldMinCorner is { } worldMinCorner
            ? new Vector3(worldMinCorner.X, worldMinCorner.Y, worldMinCorner.Z - (0.5f * terrain.HeightValueScale))
            : new Vector3(
                -0.5f * (terrain.HeightWidth - 1) * terrain.SampleSpacingX,
                -0.5f * (terrain.HeightHeight - 1) * terrain.SampleSpacingY,
                -0.5f * terrain.HeightValueScale);
        var previewTexture = terrain.Layers.FirstOrDefault(x => x.Texture is not null)?.Texture ?? terrain.HeightTexture;
        var textureRef = terrain.Layers.FirstOrDefault(x => x.TextureReference is not null)?.TextureReference ?? terrain.HeightReference;
        var triangles = new List<Triangle>((terrain.HeightWidth - 1) * (terrain.HeightHeight - 1) * 2);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        for (var y = 0; y < terrain.HeightHeight - 1; y++)
        {
            for (var x = 0; x < terrain.HeightWidth - 1; x++)
            {
                if (!IsTerrainQuadVisible(terrain, x, y))
                {
                    continue;
                }

                var a = BuildTerrainVertex(terrain, origin, x, y);
                var b = BuildTerrainVertex(terrain, origin, x + 1, y);
                var c = BuildTerrainVertex(terrain, origin, x, y + 1);
                var d = BuildTerrainVertex(terrain, origin, x + 1, y + 1);
                var flipDiagonal = IsTerrainBitSet(terrain.EdgeTurnMask, terrain, x, y);

                if (flipDiagonal)
                {
                    AddTerrainTriangle(triangles, a, b, d, ref min, ref max);
                    AddTerrainTriangle(triangles, a, d, c, ref min, ref max);
                }
                else
                {
                    AddTerrainTriangle(triangles, a, b, c, ref min, ref max);
                    AddTerrainTriangle(triangles, c, b, d, ref min, ref max);
                }
            }
        }

        if (triangles.Count == 0)
        {
            throw new InvalidOperationException($"Terrain '{terrain.ObjectName}' produced no visible preview triangles.");
        }

        return new MeshData(
            terrain.ObjectName,
            triangles,
            min,
            max,
            "terrain import preview",
            textureRef,
            BuildTerrainTextureEntries(terrain));
    }

    private static IReadOnlyList<MaterialTextureInfo> BuildTerrainTextureEntries(TerrainImportData terrain)
    {
        var entries = new List<MaterialTextureInfo>();

        if (!string.IsNullOrWhiteSpace(terrain.HeightReference))
        {
            entries.Add(new MaterialTextureInfo(terrain.HeightReference, terrain.HeightTexture?.SourcePackage));
        }

        foreach (var layer in terrain.Layers)
        {
            if (layer.Texture is not null && !string.IsNullOrWhiteSpace(layer.TextureReference))
            {
                entries.Add(new MaterialTextureInfo(layer.TextureReference, layer.Texture.SourcePackage));
            }
        }

        if (entries.Count == 0 && !string.IsNullOrWhiteSpace(terrain.HeightReference))
        {
            entries.Add(new MaterialTextureInfo(terrain.HeightReference, terrain.HeightTexture?.SourcePackage));
        }

        return entries;
    }

    private static TriangleVertex BuildTerrainVertex(TerrainImportData terrain, Vector3 origin, int x, int y)
    {
        var position = new Vector3(
            origin.X + (x * terrain.SampleSpacingX),
            origin.Y + (y * terrain.SampleSpacingY),
            origin.Z + (terrain.HeightSamples[(y * terrain.HeightWidth) + x] * terrain.HeightValueScale));
        var normal = EstimateTerrainNormal(terrain, origin, x, y);
        var uv = new Vector2(
            terrain.HeightWidth <= 1 ? 0f : x / (float)(terrain.HeightWidth - 1),
            terrain.HeightHeight <= 1 ? 0f : y / (float)(terrain.HeightHeight - 1));
        return new TriangleVertex(position, uv, normal);
    }

    private static Vector3 EstimateTerrainNormal(TerrainImportData terrain, Vector3 origin, int x, int y)
    {
        var left = GetTerrainPosition(terrain, origin, Math.Max(0, x - 1), y);
        var right = GetTerrainPosition(terrain, origin, Math.Min(terrain.HeightWidth - 1, x + 1), y);
        var down = GetTerrainPosition(terrain, origin, x, Math.Max(0, y - 1));
        var up = GetTerrainPosition(terrain, origin, x, Math.Min(terrain.HeightHeight - 1, y + 1));
        var dx = right - left;
        var dy = up - down;
        var normal = Vector3.Cross(dx, dy);
        return normal.LengthSquared() < 0.0001f
            ? Vector3.UnitZ
            : Vector3.Normalize(normal);
    }

    private static Vector3 GetTerrainPosition(TerrainImportData terrain, Vector3 origin, int x, int y)
    {
        return new Vector3(
            origin.X + (x * terrain.SampleSpacingX),
            origin.Y + (y * terrain.SampleSpacingY),
            origin.Z + (terrain.HeightSamples[(y * terrain.HeightWidth) + x] * terrain.HeightValueScale));
    }

    private static bool IsTerrainQuadVisible(TerrainImportData terrain, int quadX, int quadY)
    {
        return terrain.QuadVisibilityMask is null || IsTerrainBitSet(terrain.QuadVisibilityMask, terrain, quadX, quadY);
    }

    private static bool IsTerrainBitSet(TerrainBitMaskData? mask, TerrainImportData terrain, int quadX, int quadY)
    {
        if (mask is null || mask.Width <= 0 || mask.Height <= 0 || mask.Bits.Length == 0)
        {
            return false;
        }

        var maxQuadWidth = Math.Max(1, terrain.HeightWidth - 1);
        var maxQuadHeight = Math.Max(1, terrain.HeightHeight - 1);
        var sampleX = Math.Clamp((int)Math.Floor(quadX * (mask.Width / (double)maxQuadWidth)), 0, mask.Width - 1);
        var sampleY = Math.Clamp((int)Math.Floor(quadY * (mask.Height / (double)maxQuadHeight)), 0, mask.Height - 1);
        var bitIndex = (sampleY * mask.Width) + sampleX;
        return bitIndex >= 0 && bitIndex < mask.Bits.Length && mask.Bits[bitIndex];
    }

    private static void AddTerrainTriangle(
        ICollection<Triangle> triangles,
        TriangleVertex a,
        TriangleVertex b,
        TriangleVertex c,
        ref Vector3 min,
        ref Vector3 max)
    {
        triangles.Add(new Triangle(a, b, c, 0));
        UpdateBounds(a.Position, ref min, ref max);
        UpdateBounds(b.Position, ref min, ref max);
        UpdateBounds(c.Position, ref min, ref max);
    }

    private static HelixToolkit.SharpDX.MeshGeometry3D BuildPointGeometry(Vector3 point)
    {
        const float halfSize = 9f;
        var center = ToViewerVector(point);
        var corners = new[]
        {
            center + new Vector3(-halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, halfSize, halfSize),
            center + new Vector3(-halfSize, halfSize, halfSize)
        };
        var faces = new[]
        {
            (0, 1, 2, 3, new Vector3(0, 0, -1)),
            (4, 5, 6, 7, new Vector3(0, 0, 1)),
            (0, 4, 7, 3, new Vector3(-1, 0, 0)),
            (1, 5, 6, 2, new Vector3(1, 0, 0)),
            (3, 2, 6, 7, new Vector3(0, 1, 0)),
            (0, 1, 5, 4, new Vector3(0, -1, 0))
        };

        var positions = new HelixToolkit.Vector3Collection();
        var normals = new HelixToolkit.Vector3Collection();
        var indices = new HelixToolkit.IntCollection();

        foreach (var face in faces)
        {
            var baseIndex = positions.Count;
            positions.Add(corners[face.Item1]);
            positions.Add(corners[face.Item2]);
            positions.Add(corners[face.Item3]);
            positions.Add(corners[face.Item4]);
            normals.Add(face.Item5);
            normals.Add(face.Item5);
            normals.Add(face.Item5);
            normals.Add(face.Item5);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        return new HelixToolkit.SharpDX.MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            Indices = indices
        };
    }

    private static bool ShouldFlipVerticalUv(MeshData mesh)
    {
        return !mesh.SourceNote.Contains("terrain", StringComparison.OrdinalIgnoreCase);
    }

    private static void UpdateBounds(Vector3 point, ref Vector3 min, ref Vector3 max)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }

    private void FitCamera(Vector3 min, Vector3 max)
    {
        var viewerMin = ToViewerNumerics(min);
        var viewerMax = ToViewerNumerics(max);
        var center = (viewerMin + viewerMax) * 0.5f;
        var extents = viewerMax - viewerMin;
        var radius = Math.Max(25f, extents.Length() * 0.5f);
        var distance = Math.Max(120f, radius * 2.4f);

        Camera.Position = new Point3D(center.X + distance, center.Y + distance * 0.55f, center.Z + distance);
        Camera.LookDirection = new Vector3D(-distance, -distance * 0.55f, -distance);
        Camera.UpDirection = new Vector3D(0, 1, 0);
        Camera.NearPlaneDistance = Math.Max(0.1, radius / 2000.0);
        Camera.FarPlaneDistance = Math.Max(5000.0, distance * 24.0);
    }

    private void UpdateSelectionSummary()
    {
        if (ModelsListBox.SelectedItem is not ModelListItem selected)
        {
            SelectionSummaryTextBlock.Text = "Select a BSP component on the left to isolate a specific chunk or portal-like surface set.";
            return;
        }

        SelectionSummaryTextBlock.Text = selected.SelectionSummary;
    }

    private void ApplyCameraSettings()
    {
        Viewport.RotationSensitivity = _appSettings.RotationSensitivity;
        Viewport.LeftRightRotationSensitivity = _appSettings.InvertX ? -_appSettings.RotationSensitivity : _appSettings.RotationSensitivity;
        Viewport.UpDownRotationSensitivity = _appSettings.InvertY ? -_appSettings.RotationSensitivity : _appSettings.RotationSensitivity;
        UpdateSliderLabels();
    }

    private void UpdateSliderLabels()
    {
        OpacityValueTextBlock.Text = $"{_appSettings.GeometryOpacity:0.00}";
        SpeedValueTextBlock.Text = $"{_appSettings.CameraSpeedScale:0.0}x";
    }

    private void LoadSavedCameraPresets()
    {
        _savedCameras.Clear();
        foreach (var preset in _appSettings.SavedCameras)
        {
            if (!_savedCameras.TryGetValue(preset.MapPath, out var list))
            {
                list = [];
                _savedCameras[preset.MapPath] = list;
            }

            list.Add(new SavedCameraState
            {
                Name = preset.Name,
                Position = new Point3D(preset.PositionX, preset.PositionY, preset.PositionZ),
                LookDirection = new Vector3D(preset.LookDirectionX, preset.LookDirectionY, preset.LookDirectionZ),
                UpDirection = new Vector3D(preset.UpDirectionX, preset.UpDirectionY, preset.UpDirectionZ)
            });
        }
    }

    private void PersistSavedCameraPresets()
    {
        _appSettings.SavedCameras = _savedCameras
            .SelectMany(kvp => kvp.Value.Select(x => new SavedCameraPreset
            {
                MapPath = kvp.Key,
                Name = x.Name,
                PositionX = x.Position.X,
                PositionY = x.Position.Y,
                PositionZ = x.Position.Z,
                LookDirectionX = x.LookDirection.X,
                LookDirectionY = x.LookDirection.Y,
                LookDirectionZ = x.LookDirection.Z,
                UpDirectionX = x.UpDirection.X,
                UpDirectionY = x.UpDirection.Y,
                UpDirectionZ = x.UpDirection.Z
            }))
            .ToList();

        _appSettings.Save();
    }

    private void BindSavedCameras(string path)
    {
        if (!_savedCameras.TryGetValue(path, out var saved))
        {
            saved = [];
            _savedCameras[path] = saved;
        }

        CamerasComboBox.ItemsSource = saved;
        CamerasComboBox.SelectedIndex = -1;
    }

    private static string ResolveInitialPath()
    {
        var args = Environment.GetCommandLineArgs();
        return args.Length > 1
            ? args[1]
            : Path.Combine(DefaultClientRoot, DefaultMapRelativePath);
    }

    private static string FormatVector(Vector3 value)
    {
        return $"{value.X:0.##}, {value.Y:0.##}, {value.Z:0.##}";
    }

    private static System.Numerics.Vector3 ToViewerVector(Vector3 value)
    {
        return new System.Numerics.Vector3(value.X, value.Z, value.Y);
    }

    private static Vector3 ToViewerNumerics(Vector3 value)
    {
        return new Vector3(value.X, value.Z, value.Y);
    }

    private static Vector3 FromViewerVector(Vector3 value)
    {
        return new Vector3(value.X, value.Z, value.Y);
    }

    private static System.Numerics.Vector3 ToViewerNormal(Vector3 value)
    {
        return Vector3.Normalize(ToViewerVector(value));
    }

    private static byte[] EncodePng(TextureData texture)
    {
        var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(
            texture.Width,
            texture.Height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            ConvertRgbaToBgra(texture.RgbaBytes),
            texture.Width * 4);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static byte[] ConvertRgbaToBgra(byte[] rgba)
    {
        var bgra = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            bgra[i + 0] = rgba[i + 2];
            bgra[i + 1] = rgba[i + 1];
            bgra[i + 2] = rgba[i + 0];
            bgra[i + 3] = rgba[i + 3];
        }

        return bgra;
    }

    private static Vector2 NormalizeTextureCoordinate(Vector2 rawUv, TextureData? texture)
    {
        if (texture is null || texture.Width <= 0 || texture.Height <= 0)
        {
            return rawUv;
        }

        return new Vector2(rawUv.X / texture.Width, 1f - (rawUv.Y / texture.Height));
    }

    private static string? ResolveClientRoot(string mapPath)
    {
        var mapsDirectory = Path.GetDirectoryName(mapPath);
        if (string.IsNullOrWhiteSpace(mapsDirectory))
        {
            return null;
        }

        var mapsName = Path.GetFileName(mapsDirectory);
        if (string.Equals(mapsName, "Maps", StringComparison.OrdinalIgnoreCase))
        {
            return Directory.GetParent(mapsDirectory)?.FullName;
        }

        return Directory.Exists(DefaultClientRoot) ? DefaultClientRoot : null;
    }

    private bool IsFilteredByGlobalFlags(SceneBspMeshSection part)
    {
        foreach (var filter in _polyFlagFilters)
        {
            if (!filter.IsHidden)
            {
                continue;
            }

            if ((part.PolyFlags & (uint)filter.Flag) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private void InitializePolyFlagFilters()
    {
        _polyFlagFilters.Clear();
        var hiddenSet = ResolveInitialHiddenPolyFlags();
        foreach (var flag in Enum.GetValues<UnrPolyFlags>())
        {
            if (flag == UnrPolyFlags.None)
            {
                continue;
            }

            var item = new PolyFlagFilterItem(flag)
            {
                IsHidden = hiddenSet.Contains((uint)flag)
            };
            item.PropertyChanged += PolyFlagFilterItem_PropertyChanged;
            _polyFlagFilters.Add(item);
        }

        _appSettings.HiddenPolyFlags = _polyFlagFilters
            .Where(x => x.IsHidden)
            .Select(x => (uint)x.Flag)
            .ToList();
    }

    private void PolyFlagFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PolyFlagFilterItem.IsHidden) || !IsLoaded)
        {
            return;
        }

        _appSettings.HiddenPolyFlags = _polyFlagFilters
            .Where(x => x.IsHidden)
            .Select(x => (uint)x.Flag)
            .ToList();
        RebuildViewport();
        _appSettings.Save();
    }

    private HashSet<uint> ResolveInitialHiddenPolyFlags()
    {
        if (_appSettings.HiddenPolyFlags.Count > 0)
        {
            return _appSettings.HiddenPolyFlags.ToHashSet();
        }

        var defaults = new HashSet<uint>
        {
            (uint)UnrPolyFlags.Invisible,
            (uint)UnrPolyFlags.Portal,
            (uint)UnrPolyFlags.FakeBackdrop
        };

#pragma warning disable CS0618
        if (_appSettings.HidePortalGeometry)
        {
            defaults.Add((uint)UnrPolyFlags.Portal);
        }

        if (_appSettings.HideInvisibleGeometry)
        {
            defaults.Add((uint)UnrPolyFlags.Invisible);
        }
#pragma warning restore CS0618

        return defaults;
    }

    private void UpdateCapturePointUi()
    {
        CapturePointButton.Content = _isPointCaptureArmed ? "Click Surface..." : "Save Point";
    }

    private Vector3? TryFindHitPoint(System.Windows.Point mousePosition)
    {
        var hits = Viewport.FindHits(mousePosition);
        if (hits is null || hits.Count == 0)
        {
            return null;
        }

        return hits[0].PointHit;
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class SavedCameraState
    {
        public required string Name { get; init; }
        public required Point3D Position { get; init; }
        public required Vector3D LookDirection { get; init; }
        public required Vector3D UpDirection { get; init; }
    }

    private readonly record struct RenderTraits(bool IsTwoSided, bool TextureHasAlpha, bool IsTransparent);

    public sealed class ModelListItem : INotifyPropertyChanged
    {
        private bool _isVisible = true;

        public ModelListItem(SceneBspModel model)
        {
            Model = model;
            MeshParts = model.Chunks.SelectMany(x => x.MeshSections).ToArray();
            SelectionSummary = $"{model.Name} | export={model.ExportIndex} | world={model.IsWorldModel} | nodes={model.NodeCount} | polys={model.PolygonCount} | tris={model.TriangleCount}";
        }

        public ModelListItem(SceneBspModel model, SceneBspChunk chunk)
        {
            Model = model;
            Chunk = chunk;
            MeshParts = chunk.MeshSections;
            SelectionSummary =
                $"{chunk.Name} | export={model.ExportIndex} | world={model.IsWorldModel} | kind={chunk.Kind} | zones={string.Join(",", chunk.ZoneNumbers)} | polys={chunk.PolygonCount} | tris={chunk.TriangleCount} | surfaces={chunk.SurfaceCount} | bounds={FormatVector(chunk.WorldBoundsMin)} .. {FormatVector(chunk.WorldBoundsMax)}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SceneBspModel Model { get; }
        public SceneBspChunk? Chunk { get; }
        public SceneBspMeshSection[] MeshParts { get; }
        public string SelectionSummary { get; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                {
                    return;
                }

                _isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            }
        }

        public Thickness IndentMargin => Chunk is null ? new Thickness(0) : new Thickness(12, 0, 0, 0);

        public string Title
        {
            get
            {
                if (Chunk is null)
                {
                    return Model.IsWorldModel
                        ? $"{Model.Name} [World]"
                        : Model.Name;
                }

                var suffix = Chunk.Kind switch
                {
                    "PortalInvisible" => " [Portal+Invisible]",
                    "Portal" => " [Portal]",
                    "Invisible" => " [Invisible]",
                    _ => string.Empty
                };
                var baseTitle = Model.Chunks.Length == 1
                    ? (Model.IsWorldModel ? $"{Model.Name} [World]" : Model.Name)
                    : Chunk.Name;
                return $"{baseTitle}{suffix}";
            }
        }

        public string Summary
        {
            get
            {
                if (Chunk is null)
                {
                    return $"export={Model.ExportIndex} | nodes={Model.NodeCount} | polys={Model.PolygonCount} | tris={Model.TriangleCount}";
                }

                return $"export={Model.ExportIndex} | kind={Chunk.Kind} | zones={string.Join(",", Chunk.ZoneNumbers)} | polys={Chunk.PolygonCount} | tris={Chunk.TriangleCount} | surfaces={Chunk.SurfaceCount}";
            }
        }
    }

    public sealed class PolyFlagFilterItem : INotifyPropertyChanged
    {
        private bool _isHidden;

        public PolyFlagFilterItem(UnrPolyFlags flag)
        {
            Flag = flag;
            Name = flag.ToString();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public UnrPolyFlags Flag { get; }
        public string Name { get; }

        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                if (_isHidden == value)
                {
                    return;
                }

                _isHidden = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHidden)));
            }
        }
    }
}
