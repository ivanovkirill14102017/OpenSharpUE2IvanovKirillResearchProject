namespace L2Viewer.DatFile;

public sealed class RecipeDatReader : DatSchemaReader<RecipeDatDocument>
{
    public override string FileName => "recipe-c.dat";

    public override RecipeDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<RecipeDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new RecipeDatEntry(
                reader.ReadAsciiString(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                DatReaderPrimitives.ReadMat(reader)));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new RecipeDatDocument(path, entries);
    }
}
