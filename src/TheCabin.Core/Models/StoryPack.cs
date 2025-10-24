namespace TheCabin.Core.Models;

/// <summary>
/// Represents a complete theme/story package
/// </summary>
public class StoryPack
{
    /// <summary>
    /// Unique identifier for this story pack
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the theme
    /// </summary>
    public string Theme { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the story pack
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Version of this story pack
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Creator/author of this story pack
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// All rooms in this story pack
    /// </summary>
    public List<Room> Rooms { get; set; } = new();
    
    /// <summary>
    /// All objects available in this story pack
    /// </summary>
    public Dictionary<string, GameObject> Objects { get; set; } = new();
    
    /// <summary>
    /// Metadata about the story pack
    /// </summary>
    public StoryMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// ID of the room where the player starts
    /// </summary>
    public string StartingRoomId { get; set; } = string.Empty;
    
    /// <summary>
    /// Achievements available in this story pack
    /// </summary>
    public List<Achievement> Achievements { get; set; } = new();
    
    /// <summary>
    /// Puzzles available in this story pack
    /// </summary>
    public List<Puzzle> Puzzles { get; set; } = new();
}

/// <summary>
/// Metadata about a story pack
/// </summary>
public class StoryMetadata
{
    /// <summary>
    /// Difficulty level of this story pack
    /// </summary>
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;
    
    /// <summary>
    /// Estimated play time in minutes
    /// </summary>
    public int EstimatedPlayTime { get; set; }
    
    /// <summary>
    /// Tags for categorizing/filtering story packs
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Path to cover image file
    /// </summary>
    public string? CoverImage { get; set; }
    
    /// <summary>
    /// Primary theme color (hex format)
    /// </summary>
    public string ThemeColor { get; set; } = "#512BD4";
    
    /// <summary>
    /// When this story pack was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this story pack was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an achievement that can be unlocked
/// </summary>
public class Achievement
{
    /// <summary>
    /// Unique identifier for the achievement
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the achievement
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of how to unlock it
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Category for grouping achievements
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Points awarded for unlocking
    /// </summary>
    public int Points { get; set; }
    
    /// <summary>
    /// Path to achievement icon
    /// </summary>
    public string IconPath { get; set; } = string.Empty;
    
    /// <summary>
    /// If true, details are hidden until unlocked
    /// </summary>
    public bool IsSecret { get; set; }
    
    /// <summary>
    /// Story flags required to unlock this achievement
    /// </summary>
    public List<string> RequiredFlags { get; set; } = new();
    
    /// <summary>
    /// Trigger conditions for this achievement
    /// </summary>
    public AchievementTrigger Trigger { get; set; } = new();
    
    /// <summary>
    /// Optional reward for unlocking
    /// </summary>
    public AchievementReward? Reward { get; set; }
    
    /// <summary>
    /// Whether this achievement has been unlocked
    /// </summary>
    public bool IsUnlocked { get; set; }
}

/// <summary>
/// Defines the conditions that trigger an achievement unlock
/// </summary>
public class AchievementTrigger
{
    /// <summary>
    /// Type of event that triggers this achievement
    /// </summary>
    public TriggerType Type { get; set; }
    
    /// <summary>
    /// Specific target ID for the trigger (room, item, etc.)
    /// </summary>
    public string TargetId { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of times the trigger must occur
    /// </summary>
    public int RequiredCount { get; set; } = 1;
    
    /// <summary>
    /// Additional conditions that must be met
    /// </summary>
    public Dictionary<string, object> Conditions { get; set; } = new();
}

/// <summary>
/// Rewards granted when an achievement is unlocked
/// </summary>
public class AchievementReward
{
    /// <summary>
    /// Type of reward (points, unlock, cosmetic)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Value/ID of the reward
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the reward
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Tracks player's progress toward an achievement
/// </summary>
public class AchievementProgress
{
    /// <summary>
    /// ID of the achievement being tracked
    /// </summary>
    public string AchievementId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the achievement has been unlocked
    /// </summary>
    public bool IsUnlocked { get; set; }
    
    /// <summary>
    /// When the achievement was unlocked
    /// </summary>
    public DateTime? UnlockedDate { get; set; }
    
    /// <summary>
    /// Current progress toward completion
    /// </summary>
    public int CurrentProgress { get; set; }
    
    /// <summary>
    /// Progress required to unlock
    /// </summary>
    public int RequiredProgress { get; set; } = 1;
    
    /// <summary>
    /// Percentage toward completion
    /// </summary>
    public float PercentComplete => RequiredProgress > 0 
        ? (float)CurrentProgress / RequiredProgress * 100 
        : 0;
    
    /// <summary>
    /// Whether the achievement has been completed
    /// </summary>
    public bool IsComplete => CurrentProgress >= RequiredProgress;
}

/// <summary>
/// Notification data for achievement unlocks
/// </summary>
public class AchievementUnlocked
{
    /// <summary>
    /// The achievement that was unlocked
    /// </summary>
    public Achievement Achievement { get; set; } = new();
    
    /// <summary>
    /// When it was unlocked
    /// </summary>
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Message to display to the player
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Summary of player's achievement statistics
/// </summary>
public class AchievementStats
{
    /// <summary>
    /// Total achievements available
    /// </summary>
    public int TotalAchievements { get; set; }
    
    /// <summary>
    /// Achievements the player has unlocked
    /// </summary>
    public int UnlockedAchievements { get; set; }
    
    /// <summary>
    /// Total points available
    /// </summary>
    public int TotalPoints { get; set; }
    
    /// <summary>
    /// Points the player has earned
    /// </summary>
    public int EarnedPoints { get; set; }
    
    /// <summary>
    /// Percentage of achievements completed
    /// </summary>
    public float CompletionPercentage => TotalAchievements > 0 
        ? (float)UnlockedAchievements / TotalAchievements * 100 
        : 0;
    
    /// <summary>
    /// Progress by category
    /// </summary>
    public Dictionary<string, int> CategoryProgress { get; set; } = new();
}

/// <summary>
/// Summary information about a story pack (for UI lists)
/// </summary>
public class StoryPackInfo
{
    /// <summary>
    /// Story pack ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Theme name
    /// </summary>
    public string Theme { get; set; } = string.Empty;
    
    /// <summary>
    /// Short description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Difficulty level
    /// </summary>
    public Difficulty Difficulty { get; set; }
    
    /// <summary>
    /// Estimated play time in minutes
    /// </summary>
    public int EstimatedPlayTime { get; set; }
    
    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Cover image path
    /// </summary>
    public string? CoverImage { get; set; }
}
