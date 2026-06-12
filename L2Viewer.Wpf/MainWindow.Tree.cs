namespace L2Viewer.Wpf;

internal static class FileTreeService
{
    public static void TrySelectPathInTree(IEnumerable<FileTreeItem> rootItems, string path)
    {
        foreach (var item in rootItems)
        {
            if (TrySelectPathRecursive(item, path))
            {
                break;
            }
        }
    }

    private static bool TrySelectPathRecursive(FileTreeItem item, string path)
    {
        if (string.Equals(item.FullPath, path, StringComparison.OrdinalIgnoreCase))
        {
            item.IsSelected = true;
            return true;
        }

        if (item.HasDummyChild)
        {
            item.Children.Clear();
            foreach (var child in BuildChildren(item.FullPath))
            {
                item.Children.Add(child);
            }
        }

        foreach (var child in item.Children)
        {
            if (TrySelectPathRecursive(child, path))
            {
                item.IsExpanded = true;
                return true;
            }
        }

        return false;
    }

    public static List<FileTreeItem> BuildRootTree(string root)
    {
        var rootDir = new DirectoryInfo(root);
        var result = new List<FileTreeItem>();
        foreach (var child in BuildChildren(rootDir.FullName))
        {
            result.Add(child);
        }

        return result;
    }

    public static List<FileTreeItem> BuildChildren(string folderPath)
    {
        var result = new List<FileTreeItem>();
        var dir = new DirectoryInfo(folderPath);
        if (!dir.Exists)
        {
            return result;
        }

        foreach (var subdir in dir.EnumerateDirectories().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!ContainsSupportedFiles(subdir))
            {
                continue;
            }

            result.Add(FileTreeItem.CreateDirectory(subdir));
        }

        foreach (var file in dir.EnumerateFiles().Where(IsSupportedFile).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(FileTreeItem.CreateFile(file));
        }

        return result;
    }

    private static bool ContainsSupportedFiles(DirectoryInfo dir)
    {
        try
        {
            if (dir.EnumerateFiles().Any(IsSupportedFile))
            {
                return true;
            }

            foreach (var subdir in dir.EnumerateDirectories())
            {
                if (ContainsSupportedFiles(subdir))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsSupportedFile(FileInfo file)
    {
        return MainWindow.SupportedFileExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
    }
}
