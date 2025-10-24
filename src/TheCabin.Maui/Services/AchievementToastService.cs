using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using TheCabin.Core.Models;

namespace TheCabin.Maui.Services;

/// <summary>
/// Service for displaying achievement unlock notifications using toasts
/// </summary>
public class AchievementToastService : IAchievementNotificationService
{
    private readonly Queue<Achievement> _notificationQueue = new();
    private bool _isShowingNotification;

    /// <summary>
    /// Shows a notification when an achievement is unlocked
    /// </summary>
    public async Task ShowAchievementUnlockedAsync(Achievement achievement)
    {
        // Add to queue
        _notificationQueue.Enqueue(achievement);

        // Process queue if not already processing
        if (!_isShowingNotification)
        {
            await ProcessNotificationQueueAsync();
        }
    }

    /// <summary>
    /// Shows a notification with custom text
    /// </summary>
    public async Task ShowNotificationAsync(string title, string message)
    {
        await ShowToastAsync($"{title}\n{message}", ToastDuration.Long);
    }

    /// <summary>
    /// Processes the notification queue one at a time
    /// </summary>
    private async Task ProcessNotificationQueueAsync()
    {
        _isShowingNotification = true;

        while (_notificationQueue.Count > 0)
        {
            var achievement = _notificationQueue.Dequeue();
            await ShowAchievementToastAsync(achievement);

            // Wait a bit between notifications
            if (_notificationQueue.Count > 0)
            {
                await Task.Delay(500);
            }
        }

        _isShowingNotification = false;
    }

    /// <summary>
    /// Shows a toast notification for an achievement
    /// </summary>
    private async Task ShowAchievementToastAsync(Achievement achievement)
    {
        // Format the message
        var message = $"✨ Achievement Unlocked!\n🏆 {achievement.Name}";

        await ShowToastAsync(message, ToastDuration.Long);

        // Optional: Trigger haptic feedback
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch
        {
            // Haptic feedback not available on all devices
        }
    }

    /// <summary>
    /// Shows a toast notification
    /// </summary>
    private async Task ShowToastAsync(string message, ToastDuration duration)
    {
        try
        {
            var toast = Toast.Make(
                message,
                duration,
                14); // Font size

            await toast.Show();
        }
        catch (Exception ex)
        {
            // Log error but don't throw - notifications are non-critical
            System.Diagnostics.Debug.WriteLine($"Failed to show toast: {ex.Message}");
        }
    }
}
