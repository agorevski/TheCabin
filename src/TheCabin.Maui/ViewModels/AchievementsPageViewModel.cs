using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TheCabin.Core.Interfaces;
using TheCabin.Maui.Models;

namespace TheCabin.Maui.ViewModels;

public partial class AchievementsPageViewModel : BaseViewModel
{
    private readonly IAchievementService _achievementService;

    [ObservableProperty]
    private ObservableCollection<AchievementViewModel> achievements = new();

    [ObservableProperty]
    private ObservableCollection<AchievementViewModel> filteredAchievements = new();

    [ObservableProperty]
    private string selectedFilter = "All";

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int unlockedCount;

    [ObservableProperty]
    private int lockedCount;

    [ObservableProperty]
    private double completionPercentage;

    [ObservableProperty]
    private string completionText = string.Empty;

    [ObservableProperty]
    private bool hasAchievements;

    [ObservableProperty]
    private string emptyStateMessage = "No achievements available for this story pack.";

    public AchievementsPageViewModel(IAchievementService achievementService)
    {
        _achievementService = achievementService;
        Title = "Achievements";
    }

    /// <summary>
    /// Loads achievements from the achievement service
    /// </summary>
    [RelayCommand]
    private async Task LoadAchievementsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var allAchievements = _achievementService.GetAllAchievements();
            var allProgress = _achievementService.GetAllProgress();

            Achievements.Clear();
            foreach (var achievement in allAchievements)
            {
                var progress = allProgress.TryGetValue(achievement.Id, out var p) ? p : null;
                Achievements.Add(AchievementViewModel.FromModel(achievement, progress));
            }

            UpdateStatistics();
            ApplyFilter(SelectedFilter);
            HasAchievements = Achievements.Count > 0;

            if (!HasAchievements)
            {
                EmptyStateMessage = "No achievements available for this story pack.";
            }
        }, "Failed to load achievements");
    }

    /// <summary>
    /// Filters achievements based on selected filter
    /// </summary>
    [RelayCommand]
    private void FilterAchievements(string filter)
    {
        SelectedFilter = filter;
        ApplyFilter(filter);
    }

    /// <summary>
    /// Refreshes the achievement list
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAchievementsAsync();
    }

    /// <summary>
    /// Applies the selected filter to the achievements list
    /// </summary>
    private void ApplyFilter(string filter)
    {
        FilteredAchievements.Clear();

        var filtered = filter switch
        {
            "Unlocked" => Achievements.Where(a => a.IsUnlocked),
            "Locked" => Achievements.Where(a => !a.IsUnlocked),
            _ => Achievements // "All"
        };

        // Sort: unlocked first, then by name
        var sorted = filtered
            .OrderByDescending(a => a.IsUnlocked)
            .ThenBy(a => a.Name);

        foreach (var achievement in sorted)
        {
            FilteredAchievements.Add(achievement);
        }
    }

    /// <summary>
    /// Updates achievement statistics
    /// </summary>
    private void UpdateStatistics()
    {
        TotalCount = Achievements.Count;
        UnlockedCount = Achievements.Count(a => a.IsUnlocked);
        LockedCount = TotalCount - UnlockedCount;

        CompletionPercentage = TotalCount > 0
            ? (double)UnlockedCount / TotalCount
            : 0;

        CompletionText = $"{UnlockedCount}/{TotalCount} ({CompletionPercentage:P0})";
    }

    /// <summary>
    /// Called when the page appears
    /// </summary>
    public async Task OnAppearingAsync()
    {
        await LoadAchievementsAsync();
    }
}
