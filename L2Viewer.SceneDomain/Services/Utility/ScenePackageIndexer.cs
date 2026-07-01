using System;
using System.Collections.Generic;
using System.IO;

namespace L2Viewer.SceneDomain.Services.Utility;

public static class ScenePackageIndexer
{
    public static IReadOnlyDictionary<string, string> BuildStaticMeshPackageIndex(string clientRoot)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "staticmeshes"), "*.usx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "maps"), "*.usx", recursive: true);
        IndexPackagesFromDirectory(result, clientRoot, "*.usx", recursive: false);

        return result;
    }

    public static IReadOnlyDictionary<string, string> BuildTexturePackageIndex(string clientRoot)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "textures"), "*.utx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "systextures"), "*.utx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "maps"), "*.utx", recursive: true);
        IndexPackagesFromDirectory(result, clientRoot, "*.utx", recursive: false);

        return result;
    }

    public static IReadOnlyDictionary<string, string> BuildResourcePackageIndex(string clientRoot)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "textures"), "*.utx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "systextures"), "*.utx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "staticmeshes"), "*.usx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "animations"), "*.ukx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "maps"), "*.utx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "maps"), "*.usx", recursive: true);
        IndexPackagesFromDirectory(result, Path.Combine(clientRoot, "system"), "*.u", recursive: true);
        IndexPackagesFromDirectory(result, clientRoot, "*.utx", recursive: false);
        IndexPackagesFromDirectory(result, clientRoot, "*.usx", recursive: false);
        IndexPackagesFromDirectory(result, clientRoot, "*.ukx", recursive: false);
        IndexPackagesFromDirectory(result, clientRoot, "*.u", recursive: false);
        return result;
    }

    private static void IndexPackagesFromDirectory(
        Dictionary<string, string> result,
        string root,
        string searchPattern,
        bool recursive)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var path in Directory.EnumerateFiles(root, searchPattern, searchOption))
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            if (!result.ContainsKey(stem))
            {
                result[stem] = path;
            }
        }
    }
}
