using System.Text;

namespace L2Viewer.PackageCore;

public sealed class Lineage2IniDocument
{
    public required byte[] DecodedBytes { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<Lineage2IniEntry>> Sections { get; init; }

    public string GetRequired(string section, string key)
    {
        if (!Sections.TryGetValue(section, out var entries))
            throw new InvalidDataException($"Missing [{section}] {key} in Lineage II configuration.");
        var matches = entries.Where(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length == 0)
            throw new InvalidDataException($"Missing [{section}] {key} in Lineage II configuration.");
        if (matches.Length != 1)
            throw new InvalidDataException($"Duplicate [{section}] {key} in Lineage II configuration.");
        return matches[0].Value;
    }
}

public sealed record Lineage2IniEntry(string Key, string Value, int LineNumber);

public static class Lineage2IniReader
{
    public static Lineage2IniDocument Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var (wrapper, prefixLength) = Lineage2FileProfiles.DetectWrapper(bytes);
        var decoded = Lineage2FileProfiles.DecodeWrappedFile(bytes, wrapper, prefixLength);
        var sections = new Dictionary<string, List<Lineage2IniEntry>>(StringComparer.OrdinalIgnoreCase);
        List<Lineage2IniEntry>? current = null;

        // Latin-1 preserves every payload byte. The environment keys and values used by
        // SceneDomain are ASCII; consumers needing other text encodings can use DecodedBytes.
        using var lines = new StringReader(Encoding.GetEncoding(28591).GetString(decoded));
        var lineNumber = 0;
        while (lines.ReadLine() is { } line)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == ';' || trimmed[0] == '#')
                continue;
            if (trimmed[0] == '[' && trimmed[^1] == ']')
            {
                var section = trimmed[1..^1];
                if (!sections.TryGetValue(section, out current))
                {
                    current = new List<Lineage2IniEntry>();
                    sections.Add(section, current);
                }
                continue;
            }

            var equals = trimmed.IndexOf('=');
            if (equals <= 0 || current is null)
                continue;
            current.Add(new Lineage2IniEntry(trimmed[..equals].Trim(), trimmed[(equals + 1)..].Trim(), lineNumber));
        }

        return new Lineage2IniDocument
        {
            DecodedBytes = decoded,
            Sections = sections.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<Lineage2IniEntry>)pair.Value,
                StringComparer.OrdinalIgnoreCase)
        };
    }
}
