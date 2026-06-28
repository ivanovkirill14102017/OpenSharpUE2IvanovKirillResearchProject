namespace L2Viewer.DatFile;

public sealed class GameTipDatReader : DatSchemaReader<GameTipDatDocument>
{
    public override string FileName => "gametip-e.dat";

    public override GameTipDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<GameTipDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new GameTipDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new GameTipDatDocument(path, entries);
    }
}
