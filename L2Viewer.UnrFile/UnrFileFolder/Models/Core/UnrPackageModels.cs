namespace L2Viewer.UnrFile;

public sealed class UnrFile
{
    public required string FilePath { get; init; }
    public required string Wrapper { get; init; }
    public required UnrFileHeader Header { get; init; }
    public required UnrFileNameEntry[] Names { get; init; }
    public required UnrFileImportEntry[] Imports { get; init; }
    public required UnrFileExportObjectEntry[] ExportObjects { get; init; }
}

public sealed class UnrFileHeader
{
    public required ushort Version { get; init; }
    public required ushort LicenseeVersion { get; init; }
    public required uint Flags { get; init; }
    public required uint NameCount { get; init; }
    public required uint NameOffset { get; init; }
    public required uint ExportCount { get; init; }
    public required uint ExportOffset { get; init; }
    public required uint ImportCount { get; init; }
    public required uint ImportOffset { get; init; }
}

public sealed class UnrFileNameEntry
{
    public required int Index { get; init; }
    public required string Name { get; init; }
}

public sealed class UnrFileImportEntry
{
    public required int Index { get; init; }
    public required int ClassPackage { get; init; }
    public required int ClassName { get; init; }
    public required uint PackageIndex { get; init; }
    public required int ObjectName { get; init; }
}

public sealed class UnrFileExportEntry
{
    public required int Index { get; init; }
    public required int ClassIndex { get; init; }
    public required int SuperIndex { get; init; }
    public required uint PackageIndex { get; init; }
    public required int ObjectName { get; init; }
    public required uint ObjectFlags { get; init; }
    public required int SerialSize { get; init; }
    public required int? SerialOffset { get; init; }
}

public sealed class UnrFileExportObjectEntry
{
    public required UnrFileExportEntry Export { get; init; }
    public required UnrFileObject Object { get; init; }
}
