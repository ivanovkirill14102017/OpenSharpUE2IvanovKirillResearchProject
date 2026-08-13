using System.Numerics;
using L2Viewer.DatFile;
using L2Viewer.DbFile.DbJson;
using L2Viewer.DbFile.DbJson.Models;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

internal sealed class SceneSpawnVisualDataset
{
    private SceneSpawnVisualDataset(
        string dbRootPath,
        string clientRootPath,
        string quadrant,
        IReadOnlyList<SpawnlistRow> spawns,
        IReadOnlyDictionary<int, NpcRow> npcById,
        IReadOnlyDictionary<int, NpcNameDatEntry> npcNameById,
        IReadOnlyDictionary<int, NpcGrpDatEntry> npcVisualById,
        IReadOnlyDictionary<string, string> resourcePackageIndex)
    {
        DbRootPath = dbRootPath;
        ClientRootPath = clientRootPath;
        Quadrant = quadrant;
        Spawns = spawns;
        NpcById = npcById;
        NpcNameById = npcNameById;
        NpcVisualById = npcVisualById;
        ResourcePackageIndex = resourcePackageIndex;
    }

    public string DbRootPath { get; }
    public string ClientRootPath { get; }
    public string Quadrant { get; }
    public IReadOnlyList<SpawnlistRow> Spawns { get; }
    public IReadOnlyDictionary<int, NpcRow> NpcById { get; }
    public IReadOnlyDictionary<int, NpcNameDatEntry> NpcNameById { get; }
    public IReadOnlyDictionary<int, NpcGrpDatEntry> NpcVisualById { get; }
    public IReadOnlyDictionary<string, string> ResourcePackageIndex { get; }

    public static SceneSpawnVisualDataset Load(string dbRootPath, string clientRootPath, string quadrant)
    {
        return Load(dbRootPath, clientRootPath, quadrant, worldBoundsMin: null, worldBoundsMax: null);
    }

    public static SceneSpawnVisualDataset Load(
        string dbRootPath,
        string clientRootPath,
        string quadrant,
        Vector3? worldBoundsMin,
        Vector3? worldBoundsMax)
    {
        if (string.IsNullOrWhiteSpace(dbRootPath))
        {
            throw new ArgumentException("DB root path is required.", nameof(dbRootPath));
        }

        if (string.IsNullOrWhiteSpace(clientRootPath))
        {
            throw new ArgumentException("Client root path is required.", nameof(clientRootPath));
        }

        if (string.IsNullOrWhiteSpace(quadrant))
        {
            throw new ArgumentException("Quadrant is required.", nameof(quadrant));
        }

        var normalizedDbRoot = NormalizeDbRoot(dbRootPath);
        var normalizedClientRoot = NormalizeClientRoot(clientRootPath);
        var systemRoot = Path.Combine(normalizedClientRoot, "system");
        EnsureSystemDataExists(systemRoot, normalizedClientRoot);
        var allSpawns = TableJsonMapper.Read<SpawnlistRow>(Path.Combine(normalizedDbRoot, "spawnlist.json"));
        var filteredSpawns = FilterSpawns(allSpawns, quadrant, worldBoundsMin, worldBoundsMax)
            .Concat(LoadSupplementalVisualSpawns(normalizedDbRoot, worldBoundsMin, worldBoundsMax))
            .OrderBy(x => x.location, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.id)
            .ToArray();
        var npcById = TableJsonMapper.Read<NpcRow>(Path.Combine(normalizedDbRoot, "npc.json"))
            .ToDictionary(x => DecimalToInt32(x.id));
        var npcNameById = DatFileReader.ReadDocument<NpcNameDatDocument>(Path.Combine(systemRoot, "npcname-e.dat")).Entries
            .ToDictionary(x => checked((int)x.Id));
        var npcVisualById = DatFileReader.ReadDocument<NpcGrpDatDocument>(Path.Combine(systemRoot, "npcgrp.dat")).Entries
            .ToDictionary(x => checked((int)x.Tag));
        var resourcePackageIndex = ScenePackageIndexer.BuildResourcePackageIndex(normalizedClientRoot);

        return new SceneSpawnVisualDataset(
            normalizedDbRoot,
            normalizedClientRoot,
            Path.GetFileNameWithoutExtension(quadrant.Trim()),
            filteredSpawns,
            npcById,
            npcNameById,
            npcVisualById,
            resourcePackageIndex);
    }

