using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using L2Viewer.PackageCore;


namespace L2Viewer.Wpf;

internal sealed class ExportTreeNode
{
    public string DisplayName { get; }

    public PackageExportInfo? ExportInfo { get; }

    public ObservableCollection<ExportTreeNode> Children { get; } = [];

    public bool IsGroup => ExportInfo is null;

    private ExportTreeNode(string displayName, PackageExportInfo? exportInfo)
    {
        DisplayName = displayName;
        ExportInfo = exportInfo;
    }

    public static ExportTreeNode CreateGroup(string groupName, IEnumerable<PackageExportInfo> exports)
    {
        var node = new ExportTreeNode(groupName, null);
        foreach (var exportInfo in exports)
        {
            node.Children.Add(new ExportTreeNode(exportInfo.ObjectName, exportInfo));
        }

        return node;
    }
}

internal sealed record OperationItem(string Description);

internal sealed record TerrainTextureCard(
    string Title,
    string Reference,
    string Size,
    TextureData? PreviewTexture,
    ImageSource? Preview);

public sealed class FileTreeItem : INotifyPropertyChanged
{
    private static readonly FileTreeItem Dummy = new("<loading>", string.Empty, true, false);
    private bool _isExpanded;
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public ObservableCollection<FileTreeItem> Children { get; } = [];

    public bool HasDummyChild => Children.Count == 1 && ReferenceEquals(Children[0], Dummy);

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    private FileTreeItem(string name, string fullPath, bool isDirectory, bool lazyLoad)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        if (lazyLoad)
        {
            Children.Add(Dummy);
        }
    }

    public static FileTreeItem CreateDirectory(DirectoryInfo info) => new(info.Name, info.FullName, true, true);

    public static FileTreeItem CreateFile(FileInfo info) => new(info.Name, info.FullName, false, false);
}

