namespace L2Viewer.DatFile;

public sealed class HennaGrpDatReader : DatSchemaReader<HennaGrpDatDocument>
{
    public override string FileName => "hennagrp-e.dat";

    public override HennaGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<HennaGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new HennaGrpDatEntry(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new HennaGrpDatDocument(path, entries);
    }
}
