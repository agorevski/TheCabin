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
    /// Story flags required to unlock this achievement
    /// </summary>
    public List<string> RequiredFlags { get; set; } = new();
    
    /// <summary>
    /// Whether this achievement has been unlocked
    /// </summary>
    public bool IsUnlocked { get; set; }
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
