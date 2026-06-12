using System.Windows.Controls;

namespace L2Viewer.Wpf.Controls
{
    public partial class OptionsControl : UserControl
    {
        public OptionsControl()
        {
            InitializeComponent();
        }

        public StackPanel UnrOptionsPanelHost => UnrOptionsPanel;
        public StackPanel StaticMeshOptionsPanelHost => StaticMeshOptionsPanel;
        public StackPanel SceneLightingPanelHost => SceneLightingPanel;
        public CheckBox ShowTerrainCheckBoxHost => ShowTerrainCheckBox;
        public CheckBox ShowWaterCheckBoxHost => ShowWaterCheckBox;
        public CheckBox LoadPolysCheckBoxHost => LoadPolysCheckBox;
        public CheckBox LoadBspCheckBoxHost => LoadBspCheckBox;
        public CheckBox LoadPropsCheckBoxHost => LoadPropsCheckBox;
        public Button LoadSelectedButtonHost => LoadSelectedButton;
        public Button NextStaticMeshButtonHost => NextStaticMeshButton;
        public ComboBox CamerasComboBoxHost => CamerasComboBox;
        public Button SaveCameraButtonHost => SaveCameraButton;
        public Button CameraSettingsButtonHost => CameraSettingsButton;
        public CheckBox ShowCollisionCheckBoxHost => ShowCollisionCheckBox;
        public CheckBox ShowAmbientCheckBoxHost => ShowAmbientCheckBox;
        public CheckBox ShowLocalLightsCheckBoxHost => ShowLocalLightsCheckBox;
        public CheckBox ShowShadowsCheckBoxHost => ShowShadowsCheckBox;
        public CheckBox ShowSunCheckBoxHost => ShowSunCheckBox;
        public CheckBox ShowMoonCheckBoxHost => ShowMoonCheckBox;
    }
}
