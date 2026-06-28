namespace L2Viewer.DatFile;

public sealed class SkillSoundGrpDatReader : DatSchemaReader<SkillSoundGrpDatDocument>
{
    public override string FileName => "skillsoundgrp.dat";

    public override SkillSoundGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        try
        {
            return ReadWithCount(path, decoded);
        }
        catch (EndOfStreamException)
        {
            return ReadUntilEnd(path, decoded);
        }
        catch (InvalidDataException)
        {
            return ReadUntilEnd(path, decoded);
        }
    }

    private static SkillSoundGrpDatDocument ReadWithCount(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<SkillSoundGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(ReadEntry(reader));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SkillSoundGrpDatDocument(path, entries);
    }

    private static SkillSoundGrpDatDocument ReadUntilEnd(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var entries = new List<SkillSoundGrpDatEntry>();
        while (reader.Remaining > 0 && !reader.IsAtSafePackageRemainder())
        {
            entries.Add(ReadEntry(reader));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SkillSoundGrpDatDocument(path, entries);
    }

    private static SkillSoundGrpDatEntry ReadEntry(DatBinaryReader reader)
    {
        var skillId = reader.ReadUInt32();
        var skillLevel = reader.ReadUInt32();
        var spellSounds = ReadUnicodeArray(reader, 3);
        var spellValues = ReadSingleArray(reader, 6);
        var shotSounds = ReadUnicodeArray(reader, 3);
        var shotValues = ReadSingleArray(reader, 6);
        var expSounds = ReadUnicodeArray(reader, 3);
        var expValues = ReadSingleArray(reader, 6);
        var subSounds = ReadUnicodeArray(reader, 16);
        var throwSounds = ReadUnicodeArray(reader, 18);
        return new SkillSoundGrpDatEntry(
            skillId,
            skillLevel,
            spellSounds,
            spellValues,
            shotSounds,
            shotValues,
            expSounds,
            expValues,
            subSounds,
            throwSounds,
            reader.ReadSingle(),
            reader.ReadSingle());
    }

    private static IReadOnlyList<string> ReadUnicodeArray(DatBinaryReader reader, int count)
    {
        var values = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadUnicodeString32());
        }

        return values;
    }

    private static IReadOnlyList<float> ReadSingleArray(DatBinaryReader reader, int count)
    {
        var values = new List<float>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadSingle());
        }

        return values;
    }
}
