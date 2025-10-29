namespace TheCabin.Core.Interfaces;

/// <summary>
/// Message types for game output
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Normal descriptive text (room descriptions, narrative)
    /// </summary>
    Description,
    
    /// <summary>
    /// Success message (action completed)
    /// </summary>
    Success,
    
    /// <summary>
    /// Failure/error message
    /// </summary>
    Failure,
    
    /// <summary>
    /// System message (save/load, settings)
    /// </summary>
    SystemMessage,
    
    /// <summary>
    /// Discovery or important event
    /// </summary>
    Discovery,
    
    /// <summary>
    /// Player command echo
    /// </summary>
    PlayerCommand
}

/// <summary>
/// Abstraction for displaying game information to the user
/// Allows Console and MAUI to provide their own implementations
/// </summary>
public interface IGameDisplay
{
    /// <summary>
    /// Display a message to the user
    /// </summary>
    Task ShowMessageAsync(string message, MessageType type);
    
    /// <summary>
    /// Display a formatted room description with objects and exits
    /// </summary>
    Task ShowRoomDescriptionAsync(string roomDescription, IEnumerable<string> visibleObjects, IEnumerable<string> exits);
    
    /// <summary>
    /// Display an achievement unlock notification
    /// </summary>
    Task ShowAchievementUnlockedAsync(string achievementTitle, string achievementDescription);
    
    /// <summary>
    /// Prompt the user for text input
    /// </summary>
    Task<string?> PromptAsync(string prompt, string? defaultValue = null);
    
    /// <summary>
    /// Ask the user for confirmation (yes/no)
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message);
}
