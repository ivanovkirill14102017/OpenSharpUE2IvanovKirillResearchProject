namespace L2Viewer.DatFile;

public sealed class StaticObjectDatReader : DatSchemaReader<StaticObjectDatDocument>
{
    public override string FileName => "staticobject-e.dat";

    public override StaticObjectDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<StaticObjectDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new StaticObjectDatEntry(
                reader.ReadUInt32(),
                reader.ReadUnicodeString32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new StaticObjectDatDocument(path, entries);
    }
}
