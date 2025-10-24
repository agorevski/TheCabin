namespace TheCabin.Core.Models;

/// <summary>
/// Represents a single step in a multi-step puzzle
/// </summary>
public class PuzzleStep
{
    /// <summary>
    /// Unique identifier for this step
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Step number/order in sequence
    /// </summary>
    public int StepNumber { get; set; }
    
    /// <summary>
    /// Description of what needs to be done
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The action verb required (e.g., "use", "take", "examine")
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// The target object for the action
    /// </summary>
    public string TargetObject { get; set; } = string.Empty;
    
    /// <summary>
    /// Required story flags to attempt this step
    /// </summary>
    public List<string> RequiredFlags { get; set; } = new();
    
    /// <summary>
    /// Required items in inventory to attempt this step
    /// </summary>
    public List<string> RequiredItems { get; set; } = new();
    
    /// <summary>
    /// Required location to attempt this step
    /// </summary>
    public string RequiredLocation { get; set; } = string.Empty;
    
    /// <summary>
    /// Flag to set when step is completed
    /// </summary>
    public string CompletionFlag { get; set; } = string.Empty;
    
    /// <summary>
    /// Message shown on successful completion
    /// </summary>
    public string SuccessMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Message shown on failure
    /// </summary>
    public string FailureMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// General feedback message
    /// </summary>
    public string FeedbackMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Checks if this step can be attempted based on current game state
    /// </summary>
    public bool CanAttempt(Dictionary<string, bool> flags, List<string> inventoryItems)
    {
        // Check all required flags are set
        foreach (var flag in RequiredFlags)
        {
            if (!flags.GetValueOrDefault(flag, false))
            {
                return false;
            }
        }
        
        // Check all required items are in inventory
        foreach (var item in RequiredItems)
        {
            if (!inventoryItems.Contains(item))
            {
                return false;
            }
        }
        
        return true;
    }
}
