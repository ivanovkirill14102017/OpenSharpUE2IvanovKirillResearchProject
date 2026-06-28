namespace L2Viewer.DatFile;

public sealed class ItemNameDatReader : DatSchemaReader<ItemNameDatDocument>
{
    public override string FileName => "itemname-e.dat";

    public override ItemNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ItemNameDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new ItemNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadUnicodeString32(),
                reader.ReadUnicodeString32(),
                reader.ReadAsciiString(),
                reader.ReadInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadBytes(2),
                reader.ReadUInt32(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ItemNameDatDocument(path, entries);
    }
}
