using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.BSPServices;

namespace L2Viewer.SceneDomain.Services;

[ForExternalUse]
public sealed class SceneWorldAreaCatalogBuilder
{
    private static readonly ConcurrentDictionary<string, ParsedServerTeleportCatalog> ServerTeleportCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex HuntingGroundTeleportRegex = new(
        "SevenSigns\\s+11\\s+(?<x>-?\\d+)\\s+(?<y>-?\\d+)\\s+(?<z>-?\\d+)\\s+\\d+\"\\s+msg=\"[^\"]*;(?<name>[^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AdminTeleportRegex = new(
        "admin_move_to\\s+(?<x>-?\\d+)\\s+(?<y>-?\\d+)\\s+(?<z>-?\\d+)[^\"]*\"[^>]*>(?:<font[^>]*>)?(?<name>[^<]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlTitleRegex = new(
        "<font[^>]*>(?<title>[^<]+)</font>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SceneWorldAreaCatalogData Build(string clientRootPath, string dataRootPath, string quadrant)
    {
        return BuildCore(clientRootPath, dataRootPath, quadrant, worldBoundsMin: null, worldBoundsMax: null);
    }

    public SceneWorldAreaCatalogData Build(
        string clientRootPath,
        string dataRootPath,
        string quadrant,
        L2Viewer.UnrFile.UnrFile unr)
    {
        if (unr == null)
        {
            throw new ArgumentNullException(nameof(unr));
        }

        var worldModel = BspWorldModelPolicy.ResolvePreferredWorldModel(unr)
            ?? throw new InvalidOperationException($"World model not found in: {Path.GetFileName(unr.FilePath)}");
        var (min, max) = ComputeWorldBoundsFromPoints(worldModel);
        return BuildCore(clientRootPath, dataRootPath, quadrant, min, max);
    }

    private static SceneWorldAreaCatalogData BuildCore(
        string clientRootPath,
        string dataRootPath,
        string quadrant,
        Vector3? worldBoundsMin,
        Vector3? worldBoundsMax)
    {
        if (string.IsNullOrWhiteSpace(clientRootPath))
        {
            throw new ArgumentException("Client root path is required.", nameof(clientRootPath));
        }

        if (string.IsNullOrWhiteSpace(dataRootPath))
        {
            throw new ArgumentException("Data root path is required.", nameof(dataRootPath));
        }

        if (string.IsNullOrWhiteSpace(quadrant))
        {
            throw new ArgumentException("Quadrant is required.", nameof(quadrant));
        }

        var clientRoot = NormalizeClientRoot(clientRootPath);
        var dataRoot = NormalizeDataRoot(dataRootPath);
        var systemRoot = Path.Combine(clientRoot, "system");
        var zonename = DatFileReader.ReadDocument<ZoneNameDatDocument>(Path.Combine(systemRoot, "zonename-e.dat"));
        var huntingzone = DatFileReader.ReadDocument<HuntingZoneDatDocument>(Path.Combine(systemRoot, "huntingzone-e.dat"));
        var teleports = ServerTeleportCache.GetOrAdd(dataRoot, ParseServerTeleportCatalog);

        var areas = zonename.Entries
            .Select(BuildAreaBounds)
            .Where(x => x is not null)
            .Cast<SceneWorldAreaBoundsData>()
            .Where(x => IntersectsWorldBounds(x.BoundsMin, x.BoundsMax, worldBoundsMin, worldBoundsMax))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.AreaId)
            .ToArray();

        var interestPoints = huntingzone.Entries
            .Select(BuildInterestPoint)
            .Where(x => IsInsideWorldBounds(x.Position, worldBoundsMin, worldBoundsMax))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PointId)
            .ToArray();

        var teleportPoints = teleports.Points
            .Where(x => IsInsideWorldBounds(x.Position, worldBoundsMin, worldBoundsMax))
            .OrderBy(x => x.SourceGroup, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SceneWorldAreaCatalogData
        {
            Quadrant = Path.GetFileNameWithoutExtension(quadrant.Trim()),
            Areas = areas,
            InterestPoints = interestPoints,
            TeleportPoints = teleportPoints
        };
    }

