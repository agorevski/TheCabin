using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Repository for managing game save data
/// </summary>
public interface IGameSaveRepository
{
    /// <summary>
    /// Saves a game state
    /// </summary>
    /// <param name="saveName">Name for this save</param>
    /// <param name="gameState">Game state to save</param>
    /// <returns>ID of the saved game</returns>
    Task<int> SaveAsync(string saveName, GameState gameState);

    /// <summary>
    /// Loads a game state by ID
    /// </summary>
    /// <param name="saveId">ID of the save to load</param>
    /// <returns>The saved game state</returns>
    Task<GameState?> LoadAsync(int saveId);

    /// <summary>
    /// Gets all saved games
    /// </summary>
    /// <returns>List of save information</returns>
    Task<List<GameSaveInfo>> GetAllAsync();

    /// <summary>
    /// Deletes a saved game
    /// </summary>
    /// <param name="saveId">ID of the save to delete</param>
    Task DeleteAsync(int saveId);

    /// <summary>
    /// Checks if a save exists
    /// </summary>
    /// <param name="saveId">ID of the save to check</param>
    /// <returns>True if the save exists</returns>
    Task<bool> ExistsAsync(int saveId);
}
