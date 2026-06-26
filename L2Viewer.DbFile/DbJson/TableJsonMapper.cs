using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace L2Viewer.DbFile.DbJson
{
    public static class TableJsonMapper
    {
        public sealed class TableHeader
        {
            public string Name { get; set; } = "";
            public string DbType { get; set; } = "";
        }

        public sealed class Binding
        {
            public PropertyInfo Property { get; }
            public int Index { get; }
            public bool Required { get; }
            public string ColumnName { get; }

            public Binding(PropertyInfo property, int index, bool required, string columnName)
            {
                Property = property;
                Index = index;
                Required = required;
                ColumnName = columnName;
            }
        }

        public sealed class TableJson
        {
            public List<TableHeader> Headers { get; set; } = new List<TableHeader>();
            public List<List<JToken>> Data { get; set; } = new List<List<JToken>>();
        }

        public static List<T> Read<T>(string jsonPath) where T : new()
        {
            var json = System.IO.File.ReadAllText(jsonPath);
            var payload = JsonConvert.DeserializeObject<TableJson>(json);

            if (payload == null)
                throw new InvalidOperationException("Invalid json");

            var indexByName = payload.Headers
                .Select((header, index) => new { header.Name, Index = index })
                .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

            var bindings = BuildBindings<T>(indexByName);
            var result = new List<T>(payload.Data.Count);

            foreach (var row in payload.Data)
            {
                var item = new T();

                foreach (var binding in bindings)
                {
                    if (binding.Index >= row.Count)
                    {
                        if (binding.Required)
                            throw new InvalidOperationException($"Required column '{binding.ColumnName}' is missing in row");

                        continue;
                    }

                    var value = ConvertValue(row[binding.Index], binding.Property.PropertyType);
                    binding.Property.SetValue(item, value);
                }

                result.Add(item);
            }

            return result;
        }

        private static List<Binding> BuildBindings<T>(Dictionary<string, int> indexByName)
        {
            var result = new List<Binding>();

            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanWrite)
                    continue;

                var columnName = property.Name;
                if (!indexByName.TryGetValue(columnName, out var index))
                    throw new InvalidOperationException($"Required column '{columnName}' was not found");

                result.Add(new Binding(property, index, required: true, columnName));
            }

            return result;
        }

        private static object? ConvertValue(JToken element, Type targetType)
        {
            var nullableType = Nullable.GetUnderlyingType(targetType);
            var actualType = nullableType ?? targetType;

            if (element.Type == JTokenType.Null || element.Type == JTokenType.Undefined)
                return DefaultValue(targetType, actualType);

            if (actualType != typeof(string) &&
                element.Type == JTokenType.String &&
                string.IsNullOrWhiteSpace(element.Value<string>()))
                return DefaultValue(targetType, actualType);

            if (actualType == typeof(string))
                return ReadScalarText(element);

            if (actualType.IsEnum)
                return ConvertEnum(element, actualType);

            if (actualType == typeof(bool))
                return ConvertBool(element);

            if (actualType == typeof(Guid))
                return Guid.Parse(ReadScalarText(element)!);

            if (actualType == typeof(DateTime))
                return DateTime.Parse(ReadScalarText(element)!, CultureInfo.InvariantCulture);

            if (actualType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(ReadScalarText(element)!, CultureInfo.InvariantCulture);

            if (IsNumber(actualType))
                return ConvertNumber(element, actualType);

            return element.ToObject(actualType);
        }

        private static object? DefaultValue(Type targetType, Type actualType)
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                return null;

            return Activator.CreateInstance(actualType);
        }

        private static object ConvertEnum(JToken element, Type enumType)
        {
            if (element.Type == JTokenType.Integer)
                return Enum.ToObject(enumType, element.Value<long>());

            var text = ReadScalarText(element);

            if (string.IsNullOrWhiteSpace(text))
                return Activator.CreateInstance(enumType)!;

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                return Enum.ToObject(enumType, number);

            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();
                if (string.Equals(enumMember?.Value, text, StringComparison.OrdinalIgnoreCase))
                    return field.GetValue(null)!;
            }

            return Enum.Parse(enumType, text, true);
        }

        private static bool ConvertBool(JToken element)
        {
            if (element.Type == JTokenType.Boolean)
                return element.Value<bool>();

            var text = ReadScalarText(element);
            if (bool.TryParse(text, out var boolean))
                return boolean;

            return decimal.Parse(text!, CultureInfo.InvariantCulture) != 0;
        }

        private static object ConvertNumber(JToken element, Type targetType)
        {
            var text = ReadScalarText(element)!;

            if (targetType == typeof(decimal))
                return decimal.Parse(text, CultureInfo.InvariantCulture);

            if (targetType == typeof(double))
                return double.Parse(text, CultureInfo.InvariantCulture);

            if (targetType == typeof(float))
                return float.Parse(text, CultureInfo.InvariantCulture);

            var number = decimal.Parse(text, CultureInfo.InvariantCulture);

            if (number != decimal.Truncate(number))
                throw new InvalidOperationException($"Cannot convert '{text}' to {targetType.Name}");

            return Convert.ChangeType(number, targetType, CultureInfo.InvariantCulture);
        }

        private static string? ReadScalarText(JToken element)
        {
            return element.Type switch
            {
                JTokenType.String => element.Value<string>(),
                JTokenType.Integer => element.ToString(Formatting.None),
                JTokenType.Float => element.ToString(Formatting.None),
                JTokenType.Boolean => element.Value<bool>() ? "true" : "false",
                _ => element.ToString(Formatting.None)
            };
        }

        private static bool IsNumber(Type type)
        {
            return type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }
    }
}
