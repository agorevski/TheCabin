using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Manages puzzle logic and completion checking
/// </summary>
public interface IPuzzleEngine
{
    /// <summary>
    /// Checks if any puzzle conditions have been met
    /// </summary>
    Task<PuzzleResult> CheckPuzzleCompletionAsync(GameState gameState);
    
    /// <summary>
    /// Registers a custom puzzle checker
    /// </summary>
    void RegisterPuzzle(string puzzleId, Func<GameState, bool> checker);
}

/// <summary>
/// Result of a puzzle check
/// </summary>
public class PuzzleResult
{
    /// <summary>
    /// Whether a puzzle was completed
    /// </summary>
    public bool Completed { get; set; }
    
    /// <summary>
    /// ID of the completed puzzle
    /// </summary>
    public string? PuzzleId { get; set; }
    
    /// <summary>
    /// Message to display on completion
    /// </summary>
    public string? CompletionMessage { get; set; }
    
    /// <summary>
    /// Reward or consequence of completion
    /// </summary>
    public string? Reward { get; set; }
}
