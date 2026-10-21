using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace TheCabin.Core.Engine;

/// <summary>
/// Applies StateChange values to object properties. Values loaded from story pack JSON arrive as
/// <see cref="JsonElement"/> instances, so they must be converted to the target property type first.
/// </summary>
internal static class StateChangeValues
{
    /// <summary>
    /// Converts a state change value to the requested type.
    /// </summary>
    public static bool TryConvert(object? value, Type targetType, out object? converted)
    {
        converted = null;

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                return !targetType.IsValueType;
            }

            try
            {
                converted = JsonSerializer.Deserialize(element.GetRawText(), targetType);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        if (value == null)
        {
            return !targetType.IsValueType;
        }

        if (targetType.IsInstanceOfType(value))
        {
            converted = value;
            return true;
        }

        try
        {
            converted = Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sets a writable property by name, converting the value to the property's type.
    /// Returns false when the property is missing, read-only, or the value cannot be converted.
    /// </summary>
    public static bool TrySetProperty(object target, string propertyName, object? value)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName);
        if (property == null || !property.CanWrite)
        {
            return false;
        }

        if (!TryConvert(value, property.PropertyType, out var converted))
        {
            return false;
        }

        property.SetValue(target, converted);
        return true;
    }
}
