namespace L2Viewer.DatFile;

public sealed class SystemMsgDatReader : DatSchemaReader<SystemMsgDatDocument>
{
    public override string FileName => "systemmsg-e.dat";

    public override SystemMsgDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<SystemMsgDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new SystemMsgDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadUInt32(),
                reader.ReadBytes(4),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                ReadUInt32Array(reader, 5),
                reader.ReadAsciiString(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SystemMsgDatDocument(path, entries);
    }

    private static IReadOnlyList<uint> ReadUInt32Array(DatBinaryReader reader, int count)
    {
        var values = new List<uint>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadUInt32());
        }

        return values;
    }
}
