using System.Collections.Concurrent;
using L2Viewer.PackageCore;

namespace L2Viewer.UkxFile;

public static class UkxFileReader
{
    private static readonly ConcurrentDictionary<string, Lazy<UkxFile>> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static UkxFile Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is empty.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("UKX file was not found.", fullPath);
        }

        var lazy = Cache.GetOrAdd(
            fullPath,
            static key => new Lazy<UkxFile>(() => ReadCore(key), LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private static UkxFile ReadCore(string fullPath)
    {
        var package = PackageReader.LoadPackage(fullPath);
        var names = package.Names
            .Select((name, index) => new UkxFileNameEntry(index, name))
            .ToArray();
        var imports = package.Imports
            .Select((import, index) => new UkxFileImportEntry(index, import.ClassPackage, import.ClassName, import.PackageIndex, import.ObjectName))
            .ToArray();

        var exports = new List<UkxFileExportObjectEntry>(package.Exports.Count);
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var export = package.Exports[i];
            var className = PackageReader.ExportClassName(package, export);
            var objectName = PackageReader.SafeName(package.Names, export.ObjectName);
            var exportModel = new UkxFileExportEntry(
                i,
                export.ClassIndex,
                export.SuperIndex,
                export.PackageIndex,
                export.ObjectName,
                export.ObjectFlags,
                export.SerialSize,
                export.SerialOffset);
            var objectModel = UkxObjectReader.ReadObject(package, export, i, className, objectName);
            exports.Add(new UkxFileExportObjectEntry(exportModel, objectModel));
        }

        return new UkxFile(
            fullPath,
            package.Wrapper,
            new UkxFileHeader(
                package.Header.Version,
                package.Header.LicenseeVersion,
                package.Header.Flags,
                package.Header.NameCount,
                package.Header.NameOffset,
                package.Header.ExportCount,
                package.Header.ExportOffset,
                package.Header.ImportCount,
                package.Header.ImportOffset),
            names,
            imports,
            exports);
    }
}
