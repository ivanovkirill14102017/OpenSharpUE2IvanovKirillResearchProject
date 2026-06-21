namespace L2Viewer.DatFile;

public sealed class SkillGrpCt15DatReader : DatSchemaReader<SkillGrpCt15DatDocument>
{
    public override string FileName => "skillgrp.dat";

    public override SkillGrpCt15DatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<SkillGrpCt15DatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new SkillGrpCt15DatEntry(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadSingle(),
                reader.ReadInt32(),
                reader.ReadUnicodeString32(),
                reader.ReadUnicodeString32(),
                reader.ReadUnicodeString32(),
                reader.ReadUnicodeString32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SkillGrpCt15DatDocument(path, entries);
    }
}
