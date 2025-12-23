namespace TheCabin.Core.Models;

/// <summary>
/// Result of attempting a puzzle step
/// </summary>
public class PuzzleStepResult
{
    /// <summary>
    /// Whether the step was completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message to display to the player
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The step that was attempted
    /// </summary>
    public PuzzleStep? AttemptedStep { get; set; }

    /// <summary>
    /// Whether this completed the entire puzzle
    /// </summary>
    public bool PuzzleCompleted { get; set; }

    /// <summary>
    /// ID of achievement unlocked (if any)
    /// </summary>
    public string? AchievementUnlocked { get; set; }
}
