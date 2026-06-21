namespace L2Viewer.DatFile;

public sealed class SkillNameDatReader : DatSchemaReader<SkillNameDatDocument>
{
    public override string FileName => "skillname-e.dat";

    public override SkillNameDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<SkillNameDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new SkillNameDatEntry(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SkillNameDatDocument(path, entries);
    }
}