    private static SpawnlistRow[] FilterSpawns(
        IReadOnlyList<SpawnlistRow> allSpawns,
        string quadrant,
        Vector3? worldBoundsMin,
        Vector3? worldBoundsMax)
    {
        var directMatches = allSpawns
            .Where(x => MatchesQuadrant(x.location, quadrant))
            .OrderBy(x => x.location, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.id)
            .ToArray();
        if (directMatches.Length > 0)
        {
            return directMatches;
        }

        if (worldBoundsMin.HasValue && worldBoundsMax.HasValue)
        {
            var heuristicMatches = allSpawns
                .Where(x => IsInsideWorldBounds(x, worldBoundsMin.Value, worldBoundsMax.Value))
                .OrderBy(x => x.location, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.id)
                .ToArray();
            if (heuristicMatches.Length > 0)
            {
                return heuristicMatches;
            }
        }

        return Array.Empty<SpawnlistRow>();
    }

    private static SpawnlistRow[] LoadSupplementalVisualSpawns(
        string normalizedDbRoot,
        Vector3? worldBoundsMin,
        Vector3? worldBoundsMax)
    {
        if (!worldBoundsMin.HasValue || !worldBoundsMax.HasValue)
        {
            return Array.Empty<SpawnlistRow>();
        }

        var results = new List<SpawnlistRow>();
        results.AddRange(LoadRandomSpawnRows(normalizedDbRoot));
        results.AddRange(LoadRaidBossSpawnRows(normalizedDbRoot));

        return results
            .Where(x => IsInsideWorldBounds(x, worldBoundsMin.Value, worldBoundsMax.Value))
            .OrderBy(x => x.location, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.id)
            .ToArray();
    }

