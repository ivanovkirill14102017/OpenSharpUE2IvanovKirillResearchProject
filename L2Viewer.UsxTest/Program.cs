using System;
using System.IO;
using L2Viewer.PackageCore;
using L2Viewer.UsxFile;

var staticMeshesFolder = @"C:\Users\User\Downloads\Lineage2_Interlude_windows10\Interlude\staticmeshes";
var usxFiles = Directory.GetFiles(staticMeshesFolder, "*.usx");

Console.WriteLine($"Found {usxFiles.Length} USX files.");

int errorCount = 0;
int meshCount = 0;

foreach (var usxPath in usxFiles)
{
    var package = PackageReader.LoadPackage(usxPath);
    for (var i = 0; i < package.Exports.Count; i++)
    {
        var export = package.Exports[i];
        if (PackageReader.ExportClassName(package, export).Is(UnrealClassNames.StaticMesh))
        {
            meshCount++;
            try 
            {
                var result = StaticMeshCodec.DecodeStaticMeshExport(package, i);
            } 
            catch (Exception ex) 
            {
                var meshName = PackageReader.SafeName(package.Names, export.ObjectName);
                Console.WriteLine($"FAIL {Path.GetFileName(usxPath)} [{meshName}]: Version={package.Header.Version}, Licensee={package.Header.LicenseeVersion}");
                Console.WriteLine($"  Error: {ex.Message}");
                
                using var reader = PackageReader.OpenExportReader(package, export);
                var size = export.SerialSize;
                if (size > 0 && export.SerialOffset.HasValue)
                {
                    reader.BaseStream.Position = size - 20;
                    if (reader.BaseStream.Position < 0) reader.BaseStream.Position = 0;
                    var remaining = reader.ReadBytes(20);
                    Console.WriteLine($"  Last 20 bytes of export: {BitConverter.ToString(remaining)}");
                }
                
                errorCount++;
            }
        }
    }
}
Console.WriteLine($"Done. Processed {meshCount} meshes, {errorCount} errors.");