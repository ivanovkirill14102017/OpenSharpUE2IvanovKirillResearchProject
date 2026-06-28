namespace L2Viewer.DatFile;

public sealed class EnterEventGrpDatReader : DatSchemaReader<EnterEventGrpDatDocument>
{
    public override string FileName => "entereventgrp.dat";

    public override EnterEventGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<EnterEventGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new EnterEventGrpDatEntry(
                reader.ReadUInt32(),
                reader.ReadByte(),
                reader.ReadAsciiString(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUnicodeString32(),
                reader.ReadUnicodeString32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new EnterEventGrpDatDocument(path, entries);
    }
}
