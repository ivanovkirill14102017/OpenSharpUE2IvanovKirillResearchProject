using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;

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
        var charGrp = DatFileReader.ReadDocument<CharGrpDatDocument>(charGrpPath);

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

        var hairOptions = entry.Hair.Meshes.Count == 0 && entry.Hair.Textures.Count == 0
            ? Array.Empty<SceneCharacterHairStyleOptionData>()
            : new[]
            {
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
            };

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
