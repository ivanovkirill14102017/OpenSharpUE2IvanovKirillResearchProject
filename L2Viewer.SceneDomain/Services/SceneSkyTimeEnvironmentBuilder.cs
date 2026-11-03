using System.Globalization;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneSkyTimeEnvironmentBuilder
{
    public SceneSkyTimeEnvironmentData Build(string clientRoot)
    {
        var system = Path.Combine(clientRoot, "system");
        var setup = Lineage2IniReader.Read(Path.Combine(system, "Env.int"));
        var count = ReadInt(setup, "EnvSetup", "TimeEnvFileNum");
        if (count <= 0)
            throw new InvalidDataException("Env.int contains no time environment files.");

        var materials = new[]
        {
            (SceneSkyTimeLayerRole.SkyBox, "SKYBOX", "SkyBoxColor"),
            (SceneSkyTimeLayerRole.HazeRing, "HAZERING", "HazeringColor"),
            (SceneSkyTimeLayerRole.Cloud1, "CLOUD1", "CloudColor1"),
            (SceneSkyTimeLayerRole.Cloud2, "Cloud2", "CloudColor2"),
            (SceneSkyTimeLayerRole.Cloud3, "Cloud3", "CloudColor3")
        };
        var cloudCount = ReadInt(setup, "EnvSetup", "NumCloudColor");
        if (cloudCount != 3)
            throw new InvalidDataException($"Unsupported Env.int cloud layer count: {cloudCount}.");

        var profiles = new SceneSkyTimeProfile[count];
        var seenTypes = new HashSet<int>();
        for (var index = 0; index < count; index++)
        {
            var filename = setup.GetRequired("EnvSetup", $"TimeEnvFileName{index}");
            if (!string.Equals(Path.GetFileName(filename), filename, StringComparison.Ordinal))
                throw new InvalidDataException($"Invalid time environment filename: {filename}.");
            var source = Lineage2IniReader.Read(Path.Combine(system, filename));
            var environmentType = ReadInt(source, "EnvType", "EnvType");
            if (!seenTypes.Add(environmentType))
                throw new InvalidDataException($"Duplicate EnvType: {environmentType}.");

            var tracks = materials.Select(layer => new SceneSkyTimeColorTrack(
                layer.Item1,
                ReadMaterialName(setup, layer.Item2),
                ReadColorTrack(source, layer.Item3))).ToArray();
            profiles[index] = new SceneSkyTimeProfile(environmentType, tracks);
        }

        return new SceneSkyTimeEnvironmentData(profiles);
    }

    private static SceneSkyTimeColorKey[] ReadColorTrack(Lineage2IniDocument source, string section)
    {
        var count = ReadInt(source, section, "NUM");
        if (count <= 0)
            throw new InvalidDataException($"[{section}] has no color keys.");
        var keys = new SceneSkyTimeColorKey[count];
        for (var index = 0; index < count; index++)
        {
            var tuple = source.GetRequired(section, $"COLOR{index + 1}").Trim();
            if (tuple.Length < 2 || tuple[0] != '(' || tuple[^1] != ')')
                throw new InvalidDataException($"Invalid [{section}] COLOR{index + 1}: {tuple}.");
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in tuple[1..^1].Split(','))
            {
                var pair = part.Split('=');
                if (pair.Length != 2 || !values.TryAdd(pair[0].Trim(), pair[1].Trim()))
                    throw new InvalidDataException($"Invalid [{section}] COLOR{index + 1}: {tuple}.");
            }
            keys[index] = new SceneSkyTimeColorKey(
                float.Parse(values["T"], CultureInfo.InvariantCulture),
                byte.Parse(values["R"], CultureInfo.InvariantCulture),
                byte.Parse(values["G"], CultureInfo.InvariantCulture),
                byte.Parse(values["B"], CultureInfo.InvariantCulture));
            if (float.IsNaN(keys[index].Hour) || keys[index].Hour < 0f || keys[index].Hour > 24f ||
                (index > 0 && keys[index].Hour <= keys[index - 1].Hour))
                throw new InvalidDataException($"Invalid key time in [{section}] COLOR{index + 1}.");
        }
        return keys;
    }

    private static string ReadMaterialName(Lineage2IniDocument setup, string key)
    {
        var name = setup.GetRequired("EnvSetup", key);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidDataException($"Env.int has an empty material reference for {key}.");
        return name;
    }

    private static int ReadInt(Lineage2IniDocument source, string section, string key) =>
        int.Parse(source.GetRequired(section, key), CultureInfo.InvariantCulture);
}
