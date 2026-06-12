namespace L2Viewer.UnrFile;

internal static class PackageCorePropertyTagExtensions
{
    internal static StreamPropertyTag ToStreamPropertyTag(this UnrealPropertyTag tag)
    {
        return new StreamPropertyTag(
            tag.Name,
            tag.Type,
            tag.DataSize,
            tag.ArrayIndex,
            tag.StructName,
            tag.BoolValue,
            string.Equals(tag.Name, "None", StringComparison.Ordinal));
    }
}
