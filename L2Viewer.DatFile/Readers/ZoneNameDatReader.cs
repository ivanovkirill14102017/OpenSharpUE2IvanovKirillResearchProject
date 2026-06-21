namespace L2Viewer.DatFile;

public sealed class ZoneNameDatReader : DatSchemaReader<ZoneNameDatDocument>
{
    public override string FileName => "zonename-e.dat";

    public override ZoneNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ZoneNameDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var coordinates = new int[6];
            entries.Add(new ZoneNameDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadAsciiString(),
                ReadCoordinates(reader, coordinates),
                reader.ReadSingle(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ZoneNameDatDocument(path, entries);
    }

    private static IReadOnlyList<int> ReadCoordinates(DatBinaryReader reader, int[] coordinates)
    {
        for (var i = 0; i < coordinates.Length; i++)
        {
            coordinates[i] = reader.ReadInt32();
        }

        return coordinates;
    }
}
