using CommunityToolkit.Mvvm.ComponentModel;
using TheCabin.Core.Models;

namespace TheCabin.Maui.Models;

/// <summary>
/// View model representation of an achievement for UI binding
/// </summary>
public partial class AchievementViewModel : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string icon = "🏆";

    [ObservableProperty]
    private bool isUnlocked;

    [ObservableProperty]
    private DateTime? unlockDate;

    [ObservableProperty]
    private int currentProgress;

    [ObservableProperty]
    private int requiredProgress;

    [ObservableProperty]
    private double progressPercentage;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private bool hasProgress;

    [ObservableProperty]
    private string statusText = string.Empty;

    /// <summary>
    /// Creates an AchievementViewModel from an Achievement model and progress
    /// </summary>
    public static AchievementViewModel FromModel(Achievement achievement, AchievementProgress? progress = null)
    {
        var viewModel = new AchievementViewModel
        {
            Id = achievement.Id,
            Name = achievement.Name,
            Description = achievement.Description,
            Icon = GetIconForAchievement(achievement),
            IsUnlocked = progress?.IsUnlocked ?? false,
            UnlockDate = progress?.UnlockedDate
        };

        // Calculate progress for count-based achievements
        if (progress != null && achievement.Trigger.RequiredCount > 1)
        {
            viewModel.HasProgress = true;
            viewModel.CurrentProgress = progress.CurrentProgress;
            viewModel.RequiredProgress = progress.RequiredProgress;
            viewModel.ProgressPercentage = progress.RequiredProgress > 0
                ? (double)progress.CurrentProgress / progress.RequiredProgress
                : 0;
            viewModel.ProgressText = $"{progress.CurrentProgress}/{progress.RequiredProgress}";
        }
        else
        {
            viewModel.HasProgress = false;
            viewModel.ProgressPercentage = progress?.IsUnlocked ?? false ? 1.0 : 0.0;
        }

        // Set status text
        viewModel.StatusText = viewModel.IsUnlocked
            ? $"Unlocked {viewModel.UnlockDate:MMM d, yyyy}"
            : viewModel.HasProgress
                ? $"Progress: {viewModel.ProgressText}"
                : "Locked";

        return viewModel;
    }

    /// <summary>
    /// Updates the view model from an Achievement model and progress (for refresh scenarios)
    /// </summary>
    public void UpdateFromModel(Achievement achievement, AchievementProgress? progress = null)
    {
        IsUnlocked = progress?.IsUnlocked ?? false;
        UnlockDate = progress?.UnlockedDate;

        if (progress != null && achievement.Trigger.RequiredCount > 1)
        {
            CurrentProgress = progress.CurrentProgress;
            RequiredProgress = progress.RequiredProgress;
            ProgressPercentage = progress.RequiredProgress > 0
                ? (double)progress.CurrentProgress / progress.RequiredProgress
                : 0;
            ProgressText = $"{progress.CurrentProgress}/{progress.RequiredProgress}";
        }
        else
        {
            ProgressPercentage = IsUnlocked ? 1.0 : 0.0;
        }

        StatusText = IsUnlocked
            ? $"Unlocked {UnlockDate:MMM d, yyyy}"
            : HasProgress
                ? $"Progress: {ProgressText}"
                : "Locked";
    }

    /// <summary>
    /// Gets an appropriate icon emoji for an achievement based on its properties
    /// </summary>
    private static string GetIconForAchievement(Achievement achievement)
    {
        // You can customize this based on achievement ID, name, or other properties
        return achievement.Id switch
        {
            var id when id.Contains("first", StringComparison.OrdinalIgnoreCase) => "🎯",
            var id when id.Contains("explorer", StringComparison.OrdinalIgnoreCase) => "🗺️",
            var id when id.Contains("collector", StringComparison.OrdinalIgnoreCase) => "📦",
            var id when id.Contains("puzzle", StringComparison.OrdinalIgnoreCase) => "🧩",
            var id when id.Contains("master", StringComparison.OrdinalIgnoreCase) => "👑",
            var id when id.Contains("light", StringComparison.OrdinalIgnoreCase) => "💡",
            var id when id.Contains("dark", StringComparison.OrdinalIgnoreCase) => "🌑",
            var id when id.Contains("brave", StringComparison.OrdinalIgnoreCase) => "🦁",
            var id when id.Contains("speed", StringComparison.OrdinalIgnoreCase) => "⚡",
            var id when id.Contains("complete", StringComparison.OrdinalIgnoreCase) => "✅",
            _ => "🏆"
        };
    }
}
