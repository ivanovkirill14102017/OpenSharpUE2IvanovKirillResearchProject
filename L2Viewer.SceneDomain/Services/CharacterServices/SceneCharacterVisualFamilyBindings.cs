using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

internal static class SceneCharacterVisualFamilyBindings
{
    internal static CharacterVisualFamilyBinding Get(SceneCharacterVisualFamily family)
    {
        return family switch
        {
            SceneCharacterVisualFamily.MaleHumanFighter => new CharacterVisualFamilyBinding(0, "m_HumnFigh"),
            SceneCharacterVisualFamily.FemaleHumanFighter => new CharacterVisualFamilyBinding(1, "f_HumnFigh"),
            SceneCharacterVisualFamily.MaleDarkElf => new CharacterVisualFamilyBinding(2, "m_DarkElf"),
            SceneCharacterVisualFamily.FemaleDarkElf => new CharacterVisualFamilyBinding(3, "f_DarkElf"),
            SceneCharacterVisualFamily.MaleDwarf => new CharacterVisualFamilyBinding(4, "m_Dorf"),
            SceneCharacterVisualFamily.FemaleDwarf => new CharacterVisualFamilyBinding(5, "f_Dorf"),
            SceneCharacterVisualFamily.MaleElf => new CharacterVisualFamilyBinding(6, "m_Elf"),
            SceneCharacterVisualFamily.FemaleElf => new CharacterVisualFamilyBinding(7, "f_Elf"),
            SceneCharacterVisualFamily.MaleHumanMystic => new CharacterVisualFamilyBinding(8, "m_HumnMyst"),
            SceneCharacterVisualFamily.FemaleHumanMystic => new CharacterVisualFamilyBinding(9, "f_HumnMyst"),
            SceneCharacterVisualFamily.MaleOrcFighter => new CharacterVisualFamilyBinding(10, "m_OrcFigh"),
            SceneCharacterVisualFamily.FemaleOrcFighter => new CharacterVisualFamilyBinding(11, "f_OrcFigh"),
            SceneCharacterVisualFamily.MaleOrcMage => new CharacterVisualFamilyBinding(12, "m_OrcMage"),
            SceneCharacterVisualFamily.FemaleOrcMage => new CharacterVisualFamilyBinding(13, "f_OrcMage"),
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, null)
        };
    }
}

internal readonly record struct CharacterVisualFamilyBinding(int CharGrpIndex, string ArmorGroupName);
