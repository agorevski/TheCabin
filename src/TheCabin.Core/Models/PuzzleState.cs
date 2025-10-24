namespace TheCabin.Core.Models;

/// <summary>
/// Tracks the progress state of a puzzle for a player
/// </summary>
public class PuzzleState
{
    public string PuzzleId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
    public int HintsUsed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Attempts { get; set; }
    
    public PuzzleState()
    {
        StartedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Marks a step as completed
    /// </summary>
    public void CompleteStep(string stepId)
    {
        if (!CompletedSteps.Contains(stepId))
        {
            CompletedSteps.Add(stepId);
        }
    }
    
    /// <summary>
    /// Marks the puzzle as fully completed
    /// </summary>
    public void Complete()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Records a hint being used
    /// </summary>
    public void UseHint()
    {
        HintsUsed++;
    }
    
    /// <summary>
    /// Records an attempt at solving the puzzle
    /// </summary>
    public void RecordAttempt()
    {
        Attempts++;
    }
}
