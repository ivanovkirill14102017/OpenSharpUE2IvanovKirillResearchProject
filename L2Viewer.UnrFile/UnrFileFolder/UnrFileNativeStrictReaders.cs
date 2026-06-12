namespace L2Viewer.UnrFile;

internal static class UnrFileNativeStrictReaders
{
    internal static PolysNativeData? TryDecodePolysStrict(PackageData package, int exportIndex)
    {
        if (exportIndex < 0 || exportIndex >= package.Exports.Count)
        {
            return null;
        }

        var exportEntry = package.Exports[exportIndex];
        if (!string.Equals(PackageReader.ExportClassName(package, exportEntry), "Polys", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var blob = PackageReader.ReadExportBlob(package, exportEntry);
        if (blob.Length == 0)
        {
            return new PolysNativeData(PackageReader.SafeName(package.Names, exportEntry.ObjectName), []);
        }

        var pos = 0;
        if (blob[pos] == 0)
        {
            pos++;
        }

        using var stream = new MemoryStream(blob, writable: false);
        stream.Position = pos;
        using var reader = new BinaryReader(stream, PackageCompat.Latin1, leaveOpen: false);
        if (!TryReadInt32(reader, out var num) || !TryReadInt32(reader, out _))
        {
            return null;
        }

        var polys = new List<PolyNativeData>(Math.Max(0, num));
        for (var i = 0; i < num; i++)
        {
            pos = (int)reader.BaseStream.Position;
            if (!PackageReader.TryReadCompactIndex(blob, ref pos, out var numVertices) || numVertices < 0)
            {
                return null;
            }
            reader.BaseStream.Position = pos;

            if (!TryReadVector3(reader, out var polyBase) ||
                !TryReadVector3(reader, out var normal) ||
                !TryReadVector3(reader, out var textureU) ||
                !TryReadVector3(reader, out var textureV))
            {
                return null;
            }

            var vertices = new List<Vector3>(numVertices);
            for (var v = 0; v < numVertices; v++)
            {
                if (!TryReadVector3(reader, out var vertex))
                {
                    return null;
                }

                vertices.Add(vertex);
            }

            if (!TryReadUInt32(reader, out var polyFlags))
            {
                return null;
            }

            pos = (int)reader.BaseStream.Position;
            if (!PackageReader.TryReadCompactIndex(blob, ref pos, out var actorRef) ||
                !PackageReader.TryReadCompactIndex(blob, ref pos, out var textureRef) ||
                !PackageReader.TryReadCompactIndex(blob, ref pos, out var itemNameIndex) ||
                !PackageReader.TryReadCompactIndex(blob, ref pos, out var iLink) ||
                !PackageReader.TryReadCompactIndex(blob, ref pos, out var iBrushPoly))
            {
                return null;
            }
            reader.BaseStream.Position = pos;

            if (!TryReadSingle(reader, out var lightMapScale))
            {
                return null;
            }

            int? savePolyIndex = null;
            if (package.IsLicenseeVersionAtLeast(L2LicenseeVersion.Ver20))
            {
                if (!TryReadInt32(reader, out var spi))
                {
                    return null;
                }
                savePolyIndex = spi;
            }

            polys.Add(new PolyNativeData(
                i,
                numVertices,
                polyBase,
                normal,
                textureU,
                textureV,
                vertices,
                polyFlags,
                ResolveNativeReference(package, actorRef),
                ResolveNativeReference(package, textureRef),
                ResolveName(package, itemNameIndex),
                iLink,
                iBrushPoly,
                lightMapScale,
                savePolyIndex));
        }

        if (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            // UE2 natively ignores trailing bytes at the end of an object's serialization.
            // Some specific Polys in Lineage 2 (e.g. on 24_26.unr) have extra garbage appended.
        }

        return new PolysNativeData(PackageReader.SafeName(package.Names, exportEntry.ObjectName), polys);
    }

    private static NativeObjectReferenceInfo? ResolveNativeReference(PackageData package, int rawReference)
    {
        if (rawReference == 0)
        {
            return null;
        }

        var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference);
        if (resolved is null)
        {
            return null;
        }

        return new NativeObjectReferenceInfo(
            rawReference,
            rawReference > 0 ? rawReference - 1 : null,
            resolved.Value.ClassName,
            resolved.Value.ObjectName,
            resolved.Value.PackageName);
    }

    private static string? ResolveName(PackageData package, int nameIndex)
    {
        if (nameIndex < 0 || nameIndex >= package.Names.Count)
        {
            return null;
        }

        var name = package.Names[nameIndex];
        return string.Equals(name, "None", StringComparison.OrdinalIgnoreCase) ? null : name;
    }

    private static bool TryReadInt32(BinaryReader reader, out int value)
    {
        value = default;
        if (reader.BaseStream.Length - reader.BaseStream.Position < sizeof(int))
        {
            return false;
        }

        value = reader.ReadInt32();
        return true;
    }

    private static bool TryReadUInt32(BinaryReader reader, out uint value)
    {
        value = default;
        if (reader.BaseStream.Length - reader.BaseStream.Position < sizeof(uint))
        {
            return false;
        }

        value = reader.ReadUInt32();
        return true;
    }

    private static bool TryReadSingle(BinaryReader reader, out float value)
    {
        value = default;
        if (reader.BaseStream.Length - reader.BaseStream.Position < sizeof(float))
        {
            return false;
        }

        value = reader.ReadSingle();
        return true;
    }

    private static bool TryReadVector3(BinaryReader reader, out Vector3 value)
    {
        value = default;
        if (!TryReadSingle(reader, out var x) || !TryReadSingle(reader, out var y) || !TryReadSingle(reader, out var z))
        {
            return false;
        }

        value = new Vector3(x, y, z);
        return true;
    }

}
