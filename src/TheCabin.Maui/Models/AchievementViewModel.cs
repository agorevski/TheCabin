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
    /// Creates an AchievementViewModel from an Achievement model
    /// </summary>
    public static AchievementViewModel FromModel(Achievement achievement)
    {
        var viewModel = new AchievementViewModel
        {
            Id = achievement.Id,
            Name = achievement.Name,
            Description = achievement.Description,
            Icon = GetIconForAchievement(achievement),
            IsUnlocked = achievement.IsUnlocked,
            UnlockDate = achievement.UnlockDate
        };

        // Calculate progress for count-based achievements
        if (achievement.RequiredCount > 0)
        {
            viewModel.HasProgress = true;
            viewModel.CurrentProgress = achievement.CurrentCount;
            viewModel.RequiredProgress = achievement.RequiredCount;
            viewModel.ProgressPercentage = achievement.RequiredCount > 0
                ? (double)achievement.CurrentCount / achievement.RequiredCount
                : 0;
            viewModel.ProgressText = $"{achievement.CurrentCount}/{achievement.RequiredCount}";
        }
        else
        {
            viewModel.HasProgress = false;
            viewModel.ProgressPercentage = achievement.IsUnlocked ? 1.0 : 0.0;
        }

        // Set status text
        viewModel.StatusText = achievement.IsUnlocked
            ? $"Unlocked {achievement.UnlockDate:MMM d, yyyy}"
            : viewModel.HasProgress
                ? $"Progress: {viewModel.ProgressText}"
                : "Locked";

        return viewModel;
    }

    /// <summary>
    /// Updates the view model from an Achievement model (for refresh scenarios)
    /// </summary>
    public void UpdateFromModel(Achievement achievement)
    {
        IsUnlocked = achievement.IsUnlocked;
        UnlockDate = achievement.UnlockDate;

        if (achievement.RequiredCount > 0)
        {
            CurrentProgress = achievement.CurrentCount;
            RequiredProgress = achievement.RequiredCount;
            ProgressPercentage = achievement.RequiredCount > 0
                ? (double)achievement.CurrentCount / achievement.RequiredCount
                : 0;
            ProgressText = $"{achievement.CurrentCount}/{achievement.RequiredCount}";
        }
        else
        {
            ProgressPercentage = achievement.IsUnlocked ? 1.0 : 0.0;
        }

        StatusText = achievement.IsUnlocked
            ? $"Unlocked {achievement.UnlockDate:MMM d, yyyy}"
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
