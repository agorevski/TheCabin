using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine;

/// <summary>
/// Routes parsed commands to appropriate command handlers
/// </summary>
public class CommandRouter
{
    private readonly Dictionary<string, ICommandHandler> _handlers;
    private readonly GameStateMachine _stateMachine;
    
    public CommandRouter(
        IEnumerable<ICommandHandler> handlers,
        GameStateMachine stateMachine)
    {
        _handlers = handlers?.ToDictionary(h => h.Verb.ToLowerInvariant(), h => h)
            ?? throw new ArgumentNullException(nameof(handlers));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
    }
    
    /// <summary>
    /// Routes and executes a parsed command
    /// </summary>
    public async Task<CommandResult> RouteAsync(ParsedCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));
        
        var verb = command.Verb.ToLowerInvariant();
        
        // Find appropriate handler
        if (!_handlers.TryGetValue(verb, out var handler))
        {
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.InvalidCommand,
                Message = $"I don't understand '{command.Verb}'. Try 'help' for available commands."
            };
        }
        
        // Validate command
        var validation = await handler.ValidateAsync(command, _stateMachine.CurrentState);
        if (!validation.IsValid)
        {
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.RequirementsNotMet,
                Message = validation.Message
            };
        }
        
        // Execute command
        try
        {
            var result = await handler.ExecuteAsync(command, _stateMachine.CurrentState);
            
            // Update game stats
            _stateMachine.CurrentState.Player.Stats.CommandsExecuted++;
            _stateMachine.CurrentState.World.TurnNumber++;
            
            // Add narrative entry
            _stateMachine.AddNarrativeEntry(
                $"> {command.RawInput}",
                NarrativeType.PlayerCommand
            );
            
            _stateMachine.AddNarrativeEntry(
                result.Message,
                result.Success ? NarrativeType.Success : NarrativeType.Failure,
                result.Type == CommandResultType.Success
            );
            
            return result;
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.Failure,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Gets a list of all available verbs
    /// </summary>
    public List<string> GetAvailableVerbs()
    {
        return _handlers.Keys.ToList();
    }
    
    /// <summary>
    /// Gets help text for a specific verb
    /// </summary>
    public string GetVerbHelp(string verb)
    {
        if (_handlers.TryGetValue(verb.ToLowerInvariant(), out var handler))
        {
            return $"{handler.Verb}: Command handler available";
        }
        
        return $"Unknown command: {verb}";
    }
}
