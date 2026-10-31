namespace Portfolio.Domain.Extensions;

public static class StringExtensions
{
    private const string Ellipsis = " ...";

    public static string Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        if (maxLength <= Ellipsis.Length)
            return value[..maxLength];

        return value[..(maxLength - Ellipsis.Length)] + Ellipsis;
    }
}
