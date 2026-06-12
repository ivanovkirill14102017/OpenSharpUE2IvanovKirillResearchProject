namespace L2Viewer.BspDiagnostics.Wpf;

public sealed class AppSettings
{
    public double CameraMoveSpeed { get; set; } = 500.0;
    public double CameraShiftSpeed { get; set; } = 2500.0;
    public double CameraSpeedScale { get; set; } = 1.0;
    public double RotationSensitivity { get; set; } = 1.0;
    public double GeometryOpacity { get; set; } = 1.0;
    [Obsolete("Use HiddenPolyFlags instead.")]
    public bool HidePortalGeometry { get; set; }
    [Obsolete("Use HiddenPolyFlags instead.")]
    public bool HideInvisibleGeometry { get; set; }
    public List<uint> HiddenPolyFlags { get; set; } = [];
    public bool InvertX { get; set; } = true;
    public bool InvertY { get; set; } = true;
    public List<SavedCameraPreset> SavedCameras { get; set; } = [];

    private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
        }
    }
}

public sealed class SavedCameraPreset
{
    public string MapPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double PositionZ { get; set; }
    public double LookDirectionX { get; set; }
    public double LookDirectionY { get; set; }
    public double LookDirectionZ { get; set; }
    public double UpDirectionX { get; set; }
    public double UpDirectionY { get; set; }
    public double UpDirectionZ { get; set; }
}
