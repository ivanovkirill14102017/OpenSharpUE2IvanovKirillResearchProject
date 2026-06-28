namespace L2Viewer.DatFile;

public sealed class ObsceneDatReader : DatSchemaReader<ObsceneDatDocument>
{
    public override string FileName => "obscene-e.dat";

    public override ObsceneDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ObsceneDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new ObsceneDatEntry(reader.ReadUInt32(), reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ObsceneDatDocument(path, entries);
    }
}
