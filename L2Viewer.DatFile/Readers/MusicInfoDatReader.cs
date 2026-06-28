namespace L2Viewer.DatFile;

public sealed class MusicInfoDatReader : DatSchemaReader<MusicInfoDatDocument>
{
    public override string FileName => "musicinfo.dat";

    public override MusicInfoDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<MusicInfoDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new MusicInfoDatEntry(
                reader.ReadUInt32(),
                ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()))));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new MusicInfoDatDocument(path, entries);
    }

    private static IReadOnlyList<string> ReadUnicodeArray(DatBinaryReader reader, int count)
    {
        var values = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadUnicodeString32());
        }

        return values;
    }
}
