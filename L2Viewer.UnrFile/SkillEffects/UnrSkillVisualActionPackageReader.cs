using System.Numerics;
using L2Viewer.PackageCore;

namespace L2Viewer.UnrFile;

public sealed class UnrSkillVisualActionSet
{
    public required string GroupName { get; init; }
    public required string ObjectName { get; init; }
    public required int ExportIndex { get; init; }
    public string? Desc { get; init; }
    public required IReadOnlyList<UnrSkillVisualAction> Actions { get; init; }
    public float? FlyingTime { get; init; }
}

public sealed class UnrSkillVisualAction
{
    public required UnrSkillVisualActionPhase Phase { get; init; }
    public required int PhaseIndex { get; init; }
    public required int SpecificStage { get; init; }
    public required int ExportIndex { get; init; }
    public required string ActionClassName { get; init; }
    public string? EffectClassName { get; init; }
    public string? EffectPackageName { get; init; }
    public byte? AttachOn { get; init; }
    public string? AttachBoneName { get; init; }
    public Vector3? Offset { get; init; }
    public bool? SpawnOnTarget { get; init; }
    public bool? RelativeToCylinder { get; init; }
    public bool? UseCharacterRotation { get; init; }
    public bool? Absolute { get; init; }
    public bool? OnMultiTarget { get; init; }
    public bool? SizeScale { get; init; }
    public float? SpawnDelay { get; init; }
}

public enum UnrSkillVisualActionPhase
{
    CastingActions = 0,
    ChannelingActions = 1,
    PreshotActions = 2,
    ShotActions = 3,
    ExplosionActions = 4
}

public static class UnrSkillVisualActionPackageReader
{
    public static IReadOnlyList<UnrSkillVisualActionSet> Read(string path)
    {
        var package = PackageReader.LoadPackage(path);
        var result = new List<UnrSkillVisualActionSet>();
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var export = package.Exports[i];
            if (!PackageReader.ExportClassName(package, export).Equals("SkillVisualEffect", StringComparison.Ordinal))
            {
                continue;
            }

            var groupName = ResolveOuterName(package, export.PackageIndex);
            var actions = new List<UnrSkillVisualAction>();
            float? flyingTime = null;
            string? desc = null;
            foreach (var property in ReadProperties(package, export))
            {
                if (TryResolvePhase(property.Tag.Name, out var phase))
                {
                    using var reader = NewReader(property.Data);
                    var count = PackageReader.ReadCompactIndex(reader);
                    if (count < 0 || count > 4096)
                    {
                        throw new PackageReadException($"Invalid {phase} count: {count}.");
                    }

                    for (var actionIndex = 0; actionIndex < count; actionIndex++)
                    {
                        var actionRef = 0;
                        var specificStage = 0;
                        foreach (var field in ReadProperties(reader, package.Names))
                        {
                            switch (field.Tag.Name)
                            {
                                case "Action": actionRef = ReadIndex(field.Data); break;
                                case "SpecificStage": specificStage = BitConverter.ToInt32(field.Data, 0); break;
                                default: throw new PackageReadException($"Unsupported SkillActionInfo property '{field.Tag.Name}'.");
                            }
                        }

                        if (actionRef <= 0 || actionRef > package.Exports.Count)
                        {
                            throw new PackageReadException($"Invalid {phase} action reference: {actionRef}.");
                        }

                        actions.Add(ReadAction(package, actionRef - 1, phase, actionIndex, specificStage));
                    }
                }
                else if (property.Tag.Name == "FlyingTime")
                {
                    flyingTime = BitConverter.ToSingle(property.Data, 0);
                }
                else if (property.Tag.Name == "Desc")
                {
                    desc = PackageReader.SafeName(package.Names, ReadIndex(property.Data));
                }
                else
                {
                    throw new PackageReadException($"Unsupported SkillVisualEffect property '{property.Tag.Name}'.");
                }
            }

            result.Add(new UnrSkillVisualActionSet
            {
                GroupName = groupName,
                ObjectName = PackageReader.SafeName(package.Names, export.ObjectName),
                ExportIndex = i,
                Desc = desc,
                Actions = actions,
                FlyingTime = flyingTime
            });
        }

