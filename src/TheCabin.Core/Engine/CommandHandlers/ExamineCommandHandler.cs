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

    public string Verb => "examine";

    public ExamineCommandHandler(
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
            return Task.FromResult(CommandValidationResult.Invalid("Examine what?"));
        }

        // Check if object is visible in room or in inventory
        var visibleObject = _stateMachine.FindVisibleObject(command.Object);
        var inventoryItem = _inventoryManager.GetItem(command.Object);

        if (visibleObject == null && inventoryItem == null)
        {
            return Task.FromResult(CommandValidationResult.Invalid(
                $"You don't see a '{command.Object}' here."));
        }

        return Task.FromResult(CommandValidationResult.Valid());
    }

    public Task<CommandResult> ExecuteAsync(ParsedCommand command, GameState gameState)
    {
        // First check visible objects in room
        var targetObject = _stateMachine.FindVisibleObject(command.Object!);
        
        // If not in room, check inventory
        if (targetObject == null)
        {
            targetObject = _inventoryManager.GetItem(command.Object!);
        }

        if (targetObject == null)
        {
            return Task.FromResult(new CommandResult
            {
                Success = false,
                Type = CommandResultType.InvalidCommand,
                Message = $"You don't see a '{command.Object}' here."
            });
        }

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

        return Task.FromResult(new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = string.Join(" ", details)
        });
    }
}
