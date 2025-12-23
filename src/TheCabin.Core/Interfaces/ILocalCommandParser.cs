using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Local rule-based command parser (fallback when LLM is unavailable)
/// </summary>
public interface ILocalCommandParser
{
    /// <summary>
    /// Parses a command using local rules
    /// </summary>
    Task<ParsedCommand> ParseAsync(string input, GameContext context);

    /// <summary>
    /// Checks if this parser can handle the input
    /// </summary>
    bool CanHandle(string input);
}
