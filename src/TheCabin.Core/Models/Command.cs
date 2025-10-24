namespace TheCabin.Core.Models;

/// <summary>
/// Represents a parsed command from player input
/// </summary>
public class ParsedCommand
{
    /// <summary>
    /// The action verb (e.g., "take", "go", "use")
    /// </summary>
    public string Verb { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary object of the command (e.g., "lantern" in "take lantern")
    /// </summary>
    public string? Object { get; set; }
    
    /// <summary>
    /// Secondary object or target (e.g., "door" in "use key on door")
    /// </summary>
    public string? Target { get; set; }
    
    /// <summary>
    /// Additional context or modifiers
    /// </summary>
    public string? Context { get; set; }
    
    /// <summary>
    /// Confidence score from the parser (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Original raw input from the player
    /// </summary>
    public string RawInput { get; set; } = string.Empty;
    
    /// <summary>
    /// When the command was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the result of executing a command
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Whether the command executed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Primary message to display to the player
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional messages (for multi-part responses)
    /// </summary>
    public List<string> AdditionalMessages { get; set; } = new();
    
    /// <summary>
    /// Changes to game state that occurred
    /// </summary>
    public GameStateChange? StateChange { get; set; }
    
    /// <summary>
    /// Type of result
    /// </summary>
    public CommandResultType Type { get; set; }
}

/// <summary>
/// Represents changes to the game state from a command
/// </summary>
public class GameStateChange
{
    /// <summary>
    /// New location ID if player moved (null if no movement)
    /// </summary>
    public string? LocationChanged { get; set; }
    
    /// <summary>
    /// Items added to inventory
    /// </summary>
    public List<string> ItemsAdded { get; set; } = new();
    
    /// <summary>
    /// Items removed from inventory
    /// </summary>
    public List<string> ItemsRemoved { get; set; } = new();
    
    /// <summary>
    /// Change in health points (positive or negative)
    /// </summary>
    public int HealthChange { get; set; }
    
    /// <summary>
    /// Flags that were changed (flag name -> new value)
    /// </summary>
    public Dictionary<string, bool> FlagsChanged { get; set; } = new();
}

/// <summary>
/// Context information for command parsing
/// </summary>
public class GameContext
{
    /// <summary>
    /// Current room ID
    /// </summary>
    public string CurrentLocation { get; set; } = string.Empty;
    
    /// <summary>
    /// Object IDs visible in current room
    /// </summary>
    public List<string> VisibleObjects { get; set; } = new();
    
    /// <summary>
    /// Object IDs in player's inventory
    /// </summary>
    public List<string> InventoryItems { get; set; } = new();
    
    /// <summary>
    /// Recent commands for context (last 3-5 commands)
    /// </summary>
    public List<string> RecentCommands { get; set; } = new();
    
    /// <summary>
    /// Active story/game flags
    /// </summary>
    public Dictionary<string, bool> GameFlags { get; set; } = new();
    
    /// <summary>
    /// Creates context from game state
    /// </summary>
    public static GameContext FromGameState(GameState gameState)
    {
        var room = gameState.World.Rooms.GetValueOrDefault(gameState.Player.CurrentLocationId);
        
        return new GameContext
        {
            CurrentLocation = gameState.Player.CurrentLocationId,
            VisibleObjects = room?.State.VisibleObjectIds ?? new List<string>(),
            InventoryItems = gameState.Player.Inventory.Items.Select(i => i.Id).ToList(),
            RecentCommands = gameState.StoryLog
                .Where(e => e.Type == NarrativeType.PlayerCommand)
                .Select(e => e.Text)
                .TakeLast(5)
                .ToList(),
            GameFlags = gameState.Progress.StoryFlags
        };
    }
}
