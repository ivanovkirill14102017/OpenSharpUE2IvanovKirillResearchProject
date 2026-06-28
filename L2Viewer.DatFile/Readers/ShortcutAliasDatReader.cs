namespace L2Viewer.DatFile;

public sealed class ShortcutAliasDatReader : DatSchemaReader<ShortcutAliasDatDocument>
{
    public override string FileName => "shortcutalias.dat";

    public override ShortcutAliasDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        try
        {
            return ReadWithCount(path, decoded);
        }
        catch (EndOfStreamException)
        {
            return ReadUntilEnd(path, decoded);
        }
        catch (InvalidDataException)
        {
            return ReadUntilEnd(path, decoded);
        }
    }

    private static ShortcutAliasDatDocument ReadWithCount(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ShortcutAliasDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new ShortcutAliasDatEntry(
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadUInt32(),
                reader.ReadUInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ShortcutAliasDatDocument(path, entries);
    }

    private static ShortcutAliasDatDocument ReadUntilEnd(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var entries = new List<ShortcutAliasDatEntry>();
        while (reader.Remaining > 0 && !reader.IsAtSafePackageRemainder())
        {
            entries.Add(new ShortcutAliasDatEntry(
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadUInt32(),
                reader.ReadUInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ShortcutAliasDatDocument(path, entries);
    }
}
