using L2Viewer.PackageCore;

namespace L2Viewer.UkxFile;

internal static class UkxExportReaderHelpers
{
    internal static void SkipObjectStackIfPresent(BinaryReader reader, ExportEntry exportEntry)
    {
        if ((exportEntry.ObjectFlags & UnrPackageHelpers.ObjectFlagHasStack) == 0)
        {
            return;
        }

        var node = PackageReader.ReadCompactIndex(reader);
        _ = PackageReader.ReadCompactIndex(reader);
        reader.BaseStream.Position += 12;
        if (node != 0)
        {
            _ = PackageReader.ReadCompactIndex(reader);
        }
    }
}
