namespace L2Viewer.SceneDomain.Models;

public enum SceneMaterialBlendMode
{
    Opaque,
    AlphaBlend,
    Modulated,
    Additive,
    AlphaModulate,
    Darken
}

public static class SceneMaterialBlendModeInterpreter
{
    public static SceneMaterialBlendMode FromParticleDrawStyle(byte drawStyle)
    {
        return drawStyle switch
        {
            0 => SceneMaterialBlendMode.Opaque,
            1 => SceneMaterialBlendMode.AlphaBlend,
            2 => SceneMaterialBlendMode.Modulated,
            3 => SceneMaterialBlendMode.Additive,
            4 => SceneMaterialBlendMode.AlphaModulate,
            5 => SceneMaterialBlendMode.Darken,
            6 => SceneMaterialBlendMode.Additive,
            _ => throw new InvalidOperationException($"Unsupported Unreal particle draw style '{drawStyle}'.")
        };
    }
}
