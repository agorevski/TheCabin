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
    
    public string Verb => "take";
    
    public TakeCommandHandler(
        GameStateMachine stateMachine,
        IInventoryManager inventoryManager)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
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
    
    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        var targetObject = _stateMachine.FindVisibleObject(command.Object!)!;
        
        // Get success message from object actions or use default
        var successMessage = targetObject.Actions.TryGetValue("take", out var takeAction)
            ? takeAction.SuccessMessage
            : $"You pick up the {targetObject.Name}.";
        
        // Add to inventory
        _inventoryManager.AddItem(targetObject);
        
        // Remove from room's visible objects
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Remove(targetObject.Id);
        targetObject.IsVisible = false;
        
        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = successMessage,
            StateChange = new GameStateChange
            {
                ItemsAdded = new List<string> { targetObject.Id }
            }
        });
    }
}
