using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine;

/// <summary>
/// Integration tests for CommandRouter testing end-to-end command processing
/// </summary>
public class CommandRouterIntegrationTests
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly CommandRouter _router;
    private readonly GameState _gameState;

    public CommandRouterIntegrationTests()
    {
        // Create test story pack
        var storyPack = CreateIntegrationTestStoryPack();
        
        // Create a temporary game state for initial setup
        var tempGameState = new GameState();
        var tempInventoryManager = new InventoryManager(tempGameState);
        
        // Create state machine and initialize
        _stateMachine = new GameStateMachine(tempInventoryManager);
        _stateMachine.Initialize(storyPack);
        
        // Get the actual game state after initialization
        _gameState = _stateMachine.CurrentState;
        
        // Create inventory manager with the ACTUAL game state
        _inventoryManager = new InventoryManager(_gameState);
        
        // Create command handlers
        var handlers = new List<ICommandHandler>
        {
            new MoveCommandHandler(_stateMachine),
            new TakeCommandHandler(_stateMachine, _inventoryManager),
            new DropCommandHandler(_inventoryManager, _stateMachine),
            new UseCommandHandler(_stateMachine, _inventoryManager, new PuzzleEngine()),
            new LookCommandHandler(_stateMachine),
            new ExamineCommandHandler(_stateMachine, _inventoryManager),
            new OpenCommandHandler(_stateMachine),
            new CloseCommandHandler(_stateMachine),
            new InventoryCommandHandler(_inventoryManager)
        };
        
        // Create router
        _router = new CommandRouter(handlers, _stateMachine);
    }

    [Fact]
    public async Task IntegrationTest_CompleteGameplaySequence()
    {
        // Test a complete sequence of commands in a realistic gameplay scenario
        
        // 1. Look around at start
        var lookCmd = new ParsedCommand { Verb = "look" };
        var result = await _router.RouteAsync(lookCmd);
        Assert.True(result.Success);
        Assert.Contains("starting room", result.Message.ToLower());

        // 2. Examine an object
        var examineCmd = new ParsedCommand { Verb = "examine", Object = "key" };
        result = await _router.RouteAsync(examineCmd);
        Assert.True(result.Success);

        // 3. Take the key
        var takeCmd = new ParsedCommand { Verb = "take", Object = "key" };
        result = await _router.RouteAsync(takeCmd);
        Assert.True(result.Success);
        Assert.True(_inventoryManager.HasItem("key"));

        // 4. Check inventory
        var invCmd = new ParsedCommand { Verb = "inventory" };
        result = await _router.RouteAsync(invCmd);
        Assert.True(result.Success);
        Assert.Contains("key", result.Message.ToLower());

        // 5. Move to another room
        var moveCmd = new ParsedCommand { Verb = "go", Object = "north" };
        result = await _router.RouteAsync(moveCmd);
        Assert.True(result.Success);
        Assert.Equal("locked_room", _gameState.Player.CurrentLocationId);

        // 6. Try to open locked door
        var openCmd = new ParsedCommand { Verb = "open", Object = "chest" };
        result = await _router.RouteAsync(openCmd);
        // Should fail because it's locked
        Assert.False(result.Success);
        Assert.Contains("locked", result.Message.ToLower());

        // 7. Use key on chest
        var useCmd = new ParsedCommand { Verb = "use", Object = "key", Target = "chest" };
        result = await _router.RouteAsync(useCmd);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task IntegrationTest_InvalidCommandSequence()
    {
        // Test handling of invalid commands in sequence
        
        // 1. Try to take object that doesn't exist
        var takeCmd = new ParsedCommand { Verb = "take", Object = "unicorn" };
        var result = await _router.RouteAsync(takeCmd);
        Assert.False(result.Success);

        // 2. Try to go in invalid direction
        var moveCmd = new ParsedCommand { Verb = "go", Object = "up" };
        result = await _router.RouteAsync(moveCmd);
        Assert.False(result.Success);

        // 3. Try to use object not in inventory
        var useCmd = new ParsedCommand { Verb = "use", Object = "sword" };
        result = await _router.RouteAsync(useCmd);
        Assert.False(result.Success);

        // 4. Try to drop object not in inventory
        var dropCmd = new ParsedCommand { Verb = "drop", Object = "treasure" };
        result = await _router.RouteAsync(dropCmd);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task IntegrationTest_ObjectManipulationSequence()
    {
        // Test full object lifecycle: examine, take, use, drop
        
        // 1. Examine object in room
        var examineCmd = new ParsedCommand { Verb = "examine", Object = "key" };
        var result = await _router.RouteAsync(examineCmd);
        Assert.True(result.Success);

        // 2. Take object
        var takeCmd = new ParsedCommand { Verb = "take", Object = "key" };
        result = await _router.RouteAsync(takeCmd);
        Assert.True(result.Success);
        Assert.True(_inventoryManager.HasItem("key"));

        // 3. Examine object in inventory
        result = await _router.RouteAsync(examineCmd);
        Assert.True(result.Success);

        // 4. Drop object
        var dropCmd = new ParsedCommand { Verb = "drop", Object = "key" };
        result = await _router.RouteAsync(dropCmd);
        Assert.True(result.Success);
        Assert.False(_inventoryManager.HasItem("key"));

        // 5. Object should be visible in room again
        var room = _stateMachine.GetCurrentRoom();
        Assert.Contains("key", room.State.VisibleObjectIds);

        // 6. Can take it again
        result = await _router.RouteAsync(takeCmd);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task IntegrationTest_RoomNavigationSequence()
    {
        // Test navigation between multiple rooms
        
        var startRoom = _gameState.Player.CurrentLocationId;
        Assert.Equal("start_room", startRoom);

        // Move north
        var moveCmd = new ParsedCommand { Verb = "go", Object = "north" };
        var result = await _router.RouteAsync(moveCmd);
        Assert.True(result.Success);
        Assert.Equal("locked_room", _gameState.Player.CurrentLocationId);

        // Look around new room
        var lookCmd = new ParsedCommand { Verb = "look" };
        result = await _router.RouteAsync(lookCmd);
        Assert.True(result.Success);

        // Move back south
        moveCmd = new ParsedCommand { Verb = "go", Object = "south" };
        result = await _router.RouteAsync(moveCmd);
        Assert.True(result.Success);
        Assert.Equal("start_room", _gameState.Player.CurrentLocationId);

        // Should see original room again
        result = await _router.RouteAsync(lookCmd);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task IntegrationTest_ContainerInteractionSequence()
    {
        // Test opening container and accessing contents
        
        // Move to room with chest
        var moveCmd = new ParsedCommand { Verb = "go", Object = "north" };
        await _router.RouteAsync(moveCmd);

        // Get key first (to unlock chest)
        await _router.RouteAsync(new ParsedCommand { Verb = "go", Object = "south" });
        await _router.RouteAsync(new ParsedCommand { Verb = "take", Object = "key" });
        await _router.RouteAsync(new ParsedCommand { Verb = "go", Object = "north" });

        // Examine closed chest
        var examineCmd = new ParsedCommand { Verb = "examine", Object = "chest" };
        var result = await _router.RouteAsync(examineCmd);
        Assert.True(result.Success);

        // Use key to unlock
        var useCmd = new ParsedCommand { Verb = "use", Object = "key", Target = "chest" };
        result = await _router.RouteAsync(useCmd);
        Assert.True(result.Success);

        // Open chest
        var openCmd = new ParsedCommand { Verb = "open", Object = "chest" };
        result = await _router.RouteAsync(openCmd);
        Assert.True(result.Success);

        // Examine open chest (should show contents)
        result = await _router.RouteAsync(examineCmd);
        Assert.True(result.Success);

        // Close chest
        var closeCmd = new ParsedCommand { Verb = "close", Object = "chest" };
        result = await _router.RouteAsync(closeCmd);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task IntegrationTest_UnknownCommandHandling()
    {
        // Test that unknown verbs are handled gracefully
        
        var unknownCmd = new ParsedCommand { Verb = "fly", Object = "away" };
        var result = await _router.RouteAsync(unknownCmd);
        
        Assert.False(result.Success);
        Assert.Equal(CommandResultType.InvalidCommand, result.Type);
        Assert.Contains("don't understand", result.Message.ToLower());
    }

    [Fact]
    public async Task IntegrationTest_StateConsistencyAfterMultipleCommands()
    {
        // Ensure game state remains consistent after multiple operations
        
        var initialHealth = _gameState.Player.Health;
        var initialTurnNumber = _gameState.World.TurnNumber;

        // Execute several commands
        await _router.RouteAsync(new ParsedCommand { Verb = "look" });
        await _router.RouteAsync(new ParsedCommand { Verb = "take", Object = "key" });
        await _router.RouteAsync(new ParsedCommand { Verb = "inventory" });
        await _router.RouteAsync(new ParsedCommand { Verb = "examine", Object = "key" });

        // Verify state consistency
        Assert.Equal(initialHealth, _gameState.Player.Health); // Health unchanged
        Assert.True(_gameState.World.TurnNumber > initialTurnNumber); // Turns advanced
        Assert.True(_inventoryManager.HasItem("key")); // Key in inventory
        Assert.Equal(1, _inventoryManager.GetAllItems().Count); // Only one item
    }

    private StoryPack CreateIntegrationTestStoryPack()
    {
        // Create a simple story pack for integration testing
        var key = new GameObject
        {
            Id = "key",
            Name = "Brass Key",
            Description = "An old brass key.",
            Type = ObjectType.Item,
            IsVisible = true,
            IsCollectable = true,
            Weight = 1,
            State = new ObjectState(),
            Actions = new Dictionary<string, ActionDefinition>
            {
                ["take"] = new ActionDefinition
                {
                    Verb = "take",
                    SuccessMessage = "You take the brass key.",
                    StateChanges = new List<StateChange>()
                },
                ["use"] = new ActionDefinition
                {
                    Verb = "use",
                    SuccessMessage = "You use the key. The chest unlocks with a click.",
                    StateChanges = new List<StateChange>
                    {
                        new StateChange
                        {
                            Target = "chest",
                            Property = "is_locked",
                            NewValue = false
                        }
                    }
                }
            }
        };

        var chest = new GameObject
        {
            Id = "chest",
            Name = "Wooden Chest",
            Description = "A sturdy wooden chest.",
            Type = ObjectType.Container,
            IsVisible = true,
            IsCollectable = false,
            Weight = 20,
            State = new ObjectState
            {
                Flags = new Dictionary<string, bool>
                {
                    ["is_locked"] = true,
                    ["is_open"] = false
                }
            },
            Actions = new Dictionary<string, ActionDefinition>
            {
                ["open"] = new ActionDefinition
                {
                    Verb = "open",
                    SuccessMessage = "You open the chest.",
                    FailureMessage = "The chest is locked.",
                    StateChanges = new List<StateChange>()
                },
                ["close"] = new ActionDefinition
                {
                    Verb = "close",
                    SuccessMessage = "You close the chest.",
                    StateChanges = new List<StateChange>()
                }
            }
        };

        var startRoom = new Room
        {
            Id = "start_room",
            Description = "You are in a starting room.",
            ObjectIds = new List<string> { "key" },
            Exits = new Dictionary<string, string> { { "north", "locked_room" } },
            State = new RoomState
            {
                VisibleObjectIds = new List<string> { "key" }
            },
            LightLevel = LightLevel.Normal
        };

        var lockedRoom = new Room
        {
            Id = "locked_room",
            Description = "You are in a room with a chest.",
            ObjectIds = new List<string> { "chest" },
            Exits = new Dictionary<string, string> { { "south", "start_room" } },
            State = new RoomState
            {
                VisibleObjectIds = new List<string> { "chest" }
            },
            LightLevel = LightLevel.Normal
        };

        return new StoryPack
        {
            Id = "integration_test",
            Theme = "Integration Test",
            Description = "A test story pack for integration testing",
            StartingRoomId = "start_room",
            Rooms = new List<Room> { startRoom, lockedRoom },
            Objects = new Dictionary<string, GameObject>
            {
                { "key", key },
                { "chest", chest }
            }
        };
    }
}
