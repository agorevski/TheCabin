using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles examining objects in detail
/// </summary>
public class ExamineCommandHandler : ICommandHandler
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly IPuzzleEngine? _puzzleEngine;

    public string Verb => "examine";

    public ExamineCommandHandler(
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
            return Task.FromResult(CommandValidationResult.Invalid("Examine what?"));
        }

        // Check if object is visible in room or in inventory using gameState
        var visibleObject = _stateMachine.FindVisibleObject(command.Object);
        var inventoryItem = gameState.Player.Inventory.Items.FirstOrDefault(i =>
            i.Id.Contains(command.Object, StringComparison.OrdinalIgnoreCase) ||
            i.Name.Contains(command.Object, StringComparison.OrdinalIgnoreCase));

        if (visibleObject == null && inventoryItem == null)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You don't see a '{command.Object}' here."));
        }

        return Task.FromResult(CommandValidationResult.Valid());
    }

    public async Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        // First check visible objects in room
        var targetObject = _stateMachine.FindVisibleObject(command.Object!);

        // If not in room, check inventory using gameState
        if (targetObject == null)
        {
            targetObject = gameState.Player.Inventory.Items.FirstOrDefault(i =>
                i.Id.Contains(command.Object!, StringComparison.OrdinalIgnoreCase) ||
                i.Name.Contains(command.Object!, StringComparison.OrdinalIgnoreCase));
        }

        if (targetObject == null)
        {
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.InvalidCommand,
                Message = $"You don't see a '{command.Object}' here."
            };
        }

        // Check if this action matches any active puzzle steps
        if (_puzzleEngine != null)
        {
            // Log current game state
            var currentRoom = _stateMachine.GetCurrentRoom();
            var inventoryItems = string.Join(", ", gameState.Player.Inventory.Items.Select(i => i.Id));
            var storyFlags = string.Join(", ", gameState.Progress.StoryFlags.Where(f => f.Value).Select(f => f.Key));

            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] === Current State ===");
            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Current Room: {currentRoom.Id}");
            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Inventory: [{inventoryItems}]");
            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Story Flags: [{storyFlags}]");
            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Target Object: {targetObject.Id} ({targetObject.Name})");

            var activePuzzles = _puzzleEngine.GetActivePuzzles(gameState);
            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Checking {activePuzzles.Count} active puzzles for command: examine {command.Object}");

            foreach (var puzzle in activePuzzles)
            {
                System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Attempting puzzle '{puzzle.Id}' with {puzzle.Steps.Count} steps");
                var puzzleResult = await _puzzleEngine.AttemptStepAsync(puzzle.Id, command, gameState);
                System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Puzzle '{puzzle.Id}' attempt result: Success={puzzleResult.Success}, Message='{puzzleResult.Message}'");

                if (puzzleResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] ✓ Puzzle step matched! Using puzzle messages.");

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

                    System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] Returning puzzle result with {puzzleMessages.Count} messages");
                    return new CommandResult
                    {
                        Success = true,
                        Type = CommandResultType.Success,
                        Message = string.Join("\n\n", puzzleMessages)
                    };
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] No puzzle steps matched, using standard examine behavior");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ExamineCommandHandler] PuzzleEngine is null, using standard examine behavior");
        }

        // No puzzle step matched, use standard examine behavior
        // Get examine message from actions or use description
        string message;
        if (targetObject.Actions.TryGetValue("examine", out var examineAction))
        {
            message = examineAction.SuccessMessage;
        }
        else
        {
            message = targetObject.Description;
        }

        // Add details about the object's state if applicable
        var details = new List<string> { message };

        if (targetObject.State != null && targetObject.State.CurrentState != "default")
        {
            details.Add($"It appears to be {targetObject.State.CurrentState}.");
        }

        if (targetObject.Type == ObjectType.Container && targetObject.State?.Flags.ContainsKey("is_open") == true)
        {
            var isOpen = (bool)targetObject.State.Flags["is_open"];
            if (isOpen)
            {
                details.Add("It is open.");
            }
            else
            {
                details.Add("It is closed.");
            }
        }

        if (targetObject.Type == ObjectType.Light && targetObject.State?.CurrentState == "lit")
        {
            details.Add("It's currently lit, casting a warm glow.");
        }

        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = string.Join(" ", details)
        };
    }
}
