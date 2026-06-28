namespace L2Viewer.DatFile;

public sealed class NpcGrpDatReader : DatSchemaReader<NpcGrpDatDocument>
{
    public override string FileName => "npcgrp.dat";

    public override NpcGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<NpcGrpDatEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var tag = reader.ReadUInt32();
            var @class = reader.ReadUnicodeString32();
            var mesh = reader.ReadUnicodeString32();
            var textures1 = ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
            var textures2 = ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
            var dynamicTable1 = ReadUInt32Array(reader, reader.ReadPackedInt32());
            var npcSpeed = reader.ReadSingle();
            var unknown0 = ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
            var sounds1 = ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
            var sounds2 = ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
            var sounds3 = ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
            var rbEffectOn = reader.ReadUInt32();
            string? rbEffect = null;
            float? rbEffectScale = null;
            if (rbEffectOn == 1)
            {
                rbEffect = reader.ReadUnicodeString32();
                rbEffectScale = reader.ReadSingle();
            }

            var unknown1 = ReadUInt32Array(reader, reader.ReadPackedInt32());
            entries.Add(new NpcGrpDatEntry(
                tag,
                @class,
                mesh,
                textures1,
                textures2,
                dynamicTable1,
                npcSpeed,
                unknown0,
                sounds1,
                sounds2,
                sounds3,
                rbEffectOn,
                rbEffect,
                rbEffectScale,
                unknown1,
                reader.ReadUnicodeString32(),
                reader.ReadUInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadUInt32(),
                reader.ReadUInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new NpcGrpDatDocument(path, entries);
    }

    private static IReadOnlyList<string> ReadUnicodeArray(DatBinaryReader reader, int count)
    {
        var values = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadUnicodeString32());
        }

        return values;
    }

    private static IReadOnlyList<uint> ReadUInt32Array(DatBinaryReader reader, int count)
    {
        var values = new List<uint>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadUInt32());
        }

        return values;
    }
}
