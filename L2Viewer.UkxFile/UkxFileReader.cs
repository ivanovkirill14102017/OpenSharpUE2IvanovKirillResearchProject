using L2Viewer.PackageCore;

namespace L2Viewer.UkxFile;

public static class UkxFileReader
{
    public static UkxFile Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("UKX file was not found.", path);
        }

        var package = PackageReader.LoadPackage(path);
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
            path,
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
