using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDomain.Services.CharacterServices;

internal sealed class SceneCreatureClassEffectResolver
{
    private static readonly Regex SpawnRegex = new(
        @"(?<variable>[A-Za-z_]\w*(?:\s*\[\s*(?<arrayIndex>[A-Za-z_]\w*|\d+)\s*\])?)\s*=\s*Spawn\s*\(\s*class'(?<effect>[^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LoopRegex = new(
        @"for\s*\(\s*(?<variable>[A-Za-z_]\w*)\s*=\s*(?<start>-?\d+)\s*;\s*\k<variable>\s*<\s*(?<end>-?\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VectorRegex = new(
        @"vect\s*\(\s*(?<x>-?[\d.]+)\s*,\s*(?<y>-?[\d.]+)\s*,\s*(?<z>-?[\d.]+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RotatorRegex = new(
        @"rot\s*\(\s*(?<x>-?[\d.]+)\s*,\s*(?<y>-?[\d.]+)\s*,\s*(?<z>-?[\d.]+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly string _clientRoot;
    private readonly IReadOnlyDictionary<string, string> _packageIndex;
    private readonly Dictionary<string, IReadOnlyList<UnrScriptClassObject>> _scriptCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyDictionary<string, UnrEmitterClassObject>> _emitterCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SceneCreatureAttachedEffectData[]> _resultCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SceneParticleBuilder _particleBuilder = new();

    public SceneCreatureClassEffectResolver(string clientRoot, IReadOnlyDictionary<string, string> packageIndex)
    {
        _clientRoot = clientRoot;
        _packageIndex = packageIndex;
    }

    public SceneCreatureAttachedEffectData[] Resolve(SceneResourceLocation actorClassResource)
    {
        if (actorClassResource is null || string.IsNullOrWhiteSpace(actorClassResource.PackagePath))
        {
            return [];
        }

        var cacheKey = actorClassResource.Reference;
        if (_resultCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var classes = GetScriptClasses(actorClassResource.PackagePath);
        var byName = classes.ToDictionary(x => x.ObjectName, StringComparer.OrdinalIgnoreCase);
        if (!byName.TryGetValue(actorClassResource.ObjectName, out var actorClass))
        {
            return Cache(cacheKey, []);
        }

        var scripts = new[] { "PostBeginPlay", "PostSetPawnResource" }
            .SelectMany(x => EnumerateEffectiveFunctionBodies(actorClass, byName, x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => RemoveComments(x!))
            .ToArray();
        var effects = scripts.SelectMany(ParseAttachments).SelectMany(ResolveAttachment).ToArray();
        return Cache(cacheKey, effects);
    }

    private IEnumerable<SceneCreatureAttachedEffectData> ResolveAttachment(ParsedAttachment attachment)
    {
        if (!TrySplitReference(attachment.EffectReference, out var packageName, out var objectName) ||
            !_packageIndex.TryGetValue(packageName, out var packagePath))
        {
            yield break;
        }

        var emitterClasses = GetEmitterClasses(packagePath);
        if (!emitterClasses.TryGetValue(objectName, out var emitterClass))
        {
            yield break;
        }

        var resource = SceneReferenceUtilities.BuildResourceLocation(
            _clientRoot,
            packagePath,
            packageName,
            objectName,
            emitterClass.SuperClassName ?? "Emitter");
        foreach (var instance in attachment.Instances)
        {
            var suffix = instance.ArrayIndex.HasValue ? $"_{instance.ArrayIndex.Value:D2}" : string.Empty;
            yield return new SceneCreatureAttachedEffectData
            {
                StableName = $"AttachedEffect_{Sanitize(attachment.VariableName)}{suffix}_{Sanitize(objectName)}",
                SourceVariable = attachment.VariableName,
                EffectReference = attachment.EffectReference,
                EffectResource = resource,
                BoneName = instance.BoneName,
                BoneIndex = instance.BoneIndex,
                RelativeLocationUnreal = instance.RelativeLocation,
                RelativeRotationUnrealRaw = instance.RelativeRotationRaw,
                RelativeRotationEulerDegrees = instance.RelativeRotationEuler,
                Emitter = _particleBuilder.BuildEmitterClass(packagePath, emitterClass)
            };
        }
    }

    private IEnumerable<ParsedAttachment> ParseAttachments(string script)
    {
        var loop = LoopRegex.Match(script);
        foreach (Match spawn in SpawnRegex.Matches(script))
        {
            var variableExpression = spawn.Groups["variable"].Value;
            var variableName = Regex.Match(variableExpression, @"^[A-Za-z_]\w*").Value;
            var arrayToken = spawn.Groups["arrayIndex"].Value;
            var instances = new List<ParsedInstance>();
            var hasLiteralIndex = int.TryParse(arrayToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var literalIndex);
            if (!string.IsNullOrWhiteSpace(arrayToken) && !hasLiteralIndex)
            {
                var start = loop.Success && loop.Groups["variable"].Value.Equals(arrayToken, StringComparison.OrdinalIgnoreCase)
                    ? int.Parse(loop.Groups["start"].Value, CultureInfo.InvariantCulture)
                    : 0;
                var end = loop.Success && loop.Groups["variable"].Value.Equals(arrayToken, StringComparison.OrdinalIgnoreCase)
                    ? int.Parse(loop.Groups["end"].Value, CultureInfo.InvariantCulture)
                    : start;
                for (var index = start; index < end; index++)
                {
                    instances.Add(ParseInstance(script, variableName, arrayToken, arrayToken, index));
                }
            }
            else
            {
                instances.Add(ParseInstance(script, variableName, arrayToken, null, hasLiteralIndex ? literalIndex : null));
            }

            if (instances.Count > 0)
            {
                yield return new ParsedAttachment(variableName, spawn.Groups["effect"].Value, instances);
            }
        }
    }

    private static ParsedInstance ParseInstance(string script, string variableName, string arrayToken, string? loopVariable, int? arrayIndex)
    {
        var escaped = Regex.Escape(variableName);
        if (!string.IsNullOrWhiteSpace(arrayToken))
        {
            var indexPattern = loopVariable is null
                ? Regex.Escape(arrayToken)
                : Regex.Escape(loopVariable);
            escaped += @"\s*\[\s*" + indexPattern + @"\s*\]";
        }
        var boneNameMatch = Regex.Match(script, $@"AttachToBone\s*\(\s*{escaped}\s*,\s*'(?<bone>[^']+)'", RegexOptions.IgnoreCase);
        var boneIndexMatch = Regex.Match(script, $@"AttachToBoneWithIndex\s*\(\s*{escaped}\s*,\s*(?<index>[^\)]+)\)", RegexOptions.IgnoreCase);
        var locationMatch = Regex.Match(script, $@"{escaped}\s*\.\s*SetRelativeLocation\s*\(\s*(?<value>vect\s*\([^\)]+\))", RegexOptions.IgnoreCase);
        var rotationMatch = Regex.Match(script, $@"{escaped}\s*\.\s*SetRelativeRotation\s*\(\s*(?<value>(?:rot|Vector2Rotator\s*\(\s*vect)\s*\([^;]+)", RegexOptions.IgnoreCase);

        var location = ParseVector(locationMatch.Groups["value"].Value);
        var rotationText = rotationMatch.Groups["value"].Value;
        var rotationRaw = ParseRotator(rotationText);
        var rotationEuler = rotationText.Contains("Vector2Rotator", StringComparison.OrdinalIgnoreCase)
            ? VectorToEuler(ParseVector(rotationText))
            : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw);
        return new ParsedInstance(
            arrayIndex,
            boneNameMatch.Success ? boneNameMatch.Groups["bone"].Value : null,
            EvaluateIndex(boneIndexMatch.Groups["index"].Value, loopVariable, arrayIndex),
            location,
            rotationRaw,
            rotationEuler);
    }

    private IReadOnlyList<UnrScriptClassObject> GetScriptClasses(string path)
    {
        if (!_scriptCache.TryGetValue(path, out var classes))
        {
            classes = UnrClassEffectPackageReader.ReadScriptClasses(path);
            _scriptCache[path] = classes;
        }
        return classes;
    }

    private IReadOnlyDictionary<string, UnrEmitterClassObject> GetEmitterClasses(string path)
    {
        if (!_emitterCache.TryGetValue(path, out var classes))
        {
            classes = UnrClassEffectPackageReader.ReadEmitterClasses(path)
                .ToDictionary(x => x.ObjectName, StringComparer.OrdinalIgnoreCase);
            _emitterCache[path] = classes;
        }
        return classes;
    }

    private static IEnumerable<string> EnumerateEffectiveFunctionBodies(
        UnrScriptClassObject leaf,
        IReadOnlyDictionary<string, UnrScriptClassObject> byName,
        string functionName)
    {
        var bodies = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var current = leaf; current is not null && seen.Add(current.ObjectName);)
        {
            var body = ExtractFunctionBody(current.ScriptText, functionName);
            if (body is not null)
            {
                bodies.Add(body);
                if (!Regex.IsMatch(body, $@"(?i)\bSuper\s*\.\s*{Regex.Escape(functionName)}\s*\("))
                {
                    break;
                }
            }

            current = !string.IsNullOrWhiteSpace(current.SuperClassName) && byName.TryGetValue(current.SuperClassName, out var parent)
                ? parent
                : null;
        }
        bodies.Reverse();
        return bodies;
    }

    private static string? ExtractFunctionBody(string script, string functionName)
    {
        var match = Regex.Match(script, $@"(?i)\b(?:function|event)\s+{Regex.Escape(functionName)}\s*\([^)]*\)\s*\{{");
        if (!match.Success)
        {
            return null;
        }

        var start = match.Index + match.Length;
        var depth = 1;
        for (var i = start; i < script.Length; i++)
        {
            if (script[i] == '{') depth++;
            else if (script[i] == '}' && --depth == 0) return script[start..i];
        }
        return null;
    }

    private static string RemoveComments(string value)
    {
        value = Regex.Replace(value, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(value, @"//.*?$", string.Empty, RegexOptions.Multiline);
    }

    private static Vector3 ParseVector(string text)
    {
        var match = VectorRegex.Match(text ?? string.Empty);
        return match.Success
            ? new Vector3(ParseFloat(match, "x"), ParseFloat(match, "y"), ParseFloat(match, "z"))
            : Vector3.Zero;
    }

    private static Vector3 ParseRotator(string text)
    {
        var match = RotatorRegex.Match(text ?? string.Empty);
        return match.Success
            ? new Vector3(ParseFloat(match, "x"), ParseFloat(match, "y"), ParseFloat(match, "z"))
            : Vector3.Zero;
    }

    private static Vector3 VectorToEuler(Vector3 direction)
    {
        if (direction.LengthSquared() <= float.Epsilon)
        {
            return Vector3.Zero;
        }
        var yaw = MathF.Atan2(direction.Y, direction.X) * 180f / MathF.PI;
        var pitch = MathF.Atan2(direction.Z, MathF.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y))) * 180f / MathF.PI;
        return new Vector3(pitch, yaw, 0f);
    }

