namespace L2Viewer.DatFile;

public sealed class TransformDataDatReader : DatSchemaReader<TransformDataDatDocument>
{
    public override string FileName => "transformdata.dat";

    public override TransformDataDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<TransformDataDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new TransformDataDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUnicodeString32(),
                reader.ReadUnicodeString32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new TransformDataDatDocument(path, entries);
    }
}
