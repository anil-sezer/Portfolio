using System.Text.Json;
using Portfolio.Domain.Extensions;

namespace Portfolio.Domain.Utilities;

public static class BoundedJsonSerializer
{
    public const int DefaultMaxValueLength = 512;
    public const int DefaultMaxKeyLength = 128;

    /// <summary>
    /// Truncates individual keys and string values before serializing to JSON,
    /// omitting excess entries so the serialized JSON object never exceeds <paramref name="maxJsonLength"/>.
    /// </summary>
    public static string SerializeWithinLimit<TValue>(
        IEnumerable<KeyValuePair<string, TValue>> entries,
        int maxJsonLength,
        int maxValueLength = DefaultMaxValueLength,
        int maxKeyLength = DefaultMaxKeyLength,
        IEqualityComparer<string>? keyComparer = null)
    {
        if (maxJsonLength < 2)
            throw new ArgumentOutOfRangeException(nameof(maxJsonLength), "JSON length limit must be at least 2 characters for '{}'.");

        var comparer = keyComparer ?? StringComparer.OrdinalIgnoreCase;
        var sanitized = new Dictionary<string, object?>(comparer);

        foreach (var (key, value) in entries)
        {
            var truncatedKey = key.Truncate(maxKeyLength);
            sanitized[truncatedKey] = TruncateEntryValue(value, maxValueLength);
        }

        var fullJson = JsonSerializer.Serialize(sanitized);
        if (fullJson.Length <= maxJsonLength)
            return fullJson;

        var bounded = new Dictionary<string, object?>(comparer);
        var currentLength = 2; // "{}"

        foreach (var (key, value) in sanitized)
        {
            var singleEntryLength = JsonSerializer.Serialize(new Dictionary<string, object?>(1) { [key] = value }).Length - 2;
            var separatorLength = bounded.Count > 0 ? 1 : 0;

            if (currentLength + separatorLength + singleEntryLength <= maxJsonLength)
            {
                bounded[key] = value;
                currentLength += separatorLength + singleEntryLength;
            }

            if (currentLength >= maxJsonLength - 4)
                break;
        }

        return bounded.Count == 0 ? "{}" : JsonSerializer.Serialize(bounded);
    }

    /// <summary>
    /// Ensures <paramref name="value"/> fits within <paramref name="maxLength"/>.
    /// When <paramref name="value"/> is an oversized JSON object, trims its properties cleanly instead of slicing JSON syntax.
    /// </summary>
    public static string TruncateJsonOrText(
        string? value,
        int maxLength,
        int maxValueLength = DefaultMaxValueLength,
        int maxKeyLength = DefaultMaxKeyLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        if (maxLength >= 2 && value.AsSpan().TrimStart().StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var entries = document.RootElement
                        .EnumerateObject()
                        .Select(property => new KeyValuePair<string, JsonElement>(property.Name, property.Value));

                    return SerializeWithinLimit(entries, maxLength, maxValueLength, maxKeyLength, StringComparer.Ordinal);
                }
            }
            catch (JsonException)
            {
                // Not valid JSON; fall through to standard string truncation.
            }
        }

        return value.Truncate(maxLength);
    }

    private static object? TruncateEntryValue<TValue>(TValue value, int maxValueLength)
    {
        if (value is string stringValue)
            return stringValue.Truncate(maxValueLength);

        if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
            return element.GetString().Truncate(maxValueLength);

        if (value is JsonElement nonStringElement)
            return nonStringElement.Clone();

        return value;
    }
}
