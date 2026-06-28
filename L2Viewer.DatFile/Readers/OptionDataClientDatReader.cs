namespace L2Viewer.DatFile;

public sealed class OptionDataClientDatReader : DatSchemaReader<OptionDataClientDatDocument>
{
    public override string FileName => "optiondata_client-e.dat";

    public override OptionDataClientDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<OptionDataClientDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new OptionDataClientDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new OptionDataClientDatDocument(path, entries);
    }
}
