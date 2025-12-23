using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Service for managing and tracking player achievements
/// </summary>
public interface IAchievementService
{
    /// <summary>
    /// Initialize the achievement system with available achievements
    /// </summary>
    /// <param name="achievements">List of all available achievements</param>
    Task InitializeAsync(List<Achievement> achievements);

    /// <summary>
    /// Get all available achievements
    /// </summary>
    /// <returns>List of all achievements</returns>
    List<Achievement> GetAllAchievements();

    /// <summary>
    /// Get progress for a specific achievement
    /// </summary>
    /// <param name="achievementId">Achievement ID</param>
    /// <returns>Achievement progress or null if not found</returns>
    AchievementProgress? GetProgress(string achievementId);

    /// <summary>
    /// Get progress for all achievements
    /// </summary>
    /// <returns>Dictionary of achievement ID to progress</returns>
    Dictionary<string, AchievementProgress> GetAllProgress();

    /// <summary>
    /// Track an event that might trigger achievements
    /// </summary>
    /// <param name="triggerType">Type of event</param>
    /// <param name="targetId">ID of the target (room, item, etc.)</param>
    /// <param name="gameState">Current game state for context</param>
    /// <returns>List of newly unlocked achievements</returns>
    Task<List<AchievementUnlocked>> TrackEventAsync(
        TriggerType triggerType,
        string targetId,
        GameState gameState);

    /// <summary>
    /// Manually unlock an achievement
    /// </summary>
    /// <param name="achievementId">Achievement ID to unlock</param>
    /// <returns>Unlock notification or null if already unlocked</returns>
    Task<AchievementUnlocked?> UnlockAchievementAsync(string achievementId);

    /// <summary>
    /// Check if an achievement is unlocked
    /// </summary>
    /// <param name="achievementId">Achievement ID</param>
    /// <returns>True if unlocked</returns>
    bool IsUnlocked(string achievementId);

    /// <summary>
    /// Get overall achievement statistics
    /// </summary>
    /// <returns>Statistics summary</returns>
    AchievementStats GetStats();

    /// <summary>
    /// Get achievements by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>List of achievements in that category</returns>
    List<Achievement> GetByCategory(string category);

    /// <summary>
    /// Get all unlocked achievements
    /// </summary>
    /// <returns>List of unlocked achievements</returns>
    List<Achievement> GetUnlockedAchievements();

    /// <summary>
    /// Get all locked achievements
    /// </summary>
    /// <param name="includeSecret">Whether to include secret achievements</param>
    /// <returns>List of locked achievements</returns>
    List<Achievement> GetLockedAchievements(bool includeSecret = false);

    /// <summary>
    /// Reset all achievement progress (for testing or new game+)
    /// </summary>
    Task ResetAllProgressAsync();

    /// <summary>
    /// Save current achievement progress
    /// </summary>
    Task SaveProgressAsync();

    /// <summary>
    /// Load achievement progress from storage
    /// </summary>
    Task LoadProgressAsync();
}
