using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UkxFile;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

[ForExternalUse]
public sealed class SceneCharacterAppearanceBuilder
{
    private readonly SceneCharacterAppearanceOptionsBuilder _optionsBuilder = new();

    public SceneCharacterAppearanceData Build(string clientRootPath, SceneCharacterAppearanceRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var clientRoot = NormalizeClientRoot(clientRootPath);
        var visualFamily = ResolveVisualFamily(request.BaseClass, request.Gender);
        var familyBinding = SceneCharacterVisualFamilyBindings.Get(visualFamily);
        var appearanceOptions = _optionsBuilder.Build(clientRoot, request.BaseClass, request.Gender);
        var selectedFace = appearanceOptions.FaceOptions.FirstOrDefault(x => x.Id == request.FaceId)
            ?? throw new ArgumentOutOfRangeException(nameof(request.FaceId), request.FaceId, "Requested face id is not available in the client.");
        var selectedHairStyle = appearanceOptions.HairStyleOptions.FirstOrDefault(x => x.Id == request.HairStyleId)
            ?? throw new ArgumentOutOfRangeException(nameof(request.HairStyleId), request.HairStyleId, "Requested hair style id is not available in the client.");
        var selectedHairColor = selectedHairStyle.HairColorOptions.FirstOrDefault(x => x.Id == request.HairColorId)
            ?? throw new ArgumentOutOfRangeException(nameof(request.HairColorId), request.HairColorId, "Requested hair color id is not available for the selected hair style.");

        var charGrp = DatFileReader.ReadDocument<CharGrpDatDocument>(Path.Combine(clientRoot, "system", "chargrp.dat"));
        var armorGrp = DatFileReader.ReadDocument<ArmorGrpDatDocument>(Path.Combine(clientRoot, "system", "armorgrp.dat"));
        if (appearanceOptions.CharGrpIndex < 0 || appearanceOptions.CharGrpIndex >= charGrp.Entries.Count)
        {
            throw new InvalidOperationException($"chargrp entry '{appearanceOptions.CharGrpIndex}' is not available for visual family '{visualFamily}'.");
        }

        var charGrpEntry = charGrp.Entries[appearanceOptions.CharGrpIndex];
        var packageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);
        var parts = new[]
        {
            BuildFacePart(selectedFace),
            BuildHairPart(selectedHairStyle, selectedHairColor),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterPaperdollSlot.Chest, request.UpperItemId, charGrpEntry.Upper.Meshes, charGrpEntry.Upper.Textures),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterPaperdollSlot.Legs, request.LowerItemId, charGrpEntry.Lower.Meshes, charGrpEntry.Lower.Textures),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterPaperdollSlot.Gloves, request.GlovesItemId, charGrpEntry.Gloves.Meshes, charGrpEntry.Gloves.Textures),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterPaperdollSlot.Feet, request.BootsItemId, charGrpEntry.Boots.Meshes, charGrpEntry.Boots.Textures)
        };

        var skeletonCandidate = ResolveSkeletonCandidate(clientRoot, packageIndex, visualFamily, parts);
        var skeletonMesh = skeletonCandidate.Mesh;
        var skeletonLocation = skeletonCandidate.Location;
        var skeleton = skeletonCandidate.Skeleton;

        return new SceneCharacterAppearanceData
        {
            BaseClass = request.BaseClass,
            Gender = request.Gender,
            FaceId = selectedFace.Id,
            HairStyleId = selectedHairStyle.Id,
            HairColorId = selectedHairColor.Id,
            VisualFamily = visualFamily,
            CharGrpIndex = familyBinding.CharGrpIndex,
            SkeletonMeshResource = skeletonMesh,
            SkeletonMeshLocation = skeletonLocation,
            SkeletonName = skeleton.Name,
            SkeletonBoneCount = skeleton.BoneCount,
            Parts = parts
        };
    }

    public static SceneCharacterVisualFamily ResolveVisualFamily(SceneCharacterBaseClass baseClass, SceneCharacterGender gender)
    {
        return (baseClass, gender) switch
        {
            (SceneCharacterBaseClass.HumanFighter, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleHumanFighter,
            (SceneCharacterBaseClass.HumanFighter, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleHumanFighter,
            (SceneCharacterBaseClass.HumanMage, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleHumanMystic,
            (SceneCharacterBaseClass.HumanMage, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleHumanMystic,
            (SceneCharacterBaseClass.ElfFighter, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleElf,
            (SceneCharacterBaseClass.ElfFighter, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleElf,
            (SceneCharacterBaseClass.ElfMage, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleElf,
            (SceneCharacterBaseClass.ElfMage, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleElf,
            (SceneCharacterBaseClass.DarkElfFighter, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleDarkElf,
            (SceneCharacterBaseClass.DarkElfFighter, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleDarkElf,
            (SceneCharacterBaseClass.DarkElfMage, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleDarkElf,
            (SceneCharacterBaseClass.DarkElfMage, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleDarkElf,
            (SceneCharacterBaseClass.OrcFighter, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleOrcFighter,
            (SceneCharacterBaseClass.OrcFighter, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleOrcFighter,
            (SceneCharacterBaseClass.OrcMage, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleOrcMage,
            (SceneCharacterBaseClass.OrcMage, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleOrcMage,
            (SceneCharacterBaseClass.DwarvenFighter, SceneCharacterGender.Male) => SceneCharacterVisualFamily.MaleDwarf,
            (SceneCharacterBaseClass.DwarvenFighter, SceneCharacterGender.Female) => SceneCharacterVisualFamily.FemaleDwarf,
            _ => throw new ArgumentOutOfRangeException(nameof(baseClass), $"Unsupported base class '{baseClass}' with gender '{gender}'.")
        };
    }

    private static SceneCharacterResolvedPartData BuildFacePart(SceneCharacterFaceOptionData faceOption)
    {
        return new SceneCharacterResolvedPartData
        {
            Slot = SceneCharacterPaperdollSlot.Face,
            ItemId = null,
            IsBasePart = true,
            MeshResources = faceOption.MeshResources,
            TextureResources = faceOption.TextureResources
        };
    }

    private static SceneCharacterResolvedPartData BuildHairPart(
        SceneCharacterHairStyleOptionData hairStyleOption,
        SceneCharacterHairColorOptionData hairColorOption)
    {
        return new SceneCharacterResolvedPartData
        {
            Slot = SceneCharacterPaperdollSlot.Hair,
            ItemId = null,
            IsBasePart = true,
            MeshResources = hairStyleOption.MeshResources,
            TextureResources = hairColorOption.TextureResources
        };
    }

    private static SceneCharacterResolvedPartData BuildBodyPart(
        ArmorGrpDatDocument armorGrp,
        CharacterVisualFamilyBinding familyBinding,
        SceneCharacterPaperdollSlot slot,
        int? itemId,
        IReadOnlyList<string> baseMeshes,
        IReadOnlyList<string> baseTextures)
    {
        if (!itemId.HasValue)
        {
            return new SceneCharacterResolvedPartData
            {
                Slot = slot,
                ItemId = null,
                IsBasePart = true,
                MeshResources = BuildReferences(baseMeshes, UnrealClassNames.SkeletalMesh),
                TextureResources = BuildReferences(baseTextures, UnrealClassNames.Texture)
            };
        }

        var armorEntry = armorGrp.Entries.FirstOrDefault(x => x.Id == (uint)itemId.Value)
            ?? throw new InvalidOperationException($"Armor item '{itemId.Value}' was not found in armorgrp.dat.");
        var meshGroup = armorEntry.MeshGroups.FirstOrDefault(x => string.Equals(x.Name, familyBinding.ArmorGroupName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Armor item '{itemId.Value}' has no mesh group for '{familyBinding.ArmorGroupName}'.");

        return new SceneCharacterResolvedPartData
        {
            Slot = slot,
            ItemId = itemId,
            IsBasePart = false,
            MeshResources = BuildReferences(meshGroup.Value.Meshes, UnrealClassNames.SkeletalMesh),
            TextureResources = BuildReferences(meshGroup.Value.Textures, UnrealClassNames.Texture)
        };
    }

    private static SceneResourceReference[] BuildReferences(
        IEnumerable<string> references,
        string className)
    {
        return references
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(reference => SceneReferenceUtilities.BuildFromDbResourceReference(reference, className))
            .ToArray();
    }

    private static string NormalizeClientRoot(string clientRootPath)
    {
        if (string.IsNullOrWhiteSpace(clientRootPath))
        {
            throw new ArgumentException("Client root path is required.", nameof(clientRootPath));
        }

        if (File.Exists(clientRootPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(clientRootPath))
                   ?? throw new DirectoryNotFoundException($"Unable to determine client root for '{clientRootPath}'.");
        }

        return Path.GetFullPath(clientRootPath);
    }

    private static SceneResourceLocation ResolveSkeletonLocation(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        SceneResourceReference skeletonMesh)
    {
        if (!packageIndex.TryGetValue(skeletonMesh.PackageName, out var packagePath))
        {
            throw new InvalidOperationException($"Package '{skeletonMesh.PackageName}' for skeletal mesh '{skeletonMesh.Reference}' was not found under client root.");
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            packagePath,
            skeletonMesh.PackageName,
            skeletonMesh.ObjectName,
            skeletonMesh.ClassName);
    }

    private static ResolvedSkeleton ResolveSkeleton(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        SceneResourceReference skeletonMesh)
    {
        if (!packageIndex.TryGetValue(skeletonMesh.PackageName, out var packagePath))
        {
            throw new InvalidOperationException($"Package '{skeletonMesh.PackageName}' for skeletal mesh '{skeletonMesh.Reference}' was not found under client root.");
        }

        var ukx = UkxFileReader.Read(packagePath);
        var mesh = ukx.ExportObjects
            .Select(x => x.Object)
            .OfType<UkxSkeletalMeshObject>()
            .FirstOrDefault(x => x.ObjectName.Is(skeletonMesh.ObjectName))
            ?? throw new InvalidOperationException($"Skeletal mesh '{skeletonMesh.ObjectName}' was not found in '{packagePath}'.");
        if (mesh.RefSkeleton.Length == 0)
        {
            throw new InvalidOperationException($"Skeletal mesh '{skeletonMesh.ObjectName}' in '{packagePath}' has no reference skeleton.");
        }
        if (mesh.AnimationReference is null)
        {
            throw new InvalidOperationException($"Skeletal mesh '{skeletonMesh.ObjectName}' in '{packagePath}' has no animation reference.");
        }

        return new ResolvedSkeleton(mesh.ObjectName, mesh.RefSkeleton.Length);
    }

    private static ResolvedSkeletonCandidate ResolveSkeletonCandidate(
        string clientRoot,
        IReadOnlyDictionary<string, string> packageIndex,
        SceneCharacterVisualFamily visualFamily,
        IEnumerable<SceneCharacterResolvedPartData> parts)
    {
        var preferredMeshes = parts
            .Where(x => x.MeshResources.Length > 0 && x.Slot is SceneCharacterPaperdollSlot.Chest or SceneCharacterPaperdollSlot.Legs)
            .SelectMany(x => x.MeshResources)
            .Concat(parts.Where(x => x.MeshResources.Length > 0).SelectMany(x => x.MeshResources))
            .GroupBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();

        foreach (var mesh in preferredMeshes)
        {
            try
            {
                var location = ResolveSkeletonLocation(clientRoot, packageIndex, mesh);
                var skeleton = ResolveSkeleton(clientRoot, packageIndex, mesh);
                return new ResolvedSkeletonCandidate(mesh, location, skeleton);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"Visual family '{visualFamily}' has no skeletal mesh resources with animation references.");
    }

    private readonly record struct ResolvedSkeleton(string Name, int BoneCount);

    private readonly record struct ResolvedSkeletonCandidate(
        SceneResourceReference Mesh,
        SceneResourceLocation Location,
        ResolvedSkeleton Skeleton);
}
