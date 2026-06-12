using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.UsxFile;

public static class StaticMeshCodec
{
    internal static readonly object PackageCacheLock = new();
    internal static readonly Dictionary<string, PackageData> PackageCache = new(StringComparer.OrdinalIgnoreCase);

    internal sealed record ResolvedObjectRef(
        string ClassName,
        string ObjectName,
        string PackageName,
        int? ExportIndex,
        string? PackagePath);

    private enum StaticMeshPropertyKind
    {
        Materials,
        UseSimpleLineCollision,
        UseSimpleBoxCollision,
        UseSimpleKarmaCollision,
        UseSimpleRigidBodyCollision
    }

    private enum StaticMeshMaterialPropertyKind
    {
        Material,
        EnableCollision
    }

    internal sealed record StaticMeshProperties(
        IReadOnlyList<int?> MaterialSlots,
        IReadOnlyList<bool> MaterialCollisionFlags,
        bool UseSimpleLineCollision,
        bool UseSimpleBoxCollision,
        bool UseSimpleKarmaCollision);

    internal static StaticMeshProperties ParseStaticMeshPropertiesStrict(PackageData package, ExportEntry exportEntry, BinaryReader reader)
    {
        var useSimpleLineCollision = false;
        var useSimpleBoxCollision = false;
        var useSimpleKarmaCollision = false;
        var materialSlots = new List<int?>();
        var materialCollisionFlags = new List<bool>();

        while (true)
        {
            if (!PackageReader.TryReadPropertyTag(reader, package.Names, out var tag))
            {
                throw new PackageReadException($"StaticMesh export {exportEntry.ObjectName} has invalid property tag.");
            }

            if (tag.IsEnd)
            {
                return new StaticMeshProperties(
                    materialSlots,
                    materialCollisionFlags,
                    useSimpleLineCollision,
                    useSimpleBoxCollision,
                    useSimpleKarmaCollision);
            }

            var payloadStart = reader.BaseStream.Position;
            if (!Enum.TryParse<StaticMeshPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                reader.BaseStream.Position = payloadStart + tag.DataSize;
                continue;
            }

            switch (kind)
            {
                case StaticMeshPropertyKind.Materials:
                    var (slots, flags) = ParseStaticMeshMaterialsStrict(reader, package.Names, package.Imports.Count, package.Exports.Count, tag.DataSize);
                    materialSlots.AddRange(slots);
                    materialCollisionFlags.AddRange(flags);
                    break;
                case StaticMeshPropertyKind.UseSimpleLineCollision:
                    useSimpleLineCollision = tag.BoolValue;
                    break;
                case StaticMeshPropertyKind.UseSimpleBoxCollision:
                    useSimpleBoxCollision = tag.BoolValue;
                    break;
                case StaticMeshPropertyKind.UseSimpleKarmaCollision:
                    useSimpleKarmaCollision = tag.BoolValue;
                    break;
                case StaticMeshPropertyKind.UseSimpleRigidBodyCollision:
                    break;
            }

            reader.BaseStream.Position = payloadStart + tag.DataSize;
        }
    }

    private static (IReadOnlyList<int?> Slots, IReadOnlyList<bool> CollisionFlags) ParseStaticMeshMaterialsStrict(
        BinaryReader reader,
        IReadOnlyList<string> names,
        int importCount,
        int exportCount,
        int dataSize)
    {
        var startPos = reader.BaseStream.Position;
        var count = PackageReader.ReadCompactIndex(reader);
        if (count < 0 || count > 4096)
        {
            throw new PackageReadException($"Invalid array count for Materials: {count}");
        }

        var slots = new List<int?>(count);
        var collisionFlags = new List<bool>(count);

        for (var itemIndex = 0; itemIndex < count; itemIndex++)
        {
            int? materialRef = null;
            var enableCollision = false;
            var hasEnableCollision = false;

            while (true)
            {
                if (!PackageReader.TryReadPropertyTag(reader, names, out var tag))
                {
                    throw new PackageReadException($"Invalid property tag in Materials array element {itemIndex}.");
                }

                if (tag.IsEnd)
                {
                    break;
                }

                var payloadStart = reader.BaseStream.Position;
                if (!Enum.TryParse<StaticMeshMaterialPropertyKind>(tag.Name, ignoreCase: true, out var kind))
                {
                    reader.BaseStream.Position = payloadStart + tag.DataSize;
                    continue;
                }

                switch (kind)
                {
                    case StaticMeshMaterialPropertyKind.Material:
                        var objRef = PackageReader.ReadCompactIndex(reader);
                        if (objRef != 0 && IsValidObjectRef(objRef, importCount, exportCount))
                        {
                            materialRef = objRef;
                        }
                        break;
                    case StaticMeshMaterialPropertyKind.EnableCollision:
                        hasEnableCollision = true;
                        enableCollision = tag.BoolValue;
                        break;
                }

                reader.BaseStream.Position = payloadStart + tag.DataSize;
            }

            slots.Add(materialRef);
            collisionFlags.Add(hasEnableCollision && enableCollision);
        }

        reader.BaseStream.Position = startPos + dataSize;
        return (slots, collisionFlags);
    }

