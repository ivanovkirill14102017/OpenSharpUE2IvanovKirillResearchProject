using L2Viewer.PackageCore;
using System.Text;
using System.Text.RegularExpressions;

namespace L2Viewer.UFile;

public static class UFileReader
{
    private static readonly Regex ClassDeclarationRegex = new(
        @"\bclass\s+(?<class>[A-Za-z0-9_]+)\s+extends\s+(?<super>[A-Za-z0-9_]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ScriptRoutineRegex = new(
        @"\b(?<kind>function|event)\s+(?<name>[A-Za-z0-9_]+)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static UFileDocument Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("U package path is required.", nameof(path));
        }

        var package = PackageReader.LoadPackage(path);
        var imports = package.Imports
            .Select((x, i) => new UImportObject(
                i,
                PackageReader.SafeName(package.Names, x.ClassPackage),
                PackageReader.SafeName(package.Names, x.ClassName),
                x.PackageIndex,
                PackageReader.SafeName(package.Names, x.ObjectName)))
            .ToArray();
        var exports = new List<UExportObject>(package.Exports.Count);
        for (var i = 0; i < package.Exports.Count; i++)
        {
            exports.Add(ReadExport(package, package.Exports[i], i));
        }

        return new UFileDocument(path, package.Names.ToArray(), imports, package.Header, exports);
    }

    public static IReadOnlyList<UTextBufferExport> ReadTextBufferExports(string path)
    {
        return ReadTextBufferExports(Read(path));
    }

    public static IReadOnlyList<UTextBufferExport> ReadTextBufferExports(UFileDocument file)
    {
        return file.Exports
            .Where(static x => string.Equals(x.ClassName, "TextBuffer", StringComparison.OrdinalIgnoreCase))
            .Select(ReadTextBufferExport)
            .ToArray();
    }

    public static UTextBufferExport ReadTextBufferExport(UExportObject export)
    {
        if (!string.Equals(export.ClassName, "TextBuffer", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Export {export.ExportIndex} is not a TextBuffer.", nameof(export));
        }

        var text = ReadTextBufferText(export);
        var declaration = ParseClassDeclaration(text);
        var routines = ParseScriptRoutines(text);
        return new UTextBufferExport(export.ExportIndex, export.ObjectName, text, declaration, routines);
    }

    public static string ReadTextBufferText(UExportObject export)
    {
        if (!string.Equals(export.ClassName, "TextBuffer", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Export {export.ExportIndex} is not a TextBuffer.", nameof(export));
        }

        var payload = export.TrailingData;
        if (payload.Length < sizeof(long))
        {
            throw new PackageReadException($"TextBuffer export {export.ExportIndex} payload is too short.");
        }

        var position = sizeof(long);
        var length = PackageReader.ReadCompactIndex(payload, ref position);
        if (length == 0)
        {
            return string.Empty;
        }

        if (length > 0)
        {
            if (position + length > payload.Length)
            {
                throw new PackageReadException($"TextBuffer export {export.ExportIndex} string payload exceeds export length.");
            }

            return Encoding.GetEncoding(28591).GetString(payload, position, length).TrimEnd('\0');
        }

        var charCount = checked(-length);
        var byteLength = checked(charCount * sizeof(char));
        if (position + byteLength > payload.Length)
        {
            throw new PackageReadException($"TextBuffer export {export.ExportIndex} unicode payload exceeds export length.");
        }

        return Encoding.Unicode.GetString(payload, position, byteLength).TrimEnd('\0');
    }

    public static UScriptClassDeclaration? ParseClassDeclaration(string text)
    {
        var match = ClassDeclarationRegex.Match(text);
        return match.Success
            ? new UScriptClassDeclaration(match.Groups["class"].Value, match.Groups["super"].Value)
            : null;
    }

    public static IReadOnlyList<UScriptRoutine> ParseScriptRoutines(string text)
    {
        return ScriptRoutineRegex.Matches(text)
            .Select(static x => new UScriptRoutine(
                x.Groups["kind"].Value,
                x.Groups["name"].Value))
            .ToArray();
    }

    private static UExportObject ReadExport(PackageData package, ExportEntry export, int exportIndex)
    {
        var payload = PackageReader.ReadExportBlob(package, export);
        var properties = new List<UPropertyValue>();
        var position = 0;

        for (var order = 0; order < 4096; order++)
        {
            if (!PackageReader.TryReadPropertyTag(payload, package.Names, ref position, out var tag))
            {
                position = 0;
                properties.Clear();
                break;
            }

            if (tag.IsEnd)
            {
                break;
            }

            if (position + tag.DataSize > payload.Length)
            {
                position = 0;
                properties.Clear();
                break;
            }

            var raw = payload.AsSpan(position, tag.DataSize).ToArray();
            properties.Add(new UPropertyValue(order, tag, raw));
            position += tag.DataSize;
        }

        var trailingData = position < payload.Length
            ? payload.AsSpan(position).ToArray()
            : Array.Empty<byte>();

        return new UExportObject(
            exportIndex,
            export.ClassIndex,
            export.SuperIndex,
            export.PackageIndex,
            PackageReader.SafeName(package.Names, export.ObjectName),
            PackageReader.ExportClassName(package, export),
            export.ObjectFlags,
            export.SerialSize,
            export.SerialOffset,
            properties,
            trailingData);
    }
}
