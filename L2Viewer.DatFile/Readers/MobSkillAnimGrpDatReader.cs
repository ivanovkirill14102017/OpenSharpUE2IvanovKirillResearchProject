namespace L2Viewer.DatFile;

public sealed class MobSkillAnimGrpDatReader : DatSchemaReader<MobSkillAnimGrpDatDocument>
{
    public override string FileName => "mobskillanimgrp.dat";

    public override MobSkillAnimGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<MobSkillAnimGrpDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new MobSkillAnimGrpDatEntry(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadUnicodeString32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new MobSkillAnimGrpDatDocument(path, entries);
    }
}
