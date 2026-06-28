namespace L2Viewer.DatFile;

public sealed class EtcItemGrpDatReader : DatSchemaReader<EtcItemGrpDatDocument>
{
    public override string FileName => "etcitemgrp.dat";

    public override EtcItemGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<EtcItemGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
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
            var unknown2Count = checked((int)reader.ReadUInt32());
            var unknown2 = DatReaderPrimitives.ReadUInt32Array(reader, unknown2Count);
            var fort = reader.ReadUnicodeString32();
            var meshTexturePair = DatReaderPrimitives.ReadMtx(reader);
            var itemSound = reader.ReadUnicodeString32();
            var equipSound = reader.ReadUnicodeString32();
            var stackable = reader.ReadUInt32();
            var family = reader.ReadUInt32();
            var grade = reader.ReadUInt32();

            entries.Add(new EtcItemGrpDatEntry(
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
                fort,
                meshTexturePair,
                itemSound,
                equipSound,
                stackable,
                family,
                grade));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new EtcItemGrpDatDocument(path, entries);
    }
}
