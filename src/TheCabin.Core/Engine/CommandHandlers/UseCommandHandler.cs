using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles using items from inventory or in the environment
/// </summary>
public class UseCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly IPuzzleEngine _puzzleEngine;

    public string Verb => "use";

    public UseCommandHandler(
        GameStateMachine stateMachine,
        IInventoryManager inventoryManager,
        IPuzzleEngine puzzleEngine)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
        _puzzleEngine = puzzleEngine ?? throw new ArgumentNullException(nameof(puzzleEngine));
    }

    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(command.Object))
        {
            return Task.FromResult(CommandValidationResult.Invalid("Use what?"));
        }

        // Check if item is in inventory
        var item = _inventoryManager.GetItem(command.Object);
        if (item == null)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You don't have a {command.Object}."));
        }

        // Check if item has a use action
        if (!item.Actions.ContainsKey("use"))
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You can't use the {item.Name} like that."));
        }

        return Task.FromResult(CommandValidationResult.Valid());
    }

    public async Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var item = _inventoryManager.GetItem(command.Object!)!;
        var useAction = item.Actions["use"];

        // Check required flags
        if (!CheckRequiredFlags(useAction, gameState))
        {
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.RequirementsNotMet,
                Message = useAction.FailureMessage
            };
        }

        // Apply state changes
        ApplyStateChanges(useAction.StateChanges, item, gameState);

        // Check for puzzle completion
        var puzzleResult = await _puzzleEngine.CheckPuzzleCompletionAsync(gameState);

        var messages = new List<string> { useAction.SuccessMessage };
        if (puzzleResult.Completed)
        {
            messages.Add(puzzleResult.CompletionMessage);
            gameState.Player.Stats.PuzzlesSolved++;
        }

        // Consume item if needed
        if (useAction.ConsumesItem)
        {
            _inventoryManager.RemoveItem(item.Id);
            messages.Add($"The {item.Name} is used up.");
        }
        else
        {
            item.State.UsageCount++;
        }

        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = string.Join("\n\n", messages)
        };
    }

    private bool CheckRequiredFlags(ActionDefinition action, GameState gameState)
    {
        foreach (var flag in action.RequiredFlags)
        {
            if (!gameState.Progress.StoryFlags.GetValueOrDefault(flag, false))
            {
                return false;
            }
        }
        return true;
    }

    private void ApplyStateChanges(List<StateChange> stateChanges, GameObject item, GameState gameState)
    {
        foreach (var change in stateChanges)
        {
            switch (change.Target)
            {
                case "self":
                    ApplyToObject(item, change);
                    break;
                case "room":
                    ApplyToRoom(_stateMachine.GetCurrentRoom(), change);
                    break;
                default:
                    if (gameState.World.Objects.TryGetValue(change.Target, out var targetObj))
                    {
                        ApplyToObject(targetObj, change);
                    }
                    break;
            }
        }
    }

    private void ApplyToObject(GameObject obj, StateChange change)
    {
        // Try to set as a state flag first
        if (obj.State != null && change.Property.StartsWith("is_", StringComparison.OrdinalIgnoreCase))
        {
            var flagKey = change.Property.ToLower();
            obj.State.Flags[flagKey] = Convert.ToBoolean(change.NewValue);
            return;
        }
        
        // Handle CurrentState property
        if (change.Property == "CurrentState" && obj.State != null)
        {
            obj.State.CurrentState = change.NewValue?.ToString() ?? "default";
            return;
        }
        
        // Try to set as a property
        var property = obj.GetType().GetProperty(change.Property);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, change.NewValue);
        }
    }

    private void ApplyToRoom(Room room, StateChange change)
    {
        var property = room.GetType().GetProperty(change.Property);
        if (property != null && property.CanWrite)
        {
            property.SetValue(room, change.NewValue);
        }
    }
}
