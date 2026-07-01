using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

[ForExternalUse]
public sealed class SceneCharacterArchetypeBuilder
{
    private static readonly string[] PrimarySlotOrder = ["Upper", "Lower", "Face", "Gloves", "Boots", "Hair"];
    private readonly SceneSkeletalMeshResolver _skeletalMeshResolver = new();

    public SceneCharacterArchetypeData[] Build(string clientRootPath)
    {
        if (string.IsNullOrWhiteSpace(clientRootPath))
        {
            throw new ArgumentException("Client root path is required.", nameof(clientRootPath));
        }

        var clientRoot = NormalizeClientRoot(clientRootPath);
        var systemRoot = Path.Combine(clientRoot, "system");
        var charGrpPath = Path.Combine(systemRoot, "chargrp.dat");
        if (!File.Exists(charGrpPath))
        {
            throw new FileNotFoundException($"Required DAT file was not found: '{charGrpPath}'.", charGrpPath);
        }

        var charGrp = DatFileReader.ReadDocument<CharGrpDatDocument>(charGrpPath);
        var packageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);

        return charGrp.Entries
            .Select((entry, index) => BuildArchetype(clientRoot, packageIndex, entry, index))
            .ToArray();
    }

    private SceneCharacterArchetypeData BuildArchetype(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        CharGrpDatEntry entry,
        int archetypeIndex)
    {
        var warnings = new List<string>();
        var slots = new[]
        {
            BuildSlot(clientRoot, packageIndex, "Hair", entry.Hair, Array.Empty<string>()),
            BuildSlot(clientRoot, packageIndex, "Face", entry.Face.Meshes, entry.Face.Textures),
            BuildSlot(clientRoot, packageIndex, "Gloves", entry.Gloves.Meshes, entry.Gloves.Textures),
            BuildSlot(clientRoot, packageIndex, "Upper", entry.Upper.Meshes, entry.Upper.Textures),
            BuildSlot(clientRoot, packageIndex, "Lower", entry.Lower.Meshes, entry.Lower.Textures),
            BuildSlot(clientRoot, packageIndex, "Boots", entry.Boots.Meshes, entry.Boots.Textures)
        };

        var primarySlot = PrimarySlotOrder
            .Select(name => slots.FirstOrDefault(x => string.Equals(x.SlotName, name, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(x => x is not null && x.MeshResources.Length > 0);

        var primaryResource = primarySlot?.MeshResources.FirstOrDefault();
        var primaryAsset = TryResolveSkeletalAsset(primaryResource, out var primaryError);
        var primaryBoneSignature = primaryAsset is null ? null : BuildBoneSignature(primaryAsset);
        var primarySkeleton = primaryAsset is not null
            ? BuildSkeletonSummary(primaryResource!, primaryAsset, matchesPrimary: true, primaryBoneSignature: null)
            : new SceneCharacterSkeletonSummary
            {
                MeshReference = primaryResource?.Reference,
                PackagePath = primaryResource?.PackagePath,
                MeshObjectName = primaryResource?.ObjectName,
                Resolved = false,
                MatchesArchetypePrimarySkeleton = false,
                FailureReason = primaryError ?? "No mesh resource."
            };
        if (!primarySkeleton.Resolved)
        {
            warnings.Add($"Primary skeletal mesh could not be resolved for archetype {archetypeIndex}: {primarySkeleton.FailureReason ?? "unknown error"}.");
        }

        var normalizedSlots = slots
            .Select(slot =>
            {
                var slotSkeleton = slot.MeshResources.Length == 0
                    ? new SceneCharacterSkeletonSummary
                    {
                        Resolved = false,
                        MatchesArchetypePrimarySkeleton = false,
                        FailureReason = "Slot has no skeletal meshes."
                    }
                    : ResolveSkeleton(
                        slot.MeshResources[0],
                        matchesPrimary: primarySkeleton.Resolved,
                        primaryBoneSignature: primaryBoneSignature);

                if (slot.MeshResources.Length > 0 && !slotSkeleton.Resolved)
                {
                    warnings.Add($"{slot.SlotName}: unable to resolve skeletal mesh '{slot.MeshResources[0].Reference}': {slotSkeleton.FailureReason ?? "unknown error"}.");
                }
                else if (slotSkeleton.Resolved && primarySkeleton.Resolved && !slotSkeleton.MatchesArchetypePrimarySkeleton)
                {
                    warnings.Add($"{slot.SlotName}: skeletal mesh '{slot.MeshResources[0].Reference}' does not match primary skeleton.");
                }

                return new SceneCharacterPartSlotData
                {
                    SlotName = slot.SlotName,
                    MeshResources = slot.MeshResources,
                    TextureResources = slot.TextureResources,
                    Skeleton = slotSkeleton
                };
            })
            .ToArray();

        return new SceneCharacterArchetypeData
        {
            ArchetypeIndex = archetypeIndex,
            ArchetypeKey = $"CharGrp_{archetypeIndex:D2}",
            RaceKey = null,
            GenderKey = null,
            ClassKey = null,
            Skeleton = primarySkeleton,
            Slots = normalizedSlots,
            Warnings = warnings
        };
    }

    private static SceneCharacterPartSlotData BuildSlot(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        string slotName,
        IEnumerable<string> meshReferences,
        IEnumerable<string> textureReferences)
    {
        return new SceneCharacterPartSlotData
        {
            SlotName = slotName,
            MeshResources = ResolveResources(clientRoot, packageIndex, meshReferences, UnrealClassNames.SkeletalMesh),
            TextureResources = ResolveResources(clientRoot, packageIndex, textureReferences, UnrealClassNames.Texture),
            Skeleton = new SceneCharacterSkeletonSummary
            {
                Resolved = false,
                MatchesArchetypePrimarySkeleton = false,
                FailureReason = "Skeleton resolution has not been evaluated yet."
            }
        };
    }

    private SceneCharacterSkeletonSummary ResolveSkeleton(
        SceneResourceLocation? meshResource,
        bool matchesPrimary,
        string? primaryBoneSignature = null)
    {
        if (meshResource is null)
        {
            return new SceneCharacterSkeletonSummary
            {
                Resolved = false,
                MatchesArchetypePrimarySkeleton = false,
                FailureReason = "No mesh resource."
            };
        }

        var asset = TryResolveSkeletalAsset(meshResource, out var error);
        if (asset is null)
        {
            return new SceneCharacterSkeletonSummary
            {
                MeshReference = meshResource.Reference,
                PackagePath = meshResource.PackagePath,
                MeshObjectName = meshResource.ObjectName,
                Resolved = false,
                MatchesArchetypePrimarySkeleton = false,
                FailureReason = error ?? "unknown error"
            };
        }

        return BuildSkeletonSummary(meshResource, asset, matchesPrimary, primaryBoneSignature);
    }

    private static string BuildBoneSignature(SceneSkeletalAsset asset)
    {
        return string.Join("|", asset.Skeleton.Bones.Select(x => $"{x.Index}:{x.ParentIndex}:{x.Name}"));
    }

    private static SceneCharacterSkeletonSummary BuildSkeletonSummary(
        SceneResourceLocation meshResource,
        SceneSkeletalAsset asset,
        bool matchesPrimary,
        string? primaryBoneSignature)
    {
        var rootBone = asset.Skeleton.Bones.FirstOrDefault(x => x.ParentIndex < 0 || x.IsRoot)?.Name
                       ?? asset.Skeleton.Bones.FirstOrDefault()?.Name;
        var signature = BuildBoneSignature(asset);
        return new SceneCharacterSkeletonSummary
        {
            MeshReference = meshResource.Reference,
            PackagePath = meshResource.PackagePath,
            MeshObjectName = asset.MeshObjectName,
            SkeletonName = asset.Skeleton.Name,
            RootBoneName = rootBone,
            BoneCount = asset.Skeleton.Bones.Count,
            Resolved = true,
            MatchesArchetypePrimarySkeleton = primaryBoneSignature is null ? matchesPrimary : string.Equals(signature, primaryBoneSignature, StringComparison.Ordinal),
            FailureReason = null
        };
    }

    private SceneSkeletalAsset? TryResolveSkeletalAsset(SceneResourceLocation? meshResource, out string? error)
    {
        error = null;
        if (meshResource is null)
        {
            error = "No mesh resource.";
            return null;
        }

        try
        {
            return _skeletalMeshResolver.ResolveAssetNamed(meshResource.PackagePath, meshResource.ObjectName);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private static SceneResourceLocation[] ResolveResources(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        IEnumerable<string> references,
        string className)
    {
        return references
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(reference => ResolveResource(clientRoot, packageIndex, reference, className))
            .Where(x => x is not null)
            .Cast<SceneResourceLocation>()
            .ToArray();
    }

    private static SceneResourceLocation? ResolveResource(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        string reference,
        string className)
    {
        var parsed = SceneReferenceUtilities.ParseFromDbResourceReference(reference);
        if (!packageIndex.TryGetValue(parsed.PackageName, out var packagePath))
        {
            return null;
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            packagePath,
            parsed.PackageName,
            parsed.ObjectName,
            className);
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
}
