namespace L2Viewer.DatFile;

public sealed record DatSchemaDescriptor(
    string FileName,
    Type DocumentType,
    Type ReaderType);

public static class DatSchemaRegistry
{
    private static readonly IReadOnlyDictionary<string, IDatSchemaReader> Readers =
        new Dictionary<string, IDatSchemaReader>(StringComparer.OrdinalIgnoreCase)
        {
            ["actionname-e.dat"] = new ActionNameDatReader(),
            ["armorgrp.dat"] = new ArmorGrpDatReader(),
            ["castlename-e.dat"] = new CastleNameDatReader(),
            ["chargrp.dat"] = new CharGrpDatReader(),
            ["etcitemgrp.dat"] = new EtcItemGrpDatReader(),
            ["classinfo-e.dat"] = new ClassInfoDatReader(),
            ["commandname-e.dat"] = new CommandNameDatReader(),
            ["creditgrp-e.dat"] = new CreditGrpDatReader(),
            ["entereventgrp.dat"] = new EnterEventGrpDatReader(),
            ["eula-e.dat"] = new EulaDatReader(),
            ["gametip-e.dat"] = new GameTipDatReader(),
            ["hairaccessorylocgrp.dat"] = new HairAccessoryLocGrpDatReader(),
            ["hennagrp-e.dat"] = new HennaGrpDatReader(),
            ["itemname-e.dat"] = new ItemNameDatReader(),
            ["instantzonedata-e.dat"] = new InstantZoneDataDatReader(),
            ["logongrp.dat"] = new LogoGrpDatReader(),
            ["musicinfo.dat"] = new MusicInfoDatReader(),
            ["obscene-e.dat"] = new ObsceneDatReader(),
            ["questname-e.dat"] = new QuestNameDatReader(),
            ["optiondata_client-e.dat"] = new OptionDataClientDatReader(),
            ["raiddata-e.dat"] = new RaidDataDatReader(),
            ["recipe-c.dat"] = new RecipeDatReader(),
            ["servername-e.dat"] = new ServerNameDatReader(),
            ["shortcutalias.dat"] = new ShortcutAliasDatReader(),
            ["skillname-e.dat"] = new SkillNameDatReader(),
            ["skillgrp.dat"] = new SkillGrpCt15DatReader(),
            ["skillsoundgrp.dat"] = new SkillSoundGrpDatReader(),
            ["mobskillanimgrp.dat"] = new MobSkillAnimGrpDatReader(),
            ["npcgrp.dat"] = new NpcGrpDatReader(),
            ["npcname-e.dat"] = new NpcNameDatReader(),
            ["staticobject-e.dat"] = new StaticObjectDatReader(),
            ["symbolname-e.dat"] = new SymbolNameDatReader(),
            ["sysstring-e.dat"] = new SysStringDatReader(),
            ["systemmsg-e.dat"] = new SystemMsgDatReader(),
            ["transformdata.dat"] = new TransformDataDatReader(),
            ["zonename-e.dat"] = new ZoneNameDatReader(),
            ["huntingzone-e.dat"] = new HuntingZoneDatReader(),
            ["variationeffectgrp-e.dat"] = new VariationEffectGrpDatReader(),
            ["weapongrp.dat"] = new WeaponGrpDatReader()
        };

    public static IReadOnlyList<DatSchemaDescriptor> Known { get; } = Readers
        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        .Select(x => new DatSchemaDescriptor(x.Key, x.Value.DocumentType, x.Value.GetType()))
        .ToArray();

    public static DatSchemaDescriptor Resolve(string path)
    {
        var fileName = Path.GetFileName(path);
        return Known.FirstOrDefault(x => string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotSupportedException($"DAT schema is not registered for file '{fileName}'.");
    }

    public static IDatSchemaReader GetReader(string path)
    {
        var fileName = Path.GetFileName(path);
        if (!Readers.TryGetValue(fileName, out var reader))
        {
            throw new NotSupportedException($"DAT schema is not registered for file '{fileName}'.");
        }

        return reader;
    }

    public static IDatSchemaReader<TDocument> GetReader<TDocument>(string path)
        where TDocument : DatDocument
    {
        var reader = GetReader(path);
        if (reader is not IDatSchemaReader<TDocument> typedReader)
        {
            throw new InvalidOperationException(
                $"DAT reader for '{Path.GetFileName(path)}' returns '{reader.DocumentType.Name}', not '{typeof(TDocument).Name}'.");
        }

        return typedReader;
    }

    public static DatDocument ReadDocument(string path)
    {
        return GetReader(path).Read(path);
    }
}
