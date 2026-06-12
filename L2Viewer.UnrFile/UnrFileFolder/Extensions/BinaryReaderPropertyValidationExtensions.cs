namespace L2Viewer.UnrFile;

internal static class BinaryReaderPropertyValidationExtensions
{
    internal static void EnsureExportFullyConsumed(
        this BinaryReader reader,
        string className,
        int exportIndex,
        string objectName)
    {
        if (reader.BaseStream.Position == reader.BaseStream.Length)
        {
            return;
        }

        throw new PackageReadException(
            $"{className} export {exportIndex} ({objectName}) has unparsed tail bytes: parsed={reader.BaseStream.Position}, total={reader.BaseStream.Length}.");
    }
}
