using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UkxFile;
using System.Text.RegularExpressions;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

[ForExternalUse]
public sealed class SceneCharacterAppearanceOptionsBuilder
{
    public SceneCharacterAppearanceOptionsData Build(
        string clientRootPath,
        SceneCharacterBaseClass baseClass,
        SceneCharacterGender gender)
    {
        if (string.IsNullOrWhiteSpace(clientRootPath))
        {
            throw new ArgumentException("Client root path is required.", nameof(clientRootPath));
        }

        var clientRoot = NormalizeClientRoot(clientRootPath);
        var visualFamily = SceneCharacterAppearanceBuilder.ResolveVisualFamily(baseClass, gender);
        var binding = SceneCharacterVisualFamilyBindings.Get(visualFamily);
        var charGrpPath = Path.Combine(clientRoot, "system", "chargrp.dat");
        var hairGrpPath = Path.Combine(clientRoot, "system", "hairgrp.dat");
        var packageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);
        var charGrp = DatFileReader.ReadDocument<CharGrpDatDocument>(charGrpPath);
        var hairGrp = File.Exists(hairGrpPath)
            ? DatFileReader.ReadDocument<HairGrpDatDocument>(hairGrpPath)
            : null;

        if (binding.CharGrpIndex < 0 || binding.CharGrpIndex >= charGrp.Entries.Count)
        {
            throw new InvalidOperationException($"chargrp entry '{binding.CharGrpIndex}' is not available for visual family '{visualFamily}'.");
        }

        var entry = charGrp.Entries[binding.CharGrpIndex];
        var faceOptions = entry.Face.Textures
            .Select((texture, index) => new SceneCharacterFaceOptionData
            {
                Id = index,
                MeshResources = BuildReferences(entry.Face.Meshes, UnrealClassNames.SkeletalMesh),
                TextureResources = BuildReferences([texture], UnrealClassNames.Texture)
            })
            .ToArray();

        var hairOptions = BuildHairOptions(clientRoot, packageIndex, binding.CharGrpIndex, entry, hairGrp);

