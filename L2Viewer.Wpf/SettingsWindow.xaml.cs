using System.Windows;
using System.Globalization;

namespace L2Viewer.Wpf;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        MoveSpeedTextBox.Text = _settings.CameraMoveSpeed.ToString(CultureInfo.InvariantCulture);
        ShiftSpeedTextBox.Text = _settings.CameraShiftSpeed.ToString(CultureInfo.InvariantCulture);
        RotSensTextBox.Text = _settings.RotationSensitivity.ToString(CultureInfo.InvariantCulture);
        InvertXCheckBox.IsChecked = _settings.InvertX;
        InvertYCheckBox.IsChecked = _settings.InvertY;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MoveSpeedTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var moveSpeed))
            _settings.CameraMoveSpeed = moveSpeed;
            
        if (double.TryParse(ShiftSpeedTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var shiftSpeed))
            _settings.CameraShiftSpeed = shiftSpeed;
            
        if (double.TryParse(RotSensTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var rotSens))
            _settings.RotationSensitivity = rotSens;

        _settings.InvertX = InvertXCheckBox.IsChecked == true;
        _settings.InvertY = InvertYCheckBox.IsChecked == true;

        _settings.Save();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}