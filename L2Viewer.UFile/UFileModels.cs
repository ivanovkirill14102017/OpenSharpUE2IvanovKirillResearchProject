using L2Viewer.PackageCore;

namespace L2Viewer.UFile;

public sealed record UFileDocument(
    string Path,
    IReadOnlyList<string> Names,
    IReadOnlyList<UImportObject> Imports,
    PackageHeader Header,
    IReadOnlyList<UExportObject> Exports);

public sealed record UImportObject(
    int ImportIndex,
    string ClassPackageName,
    string ClassName,
    uint PackageIndex,
    string ObjectName);

public sealed record UExportObject(
    int ExportIndex,
    int ClassIndex,
    int SuperIndex,
    uint PackageIndex,
    string ObjectName,
    string ClassName,
    uint ObjectFlags,
    int SerialSize,
    int? SerialOffset,
    IReadOnlyList<UPropertyValue> Properties,
    byte[] TrailingData);

public sealed record UPropertyValue(
    int Order,
    UnrealPropertyTag Tag,
    byte[] RawData);

public sealed record UScriptClassDeclaration(
    string ClassName,
    string SuperClassName);

public sealed record UScriptRoutine(
    string Kind,
    string Name);

public sealed record UTextBufferExport(
    int ExportIndex,
    string ObjectName,
    string Text,
    UScriptClassDeclaration? ClassDeclaration,
    IReadOnlyList<UScriptRoutine> Routines);
