using System;

namespace L2Viewer.SceneDomain;

/// <summary>
/// Marks APIs that are intended for external use (e.g., Unity plugin).
/// Helps distinguish between actually unused legacy code and code that is externally consumed.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class ForExternalUseAttribute : Attribute
{
}
