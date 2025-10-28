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
        
        // Get visible objects and exits
        var visibleObjects = _stateMachine.GetVisibleObjects().Select(o => o.Name);
        var exits = currentRoom.Exits.Keys;
        
        // Format with separate display and TTS messages
        var (displayMessage, ttsMessage) = RoomDescriptionFormatter.FormatRoomDescription(
            currentRoom.Description,
            visibleObjects,
            exits);
        
        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = displayMessage,
            TtsMessage = ttsMessage
        });
    }
}
