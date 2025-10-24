using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles closing doors, containers, and other closable objects
/// </summary>
public class CloseCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;

    public string Verb => "close";

    public CloseCommandHandler(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
    }

    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(command.Object))
        {
            return Task.FromResult(CommandValidationResult.Invalid("Close what?"));
        }

        // Find the object
        var targetObject = _stateMachine.FindVisibleObject(command.Object);

        if (targetObject == null)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You don't see a '{command.Object}' here."));
        }

        // Check if object can be closed
        if (targetObject.Type != ObjectType.Door && 
            targetObject.Type != ObjectType.Container)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You can't close the {targetObject.Name}."));
        }

        // Check if object has a close action
        if (!targetObject.Actions.ContainsKey("close"))
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"The {targetObject.Name} can't be closed."));
        }

        return Task.FromResult(CommandValidationResult.Valid());
    }

    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var targetObject = _stateMachine.FindVisibleObject(command.Object!)!;
        var closeAction = targetObject.Actions["close"];

        // Check if already closed
        if (targetObject.State?.Flags.ContainsKey("is_open") == true)
        {
            var isOpen = (bool)targetObject.State.Flags["is_open"];
            if (!isOpen)
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Type = CommandResultType.Failure,
                    Message = $"The {targetObject.Name} is already closed."
                });
            }
        }
        else
        {
            // If no flag, assume it's already closed
            return Task.FromResult(new CommandResult
            {
                Success = false,
                Type = CommandResultType.Failure,
                Message = $"The {targetObject.Name} is already closed."
            });
        }

        // Close the object
        targetObject.State.Flags["is_open"] = false;

        // Apply any state changes defined in the action
        foreach (var stateChange in closeAction.StateChanges)
        {
            ApplyStateChange(stateChange, targetObject, gameState);
        }

        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = closeAction.SuccessMessage
        });
    }

    private void ApplyStateChange(StateChange change, GameObject targetObject, GameState gameState)
    {
        switch (change.Target)
        {
            case "self":
                var property = targetObject.GetType().GetProperty(change.Property);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(targetObject, change.NewValue);
                }
                break;
            case "room":
                var room = _stateMachine.GetCurrentRoom();
                var roomProperty = room.GetType().GetProperty(change.Property);
                if (roomProperty != null && roomProperty.CanWrite)
                {
                    roomProperty.SetValue(room, change.NewValue);
                }
                break;
            default:
                if (gameState.World.Objects.TryGetValue(change.Target, out var obj))
                {
                    var objProperty = obj.GetType().GetProperty(change.Property);
                    if (objProperty != null && objProperty.CanWrite)
                    {
                        objProperty.SetValue(obj, change.NewValue);
                    }
                }
                break;
        }
    }
}
