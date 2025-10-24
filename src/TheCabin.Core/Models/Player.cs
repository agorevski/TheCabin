namespace TheCabin.Core.Models;

/// <summary>
/// Represents the player character and their state
/// </summary>
public class Player
{
    /// <summary>
    /// Player's name (optional)
    /// </summary>
    public string Name { get; set; } = "Adventurer";
    
    /// <summary>
    /// ID of the room the player is currently in
    /// </summary>
    public string CurrentLocationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current health points
    /// </summary>
    public int Health { get; set; } = 100;
    
    /// <summary>
    /// Maximum health points
    /// </summary>
    public int MaxHealth { get; set; } = 100;
    
    /// <summary>
    /// Player's inventory
    /// </summary>
    public Inventory Inventory { get; set; } = new();
    
    /// <summary>
    /// Gameplay statistics
    /// </summary>
    public PlayerStats Stats { get; set; } = new();
    
    /// <summary>
    /// Active status effects on the player
    /// </summary>
    public List<StatusEffect> StatusEffects { get; set; } = new();
    
    /// <summary>
    /// Maximum weight the player can carry
    /// </summary>
    public int CarryCapacity { get; set; } = 20;
}

/// <summary>
/// Represents the player's inventory
/// </summary>
public class Inventory
{
    /// <summary>
    /// Items currently in inventory
    /// </summary>
    public List<GameObject> Items { get; set; } = new();
    
    /// <summary>
    /// Current total weight of all items
    /// </summary>
    public int TotalWeight { get; set; }
    
    /// <summary>
    /// Maximum capacity (weight limit)
    /// </summary>
    public int MaxCapacity { get; set; } = 20;
    
    /// <summary>
    /// Checks if an item can be added without exceeding capacity
    /// </summary>
    public bool CanAdd(GameObject item)
    {
        return TotalWeight + item.Weight <= MaxCapacity;
    }
    
    /// <summary>
    /// Checks if inventory contains an item by ID
    /// </summary>
    public bool HasItem(string itemId)
    {
        return Items.Any(i => i.Id == itemId);
    }
    
    /// <summary>
    /// Gets an item from inventory by ID
    /// </summary>
    public GameObject? GetItem(string itemId)
    {
        return Items.FirstOrDefault(i => i.Id == itemId);
    }
}

/// <summary>
/// Tracks player gameplay statistics
/// </summary>
public class PlayerStats
{
    /// <summary>
    /// Total time played in this session
    /// </summary>
    public TimeSpan PlayTime { get; set; }
    
    /// <summary>
    /// Total number of commands executed
    /// </summary>
    public int CommandsExecuted { get; set; }
    
    /// <summary>
    /// Number of unique rooms explored
    /// </summary>
    public int RoomsExplored { get; set; }
    
    /// <summary>
    /// Number of items collected
    /// </summary>
    public int ItemsCollected { get; set; }
    
    /// <summary>
    /// Number of puzzles solved
    /// </summary>
    public int PuzzlesSolved { get; set; }
    
    /// <summary>
    /// When this game session started
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a temporary status effect on the player
/// </summary>
public class StatusEffect
{
    /// <summary>
    /// Unique identifier for the effect
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the effect
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what the effect does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of turns/actions remaining (0 = permanent)
    /// </summary>
    public int Duration { get; set; }
    
    /// <summary>
    /// Type of effect
    /// </summary>
    public EffectType Type { get; set; }
    
    /// <summary>
    /// Strength or amount of the effect
    /// </summary>
    public int Magnitude { get; set; }
}
