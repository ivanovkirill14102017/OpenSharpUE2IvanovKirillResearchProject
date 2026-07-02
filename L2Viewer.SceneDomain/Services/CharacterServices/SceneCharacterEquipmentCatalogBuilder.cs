using L2Viewer.DatFile;
using L2Viewer.DbFile.DbJson;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

[ForExternalUse]
public sealed class SceneCharacterEquipmentCatalogBuilder
{
    public SceneCharacterEquipmentCatalogData Build(
        string clientRootPath,
        string dbRootPath,
        SceneCharacterBaseClass baseClass,
        SceneCharacterGender gender)
    {
        if (string.IsNullOrWhiteSpace(clientRootPath))
        {
            throw new ArgumentException("Client root path is required.", nameof(clientRootPath));
        }

        if (string.IsNullOrWhiteSpace(dbRootPath))
        {
            throw new ArgumentException("DB root path is required.", nameof(dbRootPath));
        }

        var clientRoot = NormalizeRoot(clientRootPath);
        var dbRoot = NormalizeRoot(dbRootPath);
        var armorGrpPath = Path.Combine(clientRoot, "system", "armorgrp.dat");
        var weaponGrpPath = Path.Combine(clientRoot, "system", "weapongrp.dat");
        var armorJsonPath = Path.Combine(dbRoot, "armor.json");
        var weaponJsonPath = Path.Combine(dbRoot, "weapon.json");

        if (!File.Exists(armorGrpPath))
        {
            throw new FileNotFoundException($"Required DAT file was not found: '{armorGrpPath}'.", armorGrpPath);
        }

        if (!File.Exists(weaponGrpPath))
        {
            throw new FileNotFoundException($"Required DAT file was not found: '{weaponGrpPath}'.", weaponGrpPath);
        }

        if (!File.Exists(armorJsonPath))
        {
            throw new FileNotFoundException($"Required DB export file was not found: '{armorJsonPath}'.", armorJsonPath);
        }

        if (!File.Exists(weaponJsonPath))
        {
            throw new FileNotFoundException($"Required DB export file was not found: '{weaponJsonPath}'.", weaponJsonPath);
        }

        var visualFamily = SceneCharacterAppearanceBuilder.ResolveVisualFamily(baseClass, gender);
        var armorGroupName = ResolveArmorGroupName(visualFamily);
        var armorGrp = DatFileReader.ReadDocument<ArmorGrpDatDocument>(armorGrpPath);
        var weaponGrp = DatFileReader.ReadDocument<WeaponGrpDatDocument>(weaponGrpPath);
        var armorDb = ReadEquipmentDb(armorJsonPath);
        var weaponDb = ReadEquipmentDb(weaponJsonPath);
        var warnings = new List<string>();
        var itemsBySlot = new Dictionary<SceneCharacterPaperdollSlot, List<SceneCharacterEquipmentCatalogItemData>>();

        foreach (var dbItem in armorDb)
        {
            if (!TryMapBodyPart(dbItem.BodyPartKey, out var bodyPartMapping))
            {
                warnings.Add($"Skipped item {dbItem.ItemId} '{dbItem.DisplayName}': unsupported bodypart '{dbItem.BodyPartKey}'.");
                continue;
            }

            var armorEntry = armorGrp.Entries.FirstOrDefault(x => x.Id == (uint)dbItem.ItemId);
            if (armorEntry is null)
            {
                warnings.Add($"Skipped item {dbItem.ItemId} '{dbItem.DisplayName}': no armorgrp.dat entry.");
                continue;
            }

            var meshGroup = armorEntry.MeshGroups.FirstOrDefault(x => string.Equals(x.Name, armorGroupName, StringComparison.OrdinalIgnoreCase));
            if (meshGroup is null)
            {
                warnings.Add($"Skipped item {dbItem.ItemId} '{dbItem.DisplayName}': no mesh group '{armorGroupName}'.");
                continue;
            }

            var item = BuildCatalogItem(
                dbItem,
                bodyPartMapping,
                meshGroup.Value.Meshes,
                meshGroup.Value.Textures,
                UnrealClassNames.SkeletalMesh);
            AddItem(itemsBySlot, item);
        }

        foreach (var dbItem in weaponDb)
        {
            if (!TryMapBodyPart(dbItem.BodyPartKey, out var bodyPartMapping))
            {
                warnings.Add($"Skipped item {dbItem.ItemId} '{dbItem.DisplayName}': unsupported bodypart '{dbItem.BodyPartKey}'.");
                continue;
            }

            var weaponEntry = weaponGrp.Entries.FirstOrDefault(x => x.Id == (uint)dbItem.ItemId);
            if (weaponEntry is null)
            {
                warnings.Add($"Skipped item {dbItem.ItemId} '{dbItem.DisplayName}': no weapongrp.dat entry.");
                continue;
            }

            var item = BuildCatalogItem(
                dbItem,
                bodyPartMapping,
                weaponEntry.WeaponMeshes,
                weaponEntry.WeaponTextures,
                UnrealClassNames.SkeletalMesh);
            AddItem(itemsBySlot, item);
        }

        var slots = itemsBySlot
            .OrderBy(x => (int)x.Key)
            .Select(x => new SceneCharacterEquipmentCatalogSlotData
            {
                Slot = x.Key,
                Items = x.Value
                    .OrderBy(item => item.ItemId)
                    .ToArray()
            })
            .ToArray();

        return new SceneCharacterEquipmentCatalogData
        {
            BaseClass = baseClass,
            Gender = gender,
            VisualFamily = visualFamily,
            Slots = slots,
            Warnings = warnings
        };
    }

