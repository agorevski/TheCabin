using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles opening doors, containers, and other openable objects
/// </summary>
public class OpenCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;

    public string Verb => "open";

    public OpenCommandHandler(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
    }

    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(command.Object))
        {
            return Task.FromResult(CommandValidationResult.Invalid("Open what?"));
        }

        // Find the object
        var targetObject = _stateMachine.FindVisibleObject(command.Object);

        if (targetObject == null)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You don't see a '{command.Object}' here."));
        }

        // Check if object can be opened
        if (targetObject.Type != ObjectType.Door && 
            targetObject.Type != ObjectType.Container)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You can't open the {targetObject.Name}."));
        }

        // Check if object has an open action
        if (!targetObject.Actions.ContainsKey("open"))
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"The {targetObject.Name} can't be opened."));
        }

        return Task.FromResult(CommandValidationResult.Valid());
    }

    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var targetObject = _stateMachine.FindVisibleObject(command.Object!)!;
        var openAction = targetObject.Actions["open"];

        // Check if already open
        if (targetObject.State?.Flags.ContainsKey("is_open") == true)
        {
            var isOpen = (bool)targetObject.State.Flags["is_open"];
            if (isOpen)
            {
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Type = CommandResultType.Failure,
                    Message = $"The {targetObject.Name} is already open."
                });
            }
        }

        // Check if locked
        if (targetObject.State?.Flags.ContainsKey("is_locked") == true)
        {
            var isLocked = (bool)targetObject.State.Flags["is_locked"];
            if (isLocked)
            {
                var message = !string.IsNullOrWhiteSpace(openAction.FailureMessage) 
                    ? openAction.FailureMessage 
                    : $"The {targetObject.Name} is locked.";
                    
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Type = CommandResultType.RequirementsNotMet,
                    Message = message
                });
            }
        }

        // Open the object
        if (targetObject.State == null)
        {
            targetObject.State = new ObjectState();
        }

        targetObject.State.Flags["is_open"] = true;

        // Apply any state changes defined in the action
        foreach (var stateChange in openAction.StateChanges)
        {
            ApplyStateChange(stateChange, targetObject, gameState);
        }

        // Reveal any hidden objects
        if (targetObject.Type == ObjectType.Container)
        {
            RevealContainedObjects(targetObject, gameState);
        }

        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = openAction.SuccessMessage
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

    private void RevealContainedObjects(GameObject container, GameState gameState)
    {
        // Check if container has any objects inside (defined in RequiredItems or similar)
        if (container.RequiredItems != null && container.RequiredItems.Any())
        {
            var currentRoom = _stateMachine.GetCurrentRoom();
            
            foreach (var itemId in container.RequiredItems)
            {
                if (gameState.World.Objects.TryGetValue(itemId, out var item))
                {
                    item.IsVisible = true;
                    if (!currentRoom.State.VisibleObjectIds.Contains(itemId))
                    {
                        currentRoom.State.VisibleObjectIds.Add(itemId);
                    }
                }
            }
        }
    }
}
