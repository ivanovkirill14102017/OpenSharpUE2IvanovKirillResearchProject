namespace L2Viewer.DatFile;

public sealed class SkillGrpCt15DatReader : DatSchemaReader<SkillGrpCt15DatDocument>
{
    public override string FileName => "skillgrp.dat";

    public override SkillGrpCt15DatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        try
        {
            return ReadCt15(path, decoded);
        }
        catch (EndOfStreamException)
        {
            return ReadInterlude(path, decoded);
        }
        catch (InvalidDataException)
        {
            return ReadInterlude(path, decoded);
        }
    }

    private static SkillGrpCt15DatDocument ReadCt15(string path, byte[] decoded)
    {
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

    private static SkillGrpCt15DatDocument ReadInterlude(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<SkillGrpCt15DatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var skillId = reader.ReadInt32();
            var skillLevel = reader.ReadInt32();
            var operType = reader.ReadInt32();
            var mpConsume = reader.ReadInt32();
            var castRange = reader.ReadInt32();
            var castStyle = reader.ReadInt32();
            var hitTime = reader.ReadSingle();
            var isMagic = reader.ReadInt32();
            var animationCharacter = reader.ReadUnicodeString32();
            var descriptionToken = reader.ReadUnicodeString32();
            var iconName = reader.ReadUnicodeString32();
            var extraEff = reader.ReadInt32();
            var isEnchanted = reader.ReadInt32();
            var enchantedSkillId = reader.ReadInt32();
            var hpConsume = reader.ReadInt32();
            var unknown0 = reader.ReadInt32();
            var unknown1 = reader.ReadInt32();

            entries.Add(new SkillGrpCt15DatEntry(
                skillId,
                skillLevel,
                operType,
                mpConsume,
                castRange,
                castStyle,
                0,
                hitTime,
                isMagic,
                animationCharacter,
                descriptionToken,
                iconName,
                string.Empty,
                isEnchanted,
                enchantedSkillId,
                hpConsume,
                extraEff,
                unknown0,
                unknown1));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SkillGrpCt15DatDocument(path, entries);
    }
}
