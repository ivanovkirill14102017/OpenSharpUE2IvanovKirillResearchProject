namespace L2Viewer.UnrFile;

internal static class UnrPropertyTagExtensions
{
    internal static bool ThrowIfNotTagEncodedBool(this StreamPropertyTag tag, string message)
    {
        if (tag.Type != PackageReader.PropertyTypeBool || tag.DataSize != 0)
        {
            throw new PackageReadException(message);
        }

        return tag.BoolValue;
    }

    internal static bool IsFullyEncodedInTag(this StreamPropertyTag tag)
    {
        return tag.Type == PackageReader.PropertyTypeBool && tag.DataSize == 0;
    }

    internal static void EnsurePropertyFullyConsumed(
        this StreamPropertyTag tag,
        long consumed,
        string className,
        int exportIndex,
        string objectName)
    {
        if (consumed == tag.DataSize)
        {
            return;
        }

        if (tag.Type == PackageReader.PropertyTypeStruct && consumed == tag.DataSize + 1)
        {
            return;
        }

        throw new PackageReadException(
            $"{className} export {exportIndex} ({objectName}) property '{tag.Name}' consumed {consumed} bytes, expected {tag.DataSize}.");
    }
}