        return new SceneCharacterAppearanceOptionsData
        {
            BaseClass = baseClass,
            Gender = gender,
            VisualFamily = visualFamily,
            CharGrpIndex = binding.CharGrpIndex,
            FaceOptions = faceOptions,
            HairStyleOptions = hairOptions
        };
    }

    private static IReadOnlyList<SceneCharacterHairStyleOptionData> BuildHairOptions(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        int charGrpIndex,
        CharGrpDatEntry charGrpEntry,
        HairGrpDatDocument? hairGrp)
    {
        var faceMeshReference = charGrpEntry.Face.Meshes.FirstOrDefault();
        var faceTextureReference = charGrpEntry.Face.Textures.FirstOrDefault();

        if (hairGrp is not null &&
            charGrpIndex >= 0 &&
            charGrpIndex < hairGrp.Entries.Count &&
            !string.IsNullOrWhiteSpace(faceMeshReference) &&
            !string.IsNullOrWhiteSpace(faceTextureReference))
        {
            var options = hairGrp.Entries[charGrpIndex].Values
                .Where(x => x >= 0)
                .Distinct()
                .Select(styleId => BuildDerivedHairStyleOption(clientRoot, packageIndex, styleId, faceMeshReference!, faceTextureReference!))
                .Where(x => x is not null)
                .Cast<SceneCharacterHairStyleOptionData>()
                .ToArray();
            if (options.Length > 0)
            {
                return options;
            }
        }

        return BuildLegacyHairOptions(charGrpEntry);
    }

    private static SceneCharacterHairStyleOptionData? BuildDerivedHairStyleOption(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        int styleId,
        string faceMeshReference,
        string faceTextureReference)
    {
        var meshReferences = BuildDerivedHairMeshReferences(faceMeshReference, styleId)
            .Where(reference => SkeletalMeshExists(clientRoot, packageIndex, reference))
            .ToArray();
        var meshResources = BuildReferences(meshReferences, UnrealClassNames.SkeletalMesh);
        var colorOptions = Enumerable.Range(0, 4)
            .Select(colorId => BuildDerivedHairColorOption(faceTextureReference, styleId, colorId))
            .Where(x => x.TextureResources.Length > 0)
            .ToArray();

        if (meshResources.Length == 0 || colorOptions.Length == 0)
        {
            return null;
        }

        return new SceneCharacterHairStyleOptionData
        {
            Id = styleId,
            MeshResources = meshResources,
            HairColorOptions = colorOptions
        };
    }

    private static SceneCharacterHairColorOptionData BuildDerivedHairColorOption(
        string faceTextureReference,
        int styleId,
        int colorId)
    {
        var textureResources = BuildReferences(
            BuildDerivedHairTextureReferences(faceTextureReference, styleId, colorId),
            UnrealClassNames.Texture);
        return new SceneCharacterHairColorOptionData
        {
            Id = colorId,
            TextureResources = textureResources
        };
    }

    private static bool SkeletalMeshExists(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        string reference)
    {
        var parsed = SceneReferenceUtilities.ParseFromDbResourceReference(reference);
        if (!packageIndex.TryGetValue(parsed.PackageName, out var packagePath))
        {
            return false;
        }

        if (!packagePath.EndsWith(".ukx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var location = SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            packagePath,
            parsed.PackageName,
            parsed.ObjectName,
            UnrealClassNames.SkeletalMesh);
        var ukx = UkxFileReader.Read(location.PackagePath);
        return ukx.ExportObjects
            .Select(x => x.Object)
            .OfType<UkxSkeletalMeshObject>()
            .Any(x => x.ObjectName.Is(location.ObjectName));
    }

    private static IReadOnlyList<SceneCharacterHairStyleOptionData> BuildLegacyHairOptions(CharGrpDatEntry entry)
    {
        return entry.Hair.Meshes.Count == 0 && entry.Hair.Textures.Count == 0
            ? Array.Empty<SceneCharacterHairStyleOptionData>()
            :
            [
                new SceneCharacterHairStyleOptionData
                {
                    Id = 0,
                    MeshResources = BuildReferences(entry.Hair.Meshes, UnrealClassNames.SkeletalMesh),
                    HairColorOptions =
                    [
                        new SceneCharacterHairColorOptionData
                        {
                            Id = 0,
                            TextureResources = BuildReferences(entry.Hair.Textures, UnrealClassNames.Texture)
                        }
                    ]
                }
            ];
    }

    private static IEnumerable<string> BuildDerivedHairMeshReferences(string faceMeshReference, int styleId)
    {
        var parsed = SceneReferenceUtilities.ParseFromDbResourceReference(faceMeshReference);
        var stem = Regex.Replace(parsed.ObjectName, "_m\\d{3}_f$", string.Empty, RegexOptions.IgnoreCase);
        if (string.Equals(stem, parsed.ObjectName, StringComparison.Ordinal))
        {
            yield break;
        }

        yield return $"{parsed.PackageName}.{stem}_m{styleId:D3}_m00_ah";
        yield return $"{parsed.PackageName}.{stem}_m{styleId:D3}_m00_bh";
    }

    private static IEnumerable<string> BuildDerivedHairTextureReferences(
        string faceTextureReference,
        int styleId,
        int colorId)
    {
        var parsed = SceneReferenceUtilities.ParseFromDbResourceReference(faceTextureReference);
        var stem = Regex.Replace(parsed.ObjectName, "_m\\d{3}_t\\d{2}_f$", string.Empty, RegexOptions.IgnoreCase);
        if (string.Equals(stem, parsed.ObjectName, StringComparison.Ordinal))
        {
            yield break;
        }

        yield return $"{parsed.PackageName}.{stem}_m{styleId:D3}_t{colorId:D2}_m00_ah";
        yield return $"{parsed.PackageName}.{stem}_m{styleId:D3}_t{colorId:D2}_m00_bh";
    }

    private static SceneResourceReference[] BuildReferences(IEnumerable<string> references, string className)
    {
        return references
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(reference => SceneReferenceUtilities.BuildFromDbResourceReference(reference, className))
            .ToArray();
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
