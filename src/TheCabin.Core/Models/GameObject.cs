namespace TheCabin.Core.Models;

/// <summary>
/// Represents an interactive object in the game world
/// </summary>
public class GameObject
{
    /// <summary>
    /// Unique identifier for the object
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the object
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description shown when examined
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type/category of the object
    /// </summary>
    public ObjectType Type { get; set; }

    /// <summary>
    /// Current state of the object
    /// </summary>
    public ObjectState State { get; set; } = new();

    /// <summary>
    /// Available actions for this object (verb -> action definition)
    /// </summary>
    public Dictionary<string, ActionDefinition> Actions { get; set; } = new();

    /// <summary>
    /// Items required to interact with this object
    /// </summary>
    public List<string> RequiredItems { get; set; } = new();

    /// <summary>
    /// Whether the player can collect this object
    /// </summary>
    public bool IsCollectable { get; set; }

    /// <summary>
    /// Whether the object is currently visible to the player
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Weight of the object (for inventory management)
    /// </summary>
    public int Weight { get; set; }
}

/// <summary>
/// Represents the mutable state of an object
/// </summary>
public class ObjectState
{
    /// <summary>
    /// Current state identifier (e.g., "lit", "unlit", "open", "closed")
    /// </summary>
    public string CurrentState { get; set; } = "default";

    /// <summary>
    /// Custom flags for object-specific states
    /// </summary>
    public Dictionary<string, bool> Flags { get; set; } = new();

    /// <summary>
    /// Number of times this object has been used
    /// </summary>
    public int UsageCount { get; set; }
}

/// <summary>
/// Defines an action that can be performed on an object
/// </summary>
public class ActionDefinition
{
    /// <summary>
    /// The verb that triggers this action
    /// </summary>
    public string Verb { get; set; } = string.Empty;

    /// <summary>
    /// Message shown when action succeeds
    /// </summary>
    public string SuccessMessage { get; set; } = string.Empty;

    /// <summary>
    /// Message shown when action fails
    /// </summary>
    public string FailureMessage { get; set; } = string.Empty;

    /// <summary>
    /// State changes that occur when action succeeds
    /// </summary>
    public List<StateChange> StateChanges { get; set; } = new();

    /// <summary>
    /// Flags that must be true for action to succeed
    /// </summary>
    public List<string> RequiredFlags { get; set; } = new();

    /// <summary>
    /// Whether this action consumes/removes the item
    /// </summary>
    public bool ConsumesItem { get; set; }
}

/// <summary>
/// Represents a change to game state resulting from an action
/// </summary>
public class StateChange
{
    /// <summary>
    /// Target of the state change ("self", "room", or object ID)
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Property name to change
    /// </summary>
    public string Property { get; set; } = string.Empty;

    /// <summary>
    /// New value for the property
    /// </summary>
    public object? NewValue { get; set; }
}
