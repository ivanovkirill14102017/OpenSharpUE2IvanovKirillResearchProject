using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

public sealed class SceneCreatureVisualResolver
{
    public SceneCreatureVisualData? Resolve(string clientRoot, string identifier)
    {
        if (string.IsNullOrWhiteSpace(clientRoot) || string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var npcGrpPath = Path.Combine(clientRoot, "system", "npcgrp.dat");
        if (!File.Exists(npcGrpPath))
        {
            throw new FileNotFoundException("npcgrp.dat was not found.", npcGrpPath);
        }

        var entries = DatFileReader.ReadDocument<NpcGrpDatDocument>(npcGrpPath).Entries;
        var candidates = FindCandidates(entries, identifier).ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        var distinctClasses = candidates.Select(x => x.Class).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (distinctClasses.Length > 1)
        {
            throw new InvalidOperationException(
                $"Creature identifier '{identifier}' matches multiple actor classes: {string.Join(", ", distinctClasses)}. Use the numeric NPC id or full actor class reference.");
        }

        var entry = candidates[0];
        var packageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);
        var actorClass = ResolveResource(clientRoot, packageIndex, entry.Class, "Class");
        var mesh = ResolveResource(clientRoot, packageIndex, entry.Mesh, "SkeletalMesh");
        var textures = entry.Textures1.Concat(entry.Textures2)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => ResolveResource(clientRoot, packageIndex, x, "Texture"))
            .ToArray();
        var effectResolver = new SceneCreatureClassEffectResolver(clientRoot, packageIndex);
        return new SceneCreatureVisualData
        {
            NpcId = (int)entry.Tag,
            ActorClassResource = actorClass,
            MeshResource = mesh,
            TextureResources = textures,
            AttachedEffects = effectResolver.Resolve(actorClass)
        };
    }

    private static IEnumerable<NpcGrpDatEntry> FindCandidates(IEnumerable<NpcGrpDatEntry> entries, string identifier)
    {
        var value = identifier.Trim();
        if (int.TryParse(value, out var npcId))
        {
            return entries.Where(x => x.Tag == npcId).Take(1);
        }

        return entries.Where(x =>
            ReferenceMatches(x.Class, value) ||
            ReferenceMatches(x.Mesh, value));
    }

    private static bool ReferenceMatches(string reference, string identifier)
    {
        if (reference.Equals(identifier, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var objectName = reference[(reference.LastIndexOf('.') + 1)..];
        return objectName.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
               objectName.Equals(StripKnownPrefix(identifier), StringComparison.OrdinalIgnoreCase);
    }

    private static string StripKnownPrefix(string value)
    {
        foreach (var prefix in new[] { "PF_", "NPC_", "SM_" })
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return value[prefix.Length..];
            }
        }
        return value;
    }

    private static SceneResourceLocation ResolveResource(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        string reference,
        string className)
    {
        var parsed = SceneReferenceUtilities.ParseFromDbResourceReference(reference);
        if (!packageIndex.TryGetValue(parsed.PackageName, out var packagePath))
        {
            throw new FileNotFoundException($"Package '{parsed.PackageName}' for '{reference}' was not found under '{clientRoot}'.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            packagePath,
            parsed.PackageName,
            parsed.ObjectName,
            className);
    }
}
