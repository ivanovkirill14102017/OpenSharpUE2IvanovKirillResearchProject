using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneBspRoomBuilder
{
    private const float SharedVertexEpsilonSquared = 0.01f;
    private const float DefaultMergeAngleDegrees = 45f;

    private readonly string? _clientRoot;
    private readonly SceneMaterialResolver? _materialResolver;
    private readonly BspTextureManager? _textureManager;

    public SceneBspRoomBuilder()
    {
    }

    public SceneBspRoomBuilder(string clientRoot, SceneMaterialResolver materialResolver, BspTextureManager textureManager)
    {
        _clientRoot = clientRoot;
        _materialResolver = materialResolver;
        _textureManager = textureManager;
    }

    public SceneBspScene Load(string path)
    {
        var unr = UnrFileReader.Read(path);
        return Build(unr);
    }

    public SceneBspScene Build(L2Viewer.UnrFile.UnrFile unr)
    {
        return Convert(BuildDiagnostic(unr));
    }

    private BspDiagnosticScene BuildDiagnostic(L2Viewer.UnrFile.UnrFile unr)
    {
        var worldModelExports = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrLevelObject>()
            .Where(x => x.ModelReference?.ExportIndex is not null)
            .Select(x => x.ModelReference!.ExportIndex!.Value)
            .ToHashSet();
        var builtModels = new List<BspDiagnosticModel>();
        var sceneMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var sceneMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var entry in unr.ExportObjects)
        {
            if (entry.Object is not UnrModelObject model)
            {
                continue;
            }

            var isWorldModelCandidate = worldModelExports.Contains(model.ExportIndex);
            if (!BspWorldModelPolicy.ShouldIncludeBspModel(unr.FilePath, model, isWorldModelCandidate))
            {
                continue;
            }

            var brushPolys = BspUvResolver.ResolveBrushPolys(unr, model);
            var built = BuildModel(model, brushPolys, isWorldModelCandidate);
            if (built is null)
            {
                continue;
            }

            builtModels.Add(built);
            sceneMin = Vector3.Min(sceneMin, built.BoundsMin);
            sceneMax = Vector3.Max(sceneMax, built.BoundsMax);
        }

        if (builtModels.Count == 0)
        {
            sceneMin = Vector3.Zero;
            sceneMax = Vector3.Zero;
        }

        return new BspDiagnosticScene
        {
            SourcePath = unr.FilePath,
            Models = builtModels
                .OrderByDescending(x => x.IsWorldModel)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            BoundsMin = sceneMin,
            BoundsMax = sceneMax
        };
    }

    private static BspDiagnosticModel? BuildModel(UnrModelObject model, UnrPolysObject? brushPolys, bool isWorldModel)
    {
        var polygonsByRoom = new Dictionary<int, List<RoomPolygonFragment>>();
        var modelMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var modelMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        var polygonCount = 0;
        var triangleCount = 0;

        for (var nodeIndex = 0; nodeIndex < model.Nodes.Length; nodeIndex++)
        {
            var node = model.Nodes[nodeIndex];
            if (node.SurfaceIndex < 0 || node.SurfaceIndex >= model.Surfaces.Length)
            {
                continue;
            }

            if (node.VertexCount < 3)
            {
                continue;
            }

            var polygon = new List<Vector3>(node.VertexCount);
            var validPolygon = true;
            for (var vertexOffset = 0; vertexOffset < node.VertexCount; vertexOffset++)
            {
                var vertexIndex = node.VertexPoolIndex + vertexOffset;
                if (vertexIndex < 0 || vertexIndex >= model.Vertices.Length)
                {
                    validPolygon = false;
                    break;
                }

                var pointIndex = model.Vertices[vertexIndex].PointIndex;
                if (pointIndex < 0 || pointIndex >= model.Points.Length)
                {
                    validPolygon = false;
                    break;
                }

                polygon.Add(model.Points[pointIndex]);
            }

            if (!validPolygon)
            {
                continue;
            }

            polygon = RemoveSequentialDuplicates(polygon);
            if (polygon.Count < 3)
            {
                continue;
            }

            var surface = model.Surfaces[node.SurfaceIndex];
            var normal = SafeNormalize(new Vector3(node.Plane.X, node.Plane.Y, node.Plane.Z));
            if (normal.LengthSquared() < 0.000001f)
            {
                normal = ComputePolygonNormal(polygon);
            }

            var polygonMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var polygonMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var point in polygon)
            {
                polygonMin = Vector3.Min(polygonMin, point);
                polygonMax = Vector3.Max(polygonMax, point);
                modelMin = Vector3.Min(modelMin, point);
                modelMax = Vector3.Max(modelMax, point);
            }

            var builtTriangles = new List<RoomTriangleFragment>(Math.Max(1, polygon.Count - 2));
            var first = polygon[0];
            for (var i = 1; i < polygon.Count - 1; i++)
            {
                var a = first;
                var b = polygon[i];
                var c = polygon[i + 1];

                if (IsDegenerateTriangle(a, b, c))
                {
                    continue;
                }

                builtTriangles.Add(new RoomTriangleFragment(
                    a,
                    b,
                    c,
                    BspUvResolver.ComputeRawUv(a, surface, model, brushPolys),
                    BspUvResolver.ComputeRawUv(b, surface, model, brushPolys),
                    BspUvResolver.ComputeRawUv(c, surface, model, brushPolys)));
                triangleCount++;
            }

            if (builtTriangles.Count == 0)
            {
                continue;
            }

            var fragment = new RoomPolygonFragment(
                nodeIndex,
                surface.MaterialRawReference,
                surface.MaterialReference?.PackageName,
                surface.MaterialReference?.ObjectName,
                surface.PolyFlags,
                node.Zone0,
                node.Zone1,
                node.LeafIndex0,
                node.LeafIndex1,
                CreateColor(surface.MaterialRawReference, polygonCount),
                polygon.ToArray(),
                normal,
                polygonMin,
                polygonMax,
                builtTriangles.ToArray());

            var roomId = ResolveRoomId(fragment);
            if (!polygonsByRoom.TryGetValue(roomId, out var roomPolygons))
            {
                roomPolygons = new List<RoomPolygonFragment>();
                polygonsByRoom.Add(roomId, roomPolygons);
            }

            roomPolygons.Add(fragment);
            polygonCount++;
        }

        if (polygonsByRoom.Count == 0)
        {
            return null;
        }

        if (polygonCount == 0)
        {
            modelMin = model.BoundsMin;
            modelMax = model.BoundsMax;
        }

        var chunks = BuildRoomChunks(model.ObjectName, polygonsByRoom);
        return new BspDiagnosticModel
        {
            ExportIndex = model.ExportIndex,
            Name = model.ObjectName,
            IsWorldModel = isWorldModel,
            NodeCount = model.Nodes.Length,
            PolygonCount = polygonCount,
            TriangleCount = triangleCount,
            BoundsMin = modelMin,
            BoundsMax = modelMax,
            Chunks = chunks
        };
    }

    private static int ResolveRoomId(RoomPolygonFragment polygon)
    {
        var zones = polygon.ZoneNumbers
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        if (zones.Length > 0)
        {
            var preferredZone = zones.FirstOrDefault(x => x != 0);
            return preferredZone != 0 || zones[0] == 0
                ? preferredZone != 0 ? preferredZone : zones[0]
                : zones[0];
        }

        var leaves = polygon.LeafIndices
            .Where(x => x >= 0)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        return leaves.Length > 0 ? leaves[0] : -polygon.NodeIndex - 1;
    }

    private static BspDiagnosticChunk[] BuildRoomChunks(string modelName, Dictionary<int, List<RoomPolygonFragment>> polygonsByRoom)
    {
        var chunks = new List<BspDiagnosticChunk>();
        var chunkIndex = 0;

        foreach (var roomEntry in polygonsByRoom.OrderBy(x => x.Key))
        {
            var roomId = roomEntry.Key;
            var roomComponents = SplitIntoConnectedComponents(roomEntry.Value);

            for (var componentIndex = 0; componentIndex < roomComponents.Count; componentIndex++)
            {
                var roomPolygons = roomComponents[componentIndex];
                var kindGroups = roomPolygons
                    .GroupBy(x => x.Kind, StringComparer.Ordinal)
                    .OrderBy(x => GetKindOrder(x.Key))
                    .ThenBy(x => x.Key, StringComparer.Ordinal)
                    .ToArray();

                foreach (var kindGroup in kindGroups)
                {
                    var polygonsOfKind = kindGroup.ToArray();
                    var representative = polygonsOfKind[0];
                    var boundsMin = polygonsOfKind.Select(x => x.BoundsMin).Aggregate(Vector3.Min);
                    var boundsMax = polygonsOfKind.Select(x => x.BoundsMax).Aggregate(Vector3.Max);
                    var triangleCount = polygonsOfKind.Sum(x => x.Triangles.Length);
                    var zoneNumbers = polygonsOfKind
                        .SelectMany(x => x.ZoneNumbers)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
                    var leafIndices = polygonsOfKind
                        .SelectMany(x => x.LeafIndices)
                        .Where(x => x >= 0)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
                    var zoneLabel = zoneNumbers.Length == 0 ? "ZNone" : $"Z{string.Join("_", zoneNumbers)}";
                    var roomLabel = roomId >= 0 ? roomId.ToString("D4") : $"Virtual_{Math.Abs(roomId):D4}";
                    if (roomComponents.Count > 1)
                    {
                        roomLabel = $"{roomLabel}_{componentIndex + 1:D2}";
                    }

                    var chunkName = kindGroups.Length == 1
                        ? $"{modelName} / Room_{roomLabel}_{zoneLabel}"
                        : $"{modelName} / Room_{roomLabel}_{zoneLabel}_{representative.Kind}";
                    var parts = BuildMeshParts(polygonsOfKind, DefaultMergeAngleDegrees);

                    chunks.Add(new BspDiagnosticChunk
                    {
                        ChunkIndex = chunkIndex++,
                        Name = chunkName,
                        Kind = representative.Kind,
                        IsPortalLike = representative.IsPortalLike,
                        IsInvisibleLike = representative.IsInvisibleLike,
                        HasZoneBoundary = polygonsOfKind.Any(x => x.HasZoneBoundary),
                        ZoneNumbers = zoneNumbers,
                        LeafIndices = leafIndices,
                        PolygonCount = polygonsOfKind.Length,
                        TriangleCount = triangleCount,
                        SurfaceCount = parts.Sum(x => x.SurfaceCount),
                        BoundsMin = boundsMin,
                        BoundsMax = boundsMax,
                        MeshParts = parts
                    });
                }
            }
        }

        return chunks
            .OrderByDescending(x => x.PolygonCount)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select((x, i) => new BspDiagnosticChunk
            {
                ChunkIndex = i,
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

    private static List<List<RoomPolygonFragment>> SplitIntoConnectedComponents(List<RoomPolygonFragment> polygons)
    {
        var components = new List<List<RoomPolygonFragment>>();
        var visited = new bool[polygons.Count];

        for (var i = 0; i < polygons.Count; i++)
        {
            if (visited[i])
            {
                continue;
            }

            var component = new List<RoomPolygonFragment>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            visited[i] = true;

            while (queue.Count > 0)
            {
                var currentIndex = queue.Dequeue();
                var current = polygons[currentIndex];
                component.Add(current);

                for (var nextIndex = 0; nextIndex < polygons.Count; nextIndex++)
                {
                    if (visited[nextIndex])
                    {
                        continue;
                    }

                    if (!SharesEdge(current.Vertices, polygons[nextIndex].Vertices))
                    {
                        continue;
                    }

                    visited[nextIndex] = true;
                    queue.Enqueue(nextIndex);
                }
            }

            components.Add(component);
        }

        return components;
    }

    private static BspDiagnosticMeshPart[] BuildMeshParts(IEnumerable<RoomPolygonFragment> polygons, float maxMergeAngleDegrees)
    {
        var cosineThreshold = MathF.Cos(maxMergeAngleDegrees * MathF.PI / 180f);
        var parts = new List<BspDiagnosticMeshPart>();
        var materialGroups = polygons
            .GroupBy(x => new MaterialGroupingKey(x.MaterialRawReference, x.MaterialPackageName, x.MaterialObjectName, x.PolyFlags))
            .OrderBy(x => x.Key.MaterialRawReference)
            .ThenBy(x => x.Key.MaterialPackageName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Key.MaterialObjectName, StringComparer.OrdinalIgnoreCase);

        foreach (var materialGroup in materialGroups)
        {
            var groupPolygons = materialGroup.ToArray();
            var visited = new bool[groupPolygons.Length];
            var clusterIndex = 0;

            for (var i = 0; i < groupPolygons.Length; i++)
            {
                if (visited[i])
                {
                    continue;
                }

                clusterIndex++;
                var accumulator = new MeshAccumulator(
                    BuildMeshPartName(materialGroup.Key.MaterialRawReference, materialGroup.Key.PolyFlags, clusterIndex),
                    materialGroup.Key.MaterialRawReference,
                    materialGroup.Key.MaterialPackageName,
                    materialGroup.Key.MaterialObjectName,
                    materialGroup.Key.PolyFlags,
                    groupPolygons[i].ColorArgb);

                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;

                while (queue.Count > 0)
                {
                    var currentIndex = queue.Dequeue();
                    var current = groupPolygons[currentIndex];
                    accumulator.AddPolygon(current);

                    for (var nextIndex = 0; nextIndex < groupPolygons.Length; nextIndex++)
                    {
                        if (visited[nextIndex])
                        {
                            continue;
                        }

                        var candidate = groupPolygons[nextIndex];
                        if (!CanMergeIntoSameMesh(current, candidate, cosineThreshold))
                        {
                            continue;
                        }

                        visited[nextIndex] = true;
                        queue.Enqueue(nextIndex);
                    }
                }

                parts.Add(accumulator.ToDto());
            }
        }

        return parts
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool CanMergeIntoSameMesh(RoomPolygonFragment left, RoomPolygonFragment right, float cosineThreshold)
    {
        return Vector3.Dot(left.Normal, right.Normal) >= cosineThreshold &&
               SharesEdge(left.Vertices, right.Vertices);
    }

    private static bool SharesEdge(Vector3[] left, Vector3[] right)
    {
        var sharedVertexCount = 0;
        for (var i = 0; i < left.Length; i++)
        {
            for (var j = 0; j < right.Length; j++)
            {
                if (Vector3.DistanceSquared(left[i], right[j]) > SharedVertexEpsilonSquared)
                {
                    continue;
                }

                sharedVertexCount++;
                break;
            }
        }

        return sharedVertexCount >= 2;
    }

    private SceneBspScene Convert(BspDiagnosticScene source)
    {
        var materialLookup = _materialResolver is null
            ? new Dictionary<string, ResolvedMaterialGraph?>(StringComparer.OrdinalIgnoreCase)
            : _materialResolver.ResolveMany(
                source.SourcePath,
                source.Models
                    .SelectMany(x => x.Chunks)
                    .SelectMany(x => x.MeshParts)
                    .Where(x => !string.IsNullOrWhiteSpace(x.MaterialObjectName))
                    .Select(x => new SceneMaterialRequest(x.MaterialPackageName, x.MaterialObjectName!)));
        var directTextureLookup = _textureManager is null
            ? new Dictionary<string, BspTextureManager.ResolvedTexture>(StringComparer.OrdinalIgnoreCase)
            : _textureManager.ResolveMany(
                source.Models
                    .SelectMany(x => x.Chunks)
                    .SelectMany(x => x.MeshParts)
                    .Where(x => !string.IsNullOrWhiteSpace(x.MaterialPackageName) && !string.IsNullOrWhiteSpace(x.MaterialObjectName))
                    .Select(x => new SceneTextureRequest(x.MaterialPackageName!, x.MaterialObjectName!)));

        return new SceneBspScene
        {
            SourcePath = source.SourcePath,
            WorldBoundsMin = source.BoundsMin,
            WorldBoundsMax = source.BoundsMax,
            Models = source.Models.Select(x => Convert(source.SourcePath, x, materialLookup, directTextureLookup)).ToArray()
        };
    }

    private SceneBspModel Convert(
        string mapPath,
        BspDiagnosticModel source,
        IReadOnlyDictionary<string, ResolvedMaterialGraph?> materialLookup,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> directTextureLookup)
    {
        return new SceneBspModel
        {
            ExportIndex = source.ExportIndex,
            Name = source.Name,
            StableName = SceneStableNameUtility.BuildActorStableName(mapPath,source),
            IsWorldModel = source.IsWorldModel,
            NodeCount = source.NodeCount,
            PolygonCount = source.PolygonCount,
            TriangleCount = source.TriangleCount,
            WorldBoundsMin = source.BoundsMin,
            WorldBoundsMax = source.BoundsMax,
            Chunks = source.Chunks.Select(x => Convert(mapPath, source, x, materialLookup, directTextureLookup)).ToArray()
        };
    }

    private SceneBspChunk Convert(
        string mapPath,
        BspDiagnosticModel model,
        BspDiagnosticChunk source,
        IReadOnlyDictionary<string, ResolvedMaterialGraph?> materialLookup,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> directTextureLookup)
    {
        var chunkStableName = SceneStableNameUtility.BuildChunkStableName(source);
        return new SceneBspChunk
        {
            ChunkIndex = source.ChunkIndex,
            Name = source.Name,
            StableName = chunkStableName,
            Kind = source.Kind,
            IsPortalLike = source.IsPortalLike,
            IsInvisibleLike = source.IsInvisibleLike,
            HasZoneBoundary = source.HasZoneBoundary,
            ZoneNumbers = source.ZoneNumbers,
            LeafIndices = source.LeafIndices,
            PolygonCount = source.PolygonCount,
            TriangleCount = source.TriangleCount,
            SurfaceCount = source.SurfaceCount,
            WorldBoundsMin = source.BoundsMin,
            WorldBoundsMax = source.BoundsMax,
            MeshSections = source.MeshParts.Select(x => Convert(mapPath, chunkStableName, x, materialLookup, directTextureLookup)).ToArray()
        };
    }

    private SceneBspMeshSection Convert(
        string mapPath,
        string chunkStableName,
        BspDiagnosticMeshPart source,
        IReadOnlyDictionary<string, ResolvedMaterialGraph?> materialLookup,
        IReadOnlyDictionary<string, BspTextureManager.ResolvedTexture> directTextureLookup)
    {
        var materialReference = string.IsNullOrWhiteSpace(source.MaterialObjectName)
            ? $"<null>.MaterialRaw_{source.MaterialRawReference}"
            : SceneReferenceUtilities.BuildReference(mapPath, source.MaterialPackageName, source.MaterialObjectName);
        var material = _materialResolver is null || string.IsNullOrWhiteSpace(source.MaterialObjectName)
            ? null
            : materialLookup.GetValueOrDefault(materialReference);
        var materialResource = material is null ? null : BuildMaterialResource(material);
        var directTexture = _textureManager is not null &&
                            material is null &&
                            !string.IsNullOrWhiteSpace(source.MaterialPackageName) &&
                            !string.IsNullOrWhiteSpace(source.MaterialObjectName)
            ? directTextureLookup.GetValueOrDefault($"{source.MaterialPackageName}.{source.MaterialObjectName}")
            : null;
        var orderedTextureSlots = material is null
            ? []
            : MaterialTextureSlotOrdering.OrderTextureSlots(material.TextureSlots);
        var textureReferences = orderedTextureSlots
            .Select(x => x.Reference)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (textureReferences.Length == 0 && directTexture is not null)
        {
            textureReferences = [SceneReferenceUtilities.BuildReference(mapPath, source.MaterialPackageName, source.MaterialObjectName!)];
        }

        var textureResources = orderedTextureSlots
            .Where(x => !string.IsNullOrWhiteSpace(x.PackagePath))
            .GroupBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
            .Select(x => SceneReferenceUtilities.BuildResourceLocation(
                _clientRoot!,
                x.First().PackagePath!,
                x.First().PackageName,
                x.First().ObjectName,
                x.First().ClassName))
            .ToArray();
        if (textureResources.Length == 0 && directTexture is not null)
        {
            textureResources = [directTexture.Resource];
        }

        return new SceneBspMeshSection
        {
            Name = source.Name,
            StableName = SceneStableNameUtility.BuildSectionStableName(chunkStableName, source),
            SurfaceCount = source.SurfaceCount,
            MaterialRawReference = source.MaterialRawReference,
            MaterialReference = materialReference,
            MaterialResource = materialResource,
            MaterialPackageName = source.MaterialPackageName,
            MaterialObjectName = source.MaterialObjectName,
            PrimaryTextureReference = textureReferences.FirstOrDefault(),
            PrimaryTextureResource = textureResources.FirstOrDefault(),
            TextureReferences = textureReferences,
            TextureResources = textureResources,
            Material = material,
            PolyFlags = source.PolyFlags,
            KnownPolyFlags = source.KnownPolyFlags,
            UnknownPolyFlagsMask = source.UnknownPolyFlagsMask,
            PolyFlagNames = source.PolyFlagNames,
            ColorArgb = source.ColorArgb,
            Positions = source.Positions,
            Normals = source.Normals,
            TextureCoordinates = source.TextureCoordinates,
            Indices = source.Indices
        };
    }

    private SceneResourceLocation BuildMaterialResource(ResolvedMaterialGraph material)
    {
        if (string.IsNullOrWhiteSpace(_clientRoot))
        {
            throw new PackageReadException("Material resource resolution requires a client root.");
        }

        var rootNode = material.Nodes.FirstOrDefault(x => string.Equals(x.Reference, material.RootReference, StringComparison.OrdinalIgnoreCase))
            ?? material.Nodes.FirstOrDefault()
            ?? throw new PackageReadException($"Material '{material.RootReference}' has no graph nodes.");
        if (string.IsNullOrWhiteSpace(rootNode.PackagePath))
        {
            throw new PackageReadException($"Material '{material.RootReference}' has no package path.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            _clientRoot,
            rootNode.PackagePath,
            material.RootPackageName,
            material.RootObjectName,
            material.RootClassName);
    }

    private static string BuildMeshPartName(int materialRawReference, uint polyFlags, int clusterIndex)
    {
        var baseName = materialRawReference == 0 ? $"SurfaceGroup_{polyFlags:X8}" : $"Material_{materialRawReference}";
        return $"{baseName}_Cluster_{clusterIndex:D2}";
    }

    private static int GetKindOrder(string kind)
    {
        return kind switch
        {
            "Solid" => 0,
            "Portal" => 1,
            "Invisible" => 2,
            "PortalInvisible" => 3,
            _ => 10
        };
    }

    private static uint CreateColor(int materialRawReference, int colorSeed)
    {
        var seed = materialRawReference == 0 ? colorSeed + 1 : Math.Abs(materialRawReference);
        var hue = (seed * 0.61803398875f) % 1f;
        var rgb = HsvToRgb(hue, 0.62f, 0.92f);
        return 0xFF000000u | ((uint)rgb.r << 16) | ((uint)rgb.g << 8) | rgb.b;
    }

    private static (byte r, byte g, byte b) HsvToRgb(float h, float s, float v)
    {
        var i = (int)MathF.Floor(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        var (r, g, b) = (i % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };

        return ((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f));
    }

    private static List<Vector3> RemoveSequentialDuplicates(List<Vector3> polygon)
    {
        var result = new List<Vector3>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            if (i > 0 && Vector3.DistanceSquared(polygon[i - 1], polygon[i]) < 0.0001f)
            {
                continue;
            }

            result.Add(polygon[i]);
        }

        if (result.Count > 1 && Vector3.DistanceSquared(result[0], result[^1]) < 0.0001f)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    private static bool IsDegenerateTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        var cross = Vector3.Cross(b - a, c - a);
        return cross.LengthSquared() < 0.0001f;
    }

    private static Vector3 ComputePolygonNormal(List<Vector3> polygon)
    {
        var normal = Vector3.Zero;
        for (var i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Count];
            normal.X += (current.Y - next.Y) * (current.Z + next.Z);
            normal.Y += (current.Z - next.Z) * (current.X + next.X);
            normal.Z += (current.X - next.X) * (current.Y + next.Y);
        }

        return SafeNormalize(normal);
    }

    private static Vector3 SafeNormalize(Vector3 value)
    {
        if (value.LengthSquared() < 0.000001f)
        {
            return Vector3.UnitZ;
        }

        return Vector3.Normalize(value);
    }

    private sealed class MeshAccumulator
    {
        private readonly List<Vector3> _positions = [];
        private readonly List<Vector3> _normals = [];
        private readonly List<Vector2> _textureCoordinates = [];
        private readonly List<int> _indices = [];

        public MeshAccumulator(
            string name,
            int materialRawReference,
            string? materialPackageName,
            string? materialObjectName,
            uint polyFlags,
            uint colorArgb)
        {
            Name = name;
            MaterialRawReference = materialRawReference;
            MaterialPackageName = materialPackageName;
            MaterialObjectName = materialObjectName;
            PolyFlags = polyFlags;
            ColorArgb = colorArgb;
        }

        public string Name { get; }
        public int MaterialRawReference { get; }
        public string? MaterialPackageName { get; }
        public string? MaterialObjectName { get; }
        public uint PolyFlags { get; }
        public uint ColorArgb { get; }
        public int SurfaceCount { get; private set; }

        public void AddPolygon(RoomPolygonFragment polygon)
        {
            foreach (var triangle in polygon.Triangles)
            {
                var baseIndex = _positions.Count;
                _positions.Add(triangle.A);
                _positions.Add(triangle.B);
                _positions.Add(triangle.C);
                _normals.Add(polygon.Normal);
                _normals.Add(polygon.Normal);
                _normals.Add(polygon.Normal);
                _textureCoordinates.Add(triangle.UvA);
                _textureCoordinates.Add(triangle.UvB);
                _textureCoordinates.Add(triangle.UvC);
                _indices.Add(baseIndex);
                _indices.Add(baseIndex + 1);
                _indices.Add(baseIndex + 2);
            }

            SurfaceCount++;
        }

        public BspDiagnosticMeshPart ToDto()
        {
            return new BspDiagnosticMeshPart
            {
                Name = Name,
                SurfaceCount = SurfaceCount,
                MaterialRawReference = MaterialRawReference,
                MaterialPackageName = MaterialPackageName,
                MaterialObjectName = MaterialObjectName,
                PolyFlags = PolyFlags,
                KnownPolyFlags = UnrPolyFlagsInfo.GetKnownFlags(PolyFlags),
                UnknownPolyFlagsMask = UnrPolyFlagsInfo.GetUnknownBits(PolyFlags),
                PolyFlagNames = UnrPolyFlagsInfo.GetNames(PolyFlags),
                ColorArgb = ColorArgb,
                Positions = _positions.ToArray(),
                Normals = _normals.ToArray(),
                TextureCoordinates = _textureCoordinates.ToArray(),
                Indices = _indices.ToArray()
            };
        }
    }

    private readonly record struct MaterialGroupingKey(
        int MaterialRawReference,
        string? MaterialPackageName,
        string? MaterialObjectName,
        uint PolyFlags);

    private readonly record struct RoomTriangleFragment(
        Vector3 A,
        Vector3 B,
        Vector3 C,
        Vector2 UvA,
        Vector2 UvB,
        Vector2 UvC);

    private sealed record RoomPolygonFragment(
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
        RoomTriangleFragment[] Triangles)
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
}
