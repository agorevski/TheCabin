using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles looking around or at specific objects
/// </summary>
public class LookCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;
    
    public string Verb => "look";
    
    public LookCommandHandler(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
    }
    
    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        // Look is always valid
        return Task.FromResult(CommandValidationResult.Valid());
    }
    
    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var currentRoom = _stateMachine.GetCurrentRoom();
        var message = currentRoom.Description;
        
        // Add visible objects
        var visibleObjects = _stateMachine.GetVisibleObjects();
        if (visibleObjects.Any())
        {
            var objectNames = string.Join(", ", visibleObjects.Select(o => o.Name));
            message += $"\n\nYou can see: {objectNames}";
        }
        
        // Add available exits
        if (currentRoom.Exits.Any())
        {
            var exits = string.Join(", ", currentRoom.Exits.Keys);
            message += $"\n\nExits: {exits}";
        }
        
        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = message
        });
    }
}
