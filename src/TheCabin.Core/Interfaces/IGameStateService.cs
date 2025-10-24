using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Service for managing game state operations
/// </summary>
public interface IGameStateService
{
    /// <summary>
    /// Gets the current game state
    /// </summary>
    GameState CurrentState { get; }
    
    /// <summary>
    /// Initializes a new game with the specified story pack
    /// </summary>
    /// <param name="storyPack">Story pack to use for the game</param>
    Task InitializeNewGameAsync(StoryPack storyPack);
    
    /// <summary>
    /// Saves the current game state
    /// </summary>
    /// <param name="saveName">Name for this save</param>
    /// <returns>ID of the saved game</returns>
    Task<int> SaveGameAsync(string saveName);
    
    /// <summary>
    /// Loads a saved game by ID
    /// </summary>
    /// <param name="saveId">ID of the save to load</param>
    Task LoadGameAsync(int saveId);
    
    /// <summary>
    /// Gets a list of all saved games
    /// </summary>
    Task<List<GameSaveInfo>> GetSavedGamesAsync();
    
    /// <summary>
    /// Deletes a saved game
    /// </summary>
    /// <param name="saveId">ID of the save to delete</param>
    Task DeleteSaveAsync(int saveId);
}

/// <summary>
/// Information about a saved game
/// </summary>
public class GameSaveInfo
{
    /// <summary>
    /// Save ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Save name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Theme/story pack ID
    /// </summary>
    public string ThemeId { get; set; } = string.Empty;
    
    /// <summary>
    /// When the game was saved
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Total play time
    /// </summary>
    public TimeSpan PlayTime { get; set; }
    
    /// <summary>
    /// Thumbnail image (optional)
    /// </summary>
    public byte[]? Thumbnail { get; set; }
}
