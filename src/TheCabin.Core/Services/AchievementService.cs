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
    private readonly Dictionary<string, AchievementProgress> _progress = new();
    private readonly string _progressFilePath;

    public AchievementService(ILogger<AchievementService> logger)
    {
        _logger = logger;
        _progressFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheCabin",
            "achievement_progress.json");
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(List<Achievement> achievements)
    {
        _achievements.Clear();
        _progress.Clear();

        foreach (var achievement in achievements)
        {
            _achievements[achievement.Id] = achievement;

            // Initialize fresh progress for all achievements
            _progress[achievement.Id] = new AchievementProgress
            {
                AchievementId = achievement.Id,
                RequiredProgress = achievement.Trigger.RequiredCount
            };
        }

        _logger.LogInformation("Initialized achievement system with {Count} achievements",
            achievements.Count);

        // Try to load saved progress (this will override the fresh progress if save file exists)
        await LoadProgressAsync();
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

        if (unlocked.Any())
        {
            await SaveProgressAsync();
        }

        return unlocked;
    }

    /// <inheritdoc/>
    public async Task<AchievementUnlocked?> UnlockAchievementAsync(string achievementId)
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

        _logger.LogInformation("Achievement unlocked: {Name} ({Id})",
            achievement.Name, achievement.Id);

        var notification = new AchievementUnlocked
        {
            Achievement = achievement,
            UnlockedAt = progress.UnlockedDate.Value,
            Message = $"Achievement Unlocked: {achievement.Name}!"
        };

        await SaveProgressAsync();

        return notification;
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
    public async Task ResetAllProgressAsync()
    {
        _progress.Clear();

        foreach (var achievement in _achievements.Values)
        {
            achievement.IsUnlocked = false;
            _progress[achievement.Id] = new AchievementProgress
            {
                AchievementId = achievement.Id,
                RequiredProgress = achievement.Trigger.RequiredCount
            };
        }

        await SaveProgressAsync();

        _logger.LogInformation("All achievement progress reset");
    }

    /// <inheritdoc/>
    public async Task SaveProgressAsync()
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_progressFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize progress
            var json = JsonSerializer.Serialize(_progress, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_progressFilePath, json);

            _logger.LogDebug("Achievement progress saved to {Path}", _progressFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save achievement progress");
        }
    }

    /// <inheritdoc/>
    public async Task LoadProgressAsync()
    {
        try
        {
            if (!File.Exists(_progressFilePath))
            {
                _logger.LogDebug("No saved achievement progress found");
                return;
            }

            var json = await File.ReadAllTextAsync(_progressFilePath);
            var loadedProgress = JsonSerializer.Deserialize<Dictionary<string, AchievementProgress>>(json);

            if (loadedProgress != null)
            {
                foreach (var kvp in loadedProgress)
                {
                    if (_achievements.ContainsKey(kvp.Key))
                    {
                        _progress[kvp.Key] = kvp.Value;

                        // Update achievement unlocked status
                        if (kvp.Value.IsUnlocked)
                        {
                            _achievements[kvp.Key].IsUnlocked = true;
                        }
                    }
                }

                _logger.LogInformation("Loaded achievement progress: {Unlocked}/{Total} unlocked",
                    _progress.Values.Count(p => p.IsUnlocked), _progress.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load achievement progress");
        }
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
                    if (gameState.Player.Health < Convert.ToInt32(condition.Value))
                        return false;
                    break;

                case "maxhealth":
                    if (gameState.Player.Health > Convert.ToInt32(condition.Value))
                        return false;
                    break;

                case "minplaytime":
                    if (gameState.Player.Stats.PlayTime.TotalMinutes < Convert.ToDouble(condition.Value))
                        return false;
                    break;

                case "maxplaytime":
                    if (gameState.Player.Stats.PlayTime.TotalMinutes > Convert.ToDouble(condition.Value))
                        return false;
                    break;

                case "inventorycount":
                    if (gameState.Player.Inventory.Items.Count != Convert.ToInt32(condition.Value))
                        return false;
                    break;

                case "mininventory":
                    if (gameState.Player.Inventory.Items.Count < Convert.ToInt32(condition.Value))
                        return false;
                    break;

                case "maxinventory":
                    if (gameState.Player.Inventory.Items.Count > Convert.ToInt32(condition.Value))
                        return false;
                    break;

                case "timeofday":
                    var hour = DateTime.Now.Hour;
                    var timeRange = condition.Value.ToString()?.Split('-');
                    if (timeRange?.Length == 2)
                    {
                        var startHour = int.Parse(timeRange[0]);
                        var endHour = int.Parse(timeRange[1]);
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
}
