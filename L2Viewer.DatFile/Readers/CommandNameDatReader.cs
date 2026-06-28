namespace L2Viewer.DatFile;

public sealed class CommandNameDatReader : DatSchemaReader<CommandNameDatDocument>
{
    public override string FileName => "commandname-e.dat";

    public override CommandNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<CommandNameDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new CommandNameDatEntry(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new CommandNameDatDocument(path, entries);
    }
}