    private static SceneWorldAreaBoundsData? BuildAreaBounds(ZoneNameDatEntry entry)
    {
        if (entry.Coordinates.Count < 6)
        {
            return null;
        }

        var miniMapX = entry.Coordinates[0];
        var miniMapY = entry.Coordinates[1];
        var worldX = entry.Coordinates[2];
        var worldY = entry.Coordinates[3];
        var sizeX = entry.Coordinates[4];
        var sizeY = entry.Coordinates[5];

        if (worldX < 0 || worldY < 0 || sizeX <= 0 || sizeY <= 0)
        {
            return null;
        }

        var halfSizeX = sizeX / 2f;
        var halfSizeY = sizeY / 2f;
        var min = new Vector3(worldX - halfSizeX, worldY - halfSizeY, entry.BottomZ);
        var max = new Vector3(worldX + halfSizeX, worldY + halfSizeY, entry.TopZ);
        var center = new Vector3(worldX, worldY, (entry.TopZ + entry.BottomZ) / 2f);

        return new SceneWorldAreaBoundsData
        {
            AreaId = entry.Id,
            Name = entry.ZoneName,
            BoundsMin = min,
            BoundsMax = max,
            Center = center,
            MiniMapX = miniMapX,
            MiniMapY = miniMapY,
            WorldX = worldX,
            WorldY = worldY,
            SizeX = sizeX,
            SizeY = sizeY,
            Map = entry.Map
        };
    }

    private static SceneWorldInterestPointData BuildInterestPoint(HuntingZoneDatEntry entry)
    {
        return new SceneWorldInterestPointData
        {
            PointId = entry.Id,
            Name = entry.Name,
            Position = new Vector3(entry.LocationX, entry.LocationY, entry.LocationZ),
            AffiliatedAreaId = entry.AffiliatedAreaId == 0 ? null : entry.AffiliatedAreaId,
            Extra = entry.Extra,
            SourceKind = SceneWorldPointSourceKind.HuntingZoneDat,
            SourceGroup = entry.AffiliatedAreaId == 0 ? string.Empty : $"Area_{entry.AffiliatedAreaId}"
        };
    }

