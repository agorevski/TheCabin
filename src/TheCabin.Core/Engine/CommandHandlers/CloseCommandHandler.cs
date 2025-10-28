using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles closing doors, containers, and other closable objects
/// </summary>
public class CloseCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;
    private readonly IPuzzleEngine? _puzzleEngine;

    public string Verb => "close";

    public CloseCommandHandler(GameStateMachine stateMachine, IPuzzleEngine? puzzleEngine = null)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _puzzleEngine = puzzleEngine;
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

    public async Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var targetObject = _stateMachine.FindVisibleObject(command.Object!)!;
        
        // Check if this action matches any active puzzle steps
        if (_puzzleEngine != null)
        {
            // Log current game state
            var currentRoom = _stateMachine.GetCurrentRoom();
            var inventoryItems = string.Join(", ", gameState.Player.Inventory.Items.Select(i => i.Id));
            var storyFlags = string.Join(", ", gameState.Progress.StoryFlags.Where(f => f.Value).Select(f => f.Key));
            
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] === Current State ===");
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Current Room: {currentRoom.Id}");
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Inventory: [{inventoryItems}]");
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Story Flags: [{storyFlags}]");
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Target Object: {targetObject.Id} ({targetObject.Name})");
            
            var activePuzzles = _puzzleEngine.GetActivePuzzles(gameState);
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Checking {activePuzzles.Count} active puzzles for command: close {command.Object}");
            
            foreach (var puzzle in activePuzzles)
            {
                System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Attempting puzzle '{puzzle.Id}' with {puzzle.Steps.Count} steps");
                var puzzleResult = await _puzzleEngine.AttemptStepAsync(puzzle.Id, command, gameState);
                System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Puzzle '{puzzle.Id}' attempt result: Success={puzzleResult.Success}, Message='{puzzleResult.Message}'");
                
                if (puzzleResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] ✓ Puzzle step matched! Using puzzle messages.");
                    
                    // Check if already closed
                    if (targetObject.State?.Flags.ContainsKey("is_open") == true)
                    {
                        var isOpen = (bool)targetObject.State.Flags["is_open"];
                        if (isOpen)
                        {
                            // Close the object
                            targetObject.State.Flags["is_open"] = false;
                        }
                    }
                    
                    // Set completion flag if specified
                    if (puzzleResult.AttemptedStep != null && 
                        !string.IsNullOrEmpty(puzzleResult.AttemptedStep.CompletionFlag))
                    {
                        _stateMachine.SetStoryFlag(puzzleResult.AttemptedStep.CompletionFlag, true);
                    }
                    
                    // Build result message
                    var puzzleMessages = new List<string> { puzzleResult.Message };
                    
                    if (puzzleResult.PuzzleCompleted && !string.IsNullOrEmpty(puzzle.CompletionMessage))
                    {
                        puzzleMessages.Add(puzzle.CompletionMessage);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] Returning puzzle result with {puzzleMessages.Count} messages");
                    return new CommandResult
                    {
                        Success = true,
                        Type = CommandResultType.Success,
                        Message = string.Join("\n\n", puzzleMessages)
                    };
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] No puzzle steps matched, using standard close behavior");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[CloseCommandHandler] PuzzleEngine is null, using standard close behavior");
        }
        
        // No puzzle step matched, use standard close behavior
        var closeAction = targetObject.Actions["close"];

        // Check if already closed
        if (targetObject.State?.Flags.ContainsKey("is_open") == true)
        {
            var isOpen = (bool)targetObject.State.Flags["is_open"];
            if (!isOpen)
            {
                return new CommandResult
                {
                    Success = false,
                    Type = CommandResultType.Failure,
                    Message = $"The {targetObject.Name} is already closed."
                };
            }
        }
        else
        {
            // If no flag, assume it's already closed
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.Failure,
                Message = $"The {targetObject.Name} is already closed."
            };
        }

        // Close the object
        targetObject.State.Flags["is_open"] = false;

        // Apply any state changes defined in the action
        foreach (var stateChange in closeAction.StateChanges)
        {
            ApplyStateChange(stateChange, targetObject, gameState);
        }

        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = closeAction.SuccessMessage
        };
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
