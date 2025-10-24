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

    public string Verb => "drop";

    public DropCommandHandler(IInventoryManager inventoryManager, GameStateMachine stateMachine)
    {
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
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

    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var item = gameState.Player.Inventory.Items
            .First(i => i.Id.Contains(command.Object!, StringComparison.OrdinalIgnoreCase) ||
                       i.Name.Contains(command.Object!, StringComparison.OrdinalIgnoreCase));

        // Get success message from object actions or use default
        var successMessage = item.Actions.TryGetValue("drop", out var dropAction)
            ? dropAction.SuccessMessage
            : $"You drop the {item.Name}.";

        // Remove from inventory
        _inventoryManager.RemoveItem(item.Id);

        // Add to current room
        var currentRoom = _stateMachine.GetCurrentRoom();
        if (!currentRoom.ObjectIds.Contains(item.Id))
        {
            currentRoom.ObjectIds.Add(item.Id);
        }
        currentRoom.State.VisibleObjectIds.Add(item.Id);
        item.IsVisible = true;

        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = successMessage,
            StateChange = new GameStateChange
            {
                ItemsRemoved = new List<string> { item.Id }
            }
        });
    }
}
