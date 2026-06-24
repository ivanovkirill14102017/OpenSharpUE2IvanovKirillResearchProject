using L2Viewer.SceneDomain.Models;

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
        return $"Section_{chunkStableName}_{SanitizeStableName(source.Name)}_{string.Join("-", source.PolyFlagNames)}";
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
        return (prefix != null ? prefix + "_" : "") + $"{mapKey}_{exportIndex:D6}_{sourceLabel}";
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
        return string.Join("_", [SanitizeStableName(className), SanitizeStableName(actorName)]);
    }

}
