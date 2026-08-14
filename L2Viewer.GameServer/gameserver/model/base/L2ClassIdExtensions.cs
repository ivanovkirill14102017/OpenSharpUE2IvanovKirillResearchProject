using System.Reflection;

namespace L2Viewer.GameServer.gameserver.model.@base;

public static class L2ClassIdExtensions
{
    public static ClassIdInfoAttribute GetInfo(this L2ClassId classId)
    {
        var member = typeof(L2ClassId).GetMember(classId.ToString()).FirstOrDefault();
        var info = member?.GetCustomAttribute<ClassIdInfoAttribute>();
        if (info == null)
        {
            throw new InvalidOperationException($"ClassId metadata is missing for '{classId}'.");
        }

        return info;
    }

    public static L2Race GetRace(this L2ClassId classId)
    {
        return classId.GetInfo().Race;
    }

    public static bool IsMage(this L2ClassId classId)
    {
        return classId.GetInfo().IsMage;
    }

    public static bool IsRootClass(this L2ClassId classId)
    {
        return classId.GetInfo().IsRoot;
    }

    public static L2ClassId? GetParentClassId(this L2ClassId classId)
    {
        var parent = classId.GetInfo().ParentClassId;
        return parent < 0 ? null : (L2ClassId)parent;
    }

    public static L2ClassId GetRootClassId(this L2ClassId classId)
    {
        var current = classId;
        while (current.GetParentClassId() is { } parent)
        {
            current = parent;
        }

        return current;
    }

    public static bool IsChildOf(this L2ClassId classId, L2ClassId parentClassId)
    {
        var current = classId.GetParentClassId();
        while (current.HasValue)
        {
            if (current.Value == parentClassId)
            {
                return true;
            }

            current = current.Value.GetParentClassId();
        }

        return false;
    }
}
