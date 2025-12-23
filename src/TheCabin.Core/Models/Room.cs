namespace TheCabin.Core.Models;

/// <summary>
/// Represents a physical location in the game world
/// </summary>
public class Room
{
    /// <summary>
    /// Unique identifier for the room
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Narrative description of the room
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of object IDs present in this room
    /// </summary>
    public List<string> ObjectIds { get; set; } = new();

    /// <summary>
    /// Available exits from this room (direction -> room ID)
    /// </summary>
    public Dictionary<string, string> Exits { get; set; } = new();

    /// <summary>
    /// Current state of the room
    /// </summary>
    public RoomState State { get; set; } = new();

    /// <summary>
    /// Whether the player has visited this room before
    /// </summary>
    public bool IsVisited { get; set; }

    /// <summary>
    /// Ambient sound file for this room (optional)
    /// </summary>
    public string? AmbientSound { get; set; }

    /// <summary>
    /// Current light level in the room
    /// </summary>
    public LightLevel LightLevel { get; set; } = LightLevel.Normal;
}

/// <summary>
/// Represents the mutable state of a room
/// </summary>
public class RoomState
{
    /// <summary>
    /// Whether the room or its exits are locked
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Custom flags for room-specific states
    /// </summary>
    public Dictionary<string, bool> Flags { get; set; } = new();

    /// <summary>
    /// Object IDs that are currently visible to the player
    /// </summary>
    public List<string> VisibleObjectIds { get; set; } = new();
}
