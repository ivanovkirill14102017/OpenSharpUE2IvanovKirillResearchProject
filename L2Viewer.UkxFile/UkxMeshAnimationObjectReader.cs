using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.UkxFile;

internal static class UkxMeshAnimationObjectReader
{
    internal static UkxMeshAnimationObject Read(
        PackageData package,
        ExportEntry exportEntry,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, exportEntry);
        UkxExportReaderHelpers.SkipObjectStackIfPresent(reader, exportEntry);
        var unknownProperties = UkxPropertyReader.ReadUnknownProperties(package, reader, className, exportIndex, objectName);
        var version = package.Header.Version >= 123 ? reader.ReadInt32() : 0;
        var refBones = ReadArray(reader, x => ReadNamedBone(x, package));
        var (roughArrayEndPosition, moves) = ReadMoves(reader, package);
        var animSequences = ReadArray(reader, x => ReadMeshAnimSequence(x, package));
        var tail = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

        return new UkxMeshAnimationObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            UnknownProperties = unknownProperties,
            Version = version,
            RefBones = refBones,
            RoughArrayEndPosition = roughArrayEndPosition,
            Moves = moves,
            AnimSequences = animSequences,
            UnknownNativePayloadBytes = tail
        };
    }

    private static (int? RoughArrayEndPosition, UkxMotionChunk[] Moves) ReadMoves(
        BinaryReader reader,
        PackageData package)
    {
        if (package.Header.Version < 123 || package.Header.LicenseeVersion < 0x19)
        {
            return (null, ReadArray(reader, x => ReadMotionChunk(x)));
        }

        var finalPosition = reader.ReadInt32();
        var count = PackageReader.ReadCompactIndex(reader);
        EnsureReasonableCount(count, "Lineage rough MotionChunk");
        var result = new UkxMotionChunk[count];
        for (var i = 0; i < count; i++)
        {
            var itemEndPosition = reader.ReadInt32();
            result[i] = ReadMotionChunk(reader, itemEndPosition);
        }

        return (finalPosition, result);
    }

    private static UkxNamedBone ReadNamedBone(BinaryReader reader, PackageData package)
    {
        return new UkxNamedBone(
            ReadName(reader, package.Names),
            unchecked((uint)reader.ReadInt32()),
            reader.ReadInt32());
    }

    private static UkxMotionChunk ReadMotionChunk(BinaryReader reader, int? roughArrayItemEndPosition = null)
    {
        return new UkxMotionChunk(
            roughArrayItemEndPosition,
            ReadVector3(reader),
            reader.ReadSingle(),
            reader.ReadInt32(),
            unchecked((uint)reader.ReadInt32()),
            ReadArray(reader, x => x.ReadInt32()),
            ReadArray(reader, ReadAnalogTrack),
            ReadAnalogTrack(reader));
    }

    private static UkxAnalogTrack ReadAnalogTrack(BinaryReader reader)
    {
        return new UkxAnalogTrack(
            unchecked((uint)reader.ReadInt32()),
            ReadArray(reader, ReadQuaternion),
            ReadArray(reader, ReadVector3),
            ReadArray(reader, x => x.ReadSingle()));
    }

    private static UkxMeshAnimSequence ReadMeshAnimSequence(BinaryReader reader, PackageData package)
    {
        float? value28 = package.Header.Version >= 115 ? reader.ReadSingle() : null;
        var name = ReadName(reader, package.Names);
        var groups = ReadArray(reader, x => ReadName(x, package.Names));
        var startFrame = reader.ReadInt32();
        var numFrames = reader.ReadInt32();
        var notifies = ReadArray(reader, x => ReadMeshAnimNotify(x, package));
        var rate = reader.ReadSingle();

        UkxMeshAnimSequenceLineageData? lineageData = null;
        if (package.Header.LicenseeVersion >= 1)
        {
            var value2C = reader.ReadInt32();
            var value30 = reader.ReadInt32();
            int? value34 = package.Header.LicenseeVersion >= 2 ? reader.ReadInt32() : null;
            var objectReference38 = UkxObjectReferenceDecoder.ReadObjectReference(reader, package);
            int? value3C = package.Header.LicenseeVersion >= 0x14 ? reader.ReadInt32() : null;
            int? value40 = package.Header.LicenseeVersion >= 0x19 ? reader.ReadInt32() : null;
            var unknown44Bytes = package.Header.LicenseeVersion >= 0x1A
                ? ReadLineageUnknown44(reader, package.Header.LicenseeVersion)
                : [];
            lineageData = new UkxMeshAnimSequenceLineageData(
                value2C,
                value30,
                value34,
                objectReference38,
                value3C,
                value40,
                unknown44Bytes);
        }

        return new UkxMeshAnimSequence(
            name,
            groups,
            startFrame,
            numFrames,
            rate,
            notifies,
            value28,
            lineageData);
    }

    private static UkxMeshAnimNotify ReadMeshAnimNotify(BinaryReader reader, PackageData package)
    {
        var time = reader.ReadSingle();
        var functionName = ReadName(reader, package.Names);
        var notifyObjectReference = package.Header.Version >= 112
            ? UkxObjectReferenceDecoder.ReadObjectReference(reader, package)
            : null;
        string? lineageExtraText = null;
        if (package.Header.Version >= 131)
        {
            var charCount = reader.ReadInt32();
            if (charCount < 0 || charCount > 4096)
            {
                throw new PackageReadException($"Invalid Lineage2 notify string length: {charCount}");
            }

            if (charCount > 0)
            {
                var bytes = reader.ReadBytes(charCount * 2);
                if (bytes.Length != charCount * 2)
                {
                    throw new PackageReadException("Unexpected EOF while reading Lineage2 notify string.");
                }

                if (bytes.Length >= 2 && bytes[^2] == 0 && bytes[^1] == 0)
                {
                    bytes = bytes[..^2];
                }

                lineageExtraText = System.Text.Encoding.Unicode.GetString(bytes);
            }
            else
            {
                lineageExtraText = string.Empty;
            }
        }

        return new UkxMeshAnimNotify(time, functionName, notifyObjectReference, lineageExtraText);
    }

    private static byte[] ReadLineageUnknown44(BinaryReader reader, ushort licenseeVersion)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        if (licenseeVersion == 0x1A)
        {
            WriteIntPairArray(reader, writer);
            return ms.ToArray();
        }

        if (licenseeVersion >= 0x1B)
        {
            writer.Write(reader.ReadByte());
            WriteIntPairArray(reader, writer);
            var count10 = PackageReader.ReadCompactIndex(reader);
            EnsureReasonableCount(count10, "Lineage sequence unk10");
            WriteCompactIndex(writer, count10);
            for (var i = 0; i < count10; i++)
            {
                writer.Write(reader.ReadInt32());
                WriteIntPairArray(reader, writer);
            }

            writer.Write(reader.ReadInt32());
            writer.Write(reader.ReadInt32());
            WriteIntPairArray(reader, writer);
            return ms.ToArray();
        }

        return [];
    }

    private static void WriteIntPairArray(BinaryReader reader, BinaryWriter writer)
    {
        var count = PackageReader.ReadCompactIndex(reader);
        EnsureReasonableCount(count, "Lineage int-pair array");
        WriteCompactIndex(writer, count);
        for (var i = 0; i < count; i++)
        {
            writer.Write(reader.ReadInt32());
            writer.Write(reader.ReadInt32());
        }
    }

    private static void WriteCompactIndex(BinaryWriter writer, int value)
    {
        var negative = value < 0;
        var v = negative ? -value : value;
        var first = (byte)(v & 0x3F);
        if (negative)
        {
            first |= 0x80;
        }

        v >>= 6;
        if (v != 0)
        {
            first |= 0x40;
        }

        writer.Write(first);
        while (v != 0)
        {
            var next = (byte)(v & 0x7F);
            v >>= 7;
            if (v != 0)
            {
                next |= 0x80;
            }

            writer.Write(next);
        }
    }

    private static T[] ReadArray<T>(BinaryReader reader, Func<BinaryReader, T> itemReader)
    {
        var count = PackageReader.ReadCompactIndex(reader);
        EnsureReasonableCount(count, typeof(T).Name);
        var result = new T[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = itemReader(reader);
        }

        return result;
    }

    private static void EnsureReasonableCount(int count, string name)
    {
        if (count < 0 || count > 1_000_000)
        {
            throw new PackageReadException($"Invalid {name} count: {count}");
        }
    }

    private static string ReadName(BinaryReader reader, IReadOnlyList<string> names)
    {
        var index = PackageReader.ReadCompactIndex(reader);
        if (index < 0 || index >= names.Count)
        {
            throw new PackageReadException($"Invalid name index: {index}");
        }

        return names[index];
    }

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static Quaternion ReadQuaternion(BinaryReader reader)
    {
        return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
