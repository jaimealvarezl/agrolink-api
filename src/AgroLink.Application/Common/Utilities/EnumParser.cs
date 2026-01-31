namespace AgroLink.Application.Common.Utilities;

public static class EnumParser
{
    public static T ParseOrDefault<T>(string? value, T defaultValue, string propertyName)
        where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }

        throw new ArgumentException(
            $"Invalid {propertyName}: {value}. Allowed values: {string.Join(", ", Enum.GetNames<T>())}"
        );
    }

    public static T Parse<T>(string value, string propertyName)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }

        throw new ArgumentException(
            $"Invalid {propertyName}: {value}. Allowed values: {string.Join(", ", Enum.GetNames<T>())}"
        );
    }
}
