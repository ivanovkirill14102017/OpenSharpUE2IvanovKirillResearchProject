namespace L2Viewer.PackageCore;

/// <summary>
/// Known internal Unreal Engine 2 Licensee versions found in Lineage 2 Interlude client packages.
/// </summary>
public enum L2LicenseeVersion : ushort
{
    Ver0 = 0,
    Ver6 = 6,
    Ver11 = 11,
    Ver12 = 12,
    Ver14 = 14,
    Ver15 = 15,
    Ver16 = 16,
    Ver18 = 18,
    Ver19 = 19,
    Ver20 = 20,
    Ver21 = 21,
    Ver22 = 22,
    Ver23 = 23,
    Ver25 = 25,
    Ver27 = 27,
    Ver28 = 28,
    Ver30 = 30,
    Ver31 = 31,
    Ver33 = 33
}

/// <summary>
/// Known internal Unreal Engine 2 versions.
/// </summary>
public enum L2PackageVersion : ushort
{
    Ver61 = 61,
    Ver123 = 123,
    Ver146 = 146
}

public static class PackageDataExtensions
{
    /// <summary>
    /// Checks if the package's licensee version is at least the specified version.
    /// </summary>
    public static bool IsLicenseeVersionAtLeast(this PackageData package, L2LicenseeVersion targetVersion)
    {
        return package.Header.LicenseeVersion >= (ushort)targetVersion;
    }

    /// <summary>
    /// Checks if the package's version is at least the specified version.
    /// </summary>
    public static bool IsVersionAtLeast(this PackageData package, L2PackageVersion targetVersion)
    {
        return package.Header.Version >= (ushort)targetVersion;
    }
}
