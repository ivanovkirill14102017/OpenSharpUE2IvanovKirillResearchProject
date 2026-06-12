using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.UsxFile;

public static class StaticMeshCollisionCodec
{
    internal sealed record L2LazyArrayBlock(
        int Count,
        int DataStartOffset,
        int EndOffset);

    internal static StaticMeshCollisionData? TryParseCollisionData(
        PackageData package,
        ExportEntry exportEntry,
        BinaryReader reader)
    {
        if (!package.Wrapper.StartsWith("Lineage2Ver", StringComparison.OrdinalIgnoreCase) || exportEntry.SerialOffset is null)
        {
            return null;
        }

        var serialOffset = exportEntry.SerialOffset.Value;

        // 1. KDOP Faces Array
        if (!TryReadLineage2LazyArrayBlock(reader, serialOffset, out var collisionFacesBlock)) return null;
        reader.BaseStream.Position = collisionFacesBlock.EndOffset;

        // 2. KDOP Nodes Array
        if (!TryReadLineage2LazyArrayBlock(reader, serialOffset, out var collisionNodesBlock)) return null;
        reader.BaseStream.Position = collisionNodesBlock.EndOffset;

        // 3. Strict primitive bounds skip (Lineage 2 has an extra byte here, possibly a bool flag or padding)
        (BoundingBox Box, BoundingSphere Sphere) bounds;
        byte unknownFlag;
        try 
        {
            bounds = StaticMeshCodec.ReadPrimitiveBounds(reader, package);
            unknownFlag = reader.ReadByte();
        } 
        catch (PackageReadException)
        {
            return null;
        }
        catch (EndOfStreamException)
        {
            return null;
        }

        // 4. Actual Geometry Array
        if (!TryReadLineage2LazyArrayBlock(reader, serialOffset, out var geometryBlock)) return null;

        var geometryResult = ParseLineage2StaticMeshFaces(reader, ref geometryBlock);
        if (geometryResult is null) return null;
        
        ValidateLineage2CollisionFooter(reader, geometryBlock.EndOffset);

        return BuildCollisionData(geometryResult.Value.Triangles, collisionNodesBlock.Count, collisionFacesBlock.Count, bounds.Box, bounds.Sphere, unknownFlag, geometryResult.Value.Geometries, string.Empty);
    }

    private static bool TryReadLineage2LazyArrayBlock(
        BinaryReader reader,
        int serialOffset,
        out L2LazyArrayBlock block)
    {
        block = default!;
        if (reader.BaseStream.Length - reader.BaseStream.Position < 5) return false;

        var endAbsolute = reader.ReadInt32();
        var count = PackageReader.ReadCompactIndex(reader);

        var dataStart = (int)reader.BaseStream.Position;
        var endOffset = endAbsolute - serialOffset;

        if (endOffset < dataStart || endOffset > reader.BaseStream.Length)
        {
            return false;
        }

        block = new L2LazyArrayBlock(count, dataStart, endOffset);
        return true;
    }

    private static (List<Triangle> Triangles, List<Lineage2CollisionFaceGeometry> Geometries)? ParseLineage2StaticMeshFaces(BinaryReader reader, ref L2LazyArrayBlock block)
    {
        var triangles = new List<Triangle>(block.Count);
        var geometries = new List<Lineage2CollisionFaceGeometry>(block.Count);
        
        for (var i = 0; i < block.Count; i++)
        {
            var a = StaticMeshCodec.ReadVector3(reader);
            var b = StaticMeshCodec.ReadVector3(reader);
            var c = StaticMeshCodec.ReadVector3(reader);
            var basisCount = reader.ReadInt32();

            var bases = new List<Lineage2CollisionBasis>(basisCount);
            for (var j = 0; j < basisCount; j++)
            {
                bases.Add(new Lineage2CollisionBasis(
                    StaticMeshCodec.ReadVector3(reader),
                    StaticMeshCodec.ReadVector3(reader)));
            }

            var unknownVector = StaticMeshCodec.ReadVector3(reader);
            var unknown1 = reader.ReadInt32();
            var unknown2 = reader.ReadInt32();
            
            geometries.Add(new Lineage2CollisionFaceGeometry(a, b, c, bases, unknownVector, unknown1, unknown2));

            if (MathEx.TriangleArea(a, b, c) <= 1e-6f)
            {
                continue;
            }

            var normal = MathEx.NormalizeSafe(Vector3.Cross(b - a, c - a));
            triangles.Add(new Triangle(
                new TriangleVertex(a, Vector2.Zero, normal),
                new TriangleVertex(b, Vector2.Zero, normal),
                new TriangleVertex(c, Vector2.Zero, normal),
                0));
        }

        if (reader.BaseStream.Position != block.EndOffset)
        {
            throw new PackageReadException($"StaticMesh faces block ends at {reader.BaseStream.Position} but expected {block.EndOffset}");
        }

        return (triangles, geometries);
    }

    private static void ValidateLineage2CollisionFooter(BinaryReader reader, int endOffset)
    {
        if (reader.BaseStream.Position != endOffset) 
            throw new PackageReadException($"Invalid collision footer offset: expected {endOffset}, got {reader.BaseStream.Position}");
        
        if (reader.BaseStream.Position + 9 > reader.BaseStream.Length) 
            throw new PackageReadException($"Invalid collision footer length: expected at least 9 bytes remaining, got {reader.BaseStream.Length - reader.BaseStream.Position}");

        if (reader.ReadInt32() != 8) 
            throw new PackageReadException("Invalid collision footer magic");
    }

    private static StaticMeshCollisionData? BuildCollisionData(
        IReadOnlyList<Triangle> triangles,
        int nodeCount,
        int faceCount,
        BoundingBox primitiveBoundingBox,
        BoundingSphere primitiveBoundingSphere,
        byte unknownFlag,
        IReadOnlyList<Lineage2CollisionFaceGeometry> faceGeometries,
        string sourceNote)
    {
        if (triangles.Count == 0)
        {
            return null;
        }

        var bboxMin = new Vector3(float.MaxValue);
        var bboxMax = new Vector3(float.MinValue);
        foreach (var tri in triangles)
        {
            StaticMeshCodec.UpdateBounds(tri.A.Position, ref bboxMin, ref bboxMax);
            StaticMeshCodec.UpdateBounds(tri.B.Position, ref bboxMin, ref bboxMax);
            StaticMeshCodec.UpdateBounds(tri.C.Position, ref bboxMin, ref bboxMax);
        }

        return new StaticMeshCollisionData(
            new MeshData(
                "Collision",
                triangles,
                bboxMin,
                bboxMax,
                sourceNote,
                null,
                Array.Empty<MaterialTextureInfo>()),
            nodeCount,
            faceCount,
            primitiveBoundingBox,
            primitiveBoundingSphere,
            unknownFlag,
            faceGeometries,
            sourceNote);
    }
}