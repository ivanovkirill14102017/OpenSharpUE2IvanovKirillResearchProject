using System.Xml.Linq;
using L2Viewer.DbFile.DbJson;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class ScenePlayableSkillCatalogBuilder
{
    public ScenePlayableSkillCatalogData Build(string dbRootPath)
    {
        if (string.IsNullOrWhiteSpace(dbRootPath))
        {
            throw new ArgumentException("DB root path is required.", nameof(dbRootPath));
        }

        var fullDbRoot = Path.GetFullPath(dbRootPath);
        if (!Directory.Exists(fullDbRoot))
        {
            throw new DirectoryNotFoundException($"DB root path was not found: '{fullDbRoot}'.");
        }

        var classRows = TableJsonMapper.Read<ClassListRow>(Path.Combine(fullDbRoot, "class_list.json"));
        var nonPassiveSkillIds = LoadNonPassiveSkillIds(fullDbRoot);
        var skillRows = TableJsonMapper.Read<SkillTreeRow>(Path.Combine(fullDbRoot, "skill_trees.json"));
        var skillsByClass = skillRows
            .Where(x => x.skill_id > 0 && nonPassiveSkillIds.Contains(x.skill_id) && !string.IsNullOrWhiteSpace(x.name))
            .GroupBy(x => x.class_id)
            .ToDictionary(
                x => x.Key,
                x => x
                    .GroupBy(y => y.skill_id)
                    .Select(group =>
                    {
                        var first = group
                            .OrderBy(y => y.min_level)
                            .ThenBy(y => y.level)
                            .First();
                        return new ScenePlayableSkillData
                        {
                            SkillId = first.skill_id,
                            SkillName = first.name,
                            MinLevel = group.Min(y => y.min_level)
                        };
                    })
                    .OrderBy(y => y.MinLevel)
                    .ThenBy(y => y.SkillName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(y => y.SkillId)
                    .ToArray() as IReadOnlyList<ScenePlayableSkillData>);

        var classes = classRows
            .OrderBy(x => x.id)
            .Select(x => new ScenePlayableClassSkillData
            {
                ClassId = x.id,
                ClassName = NormalizeClassName(x.class_name),
                ParentClassId = x.parent_id,
                Skills = skillsByClass.TryGetValue(x.id, out var skills)
                    ? skills
                    : Array.Empty<ScenePlayableSkillData>()
            })
            .ToArray();

        return new ScenePlayableSkillCatalogData
        {
            Classes = classes
        };
    }

    private static HashSet<int> LoadNonPassiveSkillIds(string dbRootPath)
    {
        var dbRoot = new DirectoryInfo(dbRootPath);
        var dataRoot = dbRoot.Parent?.FullName
            ?? throw new DirectoryNotFoundException($"DB root parent was not found for '{dbRootPath}'.");
        var statsSkillRoot = Path.Combine(dataRoot, "stats", "skills");
        if (!Directory.Exists(statsSkillRoot))
        {
            throw new DirectoryNotFoundException($"Skill stats root was not found: '{statsSkillRoot}'.");
        }

        var skillIds = new HashSet<int>();
        foreach (var filePath in Directory.EnumerateFiles(statsSkillRoot, "*.xml", SearchOption.TopDirectoryOnly))
        {
            var document = XDocument.Load(filePath);
            foreach (var skillElement in document.Descendants("skill"))
            {
                var idText = skillElement.Attribute("id")?.Value;
                if (!int.TryParse(idText, out var skillId))
                {
                    continue;
                }

                var operateType = skillElement
                    .Elements("set")
                    .FirstOrDefault(x => string.Equals((string?)x.Attribute("name"), "operateType", StringComparison.OrdinalIgnoreCase))
                    ?.Attribute("val")
                    ?.Value;
                if (!string.Equals(operateType, "OP_PASSIVE", StringComparison.OrdinalIgnoreCase))
                {
                    skillIds.Add(skillId);
                }
            }
        }

        return skillIds;
    }
    private static string NormalizeClassName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        var trimmed = value.Trim();
        return trimmed
            .Replace("DE_", "Dark Elf ", StringComparison.OrdinalIgnoreCase)
            .Replace("H_", "Human ", StringComparison.OrdinalIgnoreCase)
            .Replace("E_", "Elf ", StringComparison.OrdinalIgnoreCase)
            .Replace("O_", "Orc ", StringComparison.OrdinalIgnoreCase)
            .Replace("D_", "Dwarf ", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ClassListRow
    {
        public string class_name { get; set; } = string.Empty;
        public int id { get; set; }
        public int parent_id { get; set; }
    }

    private sealed class SkillTreeRow
    {
        public int class_id { get; set; }
        public int skill_id { get; set; }
        public int level { get; set; }
        public string name { get; set; } = string.Empty;
        public int sp { get; set; }
        public int min_level { get; set; }
    }
}