    private static ParsedServerTeleportCatalog ParseServerTeleportCatalog(string dataRoot)
    {
        var points = new List<SceneWorldTeleportPointData>();
        var huntingRoot = Path.Combine(dataRoot, "jscript", "teleports", "2211_HuntingGroundsTeleport");
        if (Directory.Exists(huntingRoot))
        {
            foreach (var file in Directory.EnumerateFiles(huntingRoot, "hg_*.htm", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(file);
                if (string.Equals(fileName, "hg_wrong.htm", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var html = File.ReadAllText(file);
                foreach (Match match in HuntingGroundTeleportRegex.Matches(html))
                {
                    points.Add(new SceneWorldTeleportPointData
                    {
                        Name = CleanText(match.Groups["name"].Value),
                        Position = new Vector3(
                            ParseInt(match.Groups["x"].Value),
                            ParseInt(match.Groups["y"].Value),
                            ParseInt(match.Groups["z"].Value)),
                        SourceKind = SceneWorldPointSourceKind.HuntingGroundTeleportHtml,
                        SourceGroup = Path.GetFileNameWithoutExtension(fileName),
                        SourcePageTitle = "Hunting Grounds",
                        SourceFileRelativePath = Path.GetRelativePath(dataRoot, file).Replace('\\', '/')
                    });
                }
            }
        }

        var adminTeleRoot = Path.Combine(dataRoot, "html", "admin", "tele");
        if (Directory.Exists(adminTeleRoot))
        {
            foreach (var file in Directory.EnumerateFiles(adminTeleRoot, "*.htm", SearchOption.AllDirectories))
            {
                var html = File.ReadAllText(file);
                var matches = AdminTeleportRegex.Matches(html);
                if (matches.Count == 0)
                {
                    continue;
                }

                var relative = Path.GetRelativePath(adminTeleRoot, file).Replace('\\', '/');
                var pageTitle = ResolveHtmlTitle(html, file);
                var group = Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? string.Empty;
                foreach (Match match in matches)
                {
                    points.Add(new SceneWorldTeleportPointData
                    {
                        Name = CleanText(match.Groups["name"].Value),
                        Position = new Vector3(
                            ParseInt(match.Groups["x"].Value),
                            ParseInt(match.Groups["y"].Value),
                            ParseInt(match.Groups["z"].Value)),
                        SourceKind = SceneWorldPointSourceKind.AdminTeleportHtml,
                        SourceGroup = string.IsNullOrWhiteSpace(group) ? "admin/tele" : group,
                        SourcePageTitle = pageTitle,
                        SourceFileRelativePath = Path.GetRelativePath(dataRoot, file).Replace('\\', '/')
                    });
                }
            }
        }

        return new ParsedServerTeleportCatalog(points
            .GroupBy(x => $"{x.SourceKind}|{x.SourceFileRelativePath}|{x.Name}|{x.Position.X}|{x.Position.Y}|{x.Position.Z}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray());
    }

    private static string ResolveHtmlTitle(string html, string filePath)
    {
        var titleMatch = HtmlTitleRegex.Matches(html)
            .Cast<Match>()
            .Select(x => CleanText(x.Groups["title"].Value))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return string.IsNullOrWhiteSpace(titleMatch)
            ? Path.GetFileNameWithoutExtension(filePath)
            : titleMatch;
    }

    private static string CleanText(string text)
    {
        return text
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static int ParseInt(string value)
    {
        return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static bool IntersectsWorldBounds(
        Vector3 boundsMin,
        Vector3 boundsMax,
        Vector3? worldBoundsMin,
        Vector3? worldBoundsMax)
    {
        if (!worldBoundsMin.HasValue || !worldBoundsMax.HasValue)
        {
            return true;
        }

        var min = Vector3.Min(worldBoundsMin.Value, worldBoundsMax.Value);
        var max = Vector3.Max(worldBoundsMin.Value, worldBoundsMax.Value);
        return boundsMax.X >= min.X &&
               boundsMin.X <= max.X &&
               boundsMax.Y >= min.Y &&
               boundsMin.Y <= max.Y;
    }

    private static bool IsInsideWorldBounds(Vector3 position, Vector3? worldBoundsMin, Vector3? worldBoundsMax)
    {
        if (!worldBoundsMin.HasValue || !worldBoundsMax.HasValue)
        {
            return true;
        }

        var min = Vector3.Min(worldBoundsMin.Value, worldBoundsMax.Value);
        var max = Vector3.Max(worldBoundsMin.Value, worldBoundsMax.Value);
        return position.X >= min.X &&
               position.X <= max.X &&
               position.Y >= min.Y &&
               position.Y <= max.Y;
    }

    private static (Vector3 Min, Vector3 Max) ComputeWorldBoundsFromPoints(UnrModelObject model)
    {
        if (model.Points.Length == 0)
        {
            return (Vector3.Zero, Vector3.Zero);
        }

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var point in model.Points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        return (min, max);
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

    private static string NormalizeDataRoot(string dataRootPath)
    {
        var fullPath = Path.GetFullPath(dataRootPath);
        if (File.Exists(fullPath))
        {
            return Path.GetDirectoryName(fullPath)
                   ?? throw new DirectoryNotFoundException($"Unable to determine data root for '{dataRootPath}'.");
        }

        if (Directory.Exists(Path.Combine(fullPath, "jscript")) || Directory.Exists(Path.Combine(fullPath, "html")))
        {
            return fullPath;
        }

        var candidate = Path.Combine(fullPath, "data");
        if (Directory.Exists(Path.Combine(candidate, "jscript")) || Directory.Exists(Path.Combine(candidate, "html")))
        {
            return candidate;
        }

        return fullPath;
    }

    private sealed record ParsedServerTeleportCatalog(IReadOnlyList<SceneWorldTeleportPointData> Points);
}
