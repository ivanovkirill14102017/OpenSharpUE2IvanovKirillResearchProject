namespace L2Viewer.DatFile;

public sealed class ActionNameDatReader : DatSchemaReader<ActionNameDatDocument>
{
    public override string FileName => "actionname-e.dat";

    public override ActionNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ActionNameDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new ActionNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadInt32(),
                reader.ReadUInt32(),
                ReadInt32Array(reader, reader.ReadPackedInt32()),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadUnicodeString32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ActionNameDatDocument(path, entries);
    }

    private static IReadOnlyList<int> ReadInt32Array(DatBinaryReader reader, int count)
    {
        var values = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadInt32());
        }

        return values;
    }
}
