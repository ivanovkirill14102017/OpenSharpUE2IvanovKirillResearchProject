namespace L2Viewer.DatFile;

public sealed class WeaponGrpDatReader : DatSchemaReader<WeaponGrpDatDocument>
{
    public override string FileName => "weapongrp.dat";
    private const int MaxReasonableStringByteLength = 8192;
    private const int MaxTailScanBytes = 16384;

    public override WeaponGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<WeaponGrpDatEntry>(Math.Max(count, 0));
        var safePackageStart = GetSafePackageStart(decoded);

        for (var entryIndex = 0; entryIndex < count; entryIndex++)
        {
            var isDeclaredLastEntry = entryIndex == count - 1;
            try
            {
                entries.Add(ReadEntry(reader, isDeclaredLastEntry, safePackageStart));
            }
            catch (InvalidDataException) when (!isDeclaredLastEntry &&
                                               safePackageStart - reader.Position <= MaxTailScanBytes * 2 &&
                                               TryReadTrailingEntry(reader, safePackageStart, out var trailingEntry))
            {
                entries.Add(trailingEntry);
                break;
            }
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new WeaponGrpDatDocument(path, entries);
    }

    private static WeaponGrpDatEntry ReadEntry(DatBinaryReader reader, bool isLastEntry, int safePackageStart)
    {
        var entryStart = reader.Position;
        foreach (var variant in CoreVariants)
        {
            reader.Position = entryStart;
            if (!TryReadEntryCore(reader, variant, out var knownData, out var tailStart))
            {
                continue;
            }

            if (isLastEntry)
            {
                if (tailStart > safePackageStart)
                {
                    continue;
                }

                return FinalizeEntry(reader, knownData, tailStart, safePackageStart);
            }

            var nextEntryStart = FindNextEntryStart(reader, tailStart, safePackageStart, knownData.Id);
            if (nextEntryStart >= 0)
            {
                return FinalizeEntry(reader, knownData, tailStart, nextEntryStart);
            }
        }

        reader.Position = entryStart;
        throw new InvalidDataException($"Failed to read weapongrp.dat entry at offset {entryStart}.");
    }

    private static bool TryReadTrailingEntry(DatBinaryReader reader, int safePackageStart, out WeaponGrpDatEntry entry)
    {
        var start = reader.Position;
        try
        {
            foreach (var variant in CoreVariants)
            {
                reader.Position = start;
                if (TryReadEntryCore(reader, variant, out var knownData, out var tailStart) &&
                    tailStart <= safePackageStart)
                {
                    entry = FinalizeEntry(reader, knownData, tailStart, safePackageStart);
                    return true;
                }
            }
        }
        catch
        {
            // Ignore and fall through to restore the reader position.
        }

        reader.Position = start;
        entry = default!;
        return false;
    }

