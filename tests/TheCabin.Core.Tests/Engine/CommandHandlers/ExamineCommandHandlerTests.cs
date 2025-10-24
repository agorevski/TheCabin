using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine.CommandHandlers;

public class ExamineCommandHandlerTests
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly ExamineCommandHandler _handler;
    private readonly GameState _gameState;

    public ExamineCommandHandlerTests()
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
        _handler = new ExamineCommandHandler(_stateMachine, _inventoryManager);
    }

    [Fact]
    public async Task ValidateAsync_WithNoObject_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "examine", Object = null };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Examine what?", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithObjectNotFound_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "examine", Object = "dragon" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("don't see", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithVisibleObject_ReturnsValid()
    {
        // Arrange
        var table = CreateTestObject("table", "Wooden Table");
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("table");
        var command = new ParsedCommand { Verb = "examine", Object = "table" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithInventoryItem_ReturnsValid()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "examine", Object = "lantern" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExecuteAsync_ExaminesVisibleObject_ReturnsDescription()
    {
        // Arrange
        var table = CreateTestObject("table", "A sturdy wooden table with carved legs.");
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("table");
        var command = new ParsedCommand { Verb = "examine", Object = "table" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("sturdy wooden table", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ExaminesInventoryItem_ReturnsDescription()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "An old brass lantern.", isCollectable: true);
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "examine", Object = "lantern" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("old brass lantern", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomExamineAction_ReturnsCustomMessage()
    {
        // Arrange
        var painting = CreateTestObject("painting", "A faded painting.");
        painting.Actions["examine"] = new ActionDefinition
        {
            Verb = "examine",
            SuccessMessage = "The painting depicts a mysterious cabin in the woods. Something seems off about the perspective."
        };
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("painting");
        var command = new ParsedCommand { Verb = "examine", Object = "painting" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("mysterious cabin", result.Message);
        Assert.Contains("perspective", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonDefaultState_IncludesStateDescription()
    {
        // Arrange
        var door = CreateTestObject("door", "A heavy wooden door.");
        door.State.CurrentState = "locked";
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("door");
        var command = new ParsedCommand { Verb = "examine", Object = "door" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("heavy wooden door", result.Message);
        Assert.Contains("locked", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenContainer_ShowsOpenState()
    {
        // Arrange
        var chest = CreateTestObject("chest", "An old treasure chest.");
        chest.Type = ObjectType.Container;
        chest.State.Flags["is_open"] = true;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("chest");
        var command = new ParsedCommand { Verb = "examine", Object = "chest" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("treasure chest", result.Message);
        Assert.Contains("open", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithClosedContainer_ShowsClosedState()
    {
        // Arrange
        var chest = CreateTestObject("chest", "An old treasure chest.");
        chest.Type = ObjectType.Container;
        chest.State.Flags["is_open"] = false;
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("chest");
        var command = new ParsedCommand { Verb = "examine", Object = "chest" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("treasure chest", result.Message);
        Assert.Contains("closed", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithLitLight_ShowsLitState()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "A brass lantern.", isCollectable: true);
        lantern.Type = ObjectType.Light;
        lantern.State.CurrentState = "lit";
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "examine", Object = "lantern" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("brass lantern", result.Message);
        Assert.Contains("lit", result.Message);
        Assert.Contains("glow", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_PrefersRoomObjectOverInventory()
    {
        // Arrange - Same object ID in both room and inventory (edge case)
        var roomKey = CreateTestObject("key", "A rusty iron key on the floor.");
        var inventoryKey = CreateTestObject("key_inventory", "A shiny brass key.", isCollectable: true);
        inventoryKey.Id = "key"; // Same ID for testing
        
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Add("key");
        _inventoryManager.AddItem(inventoryKey);
        
        var command = new ParsedCommand { Verb = "examine", Object = "key" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        // Should get the room object description (rusty iron) not inventory (shiny brass)
        Assert.Contains("rusty iron", result.Message);
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

    private GameObject CreateTestObject(string id, string description, bool isCollectable = false)
    {
        var obj = new GameObject
        {
            Id = id,
            Name = id.Replace("_", " "),
            Description = description,
            Type = ObjectType.Item,
            IsCollectable = isCollectable,
            IsVisible = true,
            Weight = 1,
            State = new ObjectState(),
            Actions = new Dictionary<string, ActionDefinition>()
        };

        _gameState.World.Objects[id] = obj;
        return obj;
    }
}
