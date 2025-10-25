using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Interface for the game state machine that manages world state, room transitions, and game flow
/// </summary>
public interface IGameStateMachine
{
    /// <summary>
    /// Current game state
    /// </summary>
    GameState CurrentState { get; }
    
    /// <summary>
    /// Initializes a new game from a story pack (synchronous version for backward compatibility)
    /// </summary>
    void Initialize(StoryPack storyPack);
    
    /// <summary>
    /// Initializes a new game from a story pack with achievement tracking
    /// </summary>
    Task InitializeAsync(StoryPack storyPack);
    
    /// <summary>
    /// Gets the room the player is currently in
    /// </summary>
    Room GetCurrentRoom();
    
    /// <summary>
    /// Gets all objects visible in the current room
    /// </summary>
    List<GameObject> GetVisibleObjects();
    
    /// <summary>
    /// Gets all available exits from the current room
    /// </summary>
    Dictionary<string, string> GetAvailableExits();
    
    /// <summary>
    /// Checks if the player can transition to the specified room
    /// </summary>
    bool CanTransitionTo(string roomId);
    
    /// <summary>
    /// Transitions the player to a new room (synchronous version for backward compatibility)
    /// </summary>
    void TransitionTo(string roomId);
    
    /// <summary>
    /// Transitions the player to a new room with achievement tracking
    /// </summary>
    Task TransitionToAsync(string roomId);
    
    /// <summary>
    /// Finds an object by ID or partial name match
    /// </summary>
    GameObject? FindObject(string identifier);
    
    /// <summary>
    /// Finds a visible object in the current room
    /// </summary>
    GameObject? FindVisibleObject(string identifier);
    
    /// <summary>
    /// Adds a narrative entry to the story log
    /// </summary>
    void AddNarrativeEntry(string text, NarrativeType type, bool isImportant = false);
    
    /// <summary>
    /// Updates player health
    /// </summary>
    void ModifyHealth(int amount);
    
    /// <summary>
    /// Sets a story flag
    /// </summary>
    void SetStoryFlag(string flagName, bool value);
    
    /// <summary>
    /// Gets a story flag value
    /// </summary>
    bool GetStoryFlag(string flagName);
}