    private static string NormalizeRoot(string path)
    {
        if (File.Exists(path))
        {
            return Path.GetDirectoryName(Path.GetFullPath(path))
                   ?? throw new DirectoryNotFoundException($"Unable to determine root for '{path}'.");
        }

        return Path.GetFullPath(path);
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

    private static string ResolveArmorGroupName(SceneCharacterVisualFamily family)
    {
        return family switch
        {
            SceneCharacterVisualFamily.MaleHumanFighter => "m_HumnFigh",
            SceneCharacterVisualFamily.FemaleHumanFighter => "f_HumnFigh",
            SceneCharacterVisualFamily.MaleDarkElf => "m_DarkElf",
            SceneCharacterVisualFamily.FemaleDarkElf => "f_DarkElf",
            SceneCharacterVisualFamily.MaleDwarf => "m_Dorf",
            SceneCharacterVisualFamily.FemaleDwarf => "f_Dorf",
            SceneCharacterVisualFamily.MaleElf => "m_Elf",
            SceneCharacterVisualFamily.FemaleElf => "f_Elf",
            SceneCharacterVisualFamily.MaleHumanMystic => "m_HumnMyst",
            SceneCharacterVisualFamily.FemaleHumanMystic => "f_HumnMyst",
            SceneCharacterVisualFamily.MaleOrcFighter => "m_OrcFigh",
            SceneCharacterVisualFamily.FemaleOrcFighter => "f_OrcFigh",
            SceneCharacterVisualFamily.MaleOrcMage => "m_OrcMage",
            SceneCharacterVisualFamily.FemaleOrcMage => "f_OrcMage",
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, null)
        };
    }

