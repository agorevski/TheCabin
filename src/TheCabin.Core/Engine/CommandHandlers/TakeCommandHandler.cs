using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles taking/picking up items
/// </summary>
public class TakeCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly IPuzzleEngine? _puzzleEngine;
    
    public string Verb => "take";
    
    public TakeCommandHandler(
        GameStateMachine stateMachine,
        IInventoryManager inventoryManager,
        IPuzzleEngine? puzzleEngine = null)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
        _puzzleEngine = puzzleEngine;
    }
    
    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(command.Object))
        {
            return Task.FromResult(CommandValidationResult.Invalid("Take what?"));
        }
        
        // Find the object in visible objects
        var targetObject = _stateMachine.FindVisibleObject(command.Object);
        
        if (targetObject == null)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You don't see a '{command.Object}' here."));
        }
        
        if (!targetObject.IsCollectable)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You can't take the {targetObject.Name}."));
        }
        
        if (!_inventoryManager.CanAdd(targetObject))
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                "You're carrying too much. Drop something first."));
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
            
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] === Current State ===");
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Current Room: {currentRoom.Id}");
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Inventory: [{inventoryItems}]");
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Story Flags: [{storyFlags}]");
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Target Object: {targetObject.Id} ({targetObject.Name})");
            
            var activePuzzles = _puzzleEngine.GetActivePuzzles(gameState);
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Checking {activePuzzles.Count} active puzzles for command: take {command.Object}");
            
            foreach (var puzzle in activePuzzles)
            {
                System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Attempting puzzle '{puzzle.Id}' with {puzzle.Steps.Count} steps");
                var puzzleResult = await _puzzleEngine.AttemptStepAsync(puzzle.Id, command, gameState);
                System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Puzzle '{puzzle.Id}' attempt result: Success={puzzleResult.Success}, Message='{puzzleResult.Message}'");
                
                if (puzzleResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] ✓ Puzzle step matched! Using puzzle messages.");
                    // Puzzle step matched! Use puzzle messages and complete the action
                    
                    // Add to inventory using gameState
                    gameState.Player.Inventory.Items.Add(targetObject);
                    
                    // Remove from room's visible objects
                    var room = _stateMachine.GetCurrentRoom();
                    room.State.VisibleObjectIds.Remove(targetObject.Id);
                    targetObject.IsVisible = false;
                    
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
                    
                    System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] Returning puzzle result with {puzzleMessages.Count} messages");
                    return new CommandResult
                    {
                        Success = true,
                        Type = CommandResultType.Success,
                        Message = string.Join("\n\n", puzzleMessages),
                        StateChange = new GameStateChange
                        {
                            ItemsAdded = new List<string> { targetObject.Id }
                        }
                    };
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] No puzzle steps matched, using standard take behavior");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[TakeCommandHandler] PuzzleEngine is null, using standard take behavior");
        }
        
        // No puzzle step matched, use standard take behavior
        var successMessage = targetObject.Actions.TryGetValue("take", out var takeAction)
            ? takeAction.SuccessMessage
            : $"You pick up the {targetObject.Name}.";
        
        // Add to inventory using gameState
        gameState.Player.Inventory.Items.Add(targetObject);
        
        // Remove from room's visible objects
        var roomForCleanup = _stateMachine.GetCurrentRoom();
        roomForCleanup.State.VisibleObjectIds.Remove(targetObject.Id);
        targetObject.IsVisible = false;
        
        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = successMessage,
            StateChange = new GameStateChange
            {
                ItemsAdded = new List<string> { targetObject.Id }
            }
        };
    }
}