    private static IEnumerable<SpawnlistRow> LoadRandomSpawnRows(string normalizedDbRoot)
    {
        var randomSpawnPath = Path.Combine(normalizedDbRoot, "random_spawn.json");
        var randomSpawnLocPath = Path.Combine(normalizedDbRoot, "random_spawn_loc.json");
        if (!File.Exists(randomSpawnPath) || !File.Exists(randomSpawnLocPath))
        {
            return Array.Empty<SpawnlistRow>();
        }

        var locationsByGroupId = TableJsonMapper.Read<RandomSpawnLocationRow>(randomSpawnLocPath)
            .GroupBy(x => x.groupId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var rows = new List<SpawnlistRow>();
        foreach (var spawn in TableJsonMapper.Read<RandomSpawnRow>(randomSpawnPath))
        {
            if (!locationsByGroupId.TryGetValue(spawn.groupId, out var locations) || locations.Length == 0)
            {
                continue;
            }

            for (var index = 0; index < locations.Length; index++)
            {
                var location = locations[index];
                rows.Add(new SpawnlistRow
                {
                    id = BuildRandomSpawnSyntheticId(spawn.groupId, index),
                    location = $"random_spawn_{spawn.groupId:D6}",
                    count = Math.Max(1, spawn.count),
                    npc_templateid = spawn.npcId,
                    locx = location.x,
                    locy = location.y,
                    locz = location.z,
                    randomx = 0,
                    randomy = 0,
                    heading = NormalizeHeading(location.heading),
                    respawn_delay = ClampToInt32(spawn.respawnDelay),
                    loc_id = 0,
                    periodOfDay = 0
                });
            }
        }

        return rows;
    }

    private static IEnumerable<SpawnlistRow> LoadRaidBossSpawnRows(string normalizedDbRoot)
    {
        var raidBossSpawnPath = Path.Combine(normalizedDbRoot, "raidboss_spawnlist.json");
        if (!File.Exists(raidBossSpawnPath))
        {
            return Array.Empty<SpawnlistRow>();
        }

        return TableJsonMapper.Read<RaidBossSpawnlistRow>(raidBossSpawnPath)
            .Select(x => new SpawnlistRow
            {
                id = BuildRaidBossSyntheticId(x.boss_id),
                location = "raidboss_spawnlist",
                count = Math.Max(1, x.amount),
                npc_templateid = x.boss_id,
                locx = x.loc_x,
                locy = x.loc_y,
                locz = x.loc_z,
                randomx = 0,
                randomy = 0,
                heading = NormalizeHeading(x.heading),
                respawn_delay = x.respawn_min_delay,
                loc_id = 0,
                periodOfDay = 0
            })
            .ToArray();
    }

    public NpcRow GetNpc(int npcId, int spawnId)
    {
        if (!NpcById.TryGetValue(npcId, out var npc))
        {
            throw new InvalidOperationException($"NPC template '{npcId}' from spawn '{spawnId}' was not found in npc.json.");
        }

        return npc;
    }

    public NpcGrpDatEntry GetVisual(int npcId, int spawnId)
    {
        if (!NpcVisualById.TryGetValue(npcId, out var visual))
        {
            throw new InvalidOperationException($"NPC visual '{npcId}' from spawn '{spawnId}' was not found in npcgrp.dat.");
        }

        return visual;
    }

    public NpcNameDatEntry GetName(int npcId, int spawnId)
    {
        if (!NpcNameById.TryGetValue(npcId, out var npcName))
        {
            throw new InvalidOperationException($"NPC display name '{npcId}' from spawn '{spawnId}' was not found in npcname-e.dat.");
        }

        return npcName;
    }

    public SceneResourceLocation GetResourceLocation(string reference, string className, int npcId, int spawnId)
    {
        var parsed = SceneReferenceUtilities.ParseFromDbResourceReference(reference);
        if (!ResourcePackageIndex.TryGetValue(parsed.PackageName, out var packagePath))
        {
            throw new InvalidOperationException(
                $"Resource package '{parsed.PackageName}' for {className} '{reference}' from NPC template '{npcId}' and spawn '{spawnId}' was not found under client root '{ClientRootPath}'.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            ClientRootPath,
            packagePath,
            parsed.PackageName,
            parsed.ObjectName,
            className);
    }

    public SceneResourceLocation[] GetResourceLocations(IEnumerable<string> references, string className, int npcId, int spawnId)
    {
        return references
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => GetResourceLocation(x, className, npcId, spawnId))
            .ToArray();
    }

    internal static bool MatchesQuadrant(string? spawnLocation, string quadrant)
    {
        if (string.IsNullOrWhiteSpace(spawnLocation))
        {
            return false;
        }

        var normalizedQuadrant = Path.GetFileNameWithoutExtension(quadrant.Trim());
        if (string.Equals(spawnLocation, normalizedQuadrant, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!spawnLocation.StartsWith(normalizedQuadrant, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return spawnLocation.Length == normalizedQuadrant.Length ||
               spawnLocation[normalizedQuadrant.Length] is '_' or '-';
    }

    internal static int DecimalToInt32(decimal value)
    {
        return decimal.ToInt32(decimal.Truncate(value));
    }

    internal static float DecimalToSingle(decimal value)
    {
        return (float)value;
    }

    private static string NormalizeDbRoot(string dbRootPath)
    {
        var normalizedPath = Path.GetFullPath(dbRootPath);

        if (File.Exists(dbRootPath))
        {
            return Path.GetDirectoryName(normalizedPath)
                   ?? throw new DirectoryNotFoundException($"Unable to determine DB root for '{dbRootPath}'.");
        }

        if (!Directory.Exists(normalizedPath))
        {
            return normalizedPath;
        }

        if (ContainsRequiredDbFiles(normalizedPath))
        {
            return normalizedPath;
        }

        var directCandidate = Path.Combine(normalizedPath, "InterludeDb");
        if (Directory.Exists(directCandidate) && ContainsRequiredDbFiles(directCandidate))
        {
            return directCandidate;
        }

        var nestedCandidate = Directory.EnumerateDirectories(normalizedPath, "*", SearchOption.TopDirectoryOnly)
            .Where(x => Path.GetFileName(x).IndexOf("interludedb", StringComparison.OrdinalIgnoreCase) >= 0)
            .FirstOrDefault(ContainsRequiredDbFiles);
        if (!string.IsNullOrWhiteSpace(nestedCandidate))
        {
            return nestedCandidate;
        }

        return normalizedPath;
    }

    private static bool ContainsRequiredDbFiles(string directoryPath)
    {
        return File.Exists(Path.Combine(directoryPath, "spawnlist.json")) &&
               File.Exists(Path.Combine(directoryPath, "npc.json"));
    }

    private static bool IsInsideWorldBounds(SpawnlistRow spawn, Vector3 worldBoundsMin, Vector3 worldBoundsMax)
    {
        var minX = MathF.Min(worldBoundsMin.X, worldBoundsMax.X);
        var maxX = MathF.Max(worldBoundsMin.X, worldBoundsMax.X);
        var minY = MathF.Min(worldBoundsMin.Y, worldBoundsMax.Y);
        var maxY = MathF.Max(worldBoundsMin.Y, worldBoundsMax.Y);
        return spawn.locx >= minX &&
               spawn.locx <= maxX &&
               spawn.locy >= minY &&
               spawn.locy <= maxY;
    }

    private static int BuildRandomSpawnSyntheticId(int groupId, int locationIndex)
    {
        return 900_000_000 + groupId * 1_000 + locationIndex;
    }

    private static int BuildRaidBossSyntheticId(int bossId)
    {
        return 910_000_000 + bossId;
    }

    private static int NormalizeHeading(int heading)
    {
        return heading < 0 ? 0 : heading;
    }

    private static int ClampToInt32(long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)value;
    }

    private static string NormalizeClientRoot(string clientRootPath)
    {
        if (File.Exists(clientRootPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(clientRootPath))
                   ?? throw new DirectoryNotFoundException($"Unable to determine client root for '{clientRootPath}'.");
        }

        return Path.GetFullPath(clientRootPath);
    }

    private static void EnsureSystemDataExists(string systemRoot, string clientRootPath)
    {
        var npcGrpPath = Path.Combine(systemRoot, "npcgrp.dat");
        var npcNamePath = Path.Combine(systemRoot, "npcname-e.dat");
        if (File.Exists(npcGrpPath) && File.Exists(npcNamePath))
        {
            return;
        }

        throw new DirectoryNotFoundException(
            $"Client root '{clientRootPath}' does not contain required system data. Expected '{npcGrpPath}' and '{npcNamePath}'.");
    }

    private sealed class RandomSpawnRow
    {
        public int groupId { get; set; }
        public int npcId { get; set; }
        public int count { get; set; }
        public long initialDelay { get; set; }
        public long respawnDelay { get; set; }
        public long despawnDelay { get; set; }
        public bool broadcastSpawn { get; set; }
        public bool randomSpawn { get; set; }
    }

    private sealed class RandomSpawnLocationRow
    {
        public int groupId { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
        public int heading { get; set; }
    }

    private sealed class RaidBossSpawnlistRow
    {
        public int boss_id { get; set; }
        public int amount { get; set; }
        public int loc_x { get; set; }
        public int loc_y { get; set; }
        public int loc_z { get; set; }
        public int heading { get; set; }
        public int respawn_min_delay { get; set; }
        public int respawn_max_delay { get; set; }
        public long respawn_time { get; set; }
        public decimal currentHp { get; set; }
        public decimal currentMp { get; set; }
    }
}
