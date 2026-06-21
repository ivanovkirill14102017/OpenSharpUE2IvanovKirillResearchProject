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
            ["skillname-e.dat"] = new SkillNameDatReader(),
            ["skillgrp.dat"] = new SkillGrpCt15DatReader(),
            ["mobskillanimgrp.dat"] = new MobSkillAnimGrpDatReader(),
            ["staticobject-e.dat"] = new StaticObjectDatReader(),
            ["zonename-e.dat"] = new ZoneNameDatReader(),
            ["huntingzone-e.dat"] = new HuntingZoneDatReader(),
            ["variationeffectgrp-e.dat"] = new VariationEffectGrpDatReader()
        };

    public static IReadOnlyList<DatSchemaDescriptor> Known { get; } =
    [
        new("skillname-e.dat", typeof(SkillNameDatDocument), typeof(SkillNameDatReader)),
        new("skillgrp.dat", typeof(SkillGrpCt15DatDocument), typeof(SkillGrpCt15DatReader)),
        new("mobskillanimgrp.dat", typeof(MobSkillAnimGrpDatDocument), typeof(MobSkillAnimGrpDatReader)),
        new("staticobject-e.dat", typeof(StaticObjectDatDocument), typeof(StaticObjectDatReader)),
        new("zonename-e.dat", typeof(ZoneNameDatDocument), typeof(ZoneNameDatReader)),
        new("huntingzone-e.dat", typeof(HuntingZoneDatDocument), typeof(HuntingZoneDatReader)),
        new("variationeffectgrp-e.dat", typeof(VariationEffectGrpDatDocument), typeof(VariationEffectGrpDatReader))
    ];

    public static DatSchemaDescriptor Resolve(string path)
    {
        var fileName = Path.GetFileName(path);
        foreach (var schema in Known)
        {
            if (string.Equals(schema.FileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return schema;
            }
        }

        throw new NotSupportedException($"DAT schema is not registered for file '{fileName}'.");
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
