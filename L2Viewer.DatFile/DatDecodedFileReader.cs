using L2Viewer.PackageCore;

namespace L2Viewer.DatFile;

public static class DatDecodedFileReader
{
    public static byte[] ReadDecodedBytes(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("DAT path is required.", nameof(path));
        }

        var bytes = File.ReadAllBytes(path);
        var (wrapper, prefixLength) = Lineage2FileProfiles.DetectWrapper(bytes);
        return wrapper switch
        {
            "Lineage2Ver413" => Lineage2FileProfiles.DecodeWrappedFile(bytes, wrapper, prefixLength),
            "raw" => bytes,
            _ => throw new NotSupportedException($"DAT wrapper '{wrapper}' is not supported for decoding.")
        };
    }
}
