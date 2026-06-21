using L2Viewer.PackageCore;
using L2Viewer.UFile;

namespace L2Viewer.Wpf;

internal static class TechnicalDetailsBuilder
{
    public static string BuildPackageExportJson(PackageDocument document, PackageExportInfo exportInfo)
    {
        var export = document.Package.Exports[exportInfo.Index];
        var payload = new
        {
            exportInfo.Index,
            exportInfo.ObjectName,
            exportInfo.ClassName,
            export.ObjectFlags,
            export.SerialSize,
            export.SerialOffset,
            Class = ResolveReference(document.Package, export.ClassIndex),
            Super = ResolveReference(document.Package, export.SuperIndex),
            Package = ResolvePackageReference(document.Package, export.PackageIndex)
        };

        return FileInspectionJsonSerializer.Serialize(payload);
    }

    public static string BuildUExportDetails(UFileDocument file, int exportIndex)
    {
        var export = file.Exports.FirstOrDefault(x => x.ExportIndex == exportIndex)
            ?? throw new InvalidOperationException($"Export {exportIndex} was not found.");

        if (string.Equals(export.ClassName, "TextBuffer", StringComparison.OrdinalIgnoreCase))
        {
            return FileInspectionJsonSerializer.Serialize(new
            {
                Export = export,
                Script = UFileReader.ReadTextBufferExport(export)
            });
        }

        var companionScript = FindCompanionScript(file, export.ObjectName);
        var payload = new
        {
            export.ExportIndex,
            export.ObjectName,
            export.ClassName,
            export.ObjectFlags,
            export.SerialSize,
            export.SerialOffset,
            Class = ResolveReference(file, export.ClassIndex),
            Super = ResolveReference(file, export.SuperIndex),
            Package = ResolvePackageReference(file, export.PackageIndex),
            PropertyCount = export.Properties.Count,
            Properties = export.Properties,
            TrailingData = export.TrailingData,
            CompanionScript = companionScript
        };

        return FileInspectionJsonSerializer.Serialize(payload);
    }

    private static object ResolveReference(PackageData package, int reference)
    {
        return reference switch
        {
            0 => new { Kind = "None", Reference = reference },
            < 0 => ResolveImportReference(package, -reference - 1),
            _ => ResolveExportReference(package, reference - 1)
        };
    }

    private static object ResolvePackageReference(PackageData package, uint reference)
    {
        return reference switch
        {
            0 => new { Kind = "Root", Reference = reference },
            _ => ResolveReference(package, unchecked((int)reference))
        };
    }

    private static object ResolveExportReference(PackageData package, int exportIndex)
    {
        if (exportIndex < 0 || exportIndex >= package.Exports.Count)
        {
            return new { Kind = "Export", ExportIndex = exportIndex, IsResolved = false };
        }

        var export = package.Exports[exportIndex];
        return new
        {
            Kind = "Export",
            ExportIndex = exportIndex,
            ObjectName = PackageReader.SafeName(package.Names, export.ObjectName),
            ClassName = PackageReader.ExportClassName(package, export),
            IsResolved = true
        };
    }

    private static object ResolveImportReference(PackageData package, int importIndex)
    {
        if (importIndex < 0 || importIndex >= package.Imports.Count)
        {
            return new { Kind = "Import", ImportIndex = importIndex, IsResolved = false };
        }

        var import = package.Imports[importIndex];
        return new
        {
            Kind = "Import",
            ImportIndex = importIndex,
            ObjectName = PackageReader.SafeName(package.Names, import.ObjectName),
            ClassName = PackageReader.SafeName(package.Names, import.ClassName),
            ClassPackageName = PackageReader.SafeName(package.Names, import.ClassPackage),
            IsResolved = true
        };
    }

    private static object ResolveReference(UFileDocument file, int reference)
    {
        return reference switch
        {
            0 => new { Kind = "None", Reference = reference },
            < 0 => ResolveImportReference(file, -reference - 1),
            _ => ResolveExportReference(file, reference - 1)
        };
    }

    private static object ResolvePackageReference(UFileDocument file, uint reference)
    {
        return reference switch
        {
            0 => new { Kind = "Root", Reference = reference },
            _ => ResolveReference(file, unchecked((int)reference))
        };
    }

    private static object ResolveExportReference(UFileDocument file, int exportIndex)
    {
        var export = file.Exports.FirstOrDefault(x => x.ExportIndex == exportIndex);
        return export is null
            ? new { Kind = "Export", ExportIndex = exportIndex, IsResolved = false }
            : new
            {
                Kind = "Export",
                ExportIndex = exportIndex,
                export.ObjectName,
                export.ClassName,
                IsResolved = true
            };
    }

    private static object ResolveImportReference(UFileDocument file, int importIndex)
    {
        var import = file.Imports.FirstOrDefault(x => x.ImportIndex == importIndex);
        return import is null
            ? new { Kind = "Import", ImportIndex = importIndex, IsResolved = false }
            : new
            {
                Kind = "Import",
                ImportIndex = importIndex,
                import.ObjectName,
                import.ClassName,
                import.ClassPackageName,
                IsResolved = true
            };
    }

    private static object? FindCompanionScript(UFileDocument file, string objectName)
    {
        var script = UFileReader.ReadTextBufferExports(file)
            .FirstOrDefault(x => string.Equals(x.ClassDeclaration?.ClassName, objectName, StringComparison.OrdinalIgnoreCase));

        return script is null
            ? null
            : new
            {
                script.ExportIndex,
                script.Text,
                script.ClassDeclaration,
                script.Routines
            };
    }
}
