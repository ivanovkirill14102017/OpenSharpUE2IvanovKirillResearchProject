namespace L2Viewer.UnrFile;

public sealed class UnrScriptClassObject
{
    public required int ExportIndex { get; init; }
    public required string ObjectName { get; init; }
    public string? SuperClassName { get; init; }
    public required string ScriptText { get; init; }
}

public sealed class UnrEmitterClassObject
{
    public required int ExportIndex { get; init; }
    public required string ObjectName { get; init; }
    public string? SuperClassName { get; init; }
    public required IReadOnlyList<UnrFileObject> Layers { get; init; }
}
