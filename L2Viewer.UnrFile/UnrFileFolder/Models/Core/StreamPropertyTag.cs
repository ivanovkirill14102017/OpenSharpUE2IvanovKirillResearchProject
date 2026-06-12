namespace L2Viewer.UnrFile;

internal sealed record StreamPropertyTag(
    string Name,
    int Type,
    int DataSize,
    int ArrayIndex,
    string? StructName,
    bool BoolValue,
    bool IsTerminator);
