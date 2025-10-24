using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Service for managing story packs
/// </summary>
public interface IStoryPackService
{
    /// <summary>
    /// Gets a list of all available story packs
    /// </summary>
    Task<List<StoryPackInfo>> GetAvailablePacksAsync();
    
    /// <summary>
    /// Loads a story pack by ID
    /// </summary>
    /// <param name="packId">ID of the pack to load</param>
    Task<StoryPack> LoadPackAsync(string packId);
    
    /// <summary>
    /// Validates a story pack for correctness
    /// </summary>
    /// <param name="pack">Story pack to validate</param>
    /// <returns>Validation result with any errors or warnings</returns>
    ValidationResult ValidatePack(StoryPack pack);
    
    /// <summary>
    /// Unloads a story pack from memory
    /// </summary>
    /// <param name="packId">ID of the pack to unload</param>
    void UnloadPack(string packId);
}

/// <summary>
/// Result of story pack validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the pack is valid
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Error messages that prevent the pack from loading
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Warning messages about potential issues
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
