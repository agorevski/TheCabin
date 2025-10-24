using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Service for parsing natural language commands into structured actions
/// </summary>
public interface ICommandParserService
{
    /// <summary>
    /// Parses player input text into a structured command
    /// </summary>
    /// <param name="input">Raw text input from player</param>
    /// <param name="context">Current game context for parsing hints</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Parsed command with verb, objects, and confidence</returns>
    Task<ParsedCommand> ParseAsync(string input, GameContext context, CancellationToken cancellationToken = default);
}
