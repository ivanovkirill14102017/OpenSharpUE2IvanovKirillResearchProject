namespace L2Viewer.DatFile;

public sealed class NpcNameDatReader : DatSchemaReader<NpcNameDatDocument>
{
    public override string FileName => "npcname-e.dat";

    public override NpcNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<NpcNameDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new NpcNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new NpcNameDatDocument(path, entries);
    }
}
