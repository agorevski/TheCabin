using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Services;

/// <summary>
/// Service for managing and tracking player achievements
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly ILogger<AchievementService> _logger;
    private readonly Dictionary<string, Achievement> _achievements = new();
    private Dictionary<string, AchievementProgress> _progress = new();
    private GameState? _boundState;

    public AchievementService(ILogger<AchievementService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task InitializeAsync(List<Achievement> achievements)
    {
        _achievements.Clear();
        foreach (var achievement in achievements)
        {
            _achievements[achievement.Id] = achievement;
        }

        _boundState = null;
        AttachProgressStore(new Dictionary<string, AchievementProgress>());

        _logger.LogInformation("Initialized achievement system with {Count} achievements",
            achievements.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void BindToGameState(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        _boundState = gameState;
        AttachProgressStore(gameState.Progress.AchievementProgress);
    }

    /// <inheritdoc/>
    public List<Achievement> GetAllAchievements()
    {
        return _achievements.Values.ToList();
    }

    /// <inheritdoc/>
    public AchievementProgress? GetProgress(string achievementId)
    {
        return _progress.GetValueOrDefault(achievementId);
    }

    /// <inheritdoc/>
    public Dictionary<string, AchievementProgress> GetAllProgress()
    {
        return new Dictionary<string, AchievementProgress>(_progress);
    }

    /// <inheritdoc/>
    public async Task<List<AchievementUnlocked>> TrackEventAsync(
        TriggerType triggerType,
        string targetId,
        GameState gameState)
    {
        // Progress always lands on the game it is tracked for, so it is saved with that game
        if (!ReferenceEquals(_boundState, gameState))
        {
            BindToGameState(gameState);
        }

        var unlocked = new List<AchievementUnlocked>();

        foreach (var achievement in _achievements.Values.Where(a => !IsUnlocked(a.Id)))
        {
            // Check if this event matches the achievement trigger
            if (achievement.Trigger.Type != triggerType)
                continue;

            // Check if target matches (empty string means any target)
            if (!string.IsNullOrEmpty(achievement.Trigger.TargetId) &&
                achievement.Trigger.TargetId != targetId)
                continue;

            // Check additional conditions
            if (!CheckConditions(achievement, gameState))
                continue;

            // Check required flags
            if (!CheckRequiredFlags(achievement, gameState))
                continue;

            // Increment progress
            var progress = _progress[achievement.Id];
            progress.CurrentProgress++;

            _logger.LogDebug("Achievement {Id} progress: {Current}/{Required}",
                achievement.Id, progress.CurrentProgress, progress.RequiredProgress);

            // Check if unlocked
            if (progress.IsComplete && !progress.IsUnlocked)
            {
                var notification = await UnlockAchievementAsync(achievement.Id);
                if (notification != null)
                {
                    unlocked.Add(notification);
                }
            }
        }

        return unlocked;
    }

    /// <inheritdoc/>
    public Task<AchievementUnlocked?> UnlockAchievementAsync(string achievementId)
    {
        return Task.FromResult(UnlockAchievement(achievementId));
    }

    private AchievementUnlocked? UnlockAchievement(string achievementId)
    {
        if (!_achievements.TryGetValue(achievementId, out var achievement))
        {
            _logger.LogWarning("Attempted to unlock unknown achievement: {Id}", achievementId);
            return null;
        }

        var progress = _progress[achievementId];
        if (progress.IsUnlocked)
        {
            _logger.LogDebug("Achievement {Id} already unlocked", achievementId);
            return null;
        }

        progress.IsUnlocked = true;
        progress.UnlockedDate = DateTime.UtcNow;
        achievement.IsUnlocked = true;

        // Record the unlock on the bound game so it is included when that game is saved
        if (_boundState != null && !_boundState.Progress.UnlockedAchievements.Contains(achievementId))
        {
            _boundState.Progress.UnlockedAchievements.Add(achievementId);
        }

        _logger.LogInformation("Achievement unlocked: {Name} ({Id})",
            achievement.Name, achievement.Id);

        return new AchievementUnlocked
        {
            Achievement = achievement,
            UnlockedAt = progress.UnlockedDate.Value,
            Message = $"Achievement Unlocked: {achievement.Name}!"
        };
    }

    /// <inheritdoc/>
    public bool IsUnlocked(string achievementId)
    {
        return _progress.TryGetValue(achievementId, out var progress) && progress.IsUnlocked;
    }

    /// <inheritdoc/>
    public AchievementStats GetStats()
    {
        var stats = new AchievementStats
        {
            TotalAchievements = _achievements.Count,
            UnlockedAchievements = _progress.Values.Count(p => p.IsUnlocked),
            TotalPoints = _achievements.Values.Sum(a => a.Points),
            EarnedPoints = _achievements.Values
                .Where(a => IsUnlocked(a.Id))
                .Sum(a => a.Points)
        };

        // Calculate category progress
        foreach (var category in _achievements.Values.Select(a => a.Category).Distinct())
        {
            var categoryAchievements = _achievements.Values.Where(a => a.Category == category);
            var unlockedInCategory = categoryAchievements.Count(a => IsUnlocked(a.Id));
            stats.CategoryProgress[category] = unlockedInCategory;
        }

        return stats;
    }

    /// <inheritdoc/>
    public List<Achievement> GetByCategory(string category)
    {
        return _achievements.Values
            .Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc/>
    public List<Achievement> GetUnlockedAchievements()
    {
        return _achievements.Values
            .Where(a => IsUnlocked(a.Id))
            .ToList();
    }

    /// <inheritdoc/>
    public List<Achievement> GetLockedAchievements(bool includeSecret = false)
    {
        return _achievements.Values
            .Where(a => !IsUnlocked(a.Id) && (includeSecret || !a.IsSecret))
            .ToList();
    }

    /// <inheritdoc/>
    public Task ResetAllProgressAsync()
    {
        _progress.Clear();
        _boundState?.Progress.UnlockedAchievements.Clear();
        AttachProgressStore(_progress);

        _logger.LogInformation("All achievement progress reset");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads a numeric trigger condition, which may arrive as a JSON element when loaded from a pack file
    /// </summary>
    private static int ToInt32(object value)
    {
        return value is JsonElement element
            ? element.GetInt32()
            : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static double ToDouble(object value)
    {
        return value is JsonElement element
            ? element.GetDouble()
            : Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Check if additional trigger conditions are met
    /// </summary>
    private bool CheckConditions(Achievement achievement, GameState gameState)
    {
        if (!achievement.Trigger.Conditions.Any())
            return true;

        foreach (var condition in achievement.Trigger.Conditions)
        {
            switch (condition.Key.ToLower())
            {
                case "minhealth":
                    if (gameState.Player.Health < ToInt32(condition.Value))
                        return false;
                    break;

                case "maxhealth":
                    if (gameState.Player.Health > ToInt32(condition.Value))
                        return false;
                    break;

                case "minplaytime":
                    if (gameState.Player.Stats.PlayTime.TotalMinutes < ToDouble(condition.Value))
                        return false;
                    break;

                case "maxplaytime":
                    if (gameState.Player.Stats.PlayTime.TotalMinutes > ToDouble(condition.Value))
                        return false;
                    break;

                case "inventorycount":
                    if (gameState.Player.Inventory.Items.Count != ToInt32(condition.Value))
                        return false;
                    break;

                case "mininventory":
                    if (gameState.Player.Inventory.Items.Count < ToInt32(condition.Value))
                        return false;
                    break;

                case "maxinventory":
                    if (gameState.Player.Inventory.Items.Count > ToInt32(condition.Value))
                        return false;
                    break;

                case "timeofday":
                    var hour = DateTime.Now.Hour;
                    var timeRange = condition.Value.ToString()?.Split('-');
                    if (timeRange?.Length == 2)
                    {
                        if (!int.TryParse(timeRange[0], out var startHour) || !int.TryParse(timeRange[1], out var endHour))
                            return false;

                        if (hour < startHour || hour >= endHour)
                            return false;
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown condition type: {Type}", condition.Key);
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if all required story flags are set
    /// </summary>
    private bool CheckRequiredFlags(Achievement achievement, GameState gameState)
    {
        if (!achievement.RequiredFlags.Any())
            return true;

        foreach (var flag in achievement.RequiredFlags)
        {
            if (!gameState.Progress.StoryFlags.TryGetValue(flag, out var value) || !value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Uses <paramref name="store"/> as the progress of the current game, adding fresh entries for any
    /// achievement it lacks, dropping entries for achievements the pack does not define, and syncing unlock flags.
    /// </summary>
    private void AttachProgressStore(Dictionary<string, AchievementProgress> store)
    {
        _progress = store;

        foreach (var achievement in _achievements.Values)
        {
            if (!_progress.TryGetValue(achievement.Id, out var progress))
            {
                progress = new AchievementProgress { AchievementId = achievement.Id };
                _progress[achievement.Id] = progress;
            }

            progress.RequiredProgress = achievement.Trigger.RequiredCount;
            achievement.IsUnlocked = progress.IsUnlocked;
        }

        foreach (var staleId in _progress.Keys.Where(id => !_achievements.ContainsKey(id)).ToList())
        {
            _progress.Remove(staleId);
        }
    }
}
