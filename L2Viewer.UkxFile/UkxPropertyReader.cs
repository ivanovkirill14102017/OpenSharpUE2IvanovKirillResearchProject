using L2Viewer.PackageCore;
using System.Text;

namespace L2Viewer.UkxFile;

internal static class UkxPropertyReader
{
    internal static UkxUnknownProperty[] ReadUnknownProperties(
        PackageData package,
        BinaryReader reader,
        string className,
        int exportIndex,
        string objectName)
    {
        var properties = new List<UkxUnknownProperty>();
        while (true)
        {
            if (!PackageReader.TryReadPropertyTag(reader, package.Names, out var tag))
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid property tag.");
            }

            if (tag.IsEnd)
            {
                return properties.ToArray();
            }

            properties.Add(ReadUnknownProperty(package, reader, tag, className, exportIndex, objectName, properties.Count));
        }
    }

    private static UkxUnknownProperty ReadUnknownProperty(
        PackageData package,
        BinaryReader reader,
        UnrealPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        int order)
    {
        if (tag.Type == PackageReader.PropertyTypeBool && tag.DataSize == 0)
        {
            return new UkxUnknownProperty
            {
                Order = order,
                Name = tag.Name,
                Type = tag.Type,
                DataSize = tag.DataSize,
                StructName = tag.StructName,
                BoolValue = tag.BoolValue
            };
        }

        var raw = ReadPropertyPayloadStrict(reader, tag, className, exportIndex, objectName, package.Names);
        return new UkxUnknownProperty
        {
            Order = order,
            Name = tag.Name,
            Type = tag.Type,
            DataSize = tag.DataSize,
            StructName = tag.StructName,
            BoolValue = tag.BoolValue,
            ByteValue = TryDecodeByte(tag, raw),
            IntValue = TryDecodeInt(tag, raw),
            FloatValue = TryDecodeFloat(tag, raw),
            NameValue = TryDecodeName(tag, raw, package.Names),
            StringValue = TryDecodeString(tag, raw),
            ObjectReference = TryDecodeObjectReference(tag, raw, package),
            RawHex = ToHexString(raw)
        };
    }

    internal static byte[] ReadPropertyPayloadStrict(
        BinaryReader reader,
        UnrealPropertyTag tag,
        string className,
        int exportIndex,
        string objectName,
        IReadOnlyList<string> names)
    {
        if (tag.DataSize < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid payload size in '{tag.Name}'.");
        }

        var start = reader.BaseStream.Position;
        if (tag.Type == PackageReader.PropertyTypeStruct && IsTaggedStruct(tag.StructName))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                if (!PackageReader.TryReadPropertyTag(reader, names, out var nestedTag))
                {
                    throw new PackageReadException(
                        $"{className} export {exportIndex} ({objectName}) has invalid nested property in '{tag.Name}'.");
                }

                if (nestedTag.IsEnd)
                {
                    break;
                }

                _ = ReadPropertyPayloadStrict(reader, nestedTag, className, exportIndex, objectName, names);
            }

            var end = reader.BaseStream.Position;
            var actualSize = checked((int)(end - start));
            reader.BaseStream.Position = start;
            var actual = reader.ReadBytes(actualSize);
            if (actual.Length != actualSize)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) hit EOF while reading struct '{tag.Name}'.");
            }

            return actual;
        }

        var bytes = reader.ReadBytes(tag.DataSize);
        if (bytes.Length != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) hit EOF while reading '{tag.Name}'.");
        }

        return bytes;
    }

    private static bool IsTaggedStruct(string? structName)
    {
        return !string.IsNullOrWhiteSpace(structName) &&
               !string.Equals(structName, "Vector", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(structName, "Rotator", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(structName, "Color", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(structName, "Scale", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(structName, "Plane", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(structName, "Quat", StringComparison.OrdinalIgnoreCase);
    }

    private static byte? TryDecodeByte(UnrealPropertyTag tag, byte[] raw)
    {
        return tag.Type == PackageReader.PropertyTypeByte && raw.Length == 1 ? raw[0] : null;
    }

    private static int? TryDecodeInt(UnrealPropertyTag tag, byte[] raw)
    {
        return tag.Type == PackageReader.PropertyTypeInt && raw.Length == 4
            ? BitConverter.ToInt32(raw, 0)
            : null;
    }

    private static float? TryDecodeFloat(UnrealPropertyTag tag, byte[] raw)
    {
        return tag.Type == PackageReader.PropertyTypeFloat && raw.Length == 4
            ? BitConverter.ToSingle(raw, 0)
            : null;
    }

    private static string? TryDecodeName(UnrealPropertyTag tag, byte[] raw, IReadOnlyList<string> names)
    {
        if (tag.Type != PackageReader.PropertyTypeName)
        {
            return null;
        }

        var pos = 0;
        if (!PackageReader.TryReadCompactIndex(raw, ref pos, out var nameIndex))
        {
            return null;
        }

        return nameIndex >= 0 && nameIndex < names.Count ? names[nameIndex] : null;
    }

    private static string? TryDecodeString(UnrealPropertyTag tag, byte[] raw)
    {
        if (tag.Type != PackageReader.PropertyTypeString)
        {
            return null;
        }

        var pos = 0;
        if (!PackageReader.TryReadCompactIndex(raw, ref pos, out var length))
        {
            return null;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        if (length > 0)
        {
            var count = length;
            if (pos + count > raw.Length)
            {
                return null;
            }

            var bytes = raw.AsSpan(pos, count).ToArray();
            if (bytes.Length > 0 && bytes[^1] == 0)
            {
                bytes = bytes[..^1];
            }

            return PackageCompat.Latin1.GetString(bytes);
        }

        var charCount = -length;
        var byteCount = charCount * 2;
        if (pos + byteCount > raw.Length)
        {
            return null;
        }

        var utf16 = raw.AsSpan(pos, byteCount).ToArray();
        if (utf16.Length >= 2 && utf16[^1] == 0 && utf16[^2] == 0)
        {
            utf16 = utf16[..^2];
        }

        return Encoding.Unicode.GetString(utf16);
    }

    private static UkxObjectReference? TryDecodeObjectReference(UnrealPropertyTag tag, byte[] raw, PackageData package)
    {
        if (tag.Type != PackageReader.PropertyTypeObject)
        {
            return null;
        }

        var pos = 0;
        if (!PackageReader.TryReadCompactIndex(raw, ref pos, out var rawRef))
        {
            return null;
        }

        if (rawRef == 0)
        {
            return null;
        }

        var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawRef);
        if (resolved is null)
        {
            return new UkxObjectReference
            {
                RawReference = rawRef,
                ClassName = "<unknown>",
                ObjectName = "<unknown>"
            };
        }

        return new UkxObjectReference
        {
            RawReference = rawRef,
            ClassName = resolved.Value.ClassName,
            ObjectName = resolved.Value.ObjectName,
            PackageName = resolved.Value.PackageName,
            ImportIndex = rawRef < 0 ? -rawRef - 1 : null,
            ExportIndex = rawRef > 0 ? rawRef - 1 : null
        };
    }

    private static string ToHexString(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
        {
            builder.Append(value.ToString("X2"));
        }

        return builder.ToString();
    }
}
