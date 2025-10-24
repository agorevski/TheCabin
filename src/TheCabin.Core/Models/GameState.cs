namespace TheCabin.Core.Models;

/// <summary>
/// Represents the complete state of the game
/// </summary>
public class GameState
{
    /// <summary>
    /// Unique identifier for this game state
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Name of the saved game
    /// </summary>
    public string SaveName { get; set; } = string.Empty;
    
    /// <summary>
    /// Player character state
    /// </summary>
    public Player Player { get; set; } = new();
    
    /// <summary>
    /// World/environment state
    /// </summary>
    public WorldState World { get; set; } = new();
    
    /// <summary>
    /// Progress and achievement tracking
    /// </summary>
    public ProgressState Progress { get; set; } = new();
    
    /// <summary>
    /// Metadata about the save
    /// </summary>
    public MetaState Meta { get; set; } = new();
    
    /// <summary>
    /// Log of narrative entries for this session
    /// </summary>
    public List<NarrativeEntry> StoryLog { get; set; } = new();
}

/// <summary>
/// Represents the state of the game world
/// </summary>
public class WorldState
{
    /// <summary>
    /// ID of the currently active theme/story pack
    /// </summary>
    public string CurrentThemeId { get; set; } = string.Empty;
    
    /// <summary>
    /// All rooms in the world (room ID -> room)
    /// </summary>
    public Dictionary<string, Room> Rooms { get; set; } = new();
    
    /// <summary>
    /// All objects in the world (object ID -> object)
    /// </summary>
    public Dictionary<string, GameObject> Objects { get; set; } = new();
    
    /// <summary>
    /// Global variables for story/puzzle state
    /// </summary>
    public Dictionary<string, object> GlobalVariables { get; set; } = new();
    
    /// <summary>
    /// Current turn/action number
    /// </summary>
    public int TurnNumber { get; set; }
}

/// <summary>
/// Tracks player progress and achievements
/// </summary>
public class ProgressState
{
    /// <summary>
    /// IDs of puzzles that have been completed
    /// </summary>
    public List<string> CompletedPuzzles { get; set; } = new();
    
    /// <summary>
    /// IDs of areas/rooms that have been unlocked
    /// </summary>
    public List<string> UnlockedAreas { get; set; } = new();
    
    /// <summary>
    /// IDs of secrets that have been discovered
    /// </summary>
    public List<string> DiscoveredSecrets { get; set; } = new();
    
    /// <summary>
    /// Story flags that track narrative progress
    /// </summary>
    public Dictionary<string, bool> StoryFlags { get; set; } = new();
    
    /// <summary>
    /// Player's score (if applicable)
    /// </summary>
    public int Score { get; set; }
    
    /// <summary>
    /// IDs of achievements that have been unlocked
    /// </summary>
    public List<string> UnlockedAchievements { get; set; } = new();
}

/// <summary>
/// Metadata about the game save
/// </summary>
public class MetaState
{
    /// <summary>
    /// When this save was created/last saved
    /// </summary>
    public DateTime SaveTimestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Game version this save is from
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Total time played across all sessions
    /// </summary>
    public TimeSpan TotalPlayTime { get; set; }
    
    /// <summary>
    /// Number of times this game has been saved
    /// </summary>
    public int SaveCount { get; set; }
    
    /// <summary>
    /// Device identifier (optional, for cloud sync)
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// Represents an entry in the narrative story feed
/// </summary>
public class NarrativeEntry
{
    /// <summary>
    /// Unique identifier for this entry
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The text content to display
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of narrative entry
    /// </summary>
    public NarrativeType Type { get; set; }
    
    /// <summary>
    /// When this entry was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Speaker name (for dialogue entries)
    /// </summary>
    public string? SpeakerName { get; set; }
    
    /// <summary>
    /// Whether this entry should be highlighted/emphasized
    /// </summary>
    public bool IsImportant { get; set; }
}
