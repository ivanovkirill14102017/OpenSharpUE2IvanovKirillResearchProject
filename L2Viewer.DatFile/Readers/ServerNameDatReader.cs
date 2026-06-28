namespace L2Viewer.DatFile;

public sealed class ServerNameDatReader : DatSchemaReader<ServerNameDatDocument>
{
    public override string FileName => "servername-e.dat";

    public override ServerNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ServerNameDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new ServerNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ServerNameDatDocument(path, entries);
    }
}
