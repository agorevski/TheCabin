namespace TheCabin.Core.Models;

/// <summary>
/// Represents a hint for a puzzle with progressive difficulty levels
/// </summary>
public class Hint
{
    /// <summary>
    /// Hint difficulty level (1 = subtle, 2 = moderate, 3 = obvious)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// The hint text to display to the player
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional cost for receiving this hint (e.g., score penalty, item cost)
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// Display order for hints
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Delay in minutes before this hint becomes available
    /// </summary>
    public int DelayMinutes { get; set; }
}
