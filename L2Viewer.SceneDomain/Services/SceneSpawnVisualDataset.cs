using L2Viewer.DatFile;
using L2Viewer.DbFile.DbJson;
using L2Viewer.DbFile.DbJson.Models;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

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
        var filteredSpawns = TableJsonMapper.Read<SpawnlistRow>(Path.Combine(normalizedDbRoot, "spawnlist.json"))
            .Where(x => MatchesQuadrant(x.location, quadrant))
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
        if (File.Exists(dbRootPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(dbRootPath))
                   ?? throw new DirectoryNotFoundException($"Unable to determine DB root for '{dbRootPath}'.");
        }

        return Path.GetFullPath(dbRootPath);
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
}
