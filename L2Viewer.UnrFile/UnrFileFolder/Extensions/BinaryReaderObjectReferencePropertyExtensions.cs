namespace L2Viewer.UnrFile;

internal static class BinaryReaderObjectReferencePropertyExtensions
{
    internal static UnrFileObjectReference? ReadObjectReferenceProperty(
        this BinaryReader reader,
        PackageData package,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        var reference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
        if (reference is not null)
        {
            return reference;
        }

        throw new PackageReadException(
            $"{className} export {exportIndex} ({objectName}) has null object reference in '{tag.Name}'.");
    }

    internal static UnrFileObjectReference? ReadOptionalObjectReferenceProperty(
        this BinaryReader reader,
        PackageData package,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeObject)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has field '{tag.Name}' but its type is not Object.");
        }

        var start = reader.BaseStream.Position;
        var rawReference = PackageReader.ReadCompactIndex(reader);
        var consumed = reader.BaseStream.Position - start;
        if (consumed != tag.DataSize)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid object reference size in '{tag.Name}': consumed={consumed}, expected={tag.DataSize}.");
        }

        if (rawReference == 0)
        {
            return null;
        }

        var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference);
        if (resolved is null)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) points to unresolved object reference {rawReference} in '{tag.Name}'.");
        }

        return rawReference < 0
            ? new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Import,
                ImportIndex = -rawReference - 1,
                ExportIndex = null,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            }
            : new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Export,
                ImportIndex = null,
                ExportIndex = rawReference - 1,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            };
    }

    internal static UnrFileObjectReference[] ReadObjectReferenceArrayProperty(
        this BinaryReader reader,
        PackageData package,
        StreamPropertyTag tag,
        string className,
        int exportIndex,
        string objectName)
    {
        if (tag.Type != PackageReader.PropertyTypeArray || tag.DataSize < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid object array payload in '{tag.Name}'.");
        }

        var payloadStart = reader.BaseStream.Position;
        var count = PackageReader.ReadCompactIndex(reader);
        if (count < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has invalid negative object array count in '{tag.Name}'.");
        }

        var end = payloadStart + tag.DataSize;
        var values = new List<UnrFileObjectReference>();
        for (var i = 0; i < count; i++)
        {
            var rawReference = PackageReader.ReadCompactIndex(reader);
            if (rawReference == 0)
            {
                continue;
            }

            var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference)
                ?? throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) points to unresolved object array reference {rawReference} in '{tag.Name}'.");

            values.Add(rawReference < 0
                ? new UnrFileObjectReference
                {
                    RawReference = rawReference,
                    Kind = UnrFileReferenceKind.Import,
                    ImportIndex = -rawReference - 1,
                    ExportIndex = null,
                    ClassName = resolved.ClassName,
                    ObjectName = resolved.ObjectName,
                    PackageName = resolved.PackageName
                }
                : new UnrFileObjectReference
                {
                    RawReference = rawReference,
                    Kind = UnrFileReferenceKind.Export,
                    ImportIndex = null,
                    ExportIndex = rawReference - 1,
                    ClassName = resolved.ClassName,
                    ObjectName = resolved.ObjectName,
                    PackageName = resolved.PackageName
                });
        }

        if (reader.BaseStream.Position != end)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has malformed object array payload in '{tag.Name}'.");
        }

        return values.ToArray();
    }
}
