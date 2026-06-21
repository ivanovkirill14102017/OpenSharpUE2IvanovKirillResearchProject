namespace L2Viewer.DatFile;

public sealed class VariationEffectGrpDatReader : DatSchemaReader<VariationEffectGrpDatDocument>
{
    public override string FileName => "variationeffectgrp-e.dat";

    public override VariationEffectGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<VariationEffectGrpDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            entries.Add(new VariationEffectGrpDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUnicodeString32(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new VariationEffectGrpDatDocument(path, entries);
    }
}
