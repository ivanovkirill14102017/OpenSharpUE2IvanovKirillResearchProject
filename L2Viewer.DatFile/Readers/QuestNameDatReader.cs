namespace L2Viewer.DatFile;

public sealed class QuestNameDatReader : DatSchemaReader<QuestNameDatDocument>
{
    public override string FileName => "questname-e.dat";

    public override QuestNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<QuestNameDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new QuestNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                ReadInt32Array(reader, reader.ReadPackedInt32()),
                ReadInt32Array(reader, reader.ReadPackedInt32()),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                ReadInt32Array(reader, reader.ReadPackedInt32()),
                ReadInt32Array(reader, reader.ReadPackedInt32()),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new QuestNameDatDocument(path, entries);
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
