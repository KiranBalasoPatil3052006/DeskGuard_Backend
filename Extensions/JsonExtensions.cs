using System;
using System.Text.Json;

namespace DeskGuardBackend.Extensions
{
    public static class JsonExtensions
    {
        public static string? GetStringProperty(this JsonElement element, string name, string? fallback = null)
        {
            if (element.ValueKind != JsonValueKind.Object) return fallback;
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            {
                return prop.GetString() ?? fallback;
            }
            return fallback;
        }

        public static int? GetInt32Property(this JsonElement element, string name, int? fallback = null)
        {
            if (element.ValueKind != JsonValueKind.Object) return fallback;
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val)) return val;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parseVal)) return parseVal;
            }
            return fallback;
        }

        public static long? GetInt64Property(this JsonElement element, string name, long? fallback = null)
        {
            if (element.ValueKind != JsonValueKind.Object) return fallback;
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var val)) return val;
                if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var parseVal)) return parseVal;
            }
            return fallback;
        }

        public static decimal? GetDecimalProperty(this JsonElement element, string name, decimal? fallback = null)
        {
            if (element.ValueKind != JsonValueKind.Object) return fallback;
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var val)) return val;
                if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var parseVal)) return parseVal;
            }
            return fallback;
        }

        public static bool? GetBooleanProperty(this JsonElement element, string name, bool? fallback = null)
        {
            if (element.ValueKind != JsonValueKind.Object) return fallback;
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            {
                if (prop.ValueKind == JsonValueKind.True) return true;
                if (prop.ValueKind == JsonValueKind.False) return false;
                if (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out var parseVal)) return parseVal;
            }
            return fallback;
        }

        public static JsonElement? GetPropertyOrNull(this JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            {
                return prop;
            }
            return null;
        }
    }
}
