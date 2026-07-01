using L2Viewer.DatFile;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UkxFile;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

[ForExternalUse]
public sealed class SceneCharacterAppearanceBuilder
{
    public SceneCharacterAppearanceData Build(string clientRootPath, SceneCharacterAppearanceRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var clientRoot = NormalizeClientRoot(clientRootPath);
        var visualFamily = ResolveVisualFamily(request.BaseClass, request.Gender);
        var familyBinding = GetFamilyBinding(visualFamily);
        var armorGrp = DatFileReader.ReadDocument<ArmorGrpDatDocument>(Path.Combine(clientRoot, "system", "armorgrp.dat"));
        var packageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);
        var parts = new[]
        {
            BuildFacePart(familyBinding.BaseParts),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterEquipmentSlot.Upper, request.UpperItemId, familyBinding.BaseParts.UpperMesh, familyBinding.BaseParts.UpperTexture),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterEquipmentSlot.Lower, request.LowerItemId, familyBinding.BaseParts.LowerMesh, familyBinding.BaseParts.LowerTexture),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterEquipmentSlot.Gloves, request.GlovesItemId, familyBinding.BaseParts.GlovesMesh, familyBinding.BaseParts.GlovesTexture),
            BuildBodyPart(armorGrp, familyBinding, SceneCharacterEquipmentSlot.Boots, request.BootsItemId, familyBinding.BaseParts.BootsMesh, familyBinding.BaseParts.BootsTexture)
        };

        var skeletonPart = parts.FirstOrDefault(x => x.MeshResources.Length > 0 && x.Slot is SceneCharacterEquipmentSlot.Upper or SceneCharacterEquipmentSlot.Lower)
            ?? parts.FirstOrDefault(x => x.MeshResources.Length > 0)
            ?? throw new InvalidOperationException($"Visual family '{visualFamily}' has no skeletal mesh resources.");
        var skeletonMesh = skeletonPart.MeshResources[0];
        var skeleton = ResolveSkeleton(clientRoot, packageIndex, skeletonMesh);

        return new SceneCharacterAppearanceData
        {
            BaseClass = request.BaseClass,
            Gender = request.Gender,
            VisualFamily = visualFamily,
            CharGrpIndex = familyBinding.CharGrpIndex,
            SkeletonMeshResource = skeletonMesh,
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

    private static SceneCharacterResolvedPartData BuildFacePart(
        BaseCharacterPartSet baseParts)
    {
        return new SceneCharacterResolvedPartData
        {
            Slot = SceneCharacterEquipmentSlot.Face,
            ItemId = null,
            IsBasePart = true,
            MeshResources = BuildReferences(baseParts.FaceMeshes, UnrealClassNames.SkeletalMesh),
            TextureResources = BuildReferences(baseParts.FaceTextures, UnrealClassNames.Texture)
        };
    }

    private static SceneCharacterResolvedPartData BuildBodyPart(
        ArmorGrpDatDocument armorGrp,
        CharacterVisualFamilyBinding familyBinding,
        SceneCharacterEquipmentSlot slot,
        int? itemId,
        string? baseMesh,
        string? baseTexture)
    {
        if (!itemId.HasValue)
        {
            return new SceneCharacterResolvedPartData
            {
                Slot = slot,
                ItemId = null,
                IsBasePart = true,
                MeshResources = BuildReferences(string.IsNullOrWhiteSpace(baseMesh) ? [] : [baseMesh], UnrealClassNames.SkeletalMesh),
                TextureResources = BuildReferences(string.IsNullOrWhiteSpace(baseTexture) ? [] : [baseTexture], UnrealClassNames.Texture)
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

    private static CharacterVisualFamilyBinding GetFamilyBinding(SceneCharacterVisualFamily family)
    {
        return family switch
        {
            SceneCharacterVisualFamily.MaleHumanFighter => new CharacterVisualFamilyBinding(0, "m_HumnFigh", new BaseCharacterPartSet(
                ["Fighter.MFighter_m000_f"],
                ["MFighter.MFighter_m000_t00_f", "MFighter.MFighter_m000_t01_f", "MFighter.MFighter_m000_t02_f"],
                "fighter.MFighter_m000_u", "MFighter.MFighter_m000_t1000_u",
                "fighter.MFighter_m000_l", "MFighter.MFighter_m000_t1000_l",
                "fighter.MFighter_m000_g", "MFighter.MFighter_m000_t1000_g",
                "fighter.MFighter_m000_b", "MFighter.MFighter_m000_t1000_b")),
            SceneCharacterVisualFamily.FemaleHumanFighter => new CharacterVisualFamilyBinding(1, "f_HumnFigh", new BaseCharacterPartSet(
                ["Fighter.FFighter_m000_f"],
                ["FFighter.FFighter_m000_t00_f", "FFighter.FFighter_m000_t01_f", "FFighter.FFighter_m000_t02_f"],
                "fighter.FFighter_m000_u", "NakedF.FFighter_m000_t1000_u",
                "fighter.FFighter_m000_l", "NakedF.FFighter_m000_t1000_l",
                "fighter.FFighter_m000_g", "FFighter.FFighter_m000_t1000_g",
                "fighter.FFighter_m000_b", "FFighter.FFighter_m000_t1000_b")),
            SceneCharacterVisualFamily.MaleDarkElf => new CharacterVisualFamilyBinding(2, "m_DarkElf", new BaseCharacterPartSet(
                ["DarkElf.MDarkElf_m000_f"],
                ["MDarkElf.MDarkElf_m000_t00_f", "MDarkElf.MDarkElf_m000_t01_f", "MDarkElf.MDarkElf_m000_t02_f"],
                "darkelf.MDarkElf_m000_u", "MDarkElf.MDarkElf_m000_t1000_u",
                "darkelf.MDarkElf_m000_l", "MDarkElf.MDarkElf_m000_t1000_l",
                "darkelf.MDarkElf_m000_g", "MDarkElf.MDarkElf_m000_t1000_g",
                "darkelf.MDarkElf_m000_b", "MDarkElf.MDarkElf_m000_t1000_b")),
            SceneCharacterVisualFamily.FemaleDarkElf => new CharacterVisualFamilyBinding(3, "f_DarkElf", new BaseCharacterPartSet(
                ["DarkElf.FDarkElf_m000_f"],
                ["FDarkElf.FDarkElf_m000_t00_f", "FDarkElf.FDarkElf_m000_t01_f", "FDarkElf.FDarkElf_m000_t02_f"],
                "darkelf.FDarkElf_m000_u", "NakedF.FDarkElf_m000_t1000_u",
                "darkelf.FDarkElf_m000_l", "NakedF.FDarkElf_m000_t1000_l",
                "darkelf.FDarkElf_m000_g", "FDarkElf.FDarkElf_m000_t1000_g",
                "darkelf.FDarkElf_m000_b", "FDarkElf.FDarkElf_m000_t1000_b")),
            SceneCharacterVisualFamily.MaleDwarf => new CharacterVisualFamilyBinding(4, "m_Dorf", new BaseCharacterPartSet(
                ["Dwarf.MDwarf_m000_f"],
                ["MDwarf.MDwarf_m000_t00_f", "MDwarf.MDwarf_m000_t01_f", "MDwarf.MDwarf_m000_t02_f"],
                "dwarf.MDwarf_m000_u", "MDwarf.MDwarf_m000_t1000_u",
                "dwarf.MDwarf_m000_l", "MDwarf.MDwarf_m000_t1000_l",
                "dwarf.MDwarf_m000_g", "MDwarf.MDwarf_m000_t1000_g",
                "dwarf.MDwarf_m000_b", "MDwarf.MDwarf_m000_t1000_b")),
            SceneCharacterVisualFamily.FemaleDwarf => new CharacterVisualFamilyBinding(5, "f_Dorf", new BaseCharacterPartSet(
                ["Dwarf.FDwarf_m000_f"],
                ["FDwarf.FDwarf_m000_t00_f", "FDwarf.FDwarf_m000_t01_f", "FDwarf.FDwarf_m000_t02_f"],
                "dwarf.FDwarf_m000_u", "NakedF.FDwarf_m000_t1000_u",
                "dwarf.FDwarf_m000_l", "NakedF.FDwarf_m000_t1000_l",
                "dwarf.FDwarf_m000_g", "FDwarf.FDwarf_m000_t1000_g",
                "dwarf.FDwarf_m000_b", "FDwarf.FDwarf_m000_t1000_b")),
            SceneCharacterVisualFamily.MaleElf => new CharacterVisualFamilyBinding(6, "m_Elf", new BaseCharacterPartSet(
                ["Elf.MElf_m000_f"],
                ["MElf.MElf_m000_t00_f", "MElf.MElf_m000_t01_f", "MElf.MElf_m000_t02_f"],
                "elf.MElf_m000_u", "MElf.MElf_m000_t1000_u",
                "elf.MElf_m000_l", "MElf.MElf_m000_t1000_l",
                "elf.MElf_m000_g", "MElf.MElf_m000_t1000_g",
                "elf.MElf_m000_b", "MElf.MElf_m000_t1000_b")),
            SceneCharacterVisualFamily.FemaleElf => new CharacterVisualFamilyBinding(7, "f_Elf", new BaseCharacterPartSet(
                ["Elf.FElf_m000_f"],
                ["FElf.FElf_m000_t00_f", "FElf.FElf_m000_t01_f", "FElf.FElf_m000_t02_f"],
                "elf.FElf_m000_u", "NakedF.FElf_m000_t1000_u",
                "elf.FElf_m000_l", "NakedF.FElf_m000_t1000_l",
                "elf.FElf_m000_g", "FElf.FElf_m000_t1000_g",
                "elf.FElf_m000_b", "FElf.FElf_m000_t1000_b")),
            SceneCharacterVisualFamily.MaleHumanMystic => new CharacterVisualFamilyBinding(8, "m_HumnMyst", new BaseCharacterPartSet(
                ["Magic.MMagic_m000_f"],
                ["MMagic.MMagic_m000_t00_f", "MMagic.MMagic_m000_t01_f", "MMagic.MMagic_m000_t02_f"],
                "magic.MMagic_m000_u", "MMagic.MMagic_m000_t1000_u",
                "magic.MMagic_m000_l", "MMagic.MMagic_m000_t1000_l",
                "magic.MMagic_m000_g", "MMagic.MMagic_m000_t1000_g",
                "magic.MMagic_m000_b", "MMagic.MMagic_m000_t1000_b")),
            SceneCharacterVisualFamily.FemaleHumanMystic => new CharacterVisualFamilyBinding(9, "f_HumnMyst", new BaseCharacterPartSet(
                ["Magic.FMagic_m000_f"],
                ["FMagic.FMagic_m000_t00_f", "FMagic.FMagic_m000_t01_f", "FMagic.FMagic_m000_t02_f"],
                "magic.FMagic_m000_u", "NakedF.FMagic_m000_t1000_u",
                "magic.FMagic_m000_l", "NakedF.FMagic_m000_t1000_l",
                "magic.FMagic_m000_g", "FMagic.FMagic_m000_t1000_g",
                "magic.FMagic_m000_b", "FMagic.FMagic_m000_t1000_b")),
            SceneCharacterVisualFamily.MaleOrcFighter => new CharacterVisualFamilyBinding(10, "m_OrcFigh", new BaseCharacterPartSet(
                ["Orc.MOrc_m000_f"],
                ["MOrc.MOrc_m000_t00_f", "MOrc.MOrc_m000_t01_f", "MOrc.MOrc_m000_t02_f"],
                "orc.MOrc_m000_u", "MOrc.MOrc_m000_t1000_u",
                "orc.MOrc_m000_l", "MOrc.MOrc_m000_t1000_l",
                "orc.MOrc_m000_g", "MOrc.MOrc_m000_t1000_g",
                "orc.MOrc_m000_b", "MOrc.MOrc_m000_t1000_b")),
            SceneCharacterVisualFamily.FemaleOrcFighter => new CharacterVisualFamilyBinding(11, "f_OrcFigh", new BaseCharacterPartSet(
                ["Orc.FOrc_m000_f"],
                ["FOrc.FOrc_m000_t00_f", "FOrc.FOrc_m000_t01_f", "FOrc.FOrc_m000_t02_f"],
                "orc.FOrc_m000_u", "NakedF.FOrc_m000_t1000_u",
                "orc.FOrc_m000_l", "NakedF.FOrc_m000_t1000_l",
                "orc.FOrc_m000_g", "FOrc.FOrc_m000_t1000_g",
                "orc.FOrc_m000_b", "FOrc.FOrc_m000_t1000_b")),
            SceneCharacterVisualFamily.MaleOrcMage => new CharacterVisualFamilyBinding(12, "m_OrcMage", new BaseCharacterPartSet(
                ["Shaman.MShaman_m000_f"],
                ["MShaman.MShaman_m000_t00_f", "MShaman.MShaman_m000_t01_f", "MShaman.MShaman_m000_t02_f"],
                "shaman.MShaman_m000_u", "MShaman.MShaman_m000_t1000_u",
                "shaman.MShaman_m000_l", "MShaman.MShaman_m000_t1000_l",
                "shaman.MShaman_m000_g", "MShaman.MShaman_m000_t1000_g",
                "shaman.MShaman_m000_b", "MShaman.MShaman_m000_t1000_b")),
            SceneCharacterVisualFamily.FemaleOrcMage => new CharacterVisualFamilyBinding(13, "f_OrcMage", new BaseCharacterPartSet(
                ["Shaman.FShaman_m000_f"],
                ["FShaman.FShaman_m000_t00_f", "FShaman.FShaman_m000_t01_f", "FShaman.FShaman_m000_t02_f"],
                "shaman.FShaman_m000_u", "NakedF.FShaman_m000_t1000_u",
                "shaman.FShaman_m000_l", "NakedF.FShaman_m000_t1000_l",
                "shaman.FShaman_m000_g", "FShaman.FShaman_m000_t1000_g",
                "shaman.FShaman_m000_b", "FShaman.FShaman_m000_t1000_b")),
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, null)
        };
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

        return new ResolvedSkeleton(mesh.ObjectName, mesh.RefSkeleton.Length);
    }

    private readonly record struct CharacterVisualFamilyBinding(int CharGrpIndex, string ArmorGroupName, BaseCharacterPartSet BaseParts);
    private sealed record BaseCharacterPartSet(
        IReadOnlyList<string> FaceMeshes,
        IReadOnlyList<string> FaceTextures,
        string UpperMesh,
        string UpperTexture,
        string LowerMesh,
        string LowerTexture,
        string GlovesMesh,
        string GlovesTexture,
        string BootsMesh,
        string BootsTexture);
    private readonly record struct ResolvedSkeleton(string Name, int BoneCount);
}
