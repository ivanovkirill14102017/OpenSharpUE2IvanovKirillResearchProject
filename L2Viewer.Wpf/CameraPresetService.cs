using HelixToolkit.Wpf.SharpDX;
using L2Viewer.PackageCore;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace L2Viewer.Wpf;

internal sealed class CameraPresetService
{
    private readonly AppSettings _appSettings;
    private readonly Dictionary<string, List<SavedCameraState>> _savedCameras = new(StringComparer.OrdinalIgnoreCase);

    public CameraPresetService(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public void LoadSavedCameraPresets()
    {
        _savedCameras.Clear();
        foreach (var preset in _appSettings.SavedCameras)
        {
            if (string.IsNullOrWhiteSpace(preset.MapPath) || string.IsNullOrWhiteSpace(preset.Name))
            {
                continue;
            }

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

    public SavedCameraState? CaptureCurrentCamera(Viewport3DX viewport)
    {
        if (viewport.Camera is not HelixToolkit.Wpf.SharpDX.ProjectionCamera projCamera)
        {
            return null;
        }

        return new SavedCameraState
        {
            Name = "Current",
            Position = projCamera.Position,
            LookDirection = projCamera.LookDirection,
            UpDirection = projCamera.UpDirection
        };
    }

    public void RestoreCameraState(Viewport3DX viewport, SavedCameraState? state)
    {
        if (state is null || viewport.Camera is not HelixToolkit.Wpf.SharpDX.ProjectionCamera projCamera)
        {
            return;
        }

        projCamera.Position = state.Position;
        projCamera.LookDirection = state.LookDirection;
        projCamera.UpDirection = state.UpDirection;
    }

    public void BindPackageCameras(string path, ComboBox camerasComboBox)
    {
        if (!_savedCameras.TryGetValue(path, out var savedCams))
        {
            savedCams = [];
            _savedCameras[path] = savedCams;
        }

        camerasComboBox.ItemsSource = savedCams;
        camerasComboBox.SelectedIndex = -1;
    }

    public string? SaveCurrentCamera(string? packagePath, Viewport3DX viewport, ComboBox camerasComboBox)
    {
        if (string.IsNullOrWhiteSpace(packagePath) ||
            !string.Equals(Path.GetExtension(packagePath), ".unr", StringComparison.OrdinalIgnoreCase) ||
            viewport.Camera is not HelixToolkit.Wpf.SharpDX.ProjectionCamera projCamera)
        {
            return null;
        }

        if (!_savedCameras.TryGetValue(packagePath, out var savedCams))
        {
            savedCams = [];
            _savedCameras[packagePath] = savedCams;
        }

        var camName = $"Saved View {savedCams.Count + 1}";
        savedCams.Add(new SavedCameraState
        {
            Name = camName,
            Position = projCamera.Position,
            LookDirection = projCamera.LookDirection,
            UpDirection = projCamera.UpDirection
        });

        camerasComboBox.ItemsSource = null;
        camerasComboBox.ItemsSource = savedCams;
        camerasComboBox.SelectedIndex = savedCams.Count - 1;
        PersistSavedCameraPresets();
        return camName;
    }

    public void ApplySelectedCamera(SelectionChangedEventArgs e, ComboBox camerasComboBox, Viewport3DX viewport)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is SavedCameraState savedCam && viewport.Camera is HelixToolkit.Wpf.SharpDX.ProjectionCamera projCamera)
        {
            projCamera.Position = savedCam.Position;
            projCamera.LookDirection = savedCam.LookDirection;
            projCamera.UpDirection = savedCam.UpDirection;
        }

        if (camerasComboBox.SelectedItem is MapActorSceneData camActor && viewport.Camera is HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera)
        {
            var loc = camActor.Location ?? System.Numerics.Vector3.Zero;
            camera.Position = new Point3D(loc.X, loc.Z, loc.Y);

            var pitch = camActor.Rotation?.X ?? 0f;
            var yaw = camActor.Rotation?.Y ?? 0f;
            var pitchRad = pitch * (MathF.PI / 32768f);
            var yawRad = yaw * (MathF.PI / 32768f);

            var dirX = MathF.Cos(pitchRad) * MathF.Cos(yawRad);
            var dirY = MathF.Cos(pitchRad) * MathF.Sin(yawRad);
            var dirZ = MathF.Sin(pitchRad);

            camera.LookDirection = new Vector3D(dirX, dirZ, dirY);
            camera.UpDirection = new Vector3D(0, 1, 0);
            viewport.Focus();
        }
    }

    private void PersistSavedCameraPresets()
    {
        _appSettings.SavedCameras = _savedCameras
            .SelectMany(
                kvp => kvp.Value.Select(cam => new SavedCameraPreset
                {
                    MapPath = kvp.Key,
                    Name = cam.Name,
                    PositionX = cam.Position.X,
                    PositionY = cam.Position.Y,
                    PositionZ = cam.Position.Z,
                    LookDirectionX = cam.LookDirection.X,
                    LookDirectionY = cam.LookDirection.Y,
                    LookDirectionZ = cam.LookDirection.Z,
                    UpDirectionX = cam.UpDirection.X,
                    UpDirectionY = cam.UpDirection.Y,
                    UpDirectionZ = cam.UpDirection.Z
                }))
            .ToList();
        _appSettings.Save();
    }
}

internal sealed class SavedCameraState
{
    public string Name { get; set; } = string.Empty;
    public Point3D Position { get; set; }
    public Vector3D LookDirection { get; set; }
    public Vector3D UpDirection { get; set; }

    public override string ToString() => Name;
}
