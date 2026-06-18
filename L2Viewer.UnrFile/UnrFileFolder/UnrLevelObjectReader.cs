using System.Text;

namespace L2Viewer.UnrFile;

internal static class UnrLevelObjectReader
{
    public static UnrLevelObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        UnrFileObjectReference? modelReference = null;
        UnrFileObjectReference? brushReference = null;
        string? urlProtocol = null;
        string? urlHost = null;
        string? urlMap = null;
        string? urlPortal = null;
        int? urlPort = null;
        bool? urlValid = null;

        void ReadProperty(StreamPropertyTag tag)
        {
            if (tag.IsFullyEncodedInTag())
            {
                return;
            }

            if (!Enum.TryParse<UnrLevelPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
                return;
            }

            switch (kind)
            {
                case UnrLevelPropertyKind.Model:
                    modelReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrLevelPropertyKind.Brush:
                    brushReference = reader.ReadObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                default:
                    reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names);
                    return;
            }
        }

        reader.ReadStateFrameIfPresent(export.ObjectFlags);
        var hasTerminatingNone = false;

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (tag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            if (tag.DataSize < 0)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid negative property size for '{tag.Name}'.");
            }

            var payloadStart = reader.BaseStream.Position;
            ReadProperty(tag);
            var consumed = reader.BaseStream.Position - payloadStart;
            tag.EnsurePropertyFullyConsumed(consumed, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has no terminating None property before EOF.");
        }

        var nativeTail = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        if (nativeTail.Length > 0)
        {
            if (TryReadNativeUrlTail(package, nativeTail, out var nativeUrl))
            {
                urlProtocol = nativeUrl.Protocol;
                urlHost = nativeUrl.Host;
                urlMap = nativeUrl.Map;
                urlPortal = nativeUrl.Portal;
                urlPort = nativeUrl.Port;
                urlValid = nativeUrl.Valid;

                if (modelReference is null && nativeUrl.ModelReference is not null)
                {
                    modelReference = nativeUrl.ModelReference;
                }
            }
        }

        return new UnrLevelObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            ModelReference = modelReference,
            BrushReference = brushReference,
            NativeTailSize = nativeTail.Length,
            UrlProtocol = urlProtocol,
            UrlHost = urlHost,
            UrlMap = urlMap,
            UrlPortal = urlPortal,
            UrlPort = urlPort,
            UrlValid = urlValid
        };
    }

    private static bool TryReadNativeUrlTail(PackageData package, ReadOnlySpan<byte> tail, out NativeLevelUrlTail result)
    {
        result = default;
        var mapFileName = Path.GetFileName(package.Path);
        var searchStart = Math.Max(0, tail.Length - 256);
        NativeLevelUrlTail? best = null;

        for (var offset = searchStart; offset < tail.Length; offset++)
        {
            var pos = offset;
            if (!TryReadSerializedString(tail, ref pos, out var protocol) ||
                !string.Equals(protocol, "unreal", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryReadSerializedString(tail, ref pos, out var host) ||
                !TryReadSerializedString(tail, ref pos, out var map) ||
                !TryReadSerializedString(tail, ref pos, out var portal))
            {
                continue;
            }

            if (!PackageReader.TryReadCompactIndex(tail, ref pos, out var optionCount) || optionCount < 0)
            {
                continue;
            }

            var optionsOk = true;
            for (var i = 0; i < optionCount; i++)
            {
                if (!TryReadSerializedString(tail, ref pos, out _))
                {
                    optionsOk = false;
                    break;
                }
            }

            if (!optionsOk)
            {
                continue;
            }

            if (!map.EndsWith(".unr", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(map, mapFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pos + 8 > tail.Length)
            {
                continue;
            }

            var port = BitConverter.ToInt32(tail.Slice(pos, 4));
            pos += 4;
            var validInt = BitConverter.ToInt32(tail.Slice(pos, 4));
            pos += 4;
            if (port is < 0 or > 65535 || validInt is < 0 or > 1)
            {
                continue;
            }

            if (!PackageReader.TryReadCompactIndex(tail, ref pos, out var modelRawReference) || modelRawReference == 0)
            {
                continue;
            }

            var resolvedModel = UnrPackageHelpers.ResolveObjectRef(package, modelRawReference);
            if (resolvedModel is null || !string.Equals(resolvedModel.Value.ClassName, "Model", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var modelReference = modelRawReference < 0
                ? new UnrFileObjectReference
                {
                    RawReference = modelRawReference,
                    Kind = UnrFileReferenceKind.Import,
                    ImportIndex = -modelRawReference - 1,
                    ExportIndex = null,
                    ClassName = resolvedModel.Value.ClassName,
                    ObjectName = resolvedModel.Value.ObjectName,
                    PackageName = resolvedModel.Value.PackageName
                }
                : new UnrFileObjectReference
                {
                    RawReference = modelRawReference,
                    Kind = UnrFileReferenceKind.Export,
                    ImportIndex = null,
                    ExportIndex = modelRawReference - 1,
                    ClassName = resolvedModel.Value.ClassName,
                    ObjectName = resolvedModel.Value.ObjectName,
                    PackageName = resolvedModel.Value.PackageName
                };

            var candidate = new NativeLevelUrlTail(
                offset,
                protocol,
                host,
                map,
                portal,
                port,
                validInt != 0,
                modelReference);

            if (best is null || candidate.UrlOffset > best.Value.UrlOffset)
            {
                best = candidate;
            }
        }

        if (best is null)
        {
            return false;
        }

        result = best.Value;
        return true;
    }

    private static bool TryReadSerializedString(ReadOnlySpan<byte> buffer, ref int position, out string value)
    {
        value = string.Empty;
        if (!PackageReader.TryReadCompactIndex(buffer, ref position, out var length))
        {
            return false;
        }

        if (length == 0)
        {
            return true;
        }

        if (length > 0)
        {
            if (position + length > buffer.Length)
            {
                return false;
            }

            var raw = buffer.Slice(position, length).ToArray();
            position += length;
            if (raw.Length > 0 && raw[^1] == 0)
            {
                raw = raw[..^1];
            }

            value = PackageCompat.Latin1.GetString(raw);
            return true;
        }

        var charCount = -length;
        var byteCount = checked(charCount * 2);
        if (position + byteCount > buffer.Length)
        {
            return false;
        }

        var utf16 = buffer.Slice(position, byteCount).ToArray();
        position += byteCount;
        if (utf16.Length >= 2 && utf16[^2] == 0 && utf16[^1] == 0)
        {
            utf16 = utf16[..^2];
        }

        value = Encoding.Unicode.GetString(utf16);
        return true;
    }

    private enum UnrLevelPropertyKind
    {
        Model,
        Brush
    }

    private readonly record struct NativeLevelUrlTail(
        int UrlOffset,
        string Protocol,
        string Host,
        string Map,
        string Portal,
        int Port,
        bool Valid,
        UnrFileObjectReference ModelReference);
}
