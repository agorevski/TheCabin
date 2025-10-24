using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine.CommandHandlers;

/// <summary>
/// Handles inventory display command
/// </summary>
public class InventoryCommandHandler : ICommandHandler
{
    private readonly IInventoryManager _inventoryManager;
    
    public string Verb => "inventory";
    
    public InventoryCommandHandler(IInventoryManager inventoryManager)
    {
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
    }
    
    public Task<CommandValidationResult> ValidateAsync(ParsedCommand command, GameState gameState)
    {
        // Inventory is always valid
        return Task.FromResult(CommandValidationResult.Valid());
    }
    
    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var message = _inventoryManager.GetInventoryDescription();
        
        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = message
        });
    }
}
