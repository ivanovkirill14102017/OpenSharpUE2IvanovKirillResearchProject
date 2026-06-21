namespace L2Viewer.DatFile;

public sealed class HuntingZoneDatReader : DatSchemaReader<HuntingZoneDatDocument>
{
    public override string FileName => "huntingzone-e.dat";

    public override HuntingZoneDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<HuntingZoneDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new HuntingZoneDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadAsciiString(),
                reader.ReadUInt32(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new HuntingZoneDatDocument(path, entries);
    }
}
