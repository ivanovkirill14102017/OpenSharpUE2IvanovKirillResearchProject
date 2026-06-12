namespace L2Viewer.UnrFile;

internal static class UnrCompat
{
    public static string ToHexString(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        return BitConverter.ToString(bytes).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
