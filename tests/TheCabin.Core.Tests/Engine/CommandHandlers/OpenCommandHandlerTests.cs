using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine.CommandHandlers;

public class OpenCommandHandlerTests
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly OpenCommandHandler _handler;
    private readonly GameState _gameState;

    public OpenCommandHandlerTests()
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
        _handler = new OpenCommandHandler(_stateMachine);
    }

    [Fact]
    public async Task ValidateAsync_WithNoObject_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "open", Object = null };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Open what?", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithObjectNotFound_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "open", Object = "portal" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("don't see", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithNonOpenableObject_ReturnsInvalid()
    {
        // Arrange
        var table = CreateTestObject("table", "Table");
        table.Type = ObjectType.Furniture;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("table");
        var command = new ParsedCommand { Verb = "open", Object = "table" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("can't open", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithObjectWithoutOpenAction_ReturnsInvalid()
    {
        // Arrange
        var door = CreateTestObject("door", "Door");
        door.Type = ObjectType.Door;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "open", Object = "door" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("can't be opened", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithOpenableDoor_ReturnsValid()
    {
        // Arrange
        var door = CreateOpenableDoor("door", "Wooden Door");
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "open", Object = "door" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithOpenableContainer_ReturnsValid()
    {
        // Arrange
        var chest = CreateOpenableContainer("chest", "Treasure Chest");
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("chest");
        var command = new ParsedCommand { Verb = "open", Object = "chest" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExecuteAsync_OpensDoorSuccessfully()
    {
        // Arrange
        var door = CreateOpenableDoor("door", "Wooden Door");
        door.State.Flags["is_open"] = false;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "open", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("door swings open", result.Message);
        Assert.True((bool)door.State.Flags["is_open"]);
    }

    [Fact]
    public async Task ExecuteAsync_OpensContainerSuccessfully()
    {
        // Arrange
        var chest = CreateOpenableContainer("chest", "Treasure Chest");
        chest.State.Flags["is_open"] = false;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("chest");
        var command = new ParsedCommand { Verb = "open", Object = "chest" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("chest creaks open", result.Message);
        Assert.True((bool)chest.State.Flags["is_open"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyOpen_ReturnsFailure()
    {
        // Arrange
        var door = CreateOpenableDoor("door", "Wooden Door");
        door.State.Flags["is_open"] = true; // Already open
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "open", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already open", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithLockedObject_ReturnsFailure()
    {
        // Arrange
        var door = CreateOpenableDoor("door", "Wooden Door");
        door.State.Flags["is_open"] = false;
        door.State.Flags["is_locked"] = true; // Locked
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "open", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CommandResultType.RequirementsNotMet, result.Type);
        Assert.Contains("locked", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_RevealsContainedObjects()
    {
        // Arrange
        var chest = CreateOpenableContainer("chest", "Treasure Chest");
        chest.State.Flags["is_open"] = false;
        
        // Create a hidden item inside the chest
        var gem = CreateTestObject("gem", "Ruby Gem");
        gem.IsVisible = false;
        chest.RequiredItems = new List<string> { "gem" };
        
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("chest");
        var command = new ParsedCommand { Verb = "open", Object = "chest" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.True(gem.IsVisible);
        Assert.Contains("gem", currentRoom.State.VisibleObjectIds);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesStateChanges()
    {
        // Arrange
        var door = CreateOpenableDoor("door", "Secret Door");
        door.State.Flags["is_open"] = false;
        door.Actions["open"].StateChanges.Add(new StateChange
        {
            Target = "self",
            Property = "Description",
            NewValue = "An open doorway reveals a hidden passage."
        });
        
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "open", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("An open doorway reveals a hidden passage.", door.Description);
    }

    [Fact]
    public async Task ExecuteAsync_InitializesStateIfNull()
    {
        // Arrange
        var door = CreateOpenableDoor("door", "Door");
        door.State = null!; // No state initially
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "open", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(door.State);
        Assert.True((bool)door.State.Flags["is_open"]);
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

    private GameObject CreateOpenableDoor(string id, string name)
    {
        var door = CreateTestObject(id, name);
        door.Type = ObjectType.Door;
        door.Actions["open"] = new ActionDefinition
        {
            Verb = "open",
            SuccessMessage = "The door swings open with a creak.",
            FailureMessage = "The door is locked.",
            StateChanges = new List<StateChange>()
        };
        door.State.Flags["is_open"] = false;
        return door;
    }

    private GameObject CreateOpenableContainer(string id, string name)
    {
        var container = CreateTestObject(id, name);
        container.Type = ObjectType.Container;
        container.Actions["open"] = new ActionDefinition
        {
            Verb = "open",
            SuccessMessage = "The chest creaks open, revealing its contents.",
            FailureMessage = "The chest is locked tight.",
            StateChanges = new List<StateChange>()
        };
        container.State.Flags["is_open"] = false;
        return container;
    }
}
