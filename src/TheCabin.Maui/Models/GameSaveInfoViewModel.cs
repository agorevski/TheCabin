using TheCabin.Core.Models;

namespace TheCabin.Maui.Models;

public class GameSaveInfoViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ThemeName { get; set; } = string.Empty;
    public DateTime SavedDate { get; set; }
    public TimeSpan PlayTime { get; set; }
    public string PlayerLocation { get; set; } = string.Empty;
    public int PlayerHealth { get; set; }

    // UI-specific properties
    public string Icon => GetThemeIcon();
    public string SavedDateDisplay => SavedDate.ToString("MMM dd, yyyy h:mm tt");
    public string PlayTimeDisplay => PlayTime.TotalHours >= 1
        ? $"{PlayTime.Hours}h {PlayTime.Minutes}m"
        : $"{PlayTime.Minutes}m";
    public string LocationDisplay => FormatLocation(PlayerLocation);
    public string HealthDisplay => $"{PlayerHealth}/100";
    public Color HealthColor => PlayerHealth switch
    {
        >= 75 => Color.FromArgb("#7ED321"), // Green
        >= 50 => Color.FromArgb("#F5A623"), // Orange
        >= 25 => Color.FromArgb("#FF6B35"), // Dark Orange
        _ => Color.FromArgb("#D0021B")      // Red
    };

    private string GetThemeIcon()
    {
        return ThemeName.ToLower() switch
        {
            var t when t.Contains("horror") => "🏚️",
            var t when t.Contains("arctic") || t.Contains("survival") => "🗻",
            var t when t.Contains("fantasy") || t.Contains("magic") => "🏰",
            var t when t.Contains("sci") || t.Contains("space") => "🚀",
            var t when t.Contains("cozy") || t.Contains("mystery") => "🏔️",
            _ => "📖"
        };
    }

    private string FormatLocation(string location)
    {
        if (string.IsNullOrEmpty(location))
            return "Unknown";

        return location
            .Replace("_", " ")
            .Split(' ')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1))
            .Aggregate((a, b) => a + " " + b);
    }
}