        return result;
    }

    private static bool TryResolvePhase(string propertyName, out UnrSkillVisualActionPhase phase)
    {
        switch (propertyName)
        {
            case "CastingActions": phase = UnrSkillVisualActionPhase.CastingActions; return true;
            case "ChannelingActions": phase = UnrSkillVisualActionPhase.ChannelingActions; return true;
            case "PreshotActions": phase = UnrSkillVisualActionPhase.PreshotActions; return true;
            case "ShotActions": phase = UnrSkillVisualActionPhase.ShotActions; return true;
            case "ExplosionActions": phase = UnrSkillVisualActionPhase.ExplosionActions; return true;
            default: phase = default; return false;
        }
    }

    private static UnrSkillVisualAction ReadAction(PackageData package, int exportIndex, UnrSkillVisualActionPhase phase, int phaseIndex, int specificStage)
    {
        var export = package.Exports[exportIndex];
        string? effectClass = null, effectPackage = null, attachBone = null;
        byte? attachOn = null;
        Vector3? offset = null;
        bool? onTarget = null, relative = null, characterRotation = null, absolute = null, multiTarget = null, sizeScale = null;
        float? spawnDelay = null;
        foreach (var property in ReadProperties(package, export))
        {
            switch (property.Tag.Name)
            {
                case "EffectClass":
                    var classRef = ReadIndex(property.Data);
                    (effectPackage, effectClass) = ResolveReference(package, classRef);
                    break;
                case "AttachOn": attachOn = property.Data[0]; break;
                case "AttachBoneName": attachBone = PackageReader.SafeName(package.Names, ReadIndex(property.Data)); break;
                case "offset":
                case "Offset":
                    offset = new Vector3(BitConverter.ToSingle(property.Data, 0), BitConverter.ToSingle(property.Data, 4), BitConverter.ToSingle(property.Data, 8));
                    break;
                case "bSpawnOnTarget": onTarget = property.Tag.BoolValue; break;
                case "bRelativeToCylinder": relative = property.Tag.BoolValue; break;
                case "bUseCharacterRotation": characterRotation = property.Tag.BoolValue; break;
                case "bAbsolute": absolute = property.Tag.BoolValue; break;
                case "bOnMultiTarget": multiTarget = property.Tag.BoolValue; break;
                case "bSizeScale": sizeScale = property.Tag.BoolValue; break;
                case "SpawnDelay": spawnDelay = BitConverter.ToSingle(property.Data, 0); break;
                default: throw new PackageReadException($"Unsupported SkillAction property '{property.Tag.Name}' in export {exportIndex}.");
            }
        }

        return new UnrSkillVisualAction
        {
            Phase = phase,
            PhaseIndex = phaseIndex,
            SpecificStage = specificStage,
            ExportIndex = exportIndex,
            ActionClassName = PackageReader.ExportClassName(package, export),
            EffectClassName = effectClass,
            EffectPackageName = effectPackage,
            AttachOn = attachOn,
            AttachBoneName = attachBone,
            Offset = offset,
            SpawnOnTarget = onTarget,
            RelativeToCylinder = relative,
            UseCharacterRotation = characterRotation,
            Absolute = absolute,
            OnMultiTarget = multiTarget,
            SizeScale = sizeScale,
            SpawnDelay = spawnDelay
        };
    }

    private static (string? Package, string? Name) ResolveReference(PackageData package, int reference)
    {
        if (reference < 0)
        {
            var import = package.Imports[-reference - 1];
            return (ResolveOuterName(package, import.PackageIndex), PackageReader.SafeName(package.Names, import.ObjectName));
        }

        if (reference > 0)
        {
            var export = package.Exports[reference - 1];
            return (ResolveOuterName(package, export.PackageIndex), PackageReader.SafeName(package.Names, export.ObjectName));
        }

        return (null, null);
    }

    private static string ResolveOuterName(PackageData package, uint packageIndex)
    {
        var index = unchecked((int)packageIndex);
        if (index > 0 && index <= package.Exports.Count)
        {
            return PackageReader.SafeName(package.Names, package.Exports[index - 1].ObjectName);
        }

        if (index < 0 && -index <= package.Imports.Count)
        {
            return PackageReader.SafeName(package.Names, package.Imports[-index - 1].ObjectName);
        }

        return string.Empty;
    }

    private static int ReadIndex(byte[] data)
    {
        using var reader = NewReader(data);
        return PackageReader.ReadCompactIndex(reader);
    }

    private static BinaryReader NewReader(byte[] data) => new(new MemoryStream(data, writable: false));

    private static IEnumerable<(UnrealPropertyTag Tag, byte[] Data)> ReadProperties(PackageData package, ExportEntry export)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        return ReadProperties(reader, package.Names).ToArray();
    }

    private static IEnumerable<(UnrealPropertyTag Tag, byte[] Data)> ReadProperties(BinaryReader reader, IReadOnlyList<string> names)
    {
        for (var i = 0; i < 4096; i++)
        {
            if (!PackageReader.TryReadPropertyTag(reader, names, out var tag))
            {
                throw new PackageReadException("Invalid skill visual property tag.");
            }

            if (tag.IsEnd)
            {
                yield break;
            }

            var data = reader.ReadBytes(tag.DataSize);
            if (data.Length != tag.DataSize)
            {
                throw new PackageReadException($"Truncated skill visual property '{tag.Name}'.");
            }

            yield return (tag, data);
        }

        throw new PackageReadException("Too many skill visual properties.");
    }
}
