namespace L2Viewer.DatFile;

public sealed class HairGrpDatReader : DatSchemaReader<HairGrpDatDocument>
{
    public override string FileName => "hairgrp.dat";

    public override HairGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        const int entryCount = 15;
        const int valuesPerEntry = 30;

        var entries = new List<HairGrpDatEntry>(entryCount);
        for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            var values = new List<int>(valuesPerEntry);
            for (var valueIndex = 0; valueIndex < valuesPerEntry; valueIndex++)
            {
                values.Add(reader.ReadInt32());
            }

            entries.Add(new HairGrpDatEntry(entryIndex, values));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new HairGrpDatDocument(path, entries);
    }
}
