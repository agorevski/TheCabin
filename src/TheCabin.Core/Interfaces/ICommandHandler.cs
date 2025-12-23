using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Handler for executing a specific type of command
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// The verb this handler responds to (e.g., "take", "go", "use")
    /// </summary>
    string Verb { get; }

    /// <summary>
    /// Validates whether the command can be executed
    /// </summary>
    /// <param name="command">Parsed command to validate</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Validation result</returns>
    Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState);

    /// <summary>
    /// Executes the command
    /// </summary>
    /// <param name="command">Parsed command to execute</param>
    /// <param name="gameState">Current game state (will be modified)</param>
    /// <returns>Result of the command execution</returns>
    Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState);
}

/// <summary>
/// Result of command validation
/// </summary>
public class CommandValidationResult
{
    /// <summary>
    /// Whether the command is valid and can be executed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Message explaining why validation failed (if applicable)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static CommandValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with a message
    /// </summary>
    public static CommandValidationResult Invalid(string message) =>
        new() { IsValid = false, Message = message };
}
