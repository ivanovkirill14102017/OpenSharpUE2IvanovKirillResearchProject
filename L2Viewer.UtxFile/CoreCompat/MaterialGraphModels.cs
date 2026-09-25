namespace L2Viewer.UtxFile;

public enum MaterialGraphRootClass
{
    ColorModifier,
    Combiner,
    Cubemap,
    FadeColor,
    FinalBlend,
    FireTexture,
    GlowModifier,
    Shader,
    TexCoordSource,
    TexEnvMap,
    TexOscillator,
    TexOscillatorTriggered,
    TexPanner,
    TexRotator,
    TexScaler,
    Texture,
    WetTexture
}

public static class MaterialGraphRootClassParser
{
    public static MaterialGraphRootClass Parse(string className)
    {
        if (Enum.TryParse<MaterialGraphRootClass>(className, true, out var result) &&
            Enum.IsDefined(typeof(MaterialGraphRootClass), result))
        {
            return result;
        }

        throw new InvalidDataException($"Unsupported Unreal material root class '{className}'.");
    }
}

public sealed record MaterialBooleanParameter(string Name, bool Value);

public sealed record MaterialNumericParameter(string Name, double Value, string NumericKind);

public sealed record MaterialTextParameter(string Name, string Text, string TextKind);

public sealed record MaterialObjectSlot(
    string SlotName,
    string Reference,
    string ClassName,
    string ObjectName,
    string PackageName,
    string? PackagePath);

public sealed record MaterialTextureSlot(
    string SlotName,
    string Reference,
    string ClassName,
    string ObjectName,
    string PackageName,
    string? PackagePath,
    TextureData? Texture);

public sealed record MaterialGraphNode(
    string Reference,
    string ClassName,
    string ObjectName,
    string PackageName,
    string? PackagePath,
    IReadOnlyList<MaterialTextureSlot> TextureSlots,
    IReadOnlyList<MaterialObjectSlot> ObjectSlots,
    IReadOnlyList<MaterialBooleanParameter> BooleanParameters,
    IReadOnlyList<MaterialNumericParameter> NumericParameters,
    IReadOnlyList<MaterialTextParameter> TextParameters);

public sealed record ResolvedMaterialGraph(
    string RootReference,
    MaterialGraphRootClass RootClass,
    string RootObjectName,
    string RootPackageName,
    IReadOnlyList<MaterialGraphNode> Nodes,
    IReadOnlyList<MaterialTextureSlot> TextureSlots);

public sealed record UnrResolvedObjectRef(
    string ClassName,
    string ObjectName,
    string PackageName,
    int? ExportIndex,
    string? PackagePath);
