using System.Globalization;

namespace TheCabin.Maui.Converters;

public class BoolToFontAttributesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isImportant && isImportant)
        {
            return FontAttributes.Bold;
        }
        return FontAttributes.None;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToRecordingColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isListening && isListening)
        {
            return Color.FromArgb("#D0021B"); // Red when recording
        }
        return Color.FromArgb("#4A90E2"); // Blue when idle
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

public class TtsIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool ttsEnabled && ttsEnabled)
        {
            return "🔊"; // Speaker on
        }
        return "🔇"; // Speaker off
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Achievement-related converters

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUnlocked && isUnlocked)
        {
            return 1.0; // Full opacity when unlocked
        }
        return 0.5; // Dimmed when locked
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToUnlockIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUnlocked && isUnlocked)
        {
            return "✅"; // Checkmark when unlocked
        }
        return "🔒"; // Lock when locked
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToUnlockColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUnlocked && isUnlocked)
        {
            return Color.FromArgb("#4CAF50"); // Green when unlocked
        }
        return Color.FromArgb("#9E9E9E"); // Gray when locked
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FilterToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedFilter = value as string;
        var filterParameter = parameter as string;

        if (selectedFilter == filterParameter)
        {
            return Color.FromArgb("#2196F3"); // Primary blue when selected
        }
        return Color.FromArgb("#757575"); // Gray when not selected
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FilterToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selectedFilter = value as string;
        var filterParameter = parameter as string;

        if (selectedFilter == filterParameter)
        {
            return 1.0; // Full opacity when selected
        }
        return 0.7; // Slightly dimmed when not selected
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
