namespace TheCabin.Core.Models;

/// <summary>
/// Represents a multi-step puzzle in the game
/// </summary>
public class Puzzle
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PuzzleType Type { get; set; }
    public List<PuzzleStep> Steps { get; set; } = new();
    public List<string> RequiredItems { get; set; } = new();
    public string CompletionMessage { get; set; } = string.Empty;
    public string? CompletionAchievementId { get; set; }
    public string? AchievementId { get; set; }
    public List<Hint> Hints { get; set; } = new();

    /// <summary>
    /// Gets the current step based on completed step IDs
    /// </summary>
    public PuzzleStep? GetCurrentStep(Dictionary<string, bool> flags, List<string> completedStepIds)
    {
        if (Type == PuzzleType.Sequential)
        {
            // Sequential: return first incomplete step
            foreach (var step in Steps)
            {
                if (!completedStepIds.Contains(step.Id))
                {
                    return step;
                }
            }
            return null; // All steps completed
        }
        else if (Type == PuzzleType.Combinatorial)
        {
            // Combinatorial: return any incomplete step
            foreach (var step in Steps)
            {
                if (!completedStepIds.Contains(step.Id))
                {
                    return step;
                }
            }
            return null; // All steps completed
        }

        // For other types, return first incomplete step
        return Steps.FirstOrDefault(s => !completedStepIds.Contains(s.Id));
    }

    /// <summary>
    /// Checks if all required flags are set for this puzzle
    /// </summary>
    public bool AreRequiredFlagsMet(Dictionary<string, bool> flags)
    {
        foreach (var step in Steps)
        {
            foreach (var requiredFlag in step.RequiredFlags)
            {
                if (!flags.GetValueOrDefault(requiredFlag, false))
                {
                    return false;
                }
            }
        }
        return true;
    }
}