    private static float ParseFloat(Match match, string group) =>
        float.Parse(match.Groups[group].Value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static int? EvaluateIndex(string expression, string? variable, int? value)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        expression = Regex.Replace(expression, @"\s+", string.Empty);
        if (int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture, out var literal)) return literal;
        if (variable is null || !value.HasValue) return null;
        var match = Regex.Match(expression, $@"^{Regex.Escape(variable)}(?:(?<op>[+-])(?<offset>\d+))?$", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        var offset = match.Groups["offset"].Success ? int.Parse(match.Groups["offset"].Value, CultureInfo.InvariantCulture) : 0;
        return value.Value + (match.Groups["op"].Value == "-" ? -offset : offset);
    }

    private static bool TrySplitReference(string reference, out string packageName, out string objectName)
    {
        var index = reference.LastIndexOf('.');
        packageName = index > 0 ? reference[..index] : string.Empty;
        objectName = index > 0 && index < reference.Length - 1 ? reference[(index + 1)..] : string.Empty;
        return packageName.Length > 0 && objectName.Length > 0;
    }

    private SceneCreatureAttachedEffectData[] Cache(string key, SceneCreatureAttachedEffectData[] value)
    {
        _resultCache[key] = value;
        return value;
    }

    private static string Sanitize(string value) =>
        new(value.Select(x => char.IsLetterOrDigit(x) ? x : '_').ToArray());

    private sealed record ParsedAttachment(string VariableName, string EffectReference, IReadOnlyList<ParsedInstance> Instances);
    private sealed record ParsedInstance(
        int? ArrayIndex,
        string? BoneName,
        int? BoneIndex,
        Vector3 RelativeLocation,
        Vector3 RelativeRotationRaw,
        Vector3 RelativeRotationEuler);
}
