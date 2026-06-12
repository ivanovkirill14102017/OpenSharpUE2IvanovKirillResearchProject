using System.Buffers.Binary;
using L2Viewer.PackageCore;

namespace L2Viewer.UtxFile;

public static class MaterialGraphResolver
{
    public static ResolvedMaterialGraph? ResolveMaterialGraph(
        string packagePath,
        string objectName,
        Func<string, string, TextureData?>? textureResolver = null,
        IReadOnlyDictionary<string, string>? packageIndex = null)
    {
        var package = PackageReader.LoadPackage(packagePath);
        var packageName = Path.GetFileNameWithoutExtension(package.Path);
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var exportEntry = package.Exports[i];
            if (!string.Equals(PackageReader.SafeName(package.Names, exportEntry.ObjectName), objectName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var reference = new UnrResolvedObjectRef(
                PackageReader.ExportClassName(package, exportEntry),
                objectName,
                packageName,
                i,
                package.Path);
            return ResolveMaterialGraph(package, reference, textureResolver, packageIndex);
        }

        return null;
    }

    public static ResolvedMaterialGraph? ResolveMaterialGraph(
        PackageData rootPackage,
        UnrResolvedObjectRef rootRef,
        Func<string, string, TextureData?>? textureResolver,
        IReadOnlyDictionary<string, string>? packageIndex)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodes = new List<MaterialGraphNode>();
        BuildMaterialGraphNodes(rootPackage, rootRef, textureResolver, packageIndex, visited, nodes, depth: 0);
        if (nodes.Count == 0)
        {
            return null;
        }

        var allTextureSlots = nodes
            .SelectMany(x => x.TextureSlots)
            .GroupBy(x => $"{x.SlotName}|{x.Reference}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();
        var orderedTextureSlots = MaterialTextureSlotOrdering.OrderTextureSlots(allTextureSlots);

        return new ResolvedMaterialGraph(
            $"{rootRef.PackageName}.{rootRef.ObjectName}",
            rootRef.ClassName,
            rootRef.ObjectName,
            rootRef.PackageName,
            nodes,
            orderedTextureSlots);
    }

    private static void BuildMaterialGraphNodes(
        PackageData rootPackage,
        UnrResolvedObjectRef currentRef,
        Func<string, string, TextureData?>? textureResolver,
        IReadOnlyDictionary<string, string>? packageIndex,
        HashSet<string> visited,
        List<MaterialGraphNode> nodes,
        int depth)
    {
        if (depth > 12)
        {
            return;
        }

        var visitKey = $"{currentRef.PackageName}.{currentRef.ObjectName}";
        if (!visited.Add(visitKey))
        {
            return;
        }

        if (!TryLoadResolvedObject(rootPackage, currentRef, packageIndex, out var package, out var exportIndex))
        {
            nodes.Add(new MaterialGraphNode(
                visitKey,
                currentRef.ClassName,
                currentRef.ObjectName,
                currentRef.PackageName,
                currentRef.PackagePath,
                Array.Empty<MaterialTextureSlot>(),
                Array.Empty<MaterialObjectSlot>(),
                Array.Empty<MaterialBooleanParameter>(),
                Array.Empty<MaterialNumericParameter>(),
                Array.Empty<MaterialTextParameter>()));
            return;
        }

        var exportEntry = package.Exports[exportIndex];
        var blob = PackageReader.ReadExportBlob(package, exportEntry);
        var textureSlots = new List<MaterialTextureSlot>();
        var objectSlots = new List<MaterialObjectSlot>();
        var bools = new List<MaterialBooleanParameter>();
        var numerics = new List<MaterialNumericParameter>();
        var texts = new List<MaterialTextParameter>();
        var currentClassName = PackageReader.ExportClassName(package, exportEntry);

        if (currentClassName.Is(UnrealClassNames.Texture) ||
            currentClassName.EndsWith(UnrealClassNames.Texture, StringComparison.OrdinalIgnoreCase))
        {
            var currentObjectName = PackageReader.SafeName(package.Names, exportEntry.ObjectName);
            var currentPackageName = Path.GetFileNameWithoutExtension(package.Path);
            var currentResolvedRef = new UnrResolvedObjectRef(
                currentClassName,
                currentObjectName,
                currentPackageName,
                exportIndex,
                package.Path);
            textureSlots.Add(new MaterialTextureSlot(
                "Texture",
                $"{currentPackageName}.{currentObjectName}",
                currentClassName,
                currentObjectName,
                currentPackageName,
                package.Path,
                TryLoadTexture(rootPackage, currentResolvedRef, textureResolver, packageIndex)));
        }

        var pos = 0;
        SkipObjectStackPrefixIfPresent(exportEntry, blob, ref pos);
        for (var i = 0; i < 256; i++)
        {
            if (!PackageReader.TryReadPropertyTag(blob, package.Names, ref pos, out var tag))
            {
                break;
            }

            if (tag.IsEnd)
            {
                break;
            }

            if (pos + tag.DataSize > blob.Length)
            {
                break;
            }

            var payload = blob.AsSpan(pos, tag.DataSize).ToArray();
            pos += tag.DataSize;

            switch (tag.Type)
            {
                case PackageReader.PropertyTypeBool:
                    bools.Add(new MaterialBooleanParameter(tag.Name, tag.BoolValue));
                    break;
                case PackageReader.PropertyTypeByte:
                    if (payload.Length > 0)
                    {
                        numerics.Add(new MaterialNumericParameter(tag.Name, payload[0], "Byte"));
                    }
                    break;
                case PackageReader.PropertyTypeInt:
                    if (payload.Length >= 4)
                    {
                        numerics.Add(new MaterialNumericParameter(tag.Name, BinaryPrimitives.ReadInt32LittleEndian(payload), "Int32"));
                    }
                    break;
                case PackageReader.PropertyTypeFloat:
                    if (payload.Length >= 4)
                    {
                        numerics.Add(new MaterialNumericParameter(tag.Name, BitConverter.ToSingle(payload, 0), "Single"));
                    }
                    break;
                case PackageReader.PropertyTypeName:
                    if (TryDecodeNameProperty(package, payload, out var nameText))
                    {
                        texts.Add(new MaterialTextParameter(tag.Name, nameText, "Name"));
                    }
                    break;
                case PackageReader.PropertyTypeString:
                    texts.Add(new MaterialTextParameter(tag.Name, DecodeOpaqueText(payload), "StringPayload"));
                    break;
                case PackageReader.PropertyTypeObject:
                    if (TryDecodeObjectSlot(package, rootPackage, tag.Name, payload, textureResolver, packageIndex, out var objectSlot, out var textureSlot))
                    {
                        objectSlots.Add(objectSlot);
                        if (textureSlot is not null)
                        {
                            textureSlots.Add(textureSlot);
                        }
                    }
                    break;
            }
        }

        nodes.Add(new MaterialGraphNode(
            visitKey,
            currentClassName,
            PackageReader.SafeName(package.Names, exportEntry.ObjectName),
            Path.GetFileNameWithoutExtension(package.Path),
            package.Path,
            textureSlots,
            objectSlots,
            bools,
            numerics,
            texts));

        foreach (var objectSlot in objectSlots)
        {
            var childRef = new UnrResolvedObjectRef(
                objectSlot.ClassName,
                objectSlot.ObjectName,
                objectSlot.PackageName,
                null,
                objectSlot.PackagePath);

            if (string.Equals(objectSlot.PackageName, Path.GetFileNameWithoutExtension(package.Path), StringComparison.OrdinalIgnoreCase))
            {
                var loadedChild = ResolveExportReferenceByName(package, objectSlot.ObjectName);
                if (loadedChild is not null)
                {
                    childRef = loadedChild;
                }
            }

            BuildMaterialGraphNodes(rootPackage, childRef, textureResolver, packageIndex, visited, nodes, depth + 1);
        }
    }

    private static UnrResolvedObjectRef? ResolveExportReferenceByName(PackageData package, string objectName)
    {
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var exportEntry = package.Exports[i];
            if (!string.Equals(PackageReader.SafeName(package.Names, exportEntry.ObjectName), objectName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new UnrResolvedObjectRef(
                PackageReader.ExportClassName(package, exportEntry),
                objectName,
                Path.GetFileNameWithoutExtension(package.Path),
                i,
                package.Path);
        }

        return null;
    }

    private static bool TryDecodeObjectSlot(
        PackageData package,
        PackageData rootPackage,
        string slotName,
        byte[] payload,
        Func<string, string, TextureData?>? textureResolver,
        IReadOnlyDictionary<string, string>? packageIndex,
        out MaterialObjectSlot objectSlot,
        out MaterialTextureSlot? textureSlot)
    {
        objectSlot = default!;
        textureSlot = null;
        var payloadPos = 0;
        if (!PackageReader.TryReadCompactIndex(payload, ref payloadPos, out var objectRef) || objectRef == 0)
        {
            return false;
        }

        var resolved = ResolveObjectRef(package, objectRef);
        if (resolved is null)
        {
            return false;
        }

        var reference = $"{resolved.PackageName}.{resolved.ObjectName}";
        objectSlot = new MaterialObjectSlot(
            slotName,
            reference,
            resolved.ClassName,
            resolved.ObjectName,
            resolved.PackageName,
            resolved.PackagePath);

        if (resolved.ClassName.Is(UnrealClassNames.Texture) ||
            resolved.ClassName.EndsWith(UnrealClassNames.Texture, StringComparison.OrdinalIgnoreCase))
        {
            textureSlot = new MaterialTextureSlot(
                slotName,
                reference,
                resolved.ClassName,
                resolved.ObjectName,
                resolved.PackageName,
                resolved.PackagePath,
                TryLoadTexture(rootPackage, resolved, textureResolver, packageIndex));
        }

        return true;
    }

    private static TextureData? TryLoadTexture(
        PackageData rootPackage,
        UnrResolvedObjectRef textureRef,
        Func<string, string, TextureData?>? textureResolver,
        IReadOnlyDictionary<string, string>? packageIndex)
    {
        if (textureRef.ExportIndex is not null &&
            string.Equals(textureRef.PackageName, Path.GetFileNameWithoutExtension(rootPackage.Path), StringComparison.OrdinalIgnoreCase))
        {
            return TextureCodec.DecodeTextureExport(rootPackage, textureRef.ExportIndex.Value);
        }

        var resolvedTexture = textureResolver?.Invoke(textureRef.PackageName, textureRef.ObjectName);
        if (resolvedTexture is not null)
        {
            return resolvedTexture;
        }

        if (!string.IsNullOrWhiteSpace(textureRef.PackagePath))
        {
            return TextureCodec.LoadTextureFromPackage(textureRef.PackagePath, textureRef.ObjectName);
        }

        if (packageIndex is not null && packageIndex.TryGetValue(textureRef.PackageName, out var packagePath))
        {
            return TextureCodec.LoadTextureFromPackage(packagePath, textureRef.ObjectName);
        }

        return null;
    }

    private static bool TryDecodeNameProperty(PackageData package, byte[] payload, out string nameText)
    {
        nameText = string.Empty;
        var payloadPos = 0;
        if (!PackageReader.TryReadCompactIndex(payload, ref payloadPos, out var nameIndex))
        {
            return false;
        }

        nameText = PackageReader.SafeName(package.Names, nameIndex);
        return true;
    }

    private static string DecodeOpaqueText(byte[] payload)
    {
        if (payload.Length == 0)
        {
            return string.Empty;
        }

        return BitConverter.ToString(payload).Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private static void SkipObjectStackPrefixIfPresent(ExportEntry exportEntry, byte[] blob, ref int pos)
    {
        if ((exportEntry.ObjectFlags & UnrPackageHelpers.ObjectFlagHasStack) == 0)
        {
            return;
        }

        var node = PackageReader.ReadCompactIndex(blob, ref pos);
        _ = PackageReader.ReadCompactIndex(blob, ref pos);
        pos += 12;
        if (node != 0)
        {
            _ = PackageReader.ReadCompactIndex(blob, ref pos);
        }
    }

    private static bool TryLoadResolvedObject(
        PackageData rootPackage,
        UnrResolvedObjectRef objectRef,
        IReadOnlyDictionary<string, string>? packageIndex,
        out PackageData package,
        out int exportIndex)
    {
        var rootPackageName = Path.GetFileNameWithoutExtension(rootPackage.Path);
        if (objectRef.ExportIndex is not null &&
            string.Equals(objectRef.PackageName, rootPackageName, StringComparison.OrdinalIgnoreCase))
        {
            package = rootPackage;
            exportIndex = objectRef.ExportIndex.Value;
            return exportIndex >= 0 && exportIndex < package.Exports.Count;
        }

        package = null!;
        exportIndex = -1;
        if (packageIndex is null || !packageIndex.TryGetValue(objectRef.PackageName, out var packagePath))
        {
            return false;
        }

        package = LoadPackageCached(packagePath);
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var export = package.Exports[i];
            if (!string.Equals(PackageReader.SafeName(package.Names, export.ObjectName), objectRef.ObjectName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            exportIndex = i;
            return true;
        }

        return false;
    }

    private static PackageData LoadPackageCached(string path)
    {
        lock (PackageCacheLock)
        {
            if (PackageCache.TryGetValue(path, out var package))
            {
                return package;
            }

            package = PackageReader.LoadPackage(path);
            PackageCache[path] = package;
            return package;
        }
    }

    private static UnrResolvedObjectRef? ResolveObjectRef(PackageData package, int reference)
    {
        var resolved = UnrPackageHelpers.ResolveObjectRef(package, reference);
        if (resolved is null)
        {
            return null;
        }

        var packageName = Path.GetFileNameWithoutExtension(package.Path);
        if (reference < 0)
        {
            return new UnrResolvedObjectRef(resolved.Value.ClassName, resolved.Value.ObjectName, resolved.Value.PackageName ?? packageName, null, null);
        }
        else
        {
            var index = reference - 1;
            return new UnrResolvedObjectRef(resolved.Value.ClassName, resolved.Value.ObjectName, packageName, index, package.Path);
        }
    }

    private static readonly object PackageCacheLock = new();
    private static readonly Dictionary<string, PackageData> PackageCache = new(StringComparer.OrdinalIgnoreCase);
}