    public static MeshDecodeDiagnostic LoadStaticMeshNamed(
        string packagePath,
        string meshName,
        IReadOnlyDictionary<string, string>? texturePackageIndex = null,
        Func<string, string, TextureData?>? textureResolver = null,
        IReadOnlyDictionary<string, string>? packageIndex = null)
    {
        var package = PackageReader.LoadPackage(packagePath);
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var exportEntry = package.Exports[i];
            if (!PackageReader.ExportClassName(package, exportEntry).Is(UnrealClassNames.StaticMesh))
            {
                continue;
            }

            if (string.Equals(PackageReader.SafeName(package.Names, exportEntry.ObjectName), meshName, StringComparison.OrdinalIgnoreCase))
            {
                return DecodeStaticMeshExport(package, i, texturePackageIndex, textureResolver, packageIndex);
            }
        }

        return new MeshDecodeDiagnostic(null, null, null, false);
    }

    public static MeshDecodeDiagnostic DecodeStaticMeshExport(
        PackageData package,
        int exportIndex,
        IReadOnlyDictionary<string, string>? texturePackageIndex = null,
        Func<string, string, TextureData?>? textureResolver = null,
        IReadOnlyDictionary<string, string>? packageIndex = null)
    {
        if (exportIndex < 0 || exportIndex >= package.Exports.Count)
        {
            return new MeshDecodeDiagnostic(null, null, null, false);
        }

        var exportEntry = package.Exports[exportIndex];
        if (!PackageReader.ExportClassName(package, exportEntry).Is(UnrealClassNames.StaticMesh))
        {
            return new MeshDecodeDiagnostic(null, null, null, false);
        }

        using var reader = PackageReader.OpenExportReader(package, exportEntry);
        var properties = ParseStaticMeshPropertiesStrict(package, exportEntry, reader);

        var parsedMesh = ParseStaticMeshStrict(package, exportEntry, reader);
        if (parsedMesh is null)
        {
            return new MeshDecodeDiagnostic(null, null, null, false);
        }

        var triangles = BuildTriangles(parsedMesh);
        if (triangles.Count == 0)
        {
            return new MeshDecodeDiagnostic(null, null, null, false);
        }

        var bboxMin = new Vector3(float.MaxValue);
        var bboxMax = new Vector3(float.MinValue);
        foreach (var tri in triangles)
        {
            UpdateBounds(tri.A.Position, ref bboxMin, ref bboxMax);
            UpdateBounds(tri.B.Position, ref bboxMin, ref bboxMax);
            UpdateBounds(tri.C.Position, ref bboxMin, ref bboxMax);
        }

        var collision = StaticMeshCollisionCodec.TryParseCollisionData(package, exportEntry, reader);
        var collisionInfo = new StaticMeshCollisionInfo(
            properties.UseSimpleLineCollision,
            properties.UseSimpleBoxCollision,
            properties.UseSimpleKarmaCollision,
            properties.MaterialCollisionFlags);

        var (textureRef, usedTextures) = GetMeshTextureReferences(package, properties.MaterialSlots);
var mesh = new MeshData(
    PackageReader.SafeName(package.Names, exportEntry.ObjectName),
    triangles,
    bboxMin,
    bboxMax,
    parsedMesh.SourceNote,
    textureRef,
    usedTextures);

        return new MeshDecodeDiagnostic(mesh, collision, collisionInfo, false);
    }

    public static IReadOnlyList<ResolvedMaterialGraph?> ResolveStaticMeshMaterials(
        string packagePath,
        string meshName,
        Func<string, string, TextureData?>? textureResolver = null,
        IReadOnlyDictionary<string, string>? packageIndex = null)
    {
        var package = PackageReader.LoadPackage(packagePath);
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var exportEntry = package.Exports[i];
            if (!PackageReader.ExportClassName(package, exportEntry).Is(UnrealClassNames.StaticMesh))
            {
                continue;
            }

            if (string.Equals(PackageReader.SafeName(package.Names, exportEntry.ObjectName), meshName, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveStaticMeshMaterials(package, i, textureResolver, packageIndex);
            }
        }

        return Array.Empty<ResolvedMaterialGraph?>();
    }

    public static IReadOnlyList<ResolvedMaterialGraph?> ResolveStaticMeshMaterials(
        PackageData package,
        int exportIndex,
        Func<string, string, TextureData?>? textureResolver = null,
        IReadOnlyDictionary<string, string>? packageIndex = null)
    {
        if (exportIndex < 0 || exportIndex >= package.Exports.Count)
        {
            return Array.Empty<ResolvedMaterialGraph?>();
        }

        var exportEntry = package.Exports[exportIndex];
        if (!PackageReader.ExportClassName(package, exportEntry).Is(UnrealClassNames.StaticMesh))
        {
            return Array.Empty<ResolvedMaterialGraph?>();
        }

        using var reader = PackageReader.OpenExportReader(package, exportEntry);
        var properties = ParseStaticMeshPropertiesStrict(package, exportEntry, reader);
        
        var results = new List<ResolvedMaterialGraph?>(properties.MaterialSlots.Count);
        foreach (var materialRef in properties.MaterialSlots)
        {
            if (materialRef is null)
            {
                results.Add(null);
                continue;
            }

            var resolved = ResolveObjectRef(package, materialRef.Value);
            if (resolved is null)
            {
                results.Add(null);
                continue;
            }

            var unrResolved = new UnrResolvedObjectRef(
                resolved.ClassName,
                resolved.ObjectName,
                resolved.PackageName,
                resolved.ExportIndex,
                resolved.PackagePath);
            var graph = MaterialGraphResolver.ResolveMaterialGraph(package, unrResolved, textureResolver, packageIndex);
            results.Add(graph);
        }

        return results;
    }

    internal static StaticMeshRaw? ParseStaticMeshStrict(PackageData package, ExportEntry exportEntry, BinaryReader reader)
    {
        var bounds = ReadPrimitiveBounds(reader, package);
        var sectionCount = ReadTArrayCount(reader);
        var sections = new List<StaticMeshSection>(sectionCount);
        for (var i = 0; i < sectionCount; i++)
        {
            var unused4 = reader.ReadInt32();
            var firstIndex = reader.ReadUInt16();
            var firstVertex = reader.ReadUInt16();
            var lastVertex = reader.ReadUInt16();
            var unusedE = reader.ReadUInt16();
            var numFaces = reader.ReadUInt16();
            sections.Add(new StaticMeshSection(unused4, firstIndex, firstVertex, lastVertex, unusedE, numFaces));
        }

        var box = ReadBox(reader, package);

        var vertexCount = ReadTArrayCount(reader);
        var vertices = new List<Vector3>(vertexCount);
        var normals = new List<Vector3>(vertexCount);
        for (var i = 0; i < vertexCount; i++)
        {
            vertices.Add(ReadVector3(reader));
            normals.Add(MathEx.NormalizeSafe(ReadVector3(reader)));
        }
        var vertexRevision = reader.ReadInt32();
        var vertexStream = new StaticMeshVertexStream(vertices, normals, vertexRevision);

        var color1Count = ReadTArrayCount(reader);
        var colors1 = new List<uint>(color1Count);
        for (var i = 0; i < color1Count; i++)
        {
            colors1.Add(reader.ReadUInt32());
        }
        var color1Revision = reader.ReadInt32();
        var colorStream1 = new RawColorStream(colors1, color1Revision);

        var color2Count = ReadTArrayCount(reader);
        var colors2 = new List<uint>(color2Count);
        for (var i = 0; i < color2Count; i++)
        {
            colors2.Add(reader.ReadUInt32());
        }
        var color2Revision = reader.ReadInt32();
        var colorStream2 = new RawColorStream(colors2, color2Revision);

        var uvStreamCount = ReadTArrayCount(reader);
        var uvSets = new List<StaticMeshUVStream>(uvStreamCount);
        for (var i = 0; i < uvStreamCount; i++)
        {
            var uvCount = ReadTArrayCount(reader);
            var uvs = new List<Vector2>(uvCount);
            for (var j = 0; j < uvCount; j++)
            {
                uvs.Add(new Vector2(reader.ReadSingle(), reader.ReadSingle()));
            }

            var uvRev = reader.ReadInt32();
            var uvUnused = reader.ReadInt32();
            uvSets.Add(new StaticMeshUVStream(uvs, uvRev, uvUnused));
        }

        var idx1Count = ReadTArrayCount(reader);
        var idx1 = new List<int>(idx1Count);
        for (var i = 0; i < idx1Count; i++)
        {
            idx1.Add(reader.ReadUInt16());
        }
        var idx1Revision = reader.ReadInt32();
        var indexStream1 = new RawIndexBuffer(idx1, idx1Revision);

        var idx2Count = ReadTArrayCount(reader);
        var idx2 = new List<int>(idx2Count);
        for (var i = 0; i < idx2Count; i++)
        {
            idx2.Add(reader.ReadUInt16());
        }
        var idx2Revision = reader.ReadInt32();
        var indexStream2 = new RawIndexBuffer(idx2, idx2Revision);

        var wireframeIndexCount = PackageReader.ReadCompactIndex(reader);

        if (vertices.Count == 0 || sections.Count == 0)
        {
            return null;
        }

        return new StaticMeshRaw(
            bounds.Box,
            bounds.Sphere,
            sections,
            box,
            vertexStream,
            colorStream1,
            colorStream2,
            uvSets,
            indexStream1,
            indexStream2,
            wireframeIndexCount,
            string.Empty);
    }

    internal static IReadOnlyList<Triangle> BuildTriangles(StaticMeshRaw raw)
    {
        var indexStream = raw.IndexStream1.Indices.Count > 0 ? raw.IndexStream1.Indices : raw.IndexStream2.Indices;
        if (indexStream.Count == 0)
        {
            return Array.Empty<Triangle>();
        }

        var uv0 = raw.UvSets.Count > 0 ? raw.UvSets[0].Uv : Array.Empty<Vector2>();
        var triangles = new List<Triangle>();
        for (var sectionId = 0; sectionId < raw.Sections.Count; sectionId++)
        {
            var section = raw.Sections[sectionId];
            var baseIndex = section.FirstIndex;
            for (var face = 0; face < section.NumFaces; face++)
            {
                var i0 = baseIndex + face * 3;
                var i1 = i0 + 1;
                var i2 = i0 + 2;
                if (i2 >= indexStream.Count)
                {
                    break;
                }

                var ia = indexStream[i0];
                var ib = indexStream[i1];
                var ic = indexStream[i2];
                if (ia >= raw.VertexStream.Vertices.Count || ib >= raw.VertexStream.Vertices.Count || ic >= raw.VertexStream.Vertices.Count)
                {
                    continue;
                }

                var a = raw.VertexStream.Vertices[ia];
                var b = raw.VertexStream.Vertices[ib];
                var c = raw.VertexStream.Vertices[ic];
                var uvA = ia < uv0.Count ? uv0[ia] : Vector2.Zero;
                var uvB = ib < uv0.Count ? uv0[ib] : Vector2.Zero;
                var uvC = ic < uv0.Count ? uv0[ic] : Vector2.Zero;
                var nA = ia < raw.VertexStream.Normals.Count ? raw.VertexStream.Normals[ia] : Vector3.UnitY;
                var nB = ib < raw.VertexStream.Normals.Count ? raw.VertexStream.Normals[ib] : Vector3.UnitY;
                var nC = ic < raw.VertexStream.Normals.Count ? raw.VertexStream.Normals[ic] : Vector3.UnitY;

                var faceNormal = MathEx.NormalizeSafe(Vector3.Cross(b - a, c - a));
                var avgNormal = MathEx.NormalizeSafe(nA + nB + nC);
                if (faceNormal.LengthSquared() > 1e-12f && avgNormal.LengthSquared() > 1e-12f && Vector3.Dot(faceNormal, avgNormal) < 0f)
                {
                    (b, c) = (c, b);
                    (uvB, uvC) = (uvC, uvB);
                    (nB, nC) = (nC, nB);
                }

                if (MathEx.TriangleArea(a, b, c) <= 1e-6f)
                {
                    continue;
                }

                triangles.Add(new Triangle(
                    new TriangleVertex(a, uvA, nA),
                    new TriangleVertex(b, uvB, nB),
                    new TriangleVertex(c, uvC, nC),
                    sectionId));
            }
        }

        return triangles;
    }

    internal static (string? TextureRef, IReadOnlyList<MaterialTextureInfo> UsedTextures) GetMeshTextureReferences(
        PackageData package,
        IReadOnlyList<int?> materialSlots)
    {
        var usedTextures = new List<MaterialTextureInfo>();
        var seenReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? primaryTextureRef = null;
        
        foreach (var slotRef in materialSlots)
        {
            if (slotRef is null) continue;
            var resolved = ResolveObjectRef(package, slotRef.Value);
            if (resolved is null)
            {
                continue;
            }

            var referenceText = $"{resolved.PackageName}.{resolved.ObjectName}";
            if (seenReferences.Add(referenceText))
            {
                usedTextures.Add(new MaterialTextureInfo(referenceText, resolved.PackagePath));
            }
            
            primaryTextureRef ??= referenceText;
        }

        return (primaryTextureRef, usedTextures);
    }

    internal static bool IsValidObjectRef(int value, int importCount, int exportCount)
    {
        if (value < 0)
        {
            var index = -value - 1;
            return index >= 0 && index < importCount;
        }

        if (value > 0)
        {
            var index = value - 1;
            return index >= 0 && index < exportCount;
        }

        return false;
    }

    internal static bool TryLoadResolvedObject(
        PackageData rootPackage,
        ResolvedObjectRef objectRef,
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

    internal static ResolvedObjectRef? ResolveObjectRef(PackageData package, int reference)
    {
        var resolved = UnrPackageHelpers.ResolveObjectRef(package, reference);
        if (resolved is null)
        {
            return null;
        }

        var packageName = Path.GetFileNameWithoutExtension(package.Path);
        if (reference < 0)
        {
            return new ResolvedObjectRef(resolved.Value.ClassName, resolved.Value.ObjectName, resolved.Value.PackageName ?? packageName, null, null);
        }
        else
        {
            var index = reference - 1;
            return new ResolvedObjectRef(resolved.Value.ClassName, resolved.Value.ObjectName, packageName, index, package.Path);
        }
    }

    internal static PackageData LoadPackageCached(string packagePath)
    {
        lock (PackageCacheLock)
        {
            if (PackageCache.TryGetValue(packagePath, out var cached))
            {
                return cached;
            }
        }

        var loaded = PackageReader.LoadPackage(packagePath);
        lock (PackageCacheLock)
        {
            if (PackageCache.TryGetValue(packagePath, out var cached))
            {
                return cached;
            }

            PackageCache[packagePath] = loaded;
            return loaded;
        }
    }

    internal static void UpdateBounds(Vector3 point, ref Vector3 min, ref Vector3 max)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }

    internal static (BoundingBox Box, BoundingSphere Sphere) ReadPrimitiveBounds(BinaryReader reader, PackageData package)
    {
        var box = ReadBox(reader, package);
        var sphere = ReadSphere(reader, package);
        return (box, sphere);
    }

    internal static BoundingBox ReadBox(BinaryReader reader, PackageData package)
    {
        var min = ReadVector3(reader);
        var max = ReadVector3(reader);
        byte? isValid = null;
        if (!package.IsVersionAtLeast(L2PackageVersion.Ver146))
        {
            isValid = reader.ReadByte();
        }
        return new BoundingBox(min, max, isValid);
    }

    internal static BoundingSphere ReadSphere(BinaryReader reader, PackageData package)
    {
        var center = ReadVector3(reader);
        float? radius = null;
        if (package.IsVersionAtLeast(L2PackageVersion.Ver61))
        {
            radius = reader.ReadSingle();
        }
        return new BoundingSphere(center, radius);
    }

    internal static int ReadTArrayCount(BinaryReader reader)
    {
        return PackageReader.ReadCompactIndex(reader);
    }

    internal static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}