    private static bool TryMapBodyPart(string rawBodyPart, out BodyPartMapping mapping)
    {
        switch (rawBodyPart)
        {
            case "underwear":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Underwear,
                    [SceneCharacterPaperdollSlot.Under],
                    []);
                return true;
            case "chest":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Chest,
                    [SceneCharacterPaperdollSlot.Chest],
                    [SceneCharacterPaperdollSlot.Chest]);
                return true;
            case "legs":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Legs,
                    [SceneCharacterPaperdollSlot.Legs],
                    [SceneCharacterPaperdollSlot.Legs]);
                return true;
            case "feet":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Feet,
                    [SceneCharacterPaperdollSlot.Feet],
                    [SceneCharacterPaperdollSlot.Feet]);
                return true;
            case "gloves":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Gloves,
                    [SceneCharacterPaperdollSlot.Gloves],
                    [SceneCharacterPaperdollSlot.Gloves]);
                return true;
            case "fullarmor":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.FullArmor,
                    [SceneCharacterPaperdollSlot.Chest, SceneCharacterPaperdollSlot.Legs],
                    [SceneCharacterPaperdollSlot.Chest, SceneCharacterPaperdollSlot.Legs]);
                return true;
            case "head":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Head,
                    [SceneCharacterPaperdollSlot.Head],
                    []);
                return true;
            case "face":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Face,
                    [SceneCharacterPaperdollSlot.Face],
                    [SceneCharacterPaperdollSlot.Face]);
                return true;
            case "hair":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Hair,
                    [SceneCharacterPaperdollSlot.Hair],
                    [SceneCharacterPaperdollSlot.Hair]);
                return true;
            case "dhair":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.DoubleHair,
                    [SceneCharacterPaperdollSlot.DoubleHair],
                    [SceneCharacterPaperdollSlot.Hair]);
                return true;
            case "neck":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Neck,
                    [SceneCharacterPaperdollSlot.Neck],
                    []);
                return true;
            case "rear,lear":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Ears,
                    [SceneCharacterPaperdollSlot.LeftEar, SceneCharacterPaperdollSlot.RightEar],
                    []);
                return true;
            case "rfinger,lfinger":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.Fingers,
                    [SceneCharacterPaperdollSlot.LeftFinger, SceneCharacterPaperdollSlot.RightFinger],
                    []);
                return true;
            case "rhand":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.RightHand,
                    [SceneCharacterPaperdollSlot.RightHand],
                    []);
                return true;
            case "lhand":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.LeftHand,
                    [SceneCharacterPaperdollSlot.LeftHand],
                    []);
                return true;
            case "lrhand":
                mapping = new BodyPartMapping(
                    SceneCharacterEquipmentBodyPart.LeftRightHand,
                    [SceneCharacterPaperdollSlot.LeftRightHand],
                    []);
                return true;
            default:
                mapping = default;
                return false;
        }
    }

    private static SceneCharacterEquipmentCatalogItemData BuildCatalogItem(
        EquipmentDbItem dbItem,
        BodyPartMapping bodyPartMapping,
        IEnumerable<string> meshReferences,
        IEnumerable<string> textureReferences,
        string meshClassName)
    {
        var meshResources = BuildReferences(meshReferences, meshClassName);
        var textureResources = BuildReferences(textureReferences, UnrealClassNames.Texture);
        var appearanceSlots = bodyPartMapping.AppearanceSlots;

        return new SceneCharacterEquipmentCatalogItemData
        {
            ItemId = dbItem.ItemId,
            DisplayName = dbItem.DisplayName,
            BodyPart = bodyPartMapping.BodyPart,
            BodyPartKey = dbItem.BodyPartKey,
            PaperdollSlots = bodyPartMapping.PaperdollSlots,
            AppearanceSlots = appearanceSlots,
            IsRenderableWithCurrentAppearanceBuilder = appearanceSlots.Count > 0 && (meshResources.Length > 0 || textureResources.Length > 0),
            MeshResources = meshResources,
            TextureResources = textureResources
        };
    }

    private static void AddItem(
        Dictionary<SceneCharacterPaperdollSlot, List<SceneCharacterEquipmentCatalogItemData>> itemsBySlot,
        SceneCharacterEquipmentCatalogItemData item)
    {
        foreach (var slot in item.PaperdollSlots)
        {
            if (!itemsBySlot.TryGetValue(slot, out var slotItems))
            {
                slotItems = [];
                itemsBySlot[slot] = slotItems;
            }

            slotItems.Add(item);
        }
    }

    private static IReadOnlyList<EquipmentDbItem> ReadEquipmentDb(string jsonPath)
    {
        return TableJsonMapper.Read<EquipmentDbJsonRow>(jsonPath)
            .Where(x => x.item_id > 0 && !string.IsNullOrWhiteSpace(x.bodypart))
            .Select(x => new EquipmentDbItem(
                x.item_id,
                string.IsNullOrWhiteSpace(x.name) ? x.item_id.ToString() : x.name,
                x.bodypart.Trim().ToLowerInvariant()))
            .ToArray();
    }

    private sealed class EquipmentDbJsonRow
    {
        public int item_id { get; set; }
        public string name { get; set; } = string.Empty;
        public string bodypart { get; set; } = string.Empty;
    }

    private readonly record struct EquipmentDbItem(int ItemId, string DisplayName, string BodyPartKey);

    private readonly record struct BodyPartMapping(
        SceneCharacterEquipmentBodyPart BodyPart,
        IReadOnlyList<SceneCharacterPaperdollSlot> PaperdollSlots,
        IReadOnlyList<SceneCharacterPaperdollSlot> AppearanceSlots);
}
