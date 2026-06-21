using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections;

namespace L2Viewer.Wpf;

internal static class FileInspectionJsonSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new FileInspectionContractResolver(),
        NullValueHandling = NullValueHandling.Include,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public static string Serialize(object value)
    {
        return JsonConvert.SerializeObject(value, Settings);
    }

    private sealed class FileInspectionContractResolver : DefaultContractResolver
    {
        private static readonly HashSet<string> SummarizedCollectionPropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Vertices",
            "Normals",
            "Triangles",
            "Uv",
            "UvSets",
            "Indices",
            "Colors",
            "MaterialCollisionFlags",
            "FaceGeometries",
            "Bases",
            "References",
            "PolyReferences"
        };

        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType == typeof(byte[]))
            {
                property.Converter = new ByteArraySummaryConverter();
                return property;
            }

            if (string.IsNullOrWhiteSpace(property.PropertyName) ||
                !SummarizedCollectionPropertyNames.Contains(property.PropertyName) ||
                property.PropertyType == typeof(string) ||
                !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                return property;
            }

            if (property.ValueProvider is null)
            {
                return property;
            }

            property.ValueProvider = new SummaryValueProvider(property.ValueProvider);
            return property;
        }
    }

    private sealed class SummaryValueProvider : IValueProvider
    {
        private readonly IValueProvider _inner;

        public SummaryValueProvider(IValueProvider inner)
        {
            _inner = inner;
        }

        public object? GetValue(object target)
        {
            var value = _inner.GetValue(target);
            return BuildSummary(value);
        }

        public void SetValue(object target, object? value)
        {
            _inner.SetValue(target, value);
        }

        private static object? BuildSummary(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string)
            {
                return value;
            }

            var runtimeType = value.GetType();
            var count = TryGetCount(value);
            return count.HasValue
                ? $"{runtimeType} (Count={count.Value})"
                : runtimeType.ToString();
        }

        private static int? TryGetCount(object value)
        {
            if (value is ICollection collection)
            {
                return collection.Count;
            }

            var countProperty = value.GetType().GetProperty("Count");
            return countProperty?.PropertyType == typeof(int)
                ? (int?)countProperty.GetValue(value)
                : null;
        }
    }

    private sealed class ByteArraySummaryConverter : JsonConverter<byte[]>
    {
        public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue($"System.Byte[] (Count={value.Length})");
        }

        public override byte[]? ReadJson(
            JsonReader reader,
            Type objectType,
            byte[]? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
