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
        var errors = new List<string>();

        foreach (var layout in new[]
                 {
                     ArmorGrpLayout.InterludeWithCount,
                     ArmorGrpLayout.Ct15WithCount,
                     ArmorGrpLayout.Ct15NoCount,
                     ArmorGrpLayout.Ct10NoCount,
                     ArmorGrpLayout.C5NoCount
                 })
        {
            var reader = new DatBinaryReader(decoded);
            try
            {
                var entries = ReadDocument(reader, layout);
                reader.EnsureFullyConsumedOrSafePackage();
                return new ArmorGrpDatDocument(path, entries);
            }
            catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or OverflowException)
            {
                errors.Add($"{layout}: {ex.Message}");
            }
        }

        throw new InvalidDataException(
            $"Unable to parse armorgrp.dat with supported layouts. {string.Join(" | ", errors)}");
    }

    private static IReadOnlyList<ArmorGrpDatEntry> ReadDocument(DatBinaryReader reader, ArmorGrpLayout layout)
    {
        var entries = new List<ArmorGrpDatEntry>();
        var remainingEntries = HasRecordCount(layout) ? reader.ReadInt32() : int.MaxValue;

        while (remainingEntries > 0 && reader.Remaining > 0 && !reader.IsAtSafePackageRemainder())
        {
            entries.Add(ReadEntry(reader, layout));
            remainingEntries--;
        }

        return entries;
    }

    private static ArmorGrpDatEntry ReadEntry(DatBinaryReader reader, ArmorGrpLayout layout)
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

        IReadOnlyList<uint> unknown2;
        string timeTab;

        if (HasUnknown2Table(layout))
        {
            var unknown2Count = checked((int)reader.ReadUInt32());
            unknown2 = DatReaderPrimitives.ReadUInt32Array(reader, unknown2Count);
            timeTab = reader.ReadUnicodeString32();
        }
        else
        {
            unknown2 = Array.Empty<uint>();
            timeTab = HasTimeTab(layout)
                ? reader.ReadUnicodeString32()
                : string.Empty;
        }

        var bodyPart = reader.ReadUInt32();
        var meshGroups = ReadMeshGroups(reader, layout);
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
        var unknown5 = HasTrailingUnknown5(layout) ? reader.ReadUInt32() : 0u;

        return new ArmorGrpDatEntry(
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
            unknown5);
    }

    private static IReadOnlyList<ArmorMeshGroupDatEntry> ReadMeshGroups(DatBinaryReader reader, ArmorGrpLayout layout)
    {
        var definitions = layout switch
        {
            ArmorGrpLayout.C5NoCount or ArmorGrpLayout.InterludeWithCount => MeshGroupDefinitionsC5,
            _ => MeshGroupDefinitions
        };

        var values = new List<ArmorMeshGroupDatEntry>(definitions.Length);
        foreach (var definition in definitions)
        {
            values.Add(new ArmorMeshGroupDatEntry(
                definition.Name,
                definition.UseMtx2
                    ? DatReaderPrimitives.ReadMtx2(reader)
                    : DatReaderPrimitives.ReadMtxPlain(reader)));
        }

        return values;
    }

    private static readonly (string Name, bool UseMtx2)[] MeshGroupDefinitionsC5 =
    [
        ("m_HumnFigh", false),
        ("m_HumnFigh_add", false),
        ("f_HumnFigh", false),
        ("f_HumnFigh_add", false),
        ("m_DarkElf", false),
        ("m_DarkElf_add", false),
        ("f_DarkElf", false),
        ("f_DarkElf_add", false),
        ("m_Dorf", false),
        ("m_Dorf_add", false),
        ("f_Dorf", false),
        ("f_Dorf_add", false),
        ("m_Elf", false),
        ("m_Elf_add", false),
        ("f_Elf", false),
        ("f_Elf_add", false),
        ("m_HumnMyst", false),
        ("m_HumnMyst_add", false),
        ("f_HumnMyst", false),
        ("f_HumnMyst_add", false),
        ("m_OrcFigh", false),
        ("m_OrcFigh_add", false),
        ("f_OrcFigh", false),
        ("f_OrcFigh_add", false),
        ("m_OrcMage", false),
        ("m_OrcMage_add", false),
        ("f_OrcMage", false),
        ("f_OrcMage_add", false),
        ("Unknown_MT", false),
        ("NPC_MT", false),
        ("ACC_MT", false)
    ];

    private enum ArmorGrpLayout
    {
        Ct15WithCount,
        Ct15NoCount,
        Ct10NoCount,
        C5NoCount,
        InterludeWithCount
    }

    private static bool HasRecordCount(ArmorGrpLayout layout) =>
        layout is ArmorGrpLayout.Ct15WithCount or ArmorGrpLayout.InterludeWithCount;

    private static bool HasUnknown2Table(ArmorGrpLayout layout) =>
        layout is ArmorGrpLayout.Ct15WithCount or ArmorGrpLayout.Ct15NoCount;

    private static bool HasTimeTab(ArmorGrpLayout layout) =>
        layout is not ArmorGrpLayout.C5NoCount and not ArmorGrpLayout.InterludeWithCount;

    private static bool HasTrailingUnknown5(ArmorGrpLayout layout) =>
        layout is not ArmorGrpLayout.C5NoCount and not ArmorGrpLayout.InterludeWithCount;
}
