using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine.CommandHandlers;

public class CloseCommandHandlerTests
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly CloseCommandHandler _handler;
    private readonly GameState _gameState;

    public CloseCommandHandlerTests()
    {
        // Create test story pack first
        var storyPack = CreateTestStoryPack();
        
        // Create inventory manager with temporary game state
        var tempGameState = new GameState();
        _inventoryManager = new InventoryManager(tempGameState);
        
        // Create state machine and initialize
        _stateMachine = new GameStateMachine(_inventoryManager);
        _stateMachine.Initialize(storyPack);
        
        // Get the actual game state created by Initialize
        _gameState = _stateMachine.CurrentState;
        
        // Create handler
        _handler = new CloseCommandHandler(_stateMachine);
    }

    [Fact]
    public async Task ValidateAsync_WithNoObject_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "close", Object = null };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Close what?", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithObjectNotFound_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "close", Object = "portal" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("don't see", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithNonClosableObject_ReturnsInvalid()
    {
        // Arrange
        var table = CreateTestObject("table", "Table");
        table.Type = ObjectType.Furniture;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("table");
        var command = new ParsedCommand { Verb = "close", Object = "table" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("can't close", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithObjectWithoutCloseAction_ReturnsInvalid()
    {
        // Arrange
        var door = CreateTestObject("door", "Door");
        door.Type = ObjectType.Door;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "close", Object = "door" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("can't be closed", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithClosableDoor_ReturnsValid()
    {
        // Arrange
        var door = CreateClosableDoor("door", "Wooden Door");
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "close", Object = "door" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithClosableContainer_ReturnsValid()
    {
        // Arrange
        var chest = CreateClosableContainer("chest", "Treasure Chest");
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("chest");
        var command = new ParsedCommand { Verb = "close", Object = "chest" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExecuteAsync_ClosesDoorSuccessfully()
    {
        // Arrange
        var door = CreateClosableDoor("door", "Wooden Door");
        door.State.Flags["is_open"] = true; // Door is open
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "close", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("door swings shut", result.Message);
        Assert.False((bool)door.State.Flags["is_open"]);
    }

    [Fact]
    public async Task ExecuteAsync_ClosesContainerSuccessfully()
    {
        // Arrange
        var chest = CreateClosableContainer("chest", "Treasure Chest");
        chest.State.Flags["is_open"] = true; // Chest is open
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("chest");
        var command = new ParsedCommand { Verb = "close", Object = "chest" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("chest closes", result.Message);
        Assert.False((bool)chest.State.Flags["is_open"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyClosed_ReturnsFailure()
    {
        // Arrange
        var door = CreateClosableDoor("door", "Wooden Door");
        door.State.Flags["is_open"] = false; // Already closed
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "close", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already closed", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoOpenFlag_ReturnsFailure()
    {
        // Arrange
        var door = CreateClosableDoor("door", "Wooden Door");
        door.State.Flags.Remove("is_open"); // No flag at all
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "close", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already closed", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesStateChanges()
    {
        // Arrange
        var door = CreateClosableDoor("door", "Secret Door");
        door.State.Flags["is_open"] = true;
        door.Actions["close"].StateChanges.Add(new StateChange
        {
            Target = "self",
            Property = "Description",
            NewValue = "A closed door that blends with the wall."
        });
        
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "close", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("A closed door that blends with the wall.", door.Description);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesRoomStateChanges()
    {
        // Arrange
        var door = CreateClosableDoor("door", "Heavy Door");
        door.State.Flags["is_open"] = true;
        door.Actions["close"].StateChanges.Add(new StateChange
        {
            Target = "room",
            Property = "Description",
            NewValue = "The room feels more enclosed now."
        });
        
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "close", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("The room feels more enclosed now.", currentRoom.Description);
    }

    private StoryPack CreateTestStoryPack()
    {
        var room = new Room
        {
            Id = "test_room",
            Description = "Test room",
            State = new RoomState
            {
                VisibleObjectIds = new List<string>()
            },
            LightLevel = LightLevel.Normal
        };

        return new StoryPack
        {
            Id = "test",
            Theme = "Test Theme",
            StartingRoomId = "test_room",
            Rooms = new List<Room> { room },
            Objects = new Dictionary<string, GameObject>()
        };
    }

    private GameObject CreateTestObject(string id, string name)
    {
        var obj = new GameObject
        {
            Id = id,
            Name = name,
            Description = $"A test {name}.",
            Type = ObjectType.Item,
            IsVisible = true,
            Weight = 1,
            State = new ObjectState(),
            Actions = new Dictionary<string, ActionDefinition>()
        };

        _gameState.World.Objects[id] = obj;
        return obj;
    }

    private GameObject CreateClosableDoor(string id, string name)
    {
        var door = CreateTestObject(id, name);
        door.Type = ObjectType.Door;
        door.Actions["close"] = new ActionDefinition
        {
            Verb = "close",
            SuccessMessage = "The door swings shut with a solid thud.",
            FailureMessage = "The door won't close.",
            StateChanges = new List<StateChange>()
        };
        door.State.Flags["is_open"] = false;
        return door;
    }

    private GameObject CreateClosableContainer(string id, string name)
    {
        var container = CreateTestObject(id, name);
        container.Type = ObjectType.Container;
        container.Actions["close"] = new ActionDefinition
        {
            Verb = "close",
            SuccessMessage = "The chest closes with a satisfying click.",
            FailureMessage = "The chest won't close.",
            StateChanges = new List<StateChange>()
        };
        container.State.Flags["is_open"] = false;
        return container;
    }
}
