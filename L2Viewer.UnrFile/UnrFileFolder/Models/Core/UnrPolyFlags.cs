namespace L2Viewer.UnrFile;

[Flags]
public enum UnrPolyFlags : uint
{
    None = 0,
    Invisible = 0x00000001,
    Masked = 0x00000002,
    Translucent = 0x00000004,
    NotSolid = 0x00000008,
    Environment = 0x00000010,
    SemiSolid = 0x00000020,
    Modulated = 0x00000040,
    FakeBackdrop = 0x00000080,
    TwoSided = 0x00000100,
    AutoUPan = 0x00000200,
    AutoVPan = 0x00000400,
    NoSmooth = 0x00000800,
    AlphaTexture = 0x00001000,
    Flat = 0x00004000,
    NoMerge = 0x00010000,
    NoZTest = 0x00020000,
    Additive = 0x00040000,
    SpecialLit = 0x00100000,
    Wireframe = 0x00200000,
    Unlit = 0x00400000,
    Portal = 0x04000000,
    Mirror = 0x20000000
}

public static class UnrPolyFlagsInfo
{
    private static readonly (UnrPolyFlags Flag, string Name)[] KnownFlags =
    [
        (UnrPolyFlags.Invisible, nameof(UnrPolyFlags.Invisible)),
        (UnrPolyFlags.Masked, nameof(UnrPolyFlags.Masked)),
        (UnrPolyFlags.Translucent, nameof(UnrPolyFlags.Translucent)),
        (UnrPolyFlags.NotSolid, nameof(UnrPolyFlags.NotSolid)),
        (UnrPolyFlags.Environment, nameof(UnrPolyFlags.Environment)),
        (UnrPolyFlags.SemiSolid, nameof(UnrPolyFlags.SemiSolid)),
        (UnrPolyFlags.Modulated, nameof(UnrPolyFlags.Modulated)),
        (UnrPolyFlags.FakeBackdrop, nameof(UnrPolyFlags.FakeBackdrop)),
        (UnrPolyFlags.TwoSided, nameof(UnrPolyFlags.TwoSided)),
        (UnrPolyFlags.AutoUPan, nameof(UnrPolyFlags.AutoUPan)),
        (UnrPolyFlags.AutoVPan, nameof(UnrPolyFlags.AutoVPan)),
        (UnrPolyFlags.NoSmooth, nameof(UnrPolyFlags.NoSmooth)),
        (UnrPolyFlags.AlphaTexture, nameof(UnrPolyFlags.AlphaTexture)),
        (UnrPolyFlags.Flat, nameof(UnrPolyFlags.Flat)),
        (UnrPolyFlags.NoMerge, nameof(UnrPolyFlags.NoMerge)),
        (UnrPolyFlags.NoZTest, nameof(UnrPolyFlags.NoZTest)),
        (UnrPolyFlags.Additive, nameof(UnrPolyFlags.Additive)),
        (UnrPolyFlags.SpecialLit, nameof(UnrPolyFlags.SpecialLit)),
        (UnrPolyFlags.Wireframe, nameof(UnrPolyFlags.Wireframe)),
        (UnrPolyFlags.Unlit, nameof(UnrPolyFlags.Unlit)),
        (UnrPolyFlags.Portal, nameof(UnrPolyFlags.Portal)),
        (UnrPolyFlags.Mirror, nameof(UnrPolyFlags.Mirror))
    ];

    public static UnrPolyFlags GetKnownFlags(uint rawFlags)
    {
        var value = UnrPolyFlags.None;
        foreach (var item in KnownFlags)
        {
            if ((rawFlags & (uint)item.Flag) != 0)
            {
                value |= item.Flag;
            }
        }

        return value;
    }

    public static uint GetUnknownBits(uint rawFlags)
    {
        var knownMask = 0u;
        foreach (var item in KnownFlags)
        {
            knownMask |= (uint)item.Flag;
        }

        return rawFlags & ~knownMask;
    }

    public static string[] GetNames(uint rawFlags)
    {
        var names = new List<string>();
        foreach (var item in KnownFlags)
        {
            if ((rawFlags & (uint)item.Flag) != 0)
            {
                names.Add(item.Name);
            }
        }

        var unknownBits = GetUnknownBits(rawFlags);
        if (unknownBits != 0)
        {
            for (var bit = 0; bit < 32; bit++)
            {
                var mask = 1u << bit;
                if ((unknownBits & mask) != 0)
                {
                    names.Add($"Unknown_0x{mask:X8}");
                }
            }
        }

        return names.ToArray();
    }
}
