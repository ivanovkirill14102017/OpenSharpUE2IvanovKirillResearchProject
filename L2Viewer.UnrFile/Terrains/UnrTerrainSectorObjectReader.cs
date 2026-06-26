namespace L2Viewer.UnrFile;

internal static class UnrTerrainSectorObjectReader
{
    private const int CompactByteGridCount = 8;
    private const int CompactByteGridSampleCount = 289;
    private const int CompactByteGridScanLimit = 128;
    private const int TrailingUInt16GridSampleCount = 289;

    public static UnrTerrainSectorObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        reader.ReadStateFrameIfPresent(export.ObjectFlags);

        var tag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
        if (!tag.IsTerminator)
        {
            throw new PackageReadException($"{className} export {exportIndex} ({objectName}) expected no tagged properties before native payload.");
        }

        var nativePayloadStart = reader.BaseStream.Position;
        var headerValue0 = reader.ReadInt32();
        var localSizeTimesTwo = reader.ReadInt32();
        var localOriginXTimesTwo = reader.ReadInt32();
        var localOriginYTimesTwo = reader.ReadInt32();
        var boundsPrefixByte = reader.ReadByte();
        var boundsMin = reader.ReadVector3();
        var boundsMax = reader.ReadVector3();
        var postBoundsValue0 = reader.ReadUInt16();
        var postBoundsValue1 = reader.ReadUInt16();
        var postBoundsValue2 = reader.ReadInt32();
        var postBoundsValue3 = reader.ReadUInt16();
        var compactByteGridStart = reader.BaseStream.Position;
        var compactByteGrids = TryReadCompactByteGrids(reader, out var compactByteGridOffset);
        var compactByteGridPrefixBytes = compactByteGridOffset == 0
            ? []
            : ReadPrefixBytes(reader, compactByteGridStart, compactByteGridOffset);
        var trailingUInt16Grid = TryReadTrailingUInt16Grid(reader);
        var remainingPayload = reader.ReadBytes(checked((int)(reader.BaseStream.Length - reader.BaseStream.Position)));
        var nativePayloadSize = checked((int)(reader.BaseStream.Length - nativePayloadStart));

        return new UnrTerrainSectorObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            HeaderValue0 = headerValue0,
            LocalSizeTimesTwo = localSizeTimesTwo,
            LocalOriginXTimesTwo = localOriginXTimesTwo,
            LocalOriginYTimesTwo = localOriginYTimesTwo,
            BoundsPrefixByte = boundsPrefixByte,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            PostBoundsValue0 = postBoundsValue0,
            PostBoundsValue1 = postBoundsValue1,
            PostBoundsValue2 = postBoundsValue2,
            PostBoundsValue3 = postBoundsValue3,
            CompactByteGridOffset = compactByteGridOffset,
            CompactByteGridPrefixBytes = compactByteGridPrefixBytes,
            CompactByteGrids = compactByteGrids,
            TrailingUInt16Grid = trailingUInt16Grid,
            RemainingPayload = remainingPayload,
            NativePayloadSize = nativePayloadSize,
            UnknownProperties = []
        };
    }

    private static UnrTerrainSectorCompactByteGrid[] TryReadCompactByteGrids(
        BinaryReader reader,
        out int compactByteGridOffset)
    {
        var start = reader.BaseStream.Position;
        for (var offset = 0; offset <= CompactByteGridScanLimit; offset++)
        {
            reader.BaseStream.Position = start + offset;
            if (TryReadCompactByteGridsAtCurrentOffset(reader, out var result))
            {
                compactByteGridOffset = offset;
                return result;
            }
        }

        reader.BaseStream.Position = start;
        compactByteGridOffset = 0;
        return [];
    }

    private static bool TryReadCompactByteGridsAtCurrentOffset(
        BinaryReader reader,
        out UnrTerrainSectorCompactByteGrid[] result)
    {
        var start = reader.BaseStream.Position;
        result = new UnrTerrainSectorCompactByteGrid[CompactByteGridCount];
        for (var i = 0; i < result.Length; i++)
        {
            int sampleCount;
            try
            {
                sampleCount = PackageReader.ReadCompactIndex(reader);
            }
            catch (PackageReadException)
            {
                reader.BaseStream.Position = start;
                result = [];
                return false;
            }

            if (sampleCount != CompactByteGridSampleCount)
            {
                reader.BaseStream.Position = start;
                result = [];
                return false;
            }

            var values = reader.ReadBytes(sampleCount);
            if (values.Length != sampleCount)
            {
                reader.BaseStream.Position = start;
                result = [];
                return false;
            }

            result[i] = new UnrTerrainSectorCompactByteGrid
            {
                ArrayIndex = i,
                SampleCount = sampleCount,
                Values = values
            };
        }

        return true;
    }

    private static byte[] ReadPrefixBytes(BinaryReader reader, long start, int length)
    {
        var end = reader.BaseStream.Position;
        reader.BaseStream.Position = start;
        var bytes = reader.ReadBytes(length);
        reader.BaseStream.Position = end;
        return bytes;
    }

    private static UnrTerrainSectorUInt16Grid? TryReadTrailingUInt16Grid(BinaryReader reader)
    {
        var remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
        if (remainingBytes != TrailingUInt16GridSampleCount * sizeof(ushort))
        {
            return null;
        }

        var values = new ushort[TrailingUInt16GridSampleCount];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.ReadUInt16();
        }

        return new UnrTerrainSectorUInt16Grid
        {
            SampleCount = values.Length,
            Values = values
        };
    }
}
