using L2Viewer.SceneDomain.Models;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDomain.Services;

public static class SceneStableNameUtility
{
    public static string BuildActorStableName(
        string mapPath,
         UnrActorBaseObject actor)
    {
        return BuildActorStableName(prefix: null, mapPath, actor.ObjectName, actor.ClassName, actor.ExportIndex);
    }

    public static string BuildActorStableName(
        UnrFile.UnrFile unr,
        UnrActorBaseObject actor)
    {
        return BuildActorStableName(prefix: null, unr.FilePath, actor.ObjectName, actor.ClassName, actor.ExportIndex);
    }

    public static string BuildActorStableName(
        string prefix,
        UnrFile.UnrFile unr,
        SceneParticleBuilder.SceneParticleLayerIdentity actor)
    {
        return BuildActorStableName(prefix, unr.FilePath, actorName: null, className: null, actor.ExportIndex);
    }
    
    public static string BuildActorStableName(
        string mapPath,
        BspDiagnosticModel model)
    {
        return BuildActorStableName("BSPDia", mapPath, actorName: null, className: null, model.ExportIndex);
    }

    public static string BuildModelStableName(BspDiagnosticModel source)
    {
        return $"Model_{source.ExportIndex:D6}_{SanitizeStableName(source.Name)}";
    }

    public static string BuildChunkStableName(BspDiagnosticChunk source)
    {
        return $"Chunk_{SanitizeStableName(source.Name)}";
    }

    public static string BuildSectionStableName(string chunkStableName, BspDiagnosticMeshPart source)
    {
        var partName = SanitizeStableName(source.Name);
        var flagLabel = BuildPolyFlagLabel(source.KnownPolyFlags, source.UnknownPolyFlagsMask);
        return string.IsNullOrEmpty(flagLabel)
            ? $"Section_{partName}"
            : $"Section_{partName}_{flagLabel}";
    }

    public static string BuildCreatureStableName(
        string mapKey,
        string? spawnLocationKey,
        int spawnId,
        int templateId,
        string? displayName)
    {
        var normalizedMapKey = SanitizeStableName(Path.GetFileNameWithoutExtension(mapKey));
        var normalizedLocation = SanitizeStableName(spawnLocationKey);
        var baseName = $"Creature_{normalizedMapKey}_{normalizedLocation}_{spawnId:D6}_{templateId:D6}";
        var label = SanitizeStableName(displayName);
        return string.Equals(label, "Unnamed", StringComparison.Ordinal)
            ? baseName
            : $"{baseName}_{label}";
    }

    private static string BuildActorStableName(
        string? prefix,
        string mapPath,
        string? actorName,
        string? className,
        int exportIndex)
    {
        var mapKey = SanitizeStableName(Path.GetFileNameWithoutExtension(mapPath));
        var sourceLabel = ResolveSourceLabel(actorName, className);
        var baseName = (prefix != null ? prefix + "_" : "") + $"{mapKey}_{exportIndex:D6}";
        return string.IsNullOrWhiteSpace(sourceLabel)
            ? baseName
            : $"{baseName}_{sourceLabel}";
    }

    private static string SanitizeStableName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unnamed";
        }

        var chars = value
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();
        var sanitized = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "Unnamed" : sanitized;
    }

    public static string ResolveSourceLabel(string? actorName, string? className)
    {
        var parts = new[]
            {
                className,
                actorName
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(SanitizeStableName)
            .ToArray();

        return parts.Length == 0
            ? string.Empty
            : string.Join("_", parts);
    }

    private static string BuildPolyFlagLabel(UnrPolyFlags knownFlags, uint unknownPolyFlagsMask)
    {
        var names = Enum.GetValues(typeof(UnrPolyFlags))
            .Cast<UnrPolyFlags>()
            .Where(x => x != UnrPolyFlags.None && knownFlags.HasFlag(x))
            .Select(x => x.ToString())
            .ToList();

        if (unknownPolyFlagsMask != 0)
        {
            for (var bit = 0; bit < 32; bit++)
            {
                var mask = 1u << bit;
                if ((unknownPolyFlagsMask & mask) != 0)
                {
                    names.Add($"0x{mask:X8}");
                }
            }
        }

        return string.Join("-", names.Select(SanitizeStableName));
    }
}
