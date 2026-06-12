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

        return new UnrLevelObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            ModelReference = modelReference,
            BrushReference = brushReference,
            NativeTailSize = (int)(reader.BaseStream.Length - reader.BaseStream.Position)
        };
    }

    private enum UnrLevelPropertyKind
    {
        Model,
        Brush
    }
}
