using L2Viewer.UnrFile;

namespace L2Viewer.SceneDomain.Services;

public static class BspWorldModelPolicy
{
    private const string TombMapFileName = "20_20.unr";
    private const string TombPrimaryModelName = "Model798";
    private static readonly int[] TombForcedOutdoorZones = [1];

    public static bool ShouldIncludeBspModel(string? mapPath, UnrModelObject model, bool isWorldModelCandidate)
    {
        if (IsTombMap(mapPath))
        {
            return string.Equals(model.ObjectName, TombPrimaryModelName, StringComparison.OrdinalIgnoreCase);
        }

        return isWorldModelCandidate;
    }

    public static UnrModelObject? ResolvePreferredWorldModel(L2Viewer.UnrFile.UnrFile unr)
    {
        var worldModelExportIndices = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrLevelObject>()
            .Where(x => x.ModelReference?.ExportIndex is not null)
            .Select(x => x.ModelReference!.ExportIndex!.Value)
            .ToHashSet();

        var allModels = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrModelObject>()
            .ToArray();

        if (IsTombMap(unr.FilePath))
        {
            var explicitTombModel = allModels.FirstOrDefault(x => string.Equals(x.ObjectName, TombPrimaryModelName, StringComparison.OrdinalIgnoreCase));
            if (explicitTombModel != null)
            {
                return explicitTombModel;
            }
        }

        var explicitWorldModel = allModels
            .Where(x => worldModelExportIndices.Contains(x.ExportIndex))
            .OrderByDescending(x => x.Nodes.Length)
            .FirstOrDefault();

        return explicitWorldModel
               ?? allModels.OrderByDescending(x => x.Nodes.Length).FirstOrDefault();
    }

    public static bool IsTombMap(string? mapPath)
    {
        if (string.IsNullOrWhiteSpace(mapPath))
        {
            return false;
        }

        return string.Equals(Path.GetFileName(mapPath), TombMapFileName, StringComparison.OrdinalIgnoreCase);
    }

    public static int[] ResolveForcedOutdoorZones(string? mapPath)
    {
        if (IsTombMap(mapPath))
        {
            return TombForcedOutdoorZones.ToArray();
        }

        return [];
    }
}
