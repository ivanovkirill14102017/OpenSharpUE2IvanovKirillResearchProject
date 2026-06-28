namespace L2Viewer.DatFile;

public sealed class CreditGrpDatReader : DatSchemaReader<CreditGrpDatDocument>
{
    public override string FileName => "creditgrp-e.dat";

    public override CreditGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<CreditGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new CreditGrpDatEntry(
                reader.ReadUInt32(),
                reader.ReadAsciiString(),
                reader.ReadAsciiString(),
                reader.ReadUInt32(),
                reader.ReadUInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new CreditGrpDatDocument(path, entries);
    }
}
