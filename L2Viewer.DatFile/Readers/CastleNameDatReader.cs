namespace L2Viewer.DatFile;

public sealed class CastleNameDatReader : DatSchemaReader<CastleNameDatDocument>
{
    public override string FileName => "castlename-e.dat";

    public override CastleNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<CastleNameDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new CastleNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new CastleNameDatDocument(path, entries);
    }
}
