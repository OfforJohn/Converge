using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Converge.Configuration.API.Json
{
    public class CaseInsensitiveEnumConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var t = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return t.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            var converterType = typeof(CaseInsensitiveEnumConverter<>).MakeGenericType(enumType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    public class CaseInsensitiveEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s))
                    throw new JsonException();

                if (Enum.TryParse<T>(s, true, out var value))
                    return value;

                // Try to convert camelCase to PascalCase match
                // e.g., "global" -> "Global"
                var transformed = char.ToUpperInvariant(s[0]) + s.Substring(1);
                if (Enum.TryParse<T>(transformed, true, out value))
                    return value;

                throw new JsonException($"Unable to convert '{s}' to enum {typeof(T)}");
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var l))
                {
                    return (T)Enum.ToObject(typeof(T), l);
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
