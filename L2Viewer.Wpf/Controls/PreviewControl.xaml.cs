using System.Windows.Controls;
using HelixToolkit.Wpf.SharpDX;

namespace L2Viewer.Wpf.Controls
{
    public partial class PreviewControl : UserControl
    {
        public PreviewControl()
        {
            InitializeComponent();
        }

        public Border EmptyPreviewBorderHost => EmptyPreviewBorder;
        public Border TexturePreviewBorderHost => TexturePreviewBorder;
        public Border MeshPreviewBorderHost => MeshPreviewBorder;
        public Image TextureImageHost => TextureImage;
        public Viewport3DX ViewportHost => Viewport;
        public HelixToolkit.Wpf.SharpDX.PerspectiveCamera CameraHost => Camera;
        public AmbientLight3D AmbientLightHost => AmbientLight;
        public ShadowMap3D ShadowMapHost => ShadowMap;
        public DirectionalLight3D PreviewKeyLightHost => PreviewKeyLight;
        public DirectionalLight3D PreviewFillLightHost => PreviewFillLight;
        public GroupModel3D MapSunLightsGroupHost => MapSunLightsGroup;
        public GroupModel3D MapMoonLightsGroupHost => MapMoonLightsGroup;
        public GroupModel3D MapLightsGroupHost => MapLightsGroup;
        public MeshGeometryModel3D MeshModelHost => MeshModel;
        public MeshGeometryModel3D CollisionMeshModelHost => CollisionMeshModel;
        public GroupModel3D WaterSceneGroupHost => WaterSceneGroup;
        public GroupModel3D VolumeSceneGroupHost => VolumeSceneGroup;
        public GroupModel3D PropSceneGroupHost => PropSceneGroup;
        public GroupModel3D PolySceneGroupHost => PolySceneGroup;
        public LineGeometryModel3D BoundsModelHost => BoundsModel;
        public LineGeometryModel3D BoneOverlayModelHost => BoneOverlayModel;
        public PointGeometryModel3D VertexDebugModelHost => VertexDebugModel;
    }
}
