using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

internal static class BspChunkPartitioner
{
    public static BspDiagnosticChunk[] BuildChunks(
        string modelName,
        IReadOnlyList<BspPolygonFragment> polygons,
        Func<IEnumerable<BspPolygonFragment>, BspDiagnosticMeshPart[]> meshPartBuilder)
    {
        var remaining = Enumerable.Range(0, polygons.Count).ToHashSet();
        var chunks = new List<BspDiagnosticChunk>();

        while (remaining.Count > 0)
        {
            var startIndex = remaining.First();
            remaining.Remove(startIndex);

            var startPolygon = polygons[startIndex];
            var chunkIndices = new List<int> { startIndex };
            var queue = new Queue<int>();
            queue.Enqueue(startIndex);
            var startSignature = BuildChunkSignature(startPolygon);

            while (queue.Count > 0)
            {
                var currentIndex = queue.Dequeue();
                var current = polygons[currentIndex];

                foreach (var candidateIndex in remaining.ToArray())
                {
                    var candidate = polygons[candidateIndex];
                    if (candidate.Kind != current.Kind)
                    {
                        continue;
                    }

                    if (BuildChunkSignature(candidate) != startSignature)
                    {
                        continue;
                    }

                    if (!SharesEdge(current.Vertices, candidate.Vertices))
                    {
                        continue;
                    }

                    remaining.Remove(candidateIndex);
                    chunkIndices.Add(candidateIndex);
                    queue.Enqueue(candidateIndex);
                }
            }

            chunks.Add(BuildChunkDto(modelName, chunks.Count, chunkIndices, polygons, meshPartBuilder));
        }

        return chunks
            .OrderByDescending(x => x.PolygonCount)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) => new BspDiagnosticChunk
            {
                ChunkIndex = index,
                Name = x.Name,
                Kind = x.Kind,
                IsPortalLike = x.IsPortalLike,
                IsInvisibleLike = x.IsInvisibleLike,
                HasZoneBoundary = x.HasZoneBoundary,
                ZoneNumbers = x.ZoneNumbers,
                LeafIndices = x.LeafIndices,
                PolygonCount = x.PolygonCount,
                TriangleCount = x.TriangleCount,
                SurfaceCount = x.SurfaceCount,
                BoundsMin = x.BoundsMin,
                BoundsMax = x.BoundsMax,
                MeshParts = x.MeshParts
            })
            .ToArray();
    }

    private static BspDiagnosticChunk BuildChunkDto(
        string modelName,
        int chunkIndex,
        IReadOnlyList<int> polygonIndices,
        IReadOnlyList<BspPolygonFragment> polygons,
        Func<IEnumerable<BspPolygonFragment>, BspDiagnosticMeshPart[]> meshPartBuilder)
    {
        var chunkPolygons = polygonIndices
            .Select(x => polygons[x])
            .ToArray();
        var parts = meshPartBuilder(chunkPolygons);
        var boundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var boundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        var triangleCount = 0;

        foreach (var polygon in chunkPolygons)
        {
            boundsMin = Vector3.Min(boundsMin, polygon.BoundsMin);
            boundsMax = Vector3.Max(boundsMax, polygon.BoundsMax);
            triangleCount += polygon.Triangles.Length;
        }

        var representative = chunkPolygons[0];
        var zoneNumbers = chunkPolygons
            .SelectMany(x => x.ZoneNumbers)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        var leafIndices = chunkPolygons
            .SelectMany(x => x.LeafIndices)
            .Where(x => x >= 0)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        var materialLabel = BuildMaterialLabel(representative);
        var zoneLabel = zoneNumbers.Length == 0 ? "ZNone" : $"Z{string.Join("_", zoneNumbers.Select(x => x.ToString("D2")))}";
        var leafLabel = leafIndices.Length == 0 ? "LNone" : $"L{leafIndices[0]}";
        var label = $"{modelName} / {materialLabel}_{representative.Kind}_{zoneLabel}_{leafLabel}_{chunkIndex + 1:000}";

        return new BspDiagnosticChunk
        {
            ChunkIndex = chunkIndex,
            Name = label,
            Kind = representative.Kind,
            IsPortalLike = representative.IsPortalLike,
            IsInvisibleLike = representative.IsInvisibleLike,
            HasZoneBoundary = representative.HasZoneBoundary,
            ZoneNumbers = zoneNumbers,
            LeafIndices = leafIndices,
            PolygonCount = chunkPolygons.Length,
            TriangleCount = triangleCount,
            SurfaceCount = parts.Sum(x => x.SurfaceCount),
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            MeshParts = parts
        };
    }

    private static BspChunkSignature BuildChunkSignature(BspPolygonFragment polygon)
    {
        return new BspChunkSignature(
            polygon.MaterialRawReference,
            polygon.MaterialPackageName,
            polygon.MaterialObjectName,
            polygon.PolyFlags,
            polygon.Kind,
            polygon.ZoneNumbers,
            polygon.LeafIndices,
            polygon.HasZoneBoundary);
    }

    private static string BuildMaterialLabel(BspPolygonFragment polygon)
    {
        if (!string.IsNullOrWhiteSpace(polygon.MaterialObjectName))
        {
            return $"MAT_{polygon.MaterialObjectName}";
        }

        return polygon.MaterialRawReference == 0
            ? $"MAT_NULL_{polygon.PolyFlags:X8}"
            : $"MAT_RAW_{polygon.MaterialRawReference}";
    }

    private static bool SharesEdge(IReadOnlyList<Vector3> left, IReadOnlyList<Vector3> right)
    {
        if (left.Count < 2 || right.Count < 2)
        {
            return false;
        }

        var rightEdges = new HashSet<EdgeKey>();
        for (var i = 0; i < right.Count; i++)
        {
            var a = Quantize(right[i]);
            var b = Quantize(right[(i + 1) % right.Count]);
            if (a != b)
            {
                rightEdges.Add(new EdgeKey(a, b));
            }
        }

        for (var i = 0; i < left.Count; i++)
        {
            var a = Quantize(left[i]);
            var b = Quantize(left[(i + 1) % left.Count]);
            if (a == b)
            {
                continue;
            }

            if (rightEdges.Contains(new EdgeKey(a, b)))
            {
                return true;
            }
        }

        return false;
    }

    private static VertexKey Quantize(Vector3 point)
    {
        return new VertexKey(
            (int)MathF.Round(point.X),
            (int)MathF.Round(point.Y),
            (int)MathF.Round(point.Z));
    }

    internal readonly record struct BspTriangleFragment(
        Vector3 A,
        Vector3 B,
        Vector3 C,
        Vector2 UvA,
        Vector2 UvB,
        Vector2 UvC);

    internal sealed record BspPolygonFragment(
        int NodeIndex,
        int MaterialRawReference,
        string? MaterialPackageName,
        string? MaterialObjectName,
        uint PolyFlags,
        byte Zone0,
        byte Zone1,
        int LeafIndex0,
        int LeafIndex1,
        uint ColorArgb,
        Vector3[] Vertices,
        Vector3 Normal,
        Vector3 BoundsMin,
        Vector3 BoundsMax,
        BspTriangleFragment[] Triangles)
    {
        public bool IsPortalLike => (PolyFlags & (uint)UnrPolyFlags.Portal) != 0;
        public bool IsInvisibleLike => (PolyFlags & (uint)UnrPolyFlags.Invisible) != 0;
        public string Kind => (IsPortalLike, IsInvisibleLike) switch
        {
            (true, true) => "PortalInvisible",
            (true, false) => "Portal",
            (false, true) => "Invisible",
            _ => "Solid"
        };
        public bool HasZoneBoundary => Zone0 != Zone1;
        public byte[] ZoneNumbers => HasZoneBoundary ? [Zone0, Zone1] : [Zone0];
        public int[] LeafIndices => LeafIndex0 == LeafIndex1 ? [LeafIndex0] : [LeafIndex0, LeafIndex1];
    }

    private readonly record struct BspChunkSignature(
        int MaterialRawReference,
        string? MaterialPackageName,
        string? MaterialObjectName,
        uint PolyFlags,
        string Kind,
        byte[] ZoneNumbers,
        int[] LeafIndices,
        bool HasZoneBoundary)
    {
        public bool Equals(BspChunkSignature other)
        {
            return MaterialRawReference == other.MaterialRawReference &&
                   string.Equals(MaterialPackageName, other.MaterialPackageName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(MaterialObjectName, other.MaterialObjectName, StringComparison.OrdinalIgnoreCase) &&
                   PolyFlags == other.PolyFlags &&
                   string.Equals(Kind, other.Kind, StringComparison.Ordinal) &&
                   HasZoneBoundary == other.HasZoneBoundary &&
                   ZoneNumbers.SequenceEqual(other.ZoneNumbers) &&
                   LeafIndices.SequenceEqual(other.LeafIndices);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(MaterialRawReference);
            hash.Add(MaterialPackageName, StringComparer.OrdinalIgnoreCase);
            hash.Add(MaterialObjectName, StringComparer.OrdinalIgnoreCase);
            hash.Add(PolyFlags);
            hash.Add(Kind, StringComparer.Ordinal);
            hash.Add(HasZoneBoundary);
            foreach (var zone in ZoneNumbers)
            {
                hash.Add(zone);
            }

            foreach (var leaf in LeafIndices)
            {
                hash.Add(leaf);
            }

            return hash.ToHashCode();
        }
    }

    private readonly record struct VertexKey(int X, int Y, int Z);

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public VertexKey A { get; }
        public VertexKey B { get; }

        public EdgeKey(VertexKey a, VertexKey b) : this()
        {
            if (Compare(a, b) <= 0)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        private static int Compare(VertexKey left, VertexKey right)
        {
            var byX = left.X.CompareTo(right.X);
            if (byX != 0)
            {
                return byX;
            }

            var byY = left.Y.CompareTo(right.Y);
            return byY != 0 ? byY : left.Z.CompareTo(right.Z);
        }

        public bool Equals(EdgeKey other)
        {
            return A.Equals(other.A) && B.Equals(other.B);
        }

        public override bool Equals(object? obj)
        {
            return obj is EdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, B);
        }
    }
}
