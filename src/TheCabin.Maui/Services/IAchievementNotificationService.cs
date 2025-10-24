using TheCabin.Core.Models;

namespace TheCabin.Maui.Services;

/// <summary>
/// Service for displaying achievement unlock notifications
/// </summary>
public interface IAchievementNotificationService
{
    /// <summary>
    /// Shows a notification when an achievement is unlocked
    /// </summary>
    Task ShowAchievementUnlockedAsync(Achievement achievement);

    /// <summary>
    /// Shows a notification with custom text
    /// </summary>
    Task ShowNotificationAsync(string title, string message);
}
