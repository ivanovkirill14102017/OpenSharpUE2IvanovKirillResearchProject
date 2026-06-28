namespace L2Viewer.DatFile;

public sealed class CharGrpDatReader : DatSchemaReader<CharGrpDatDocument>
{
    public override string FileName => "chargrp.dat";

    public override CharGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var errors = new List<string>();

        foreach (var layout in BuildLayouts())
        {
            var reader = new DatBinaryReader(decoded);
            try
            {
                var document = ReadDocument(path, reader, layout);
                try
                {
                    reader.EnsureFullyConsumedOrSafePackage();
                }
                catch (InvalidDataException ex) when (reader.Remaining > 0 && reader.Remaining <= 16)
                {
                    var position = reader.Position;
                    var tail = reader.ReadBytes(reader.Remaining);
                    reader.Position = position;
                    var tailHex = BitConverter.ToString(tail);
                    throw new InvalidDataException($"{ex.Message} Tail={tailHex}");
                }

                return document;
            }
            catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or OverflowException)
            {
                errors.Add($"{layout}: {ex.Message}");
            }
        }

        throw new InvalidDataException(
            $"Unable to parse chargrp.dat with supported layouts. {string.Join(" | ", errors)}");
    }

    private static CharGrpDatDocument ReadDocument(string path, DatBinaryReader reader, CharLayout layout)
    {
        const int count = 17;
        var entries = new List<CharGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var currentField = "hair";
            try
            {
                var hair = DatReaderPrimitives.ReadUnicodeArray(reader, 240);
                currentField = "faceMeshCount";
                var faceMeshCount = checked((int)reader.ReadUInt32());
                currentField = "faceMeshes";
                var faceMeshes = DatReaderPrimitives.ReadUnicodeArray(reader, faceMeshCount);
                currentField = "faceTextureCount";
                var faceTextureCount = checked((int)reader.ReadUInt32());
                currentField = "faceTextures";
                var faceTextures = DatReaderPrimitives.ReadUnicodeArray(reader, faceTextureCount);
                currentField = "reservedBlock1";
                var reservedBlock1 = reader.ReadBytes(layout.ReservedBlock1Size);
                currentField = "gloves";
                var gloves = ReadEquipment(reader, layout.UseByteEquipmentTail);
                currentField = "upper";
                var upper = ReadEquipment(reader, layout.UseByteEquipmentTail);
                currentField = "lower";
                var lower = ReadEquipment(reader, layout.UseByteEquipmentTail);
                currentField = "boots";
                var boots = ReadEquipment(reader, layout.UseByteEquipmentTail);
                currentField = "reservedBlock2";
                var reservedBlock2 = reader.ReadBytes(layout.ReservedBlock2Size);
                currentField = "attackEffect";
                var attackEffect = reader.ReadUnicodeString32();
                currentField = "walkAnimationFrame";
                var walkAnimationFrame = reader.ReadUInt32();
                currentField = "attackCount";
                var attackCount = checked((int)reader.ReadUInt32());
                currentField = "defenseCount";
                var defenseCount = checked((int)reader.ReadUInt32());
                currentField = "damageCount";
                var damageCount = checked((int)reader.ReadUInt32());
                currentField = "attackSounds";
                var attackSounds = DatReaderPrimitives.ReadUnicodeArray(reader, attackCount);
                currentField = "defenseSounds";
                var defenseSounds = DatReaderPrimitives.ReadUnicodeArray(reader, defenseCount);
                currentField = "damageSounds";
                var damageSounds = DatReaderPrimitives.ReadUnicodeArray(reader, damageCount);
                currentField = "voiceHandSounds";
                var voiceHandSounds = DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
                currentField = "voiceOneHandSwordSounds";
                var voiceOneHandSwordSounds = DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
                currentField = "voiceTwoHandSwordSounds";
                var voiceTwoHandSwordSounds = DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
                currentField = "voiceDualSounds";
                var voiceDualSounds = DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
                currentField = "voicePoleSounds";
                var voicePoleSounds = DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));

                currentField = "voiceSpacer1";
                SkipUInt32(reader, layout.Spacer1Length);

                currentField = "reserve1Sounds";
                var reserve1Sounds = DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));
                currentField = "reserve2Sounds";
                var reserve2Sounds = DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()));

                currentField = "voiceSpacer2";
                SkipUInt32(reader, layout.Spacer2Length);

                currentField = "reserve3Sounds";
                var reserve3Sounds = layout.ReserveTailGroups >= 1
                    ? DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()))
                    : Array.Empty<string>();
                currentField = "reserve4Sounds";
                var reserve4Sounds = layout.ReserveTailGroups >= 2
                    ? DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()))
                    : Array.Empty<string>();
                currentField = "reserve5Sounds";
                var reserve5Sounds = layout.ReserveTailGroups >= 3
                    ? DatReaderPrimitives.ReadUnicodeArray(reader, checked((int)reader.ReadUInt32()))
                    : Array.Empty<string>();

                currentField = "final";
                var finalValue = ShouldReadFinal(layout, i, count)
                    ? reader.ReadUInt32()
                    : 0u;

                if (layout.HasSecondFinalUInt32)
                {
                    _ = reader.ReadUInt32();
                }

                if (layout.HasNameTail)
                {
                    currentField = "name";
                    _ = reader.ReadAsciiString();
                    currentField = "tailUnknown";
                    _ = DatReaderPrimitives.ReadInt32Array(reader, 3);

                    if (layout.HasExtraUnicodeTail)
                    {
                        currentField = "tailP1";
                        var p1Count = checked((int)reader.ReadUInt32());
                        _ = DatReaderPrimitives.ReadUnicodeArray(reader, p1Count);
                        currentField = "tailP2";
                        var p2Count = checked((int)reader.ReadUInt32());
                        _ = DatReaderPrimitives.ReadUnicodeArray(reader, p2Count);
                    }
                }

                entries.Add(new CharGrpDatEntry(
                    hair,
                    new DatMeshTextureSet(faceMeshes, faceTextures),
                    reservedBlock1,
                    gloves,
                    upper,
                    lower,
                    boots,
                    reservedBlock2,
                    attackEffect,
                    walkAnimationFrame,
                    attackSounds,
                    defenseSounds,
                    damageSounds,
                    voiceHandSounds,
                    voiceOneHandSwordSounds,
                    voiceTwoHandSwordSounds,
                    voiceDualSounds,
                    voicePoleSounds,
                    reserve1Sounds,
                    reserve2Sounds,
                    reserve3Sounds,
                    reserve4Sounds,
                    reserve5Sounds,
                    finalValue));
            }
            catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or OverflowException)
            {
                throw new InvalidDataException(
                    $"Failed to parse chargrp.dat entry {i} at field '{currentField}' using {layout}: {ex.Message}",
                    ex);
            }
        }

        return new CharGrpDatDocument(path, entries);
    }

    private static CharGrpEquipmentDatEntry ReadEquipment(DatBinaryReader reader, bool byteTail)
    {
        var meshCount = checked((int)reader.ReadUInt32());
        var meshes = DatReaderPrimitives.ReadUnicodeArray(reader, meshCount);
        var textureCount = checked((int)reader.ReadUInt32());
        var textures = DatReaderPrimitives.ReadUnicodeArray(reader, textureCount);
        var additionalMeshCount = checked((int)reader.ReadUInt32());
        var additionalMeshes = DatReaderPrimitives.ReadUnicodeArray(reader, additionalMeshCount);
        var additionalTextureCount = checked((int)reader.ReadUInt32());
        var additionalTextures = DatReaderPrimitives.ReadUnicodeArray(reader, additionalTextureCount);
        var additionalByteCount = reader.ReadPackedInt32();
        var additionalBytes = reader.ReadBytes(additionalByteCount);
        var additionalIntCount = reader.ReadPackedInt32();
        var additionalIntegers = byteTail
            ? reader.ReadBytes(additionalIntCount).Select(x => (uint)x).ToArray()
            : DatReaderPrimitives.ReadUInt32Array(reader, additionalIntCount);

        return new CharGrpEquipmentDatEntry(
            meshes,
            textures,
            additionalMeshes,
            additionalTextures,
            additionalBytes,
            additionalIntegers);
    }

    private static void SkipUInt32(DatBinaryReader reader, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _ = reader.ReadUInt32();
        }
    }

    private static IReadOnlyList<CharLayout> BuildLayouts()
    {
        return
        [
            new CharLayout("Ct15-S3-0-R1-NoFinal", 162, 270, false, 3, 0, 1, false, false, false, false, false),
            new CharLayout("Ct15-S3-0-R1-SkipLastFinal", 162, 270, false, 3, 0, 1, true, true, false, false, false),
            new CharLayout("Ct15-S3-3-R3", 162, 270, false, 3, 3, 3, true, false, false, false, false),
            new CharLayout("Ct15-S3-1-R3", 162, 270, false, 3, 1, 3, true, false, false, false, false),
            new CharLayout("Ct15-S3-0-R3", 162, 270, false, 3, 0, 3, true, false, false, false, false),
            new CharLayout("Ct15-S3-0-R2", 162, 270, false, 3, 0, 2, true, false, false, false, false),
            new CharLayout("Ct15-S3-0-R1", 162, 270, false, 3, 0, 1, true, false, false, false, false),
            new CharLayout("Ct21-S3-3-R3", 162, 270, false, 3, 3, 3, true, false, false, true, false),
            new CharLayout("Ct21-S3-1-R3", 162, 270, false, 3, 1, 3, true, false, false, true, false),
            new CharLayout("Ct21-S3-0-R3", 162, 270, false, 3, 0, 3, true, false, false, true, false),
            new CharLayout("Ct21-S3-0-R2", 162, 270, false, 3, 0, 2, true, false, false, true, false),
            new CharLayout("Ct21-S3-0-R1", 162, 270, false, 3, 0, 1, true, false, false, true, false),
            new CharLayout("Ct23-S3-3-R3", 360, 90, true, 3, 3, 3, true, false, true, true, true),
            new CharLayout("Ct23-S3-1-R3", 360, 90, true, 3, 1, 3, true, false, true, true, true),
            new CharLayout("Ct23-S3-0-R3", 360, 90, true, 3, 0, 3, true, false, true, true, true)
        ];
    }

    private static bool ShouldReadFinal(CharLayout layout, int entryIndex, int totalEntries)
    {
        if (!layout.HasFinalUInt32)
        {
            return false;
        }

        if (layout.SkipFinalOnLastEntry && entryIndex == totalEntries - 1)
        {
            return false;
        }

        return true;
    }

    private sealed record CharLayout(
        string Name,
        int ReservedBlock1Size,
        int ReservedBlock2Size,
        bool UseByteEquipmentTail,
        int Spacer1Length,
        int Spacer2Length,
        int ReserveTailGroups,
        bool HasFinalUInt32,
        bool SkipFinalOnLastEntry,
        bool HasSecondFinalUInt32,
        bool HasNameTail,
        bool HasExtraUnicodeTail)
    {
        public override string ToString() => Name;
    }
}
