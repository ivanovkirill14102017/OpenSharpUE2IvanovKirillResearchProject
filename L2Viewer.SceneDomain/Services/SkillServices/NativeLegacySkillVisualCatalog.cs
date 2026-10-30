using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services.SkillServices;

// Offline transcription of direct skill-ID branches in the Interlude engine.dll
// SHA-256: 508974C711F207402719E92737E211A2F029C95C2F68FC0E1C31FCBB9DBB232D.
// Entries are data for the legacy visual path; importing never reads the DLL.
internal static class NativeLegacySkillVisualCatalog
{
    internal sealed record Stage(string ClassName, SceneSkillVisualPhase Phase, bool SpawnOnTarget, bool ReturnsToCaster);

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<Stage>> Entries = Build();

    internal static bool TryGet(int skillId, out IReadOnlyList<Stage> stages) => Entries.TryGetValue(skillId, out stages!);

    private static Stage C(string name) => new(name, SceneSkillVisualPhase.Casting, false, false);
    private static Stage T(string name) => new(name, SceneSkillVisualPhase.Shot, true, false);
    private static Stage P(string name) => new(name, SceneSkillVisualPhase.Shot, false, false);
    private static Stage R(string name) => new(name, SceneSkillVisualPhase.Shot, false, true);

    private static IReadOnlyDictionary<int, IReadOnlyList<Stage>> Build()
    {
        var result = new Dictionary<int, IReadOnlyList<Stage>>();
        Add(result, [1], [C("s_u010_b"), C("s_u010_a")]);
        Add(result, [2, 11, 12, 15, 51, 65, 106, 115, 122, 1064, 1069, 1071, 1072, 1074, 1075, 1083, 1223, 1224, 1273], [C("m_u018_a"), T("m_u018_b")]);
        Add(result, [3, 245], [C("s_u002_a"), T("p_u004_a")]);
        Add(result, [5, 261], [C("s_u010_b"), C("s_u010_a"), T("s_u015_b")]);
        Add(result, [6, 7], [C("s_u010_a"), P("s_u015_a")]);
        Add(result, [9], [C("s_u010_a"), T("m_u006_d"), T("m_u006_e")]);
        Add(result, [10, 13, 22, 25, 33, 67, 278, 283, 299, 301, 1111, 1128, 1129, 1154, 1225, 1226, 1227, 1228, 1276, 1277, 1278, 1279, 1280, 1281], [C("e_u011_a")]);
        Add(result, [16], [C("s_u001_a"), T("s_u001_b")]);
        Add(result, [17, 81], [C("s_u010_a"), T("m_u006_e")]);
        Add(result, [19, 314, 369], [C("s_u010_a"), T("s_u019_b"), P("s_u019_c")]);
        Add(result, [24, 56], [C("s_u003_a"), T("s_u019_b"), P("s_u019_c")]);
        Add(result, [27, 1068, 1182, 1191], [C("m_u004_a"), T("m_u004_b")]);
        Add(result, [29, 95], [C("s_u006_a"), T("s_u006_b")]);
        Add(result, [30, 36, 190, 255], [C("s_u010_b"), C("s_u010_a"), T("p_u004_a")]);
        Add(result, [35], [C("s_u010_a"), P("m_u006_c")]);
        Add(result, [46, 289], [C("s_u020_a"), T("s_u020_b"), R("s_u020_c")]);
        Add(result, [49], [C("m_u017_a"), T("m_u017_b")]);
        Add(result, [54], [C("s_u010_a"), P("m_u000_c")]);
        Add(result, [70], [C("s_u019_a"), T("s_u019_b"), R("s_u019_c")]);
        Add(result, [84], [C("s_u010_a")]);
        Add(result, [98, 1201], [C("m_u013_a"), T("m_u013_c")]);
        Add(result, [101], [C("s_u505_a"), T("s_u505_b")]);
        Add(result, [103, 1167, 1272], [C("m_u021_a"), T("m_u021_b")]);
        Add(result, [111, 123], [C("s_u017_a"), T("s_u017_b")]);
        Add(result, [120, 281, 284], [C("s_u010_a"), T("p_u004_a")]);
        Add(result, [129, 1156, 1160, 1163, 1164, 1168, 1169, 1170, 1222, 1263, 1269], [C("m_u007_a"), T("m_u007_b")]);
        Add(result, [254, 302], [C("s_u007_a"), T("s_u007_b")]);
        Add(result, [266, 304, 305, 306, 308], [T("s_u009_a")]);
        Add(result, [279], [C("s_u010_a"), T("s_u011_a")]);
        Add(result, [307, 309, 310, 311], [T("s_u012_a")]);
        Add(result, [343, 354], [C("s_u509_a"), T("s_u509_c"), T("s_u509_b")]);
        Add(result, [362], [C("s_u513_a"), T("s_u513_b"), T("s_u513_c")]);
        Add(result, [1002, 1003, 1004, 1007, 1229, 1249, 1251, 1253, 1256, 1282, 1364, 1365], [C("m_u024_a"), T("m_u024_b"), T("m_u024_c")]);
        Add(result, [1005, 1006, 1008, 1009, 1010, 1250, 1252, 1260, 1261, 1284], [C("m_u034_a"), T("m_u034_b"), T("m_u034_c")]);
        Add(result, [1016, 1254], [C("m_u023_a"), T("m_u023_b")]);
        Add(result, [1034, 1049], [C("m_u030_a"), T("m_u030_b")]);
        Add(result, [1040, 1232, 1238], [C("m_u008_a"), T("m_u008_b")]);
        Add(result, [1050, 1255], [C("e_u031_a"), T("e_u005_a")]);
        Add(result, [1056, 1073, 1139, 1140, 1141, 1144, 1145, 1146, 1257], [C("m_u016_a"), T("m_u016_b")]);
        Add(result, [1077, 1268], [C("m_u036_a"), T("m_u036_b")]);
        Add(result, [1090, 1147, 1151, 1234, 1245], [C("m_u003_a"), T("m_u003_c"), R("m_u003_b")]);
        Add(result, [1092, 1095, 1096, 1097, 1099, 1102, 1104, 1105, 1208, 1209, 1210, 1213, 1246, 1247, 1248, 1366, 1367], [C("m_u025_a"), T("m_u025_b"), T("m_u025_c")]);
        Add(result, [1100, 1107, 1244], [C("m_u028_a"), P("m_u028_b")]);
        Add(result, [1101, 1108], [C("m_u027_a"), T("m_u027_b")]);
        Add(result, [1148], [C("m_u003_a"), P("m_u000_c")]);
        Add(result, [1155], [C("m_u003_a"), T("m_u006_e"), T("m_u006_d")]);
        Add(result, [1159, 1266, 1267], [C("m_u003_a"), P("m_u006_c")]);
        Add(result, [1177], [C("m_u000_a"), C("m_u000_b"), P("m_u000_c")]);
        Add(result, [1189, 1204], [C("m_u010_a"), T("m_u010_c")]);
        Add(result, [1206], [C("m_u009_a"), T("m_u009_c")]);
        Add(result, [1243, 1304], [C("m_u044_a"), T("m_u044_b")]);
        Add(result, [1285], [C("m_u006_a"), T("m_u022_b")]);
        Add(result, [1287], [C("m_u038_a"), T("m_u022_b")]);
        Add(result, [1362, 1363], [C("m_u800_a"), T("m_u800_b")]);
        return result;
    }

    private static void Add(Dictionary<int, IReadOnlyList<Stage>> result, int[] ids, Stage[] stages)
    {
        foreach (var id in ids)
        {
            result.Add(id, stages);
        }
    }
}
