namespace L2Viewer.DatFile;

public sealed class InstantZoneDataDatReader : DatSchemaReader<InstantZoneDataDatDocument>
{
    public override string FileName => "instantzonedata-e.dat";

    public override InstantZoneDataDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<InstantZoneDataDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new InstantZoneDataDatEntry(reader.ReadUInt32(), reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new InstantZoneDataDatDocument(path, entries);
    }
}
