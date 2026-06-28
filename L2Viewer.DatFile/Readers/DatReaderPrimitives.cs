namespace L2Viewer.DatFile;

internal static class DatReaderPrimitives
{
    public static IReadOnlyList<string> ReadUnicodeArray(DatBinaryReader reader, int count)
    {
        var values = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadUnicodeString32());
        }

        return values;
    }

    public static IReadOnlyList<uint> ReadUInt32Array(DatBinaryReader reader, int count)
    {
        var values = new List<uint>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadUInt32());
        }

        return values;
    }

    public static IReadOnlyList<int> ReadInt32Array(DatBinaryReader reader, int count)
    {
        var values = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadInt32());
        }

        return values;
    }

    public static IReadOnlyList<float> ReadSingleArray(DatBinaryReader reader, int count)
    {
        var values = new List<float>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(reader.ReadSingle());
        }

        return values;
    }

    public static IReadOnlyList<DatMaterialPair> ReadMat(DatBinaryReader reader)
    {
        var count = reader.ReadInt32();
        var values = new List<DatMaterialPair>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(new DatMaterialPair(reader.ReadUInt32(), reader.ReadUInt32()));
        }

        return values;
    }

    public static DatMeshTextureSet ReadMtx(DatBinaryReader reader)
    {
        var meshCount = reader.ReadInt32();
        var meshes = ReadUnicodeArray(reader, meshCount);
        var textureCount = reader.ReadInt32();
        var textures = ReadUnicodeArray(reader, textureCount);
        return new DatMeshTextureSet(meshes, textures);
    }

    public static DatMeshTextureVariantSet ReadMtxPlain(DatBinaryReader reader)
    {
        var value = ReadMtx(reader);
        return new DatMeshTextureVariantSet(value.Meshes, Array.Empty<DatMeshVariantEntry>(), value.Textures);
    }

    public static DatMeshTextureVariantSet ReadMtx2(DatBinaryReader reader)
    {
        var meshCount = reader.ReadInt32();
        var variants = new List<DatMeshVariantEntry>(meshCount);
        for (var i = 0; i < meshCount; i++)
        {
            variants.Add(new DatMeshVariantEntry(
                reader.ReadUnicodeString32(),
                reader.ReadUInt32(),
                reader.ReadByte()));
        }

        var textureCount = reader.ReadInt32();
        var textures = ReadUnicodeArray(reader, textureCount);
        return new DatMeshTextureVariantSet(Array.Empty<string>(), variants, textures);
    }
}
