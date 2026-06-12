using System.Windows.Controls;

namespace L2Viewer.Wpf.Controls;

public partial class FileBrowserControl : UserControl
{
    public FileBrowserControl()
    {
        InitializeComponent();
    }

    // Проксируем доступ наружу, чтобы MainWindow мог легко подписываться на события
    public TreeView FilesTreeView => InternalFilesTreeView;
    public TreeView ExportsTreeView => InternalExportsTreeView;
}