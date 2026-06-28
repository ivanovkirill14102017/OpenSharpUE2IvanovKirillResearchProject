namespace L2Viewer.DatFile;

public sealed class HairAccessoryLocGrpDatReader : DatSchemaReader<HairAccessoryLocGrpDatDocument>
{
    public override string FileName => "hairaccessorylocgrp.dat";

    public override HairAccessoryLocGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<HairAccessoryLocGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var floatTriples = new List<float[]>(15);
            var intTriples = new List<int[]>(15);
            var name = reader.ReadUnicodeString32();
            for (var j = 0; j < 15; j++)
            {
                floatTriples.Add([reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()]);
                intTriples.Add([reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()]);
            }

            entries.Add(new HairAccessoryLocGrpDatEntry(name, floatTriples, intTriples));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new HairAccessoryLocGrpDatDocument(path, entries);
    }
}
