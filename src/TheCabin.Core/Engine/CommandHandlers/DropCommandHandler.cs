using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles dropping items from inventory
/// </summary>
public class DropCommandHandler : ICommandHandler
{
    private readonly IInventoryManager _inventoryManager;
    private readonly GameStateMachine _stateMachine;
    private readonly IPuzzleEngine? _puzzleEngine;

    public string Verb => "drop";

    public DropCommandHandler(
        IInventoryManager inventoryManager,
        GameStateMachine stateMachine,
        IPuzzleEngine? puzzleEngine = null)
    {
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _puzzleEngine = puzzleEngine;
    }

    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(command.Object))
        {
            return Task.FromResult(CommandValidationResult.Invalid("Drop what?"));
        }

        // Check if player has the item
        var item = gameState.Player.Inventory.Items
            .FirstOrDefault(i => i.Id.Contains(command.Object, StringComparison.OrdinalIgnoreCase) ||
                               i.Name.Contains(command.Object, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            return Task.FromResult(CommandValidationResult.Invalid($"You don't have a {command.Object}."));
        }

        return Task.FromResult(CommandValidationResult.Valid());
    }

    public async Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var item = gameState.Player.Inventory.Items
            .First(i => i.Id.Contains(command.Object!, StringComparison.OrdinalIgnoreCase) ||
                       i.Name.Contains(command.Object!, StringComparison.OrdinalIgnoreCase));

        // Check if this action matches any active puzzle steps
        if (_puzzleEngine != null)
        {
            // Log current game state
            var currentRoom = _stateMachine.GetCurrentRoom();
            var inventoryItems = string.Join(", ", gameState.Player.Inventory.Items.Select(i => i.Id));
            var storyFlags = string.Join(", ", gameState.Progress.StoryFlags.Where(f => f.Value).Select(f => f.Key));

            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] === Current State ===");
            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Current Room: {currentRoom.Id}");
            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Inventory: [{inventoryItems}]");
            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Story Flags: [{storyFlags}]");
            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Target Item: {item.Id} ({item.Name})");

            var activePuzzles = _puzzleEngine.GetActivePuzzles(gameState);
            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Checking {activePuzzles.Count} active puzzles for command: drop {command.Object}");

            foreach (var puzzle in activePuzzles)
            {
                System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Attempting puzzle '{puzzle.Id}' with {puzzle.Steps.Count} steps");
                var puzzleResult = await _puzzleEngine.AttemptStepAsync(puzzle.Id, command, gameState);
                System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Puzzle '{puzzle.Id}' attempt result: Success={puzzleResult.Success}, Message='{puzzleResult.Message}'");

                if (puzzleResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] ✓ Puzzle step matched! Using puzzle messages.");

                    // Remove from inventory using gameState
                    gameState.Player.Inventory.Items.Remove(item);

                    // Add to current room
                    if (!currentRoom.ObjectIds.Contains(item.Id))
                    {
                        currentRoom.ObjectIds.Add(item.Id);
                    }
                    currentRoom.State.VisibleObjectIds.Add(item.Id);
                    item.IsVisible = true;

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

                    System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] Returning puzzle result with {puzzleMessages.Count} messages");
                    return new CommandResult
                    {
                        Success = true,
                        Type = CommandResultType.Success,
                        Message = string.Join("\n\n", puzzleMessages),
                        StateChange = new GameStateChange
                        {
                            ItemsRemoved = new List<string> { item.Id }
                        }
                    };
                }
            }

            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] No puzzle steps matched, using standard drop behavior");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[DropCommandHandler] PuzzleEngine is null, using standard drop behavior");
        }

        // No puzzle step matched, use standard drop behavior
        // Get success message from object actions or use default
        var successMessage = item.Actions.TryGetValue("drop", out var dropAction)
            ? dropAction.SuccessMessage
            : $"You drop the {item.Name}.";

        // Remove from inventory using gameState
        gameState.Player.Inventory.Items.Remove(item);

        // Add to current room
        var roomForCleanup = _stateMachine.GetCurrentRoom();
        if (!roomForCleanup.ObjectIds.Contains(item.Id))
        {
            roomForCleanup.ObjectIds.Add(item.Id);
        }
        roomForCleanup.State.VisibleObjectIds.Add(item.Id);
        item.IsVisible = true;

        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = successMessage,
            StateChange = new GameStateChange
            {
                ItemsRemoved = new List<string> { item.Id }
            }
        };
    }
}
