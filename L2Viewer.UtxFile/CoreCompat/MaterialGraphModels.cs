namespace L2Viewer.UtxFile;

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
    string RootClassName,
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
