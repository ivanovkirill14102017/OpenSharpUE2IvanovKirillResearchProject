using L2Viewer.PackageCore;
using L2Viewer.UFile;

namespace L2Viewer.UnrFile;

public static class UnrClassEffectPackageReader
{
    public static IReadOnlyList<UnrScriptClassObject> ReadScriptClasses(string path)
    {
        ValidatePath(path);
        var file = UFileReader.Read(path);
        return UFileReader.ReadTextBufferExports(file)
            .Where(x => x.ClassDeclaration is not null)
            .Select(x => new UnrScriptClassObject
            {
                ExportIndex = x.ExportIndex,
                ObjectName = x.ClassDeclaration!.ClassName,
                SuperClassName = x.ClassDeclaration.SuperClassName,
                ScriptText = x.Text
            })
            .ToArray();
    }

    public static IReadOnlyList<UnrEmitterClassObject> ReadEmitterClasses(string path)
    {
        ValidatePath(path);
        var package = PackageReader.LoadPackage(path);
        var file = UFileReader.Read(path);
        var scriptByClassName = UFileReader.ReadTextBufferExports(file)
            .Where(x => x.ClassDeclaration is not null)
            .ToDictionary(x => x.ClassDeclaration!.ClassName, x => x.ClassDeclaration!.SuperClassName, StringComparer.OrdinalIgnoreCase);

        var result = new List<UnrEmitterClassObject>();
        foreach (var classExport in file.Exports)
        {
            var layers = package.Exports
                .Select((export, index) => new { Export = export, Index = index })
                .Where(x => unchecked((int)x.Export.PackageIndex) == classExport.ExportIndex + 1)
                .Select(x => TryReadLayer(package, x.Export, x.Index))
                .Where(static x => x is not null)
                .Cast<UnrFileObject>()
                .ToArray();
            if (layers.Length == 0)
            {
                continue;
            }

            result.Add(new UnrEmitterClassObject
            {
                ExportIndex = classExport.ExportIndex,
                ObjectName = classExport.ObjectName,
                SuperClassName = scriptByClassName.GetValueOrDefault(classExport.ObjectName),
                Layers = layers
            });
        }

        return result;
    }

    private static UnrFileObject? TryReadLayer(PackageData package, ExportEntry export, int exportIndex)
    {
        var className = PackageReader.ExportClassName(package, export);
        var objectName = PackageReader.SafeName(package.Names, export.ObjectName);
        return className switch
        {
            "SpriteEmitter" => UnrSpriteEmitterObjectReader.Read(package, export, exportIndex, className, objectName),
            "MeshEmitter" => UnrMeshEmitterObjectReader.Read(package, export, exportIndex, className, objectName),
            "BeamEmitter" => UnrBeamEmitterObjectReader.Read(package, export, exportIndex, className, objectName),
            "VertMeshEmitter" => UnrVertMeshEmitterObjectReader.Read(package, export, exportIndex, className, objectName),
            _ => null
        };
    }

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Unreal class package was not found.", path);
        }
    }
}
