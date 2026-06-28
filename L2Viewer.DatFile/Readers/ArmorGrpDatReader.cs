namespace L2Viewer.DatFile;

public sealed class ArmorGrpDatReader : DatSchemaReader<ArmorGrpDatDocument>
{
    private static readonly (string Name, bool UseMtx2)[] MeshGroupDefinitions =
    [
        ("m_HumnFigh", false),
        ("m_HumnFigh_add", true),
        ("f_HumnFigh", false),
        ("f_HumnFigh_add", true),
        ("m_DarkElf", false),
        ("m_DarkElf_add", true),
        ("f_DarkElf", false),
        ("f_DarkElf_add", true),
        ("m_Dorf", false),
        ("m_Dorf_add", true),
        ("f_Dorf", false),
        ("f_Dorf_add", true),
        ("m_Elf", false),
        ("m_Elf_add", true),
        ("f_Elf", false),
        ("f_Elf_add", true),
        ("m_HumnMyst", false),
        ("m_HumnMyst_add", true),
        ("f_HumnMyst", false),
        ("f_HumnMyst_add", true),
        ("m_OrcFigh", false),
        ("m_OrcFigh_add", true),
        ("f_OrcFigh", false),
        ("f_OrcFigh_add", true),
        ("m_OrcMage", false),
        ("m_OrcMage_add", true),
        ("f_OrcMage", false),
        ("f_OrcMage_add", true),
        ("m_Kamael", false),
        ("m_Kamael_add", true),
        ("f_Kamael", false),
        ("f_Kamael_add", true),
        ("NPC", false),
        ("NPC_add", true)
    ];

    public override string FileName => "armorgrp.dat";

    public override ArmorGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<ArmorGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var tag = reader.ReadUInt32();
            var id = reader.ReadUInt32();
            var dropType = reader.ReadUInt32();
            var dropAnimationType = reader.ReadUInt32();
            var dropRadius = reader.ReadUInt32();
            var dropHeight = reader.ReadUInt32();
            var unknown0 = reader.ReadUInt32();
            var dropMesh1 = reader.ReadUnicodeString32();
            var dropMesh2 = reader.ReadUnicodeString32();
            var dropMesh3 = reader.ReadUnicodeString32();
            var dropTexture1 = reader.ReadUnicodeString32();
            var dropTexture2 = reader.ReadUnicodeString32();
            var dropTexture3 = reader.ReadUnicodeString32();
            var icons = DatReaderPrimitives.ReadUnicodeArray(reader, 5);
            var durability = unchecked((uint)reader.ReadInt32());
            var weight = reader.ReadUInt32();
            var material = reader.ReadUInt32();
            var crystallizable = reader.ReadUInt32();
            var unknown1 = reader.ReadUInt32();
            var unknown2Count = checked((int)reader.ReadUInt32());
            var unknown2 = DatReaderPrimitives.ReadUInt32Array(reader, unknown2Count);
            var timeTab = reader.ReadUnicodeString32();
            var bodyPart = reader.ReadUInt32();
            var meshGroups = ReadMeshGroups(reader);
            var attackEffect = reader.ReadUnicodeString32();
            var itemSoundCount = checked((int)reader.ReadUInt32());
            var itemSounds = DatReaderPrimitives.ReadUnicodeArray(reader, itemSoundCount);
            var dropSound = reader.ReadUnicodeString32();
            var equipSound = reader.ReadUnicodeString32();
            var unknown3 = reader.ReadUInt32();
            var unknown4 = reader.ReadUInt32();
            var armorType = reader.ReadUInt32();
            var crystalType = reader.ReadUInt32();
            var avoidModifier = reader.ReadUInt32();
            var physicalDefense = reader.ReadUInt32();
            var magicalDefense = reader.ReadUInt32();
            var mpBonus = reader.ReadUInt32();
            var unknown5 = reader.ReadUInt32();

            entries.Add(new ArmorGrpDatEntry(
                tag,
                id,
                dropType,
                dropAnimationType,
                dropRadius,
                dropHeight,
                unknown0,
                dropMesh1,
                dropMesh2,
                dropMesh3,
                dropTexture1,
                dropTexture2,
                dropTexture3,
                icons,
                durability,
                weight,
                material,
                crystallizable,
                unknown1,
                unknown2,
                timeTab,
                bodyPart,
                meshGroups,
                attackEffect,
                itemSounds,
                dropSound,
                equipSound,
                unknown3,
                unknown4,
                armorType,
                crystalType,
                avoidModifier,
                physicalDefense,
                magicalDefense,
                mpBonus,
                unknown5));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new ArmorGrpDatDocument(path, entries);
    }

    private static IReadOnlyList<ArmorMeshGroupDatEntry> ReadMeshGroups(DatBinaryReader reader)
    {
        var values = new List<ArmorMeshGroupDatEntry>(MeshGroupDefinitions.Length);
        foreach (var definition in MeshGroupDefinitions)
        {
            values.Add(new ArmorMeshGroupDatEntry(
                definition.Name,
                definition.UseMtx2
                    ? DatReaderPrimitives.ReadMtx2(reader)
                    : DatReaderPrimitives.ReadMtxPlain(reader)));
        }

        return values;
    }
}
