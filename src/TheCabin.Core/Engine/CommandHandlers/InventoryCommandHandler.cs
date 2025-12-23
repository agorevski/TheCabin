using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles inventory display command
/// </summary>
public class InventoryCommandHandler : ICommandHandler
{
    public string Verb => "inventory";

    public InventoryCommandHandler(IInventoryManager inventoryManager)
    {
        ArgumentNullException.ThrowIfNull(inventoryManager);
    }

    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        // Inventory is always valid
        return Task.FromResult(CommandValidationResult.Valid());
    }

    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        // Use gameState inventory instead of inventory manager
        var items = gameState.Player.Inventory.Items;

        string message;
        if (!items.Any())
        {
            message = "You aren't carrying anything.";
        }
        else
        {
            var itemList = string.Join("\n", items.Select(i => $"- {i.Name}"));
            message = $"You are carrying:\n{itemList}";
        }

        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = message
        });
    }
}