    private static bool TryReadEntryCore(
        DatBinaryReader reader,
        WeaponEntryVariant variant,
        out WeaponEntryKnownData entry,
        out int tailStart)
    {
        var start = reader.Position;

        try
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

            if (variant.HasCt23DropPrefix)
            {
                _ = reader.ReadUnicodeString32();
                _ = DatReaderPrimitives.ReadUInt32Array(reader, 8);
            }

            var icons = DatReaderPrimitives.ReadUnicodeArray(reader, 5);
            var durability = reader.ReadInt32();
            var weight = reader.ReadUInt32();
            var material = reader.ReadUInt32();
            var crystallizable = reader.ReadUInt32();
            var unknown1 = reader.ReadUInt32();
            var unknownTableCount = checked((int)reader.ReadUInt32());
            var unknownTable1 = DatReaderPrimitives.ReadUInt32Array(reader, unknownTableCount);
            var timeTab = reader.ReadUnicodeString32();
            var bodyPart = reader.ReadUInt32();
            var handness = reader.ReadUInt32();
            var weaponMeshCount = checked((int)reader.ReadUInt32());
            var weaponMeshes = DatReaderPrimitives.ReadUnicodeArray(reader, weaponMeshCount);

            if (variant.HasWeaponMeshUnknownValues)
            {
                _ = DatReaderPrimitives.ReadUInt32Array(reader, weaponMeshCount);
            }

            var weaponTextureCount = checked((int)reader.ReadUInt32());
            var weaponTextures = DatReaderPrimitives.ReadUnicodeArray(reader, weaponTextureCount);
            var itemSoundCount = checked((int)reader.ReadUInt32());
            var itemSounds = DatReaderPrimitives.ReadUnicodeArray(reader, itemSoundCount);
            var dropSound = reader.ReadUnicodeString32();
            var equipSound = reader.ReadUnicodeString32();
            var effect = reader.ReadUnicodeString32();
            var randomDamage = reader.ReadUInt32();
            var physicalAttack = reader.ReadUInt32();
            var magicalAttack = reader.ReadUInt32();
            var weaponType = reader.ReadUInt32();
            var crystalType = reader.ReadUInt32();
            var critical = reader.ReadUInt32();
            var hitModifier = reader.ReadInt32();
            var avoidModifier = reader.ReadInt32();
            var shieldPhysicalDefense = reader.ReadUInt32();
            var shieldRate = reader.ReadUInt32();
            var speed = reader.ReadUInt32();
            var mpConsume = reader.ReadUInt32();
            var soulShotCount = reader.ReadUInt32();
            var spiritShotCount = reader.ReadUInt32();
            var curvature = reader.ReadUInt32();
            var unknown3 = reader.ReadUInt32();
            var isHero = reader.ReadInt32();
            var unknown4 = reader.ReadUInt32();
            tailStart = reader.Position;
            entry = new WeaponEntryKnownData(
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
                unknownTable1,
                timeTab,
                bodyPart,
                handness,
                weaponMeshes,
                weaponTextures,
                itemSounds,
                dropSound,
                equipSound,
                effect,
                randomDamage,
                physicalAttack,
                magicalAttack,
                weaponType,
                crystalType,
                critical,
                hitModifier,
                avoidModifier,
                shieldPhysicalDefense,
                shieldRate,
                speed,
                mpConsume,
                soulShotCount,
                spiritShotCount,
                curvature,
                unknown3,
                isHero,
                unknown4);

            return true;
        }
        catch
        {
            reader.Position = start;
            entry = default!;
            tailStart = start;
            return false;
        }
    }

    private static WeaponGrpDatEntry FinalizeEntry(
        DatBinaryReader reader,
        WeaponEntryKnownData knownData,
        int tailStart,
        int nextEntryStart)
    {
        reader.Position = tailStart;
        var unknownTail = reader.ReadBytes(nextEntryStart - tailStart);
        reader.Position = nextEntryStart;

        return new WeaponGrpDatEntry(
            knownData.Tag,
            knownData.Id,
            knownData.DropType,
            knownData.DropAnimationType,
            knownData.DropRadius,
            knownData.DropHeight,
            knownData.Unknown0,
            knownData.DropMesh1,
            knownData.DropMesh2,
            knownData.DropMesh3,
            knownData.DropTexture1,
            knownData.DropTexture2,
            knownData.DropTexture3,
            knownData.Icons,
            knownData.Durability,
            knownData.Weight,
            knownData.Material,
            knownData.Crystallizable,
            knownData.Unknown1,
            knownData.UnknownTable1,
            knownData.TimeTab,
            knownData.BodyPart,
            knownData.Handness,
            knownData.WeaponMeshes,
            knownData.WeaponTextures,
            knownData.ItemSounds,
            knownData.DropSound,
            knownData.EquipSound,
            knownData.Effect,
            knownData.RandomDamage,
            knownData.PhysicalAttack,
            knownData.MagicalAttack,
            knownData.WeaponType,
            knownData.CrystalType,
            knownData.Critical,
            knownData.HitModifier,
            knownData.AvoidModifier,
            knownData.ShieldPhysicalDefense,
            knownData.ShieldRate,
            knownData.Speed,
            knownData.MpConsume,
            knownData.SoulShotCount,
            knownData.SpiritShotCount,
            knownData.Curvature,
            knownData.Unknown3,
            knownData.IsHero,
            knownData.Unknown4,
            unknownTail);
    }

    private static int FindNextEntryStart(DatBinaryReader reader, int start, int safePackageStart, uint currentId)
    {
        var scanLimit = Math.Min(safePackageStart, start + MaxTailScanBytes);
        for (var candidate = start; candidate <= scanLimit; candidate += 2)
        {
            if (LooksLikeNextEntryHeader(reader, candidate, safePackageStart, currentId) &&
                CanReadEntryCoreAt(reader, candidate))
            {
                return candidate;
            }
        }

        return -1;
    }

    private static bool LooksLikeNextEntryHeader(DatBinaryReader reader, int candidate, int safePackageStart, uint currentId)
    {
        var savedPosition = reader.Position;
        try
        {
            if (candidate < 0 || candidate + (sizeof(int) * 8) > safePackageStart)
            {
                return false;
            }

            reader.Position = candidate;
            var tag = reader.ReadUInt32();
            var id = reader.ReadUInt32();
            var dropType = reader.ReadUInt32();
            var dropAnimationType = reader.ReadUInt32();
            var dropRadius = reader.ReadUInt32();
            var dropHeight = reader.ReadUInt32();
            var unknown0 = reader.ReadUInt32();
            var firstStringLength = reader.ReadInt32();

            if (tag > 1)
            {
                return false;
            }

            if (id <= currentId || id - currentId > 10000)
            {
                return false;
            }

            if (dropType > 64 || dropAnimationType > 64 || dropRadius > 4096 || dropHeight > 4096 || unknown0 > 1024)
            {
                return false;
            }

            if (firstStringLength <= 0 ||
                (firstStringLength & 1) != 0 ||
                firstStringLength > MaxReasonableStringByteLength ||
                firstStringLength > safePackageStart - reader.Position)
            {
                return false;
            }

            reader.Position += firstStringLength;
            return TrySkipUnicodeArray(reader, 5);
        }
        finally
        {
            reader.Position = savedPosition;
        }
    }

    private static bool CanReadEntryCoreAt(DatBinaryReader reader, int candidate)
    {
        var savedPosition = reader.Position;
        try
        {
            foreach (var variant in CoreVariants)
            {
                reader.Position = candidate;
                if (TryReadEntryCore(reader, variant, out _, out _))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            reader.Position = savedPosition;
        }
    }

    private static bool TrySkipUnicodeArray(DatBinaryReader reader, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (!TrySkipUnicodeString(reader))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TrySkipUnicodeString(DatBinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length < 0 || (length & 1) != 0 || length > MaxReasonableStringByteLength || length > reader.Remaining)
        {
            return false;
        }

        reader.Position += length;
        return true;
    }

    private static int GetSafePackageStart(byte[] decoded)
    {
        if (decoded.Length >= 13 &&
            decoded[^13] == 0x0C &&
            decoded[^12] == (byte)'S' &&
            decoded[^11] == (byte)'a' &&
            decoded[^10] == (byte)'f' &&
            decoded[^9] == (byte)'e' &&
            decoded[^8] == (byte)'P' &&
            decoded[^7] == (byte)'a' &&
            decoded[^6] == (byte)'c' &&
            decoded[^5] == (byte)'k' &&
            decoded[^4] == (byte)'a' &&
            decoded[^3] == (byte)'g' &&
            decoded[^2] == (byte)'e' &&
            decoded[^1] == 0)
        {
            return decoded.Length - 13;
        }

        return decoded.Length;
    }

    private static readonly WeaponEntryVariant[] CoreVariants =
    [
        new(false, false),
        new(false, true),
        new(true, false),
        new(true, true)
    ];

    private sealed record WeaponEntryVariant(bool HasCt23DropPrefix, bool HasWeaponMeshUnknownValues);

    private sealed record WeaponEntryKnownData(
        uint Tag,
        uint Id,
        uint DropType,
        uint DropAnimationType,
        uint DropRadius,
        uint DropHeight,
        uint Unknown0,
        string DropMesh1,
        string DropMesh2,
        string DropMesh3,
        string DropTexture1,
        string DropTexture2,
        string DropTexture3,
        IReadOnlyList<string> Icons,
        int Durability,
        uint Weight,
        uint Material,
        uint Crystallizable,
        uint Unknown1,
        IReadOnlyList<uint> UnknownTable1,
        string TimeTab,
        uint BodyPart,
        uint Handness,
        IReadOnlyList<string> WeaponMeshes,
        IReadOnlyList<string> WeaponTextures,
        IReadOnlyList<string> ItemSounds,
        string DropSound,
        string EquipSound,
        string Effect,
        uint RandomDamage,
        uint PhysicalAttack,
        uint MagicalAttack,
        uint WeaponType,
        uint CrystalType,
        uint Critical,
        int HitModifier,
        int AvoidModifier,
        uint ShieldPhysicalDefense,
        uint ShieldRate,
        uint Speed,
        uint MpConsume,
        uint SoulShotCount,
        uint SpiritShotCount,
        uint Curvature,
        uint Unknown3,
        int IsHero,
        uint Unknown4);
}
