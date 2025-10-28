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

        // Check if item is in inventory using gameState
        var inventoryItem = gameState.Player.Inventory.Items.FirstOrDefault(i =>
            i.Id.Contains(command.Object, StringComparison.OrdinalIgnoreCase) ||
            i.Name.Contains(command.Object, StringComparison.OrdinalIgnoreCase));
        
        // If not in inventory, check if it's a usable object in the current room
        GameObject? item = inventoryItem;
        if (item == null)
        {
            item = _stateMachine.FindVisibleObject(command.Object);
            if (item == null)
            {
                return Task.FromResult(CommandValidationResult.Invalid(
                    $"You don't see a {command.Object} here."));
            }
            
            // Must have a "use" action if not in inventory
            if (!item.Actions.ContainsKey("use"))
            {
                return Task.FromResult(CommandValidationResult.Invalid(
                    $"You can't use the {item.Name}."));
            }
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
        // Get item from gameState inventory first
        var item = gameState.Player.Inventory.Items.FirstOrDefault(i =>
            i.Id.Contains(command.Object!, StringComparison.OrdinalIgnoreCase) ||
            i.Name.Contains(command.Object!, StringComparison.OrdinalIgnoreCase));
        
        // If not in inventory, check if it's in the room
        bool isInInventory = item != null;
        if (item == null)
        {
            item = _stateMachine.FindVisibleObject(command.Object!);
        }
        
        if (item == null)
        {
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.InvalidCommand,
                Message = $"You don't see a {command.Object} here."
            };
        }
        
        // Log current game state
        var currentRoom = _stateMachine.GetCurrentRoom();
        var inventoryItems = string.Join(", ", gameState.Player.Inventory.Items.Select(i => i.Id));
        var storyFlags = string.Join(", ", gameState.Progress.StoryFlags.Where(f => f.Value).Select(f => f.Key));
        
        System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] === Current State ===");
        System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Current Room: {currentRoom.Id}");
        System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Inventory: [{inventoryItems}]");
        System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Story Flags: [{storyFlags}]");
        System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Target Item: {item.Id} ({item.Name})");
        
        // Check if this action matches any active puzzle steps
        var activePuzzles = _puzzleEngine.GetActivePuzzles(gameState);
        System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Checking {activePuzzles.Count} active puzzles for command: use {command.Object}");
        
        foreach (var puzzle in activePuzzles)
        {
            System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Attempting puzzle '{puzzle.Id}' with {puzzle.Steps.Count} steps");
            var puzzleStepResult = await _puzzleEngine.AttemptStepAsync(puzzle.Id, command, gameState);
            System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Puzzle '{puzzle.Id}' attempt result: Success={puzzleStepResult.Success}, Message='{puzzleStepResult.Message}'");
            
            if (puzzleStepResult.Success)
            {
                System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] ✓ Puzzle step matched! Using puzzle messages.");
                // Puzzle step matched! Use puzzle messages
                
                // Set completion flag if specified
                if (puzzleStepResult.AttemptedStep != null && 
                    !string.IsNullOrEmpty(puzzleStepResult.AttemptedStep.CompletionFlag))
                {
                    _stateMachine.SetStoryFlag(puzzleStepResult.AttemptedStep.CompletionFlag, true);
                }
                
                // Build result message
                var puzzleMessages = new List<string> { puzzleStepResult.Message };
                
                if (puzzleStepResult.PuzzleCompleted && !string.IsNullOrEmpty(puzzle.CompletionMessage))
                {
                    puzzleMessages.Add(puzzle.CompletionMessage);
                }
                
                // Handle item consumption if action exists (only for inventory items)
                if (item.Actions.TryGetValue("use", out var itemUseAction))
                {
                    if (itemUseAction.ConsumesItem && isInInventory)
                    {
                        gameState.Player.Inventory.Items.Remove(item);
                        puzzleMessages.Add($"The {item.Name} is used up.");
                    }
                    else
                    {
                        item.State.UsageCount++;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] Returning puzzle result with {puzzleMessages.Count} messages");
                return new CommandResult
                {
                    Success = true,
                    Type = CommandResultType.Success,
                    Message = string.Join("\n\n", puzzleMessages)
                };
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[UseCommandHandler] No puzzle steps matched, using standard use behavior");
        
        // No puzzle step matched, use standard use behavior
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
        if (puzzleResult.Completed && !string.IsNullOrEmpty(puzzleResult.CompletionMessage))
        {
            messages.Add(puzzleResult.CompletionMessage);
            gameState.Player.Stats.PuzzlesSolved++;
        }

        // Consume item if needed (only for inventory items)
        if (useAction.ConsumesItem && isInInventory)
        {
            gameState.Player.Inventory.Items.Remove(item);
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
