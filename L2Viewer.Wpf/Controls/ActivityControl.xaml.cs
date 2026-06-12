using System.Windows.Controls;

namespace L2Viewer.Wpf.Controls
{
    public partial class ActivityControl : UserControl
    {
        public ActivityControl()
        {
            InitializeComponent();
        }

        public StackPanel ActivityPanelHost => ActivityPanel;
        public ItemsControl ActiveOperationsHost => ActiveOperationsItemsControl;
        public TextBlock StatusTextBlockHost => StatusTextBlock;
    }
}
