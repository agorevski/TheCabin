namespace TheCabin.Core.Models;

/// <summary>
/// Light levels in rooms affecting visibility and gameplay
/// </summary>
public enum LightLevel
{
    /// <summary>Full visibility</summary>
    Bright,
    
    /// <summary>Standard visibility</summary>
    Normal,
    
    /// <summary>Reduced visibility</summary>
    Dim,
    
    /// <summary>Very low visibility</summary>
    Dark,
    
    /// <summary>No visibility without light source</summary>
    PitchBlack
}

/// <summary>
/// Types of interactive objects in the game world
/// </summary>
public enum ObjectType
{
    /// <summary>Generic item that can be collected</summary>
    Item,
    
    /// <summary>Tool that can be used for actions</summary>
    Tool,
    
    /// <summary>Static furniture in rooms</summary>
    Furniture,
    
    /// <summary>Door or passage between rooms</summary>
    Door,
    
    /// <summary>Container that can hold items</summary>
    Container,
    
    /// <summary>Light source object</summary>
    Light,
    
    /// <summary>Puzzle-related object</summary>
    Puzzle,
    
    /// <summary>Decorative object with no interaction</summary>
    Decoration
}

/// <summary>
/// Result types from command execution
/// </summary>
public enum CommandResultType
{
    /// <summary>Command executed successfully</summary>
    Success,
    
    /// <summary>Command failed to execute</summary>
    Failure,
    
    /// <summary>Command not recognized</summary>
    InvalidCommand,
    
    /// <summary>Command valid but requirements not met</summary>
    RequirementsNotMet,
    
    /// <summary>Command interpretation unclear</summary>
    AmbiguousCommand,
    
    /// <summary>System message to player</summary>
    SystemMessage
}

/// <summary>
/// Difficulty levels for story packs
/// </summary>
public enum Difficulty
{
    /// <summary>Simple puzzles, forgiving gameplay</summary>
    Easy,
    
    /// <summary>Moderate challenge</summary>
    Medium,
    
    /// <summary>Complex puzzles and mechanics</summary>
    Hard,
    
    /// <summary>Most challenging experience</summary>
    Expert
}

/// <summary>
/// Types of status effects that can affect the player
/// </summary>
public enum EffectType
{
    /// <summary>Affects player health</summary>
    Health,
    
    /// <summary>Affects movement speed</summary>
    Speed,
    
    /// <summary>Affects visibility/perception</summary>
    Vision,
    
    /// <summary>Affects ability to interact with objects</summary>
    Strength
}

/// <summary>
/// Types of narrative entries in the story feed
/// </summary>
public enum NarrativeType
{
    /// <summary>Room or object description</summary>
    Description,
    
    /// <summary>Player-issued command</summary>
    PlayerCommand,
    
    /// <summary>System notification</summary>
    SystemMessage,
    
    /// <summary>Successful action result</summary>
    Success,
    
    /// <summary>Failed action result</summary>
    Failure,
    
    /// <summary>NPC dialogue</summary>
    Dialogue,
    
    /// <summary>Discovery or revelation</summary>
    Discovery
}

/// <summary>
/// Types of events that can trigger achievements
/// </summary>
public enum TriggerType
{
    /// <summary>Player entered a room</summary>
    RoomVisited,
    
    /// <summary>Player collected an item</summary>
    ItemCollected,
    
    /// <summary>Player dropped an item</summary>
    ItemDropped,
    
    /// <summary>Player used an item</summary>
    ItemUsed,
    
    /// <summary>Player solved a puzzle</summary>
    PuzzleSolved,
    
    /// <summary>Player executed a command</summary>
    CommandExecuted,
    
    /// <summary>Player completed a story pack</summary>
    StoryCompleted,
    
    /// <summary>Time-based trigger</summary>
    TimeElapsed,
    
    /// <summary>Player stat reached threshold</summary>
    StatThreshold,
    
    /// <summary>Player examined an object</summary>
    ObjectExamined,
    
    /// <summary>Player opened a container</summary>
    ContainerOpened,
    
    /// <summary>Game flag was set</summary>
    FlagSet
}
