namespace L2Viewer.DatFile;

public sealed class RaidDataDatReader : DatSchemaReader<RaidDataDatDocument>
{
    public override string FileName => "raiddata-e.dat";

    public override RaidDataDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<RaidDataDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new RaidDataDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new RaidDataDatDocument(path, entries);
    }
}
