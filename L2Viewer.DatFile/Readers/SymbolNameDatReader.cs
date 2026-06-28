namespace L2Viewer.DatFile;

public sealed class SymbolNameDatReader : DatSchemaReader<SymbolNameDatDocument>
{
    public override string FileName => "symbolname-e.dat";

    public override SymbolNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<SymbolNameDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new SymbolNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadUInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SymbolNameDatDocument(path, entries);
    }
}
