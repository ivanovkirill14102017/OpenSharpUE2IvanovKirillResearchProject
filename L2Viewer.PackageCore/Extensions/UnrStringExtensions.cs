public static class UnrStringExtensions
{
    public static bool Is(this string? value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}
