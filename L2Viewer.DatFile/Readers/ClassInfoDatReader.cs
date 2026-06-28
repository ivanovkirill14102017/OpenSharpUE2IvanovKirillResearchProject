namespace L2Viewer.DatFile;

public sealed class ClassInfoDatReader : DatSchemaReader<ClassInfoDatDocument>
{
    public override string FileName => "classinfo-e.dat";

    public override ClassInfoDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ClassInfoDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new ClassInfoDatEntry(reader.ReadUInt32(), reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ClassInfoDatDocument(path, entries);
    }
}
