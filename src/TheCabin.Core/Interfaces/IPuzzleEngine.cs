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

    /// <summary>
    /// Initializes puzzles from a story pack
    /// </summary>
    void InitializePuzzles(List<Puzzle> puzzles);

    /// <summary>
    /// Attempts to complete a puzzle step
    /// </summary>
    Task<PuzzleStepResult> AttemptStepAsync(string puzzleId, ParsedCommand command, GameState gameState);

    /// <summary>
    /// Gets the current state of a puzzle
    /// </summary>
    PuzzleState? GetPuzzleState(string puzzleId, GameState gameState);

    /// <summary>
    /// Gets all active puzzles
    /// </summary>
    List<Puzzle> GetActivePuzzles(GameState gameState);

    /// <summary>
    /// Gets available hints for a puzzle
    /// </summary>
    List<Hint> GetAvailableHints(string puzzleId, GameState gameState);

    /// <summary>
    /// Checks if a puzzle step's conditions are met
    /// </summary>
    bool CheckStepConditions(PuzzleStep step, GameState gameState);
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
