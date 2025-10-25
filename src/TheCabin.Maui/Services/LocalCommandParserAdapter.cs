using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Maui.Services;

/// <summary>
/// Adapter to wrap ILocalCommandParser and expose it as ICommandParserService
/// </summary>
public class LocalCommandParserAdapter : ICommandParserService
{
    private readonly ILocalCommandParser _localParser;

    public LocalCommandParserAdapter(ILocalCommandParser localParser)
    {
        _localParser = localParser ?? throw new ArgumentNullException(nameof(localParser));
    }

    public Task<ParsedCommand> ParseAsync(string input, GameContext context, CancellationToken cancellationToken = default)
    {
        // Adapt the ILocalCommandParser interface (which doesn't take CancellationToken)
        // to the ICommandParserService interface (which does)
        return _localParser.ParseAsync(input, context);
    }
}
