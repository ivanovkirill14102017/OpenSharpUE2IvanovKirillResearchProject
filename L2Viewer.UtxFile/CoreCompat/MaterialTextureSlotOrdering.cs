namespace L2Viewer.UtxFile;

public static class MaterialTextureSlotOrdering
{
    public static IReadOnlyList<MaterialTextureSlot> OrderTextureSlots(IEnumerable<MaterialTextureSlot> slots)
    {
        return slots
            .OrderBy(GetAvailabilityRank)
            .ThenBy(GetSemanticRank)
            .ThenBy(x => x.SlotName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static MaterialTextureSlot? GetPreferredTextureSlot(IEnumerable<MaterialTextureSlot> slots)
    {
        return OrderTextureSlots(slots).FirstOrDefault();
    }

    private static int GetAvailabilityRank(MaterialTextureSlot slot)
    {
        if (slot.Texture is not null)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(slot.PackagePath))
        {
            return 1;
        }

        return 2;
    }

    private static int GetSemanticRank(MaterialTextureSlot slot)
    {
        var slotName = slot.SlotName ?? string.Empty;
        if (slotName.Equals("Diffuse", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Material", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Material1", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Material2", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("DefaultMaterial", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (slotName.Equals("Texture", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (slotName.Equals("Detail", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (slotName.Equals("SelfIllumination", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("SelfIlluminationMask", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Emissive", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (slotName.Equals("Opacity", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Mask", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Specular", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("SpecularityMask", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Normal", StringComparison.OrdinalIgnoreCase) ||
            slotName.Equals("Bump", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 2;
    }
}
