using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles movement commands (go, move, walk)
/// </summary>
public class MoveCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;
    
    public string Verb => "go";
    
    public MoveCommandHandler(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
    }
    
    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(command.Object))
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                "Go where? Please specify a direction (north, south, east, west, up, down)."));
        }
        
        var direction = NormalizeDirection(command.Object);
        var currentRoom = _stateMachine.GetCurrentRoom();
        
        // Check if exit exists in that direction
        if (!currentRoom.Exits.TryGetValue(direction, out var targetRoomId))
        {
            var availableExits = string.Join(", ", currentRoom.Exits.Keys);
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You can't go {direction} from here. Available exits: {availableExits}"));
        }
        
        // Check if player can transition (handles locked rooms, etc.)
        if (!_stateMachine.CanTransitionTo(targetRoomId))
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                "The way is blocked or locked."));
        }
        
        return Task.FromResult(CommandValidationResult.Valid());
    }
    
    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var direction = NormalizeDirection(command.Object!);
        var currentRoom = _stateMachine.GetCurrentRoom();
        var targetRoomId = currentRoom.Exits[direction];
        
        // Perform the transition
        _stateMachine.TransitionTo(targetRoomId);
        
        var newRoom = _stateMachine.GetCurrentRoom();
        var message = $"You move {direction}.\n\n{newRoom.Description}";
        
        // Add visible objects description if any
        var visibleObjects = _stateMachine.GetVisibleObjects();
        if (visibleObjects.Any())
        {
            var objectNames = string.Join(", ", visibleObjects.Select(o => o.Name));
            message += $"\n\nYou can see: {objectNames}";
        }
        
        // Add available exits
        if (newRoom.Exits.Any())
        {
            var exits = string.Join(", ", newRoom.Exits.Keys);
            message += $"\n\nExits: {exits}";
        }
        
        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = message,
            StateChange = new GameStateChange
            {
                LocationChanged = targetRoomId
            }
        });
    }
    
    private string NormalizeDirection(string input)
    {
        return input.ToLowerInvariant() switch
        {
            "n" => "north",
            "s" => "south",
            "e" => "east",
            "w" => "west",
            "u" or "upstairs" => "up",
            "d" or "downstairs" => "down",
            "ne" or "northeast" => "northeast",
            "nw" or "northwest" => "northwest",
            "se" or "southeast" => "southeast",
            "sw" or "southwest" => "southwest",
            _ => input.ToLowerInvariant()
        };
    }
}
