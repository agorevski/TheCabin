using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine.CommandHandlers;

public class DropCommandHandlerTests
{
    private readonly IInventoryManager _inventoryManager;
    private readonly DropCommandHandler _handler;
    private readonly GameState _gameState;
    private readonly GameStateMachine _stateMachine;

    public DropCommandHandlerTests()
    {
        _gameState = CreateTestGameState();
        _inventoryManager = new InventoryManager(_gameState);
        _stateMachine = new GameStateMachine(_inventoryManager);

        // Initialize the state machine with a simple story pack
        var storyPack = CreateTestStoryPack();
        _stateMachine.Initialize(storyPack);

        _handler = new DropCommandHandler(_inventoryManager, _stateMachine);
    }

    [Fact]
    public async Task ValidateAsync_WithNoObject_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "drop", Object = null };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Drop what?", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithItemNotInInventory_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "drop", Object = "sword" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("don't have", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithItemInInventory_ReturnsValid()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "drop", Object = "lantern" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExecuteAsync_DropsItemSuccessfully()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "drop", Object = "lantern" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.False(_inventoryManager.HasItem("lantern"));
        Assert.True(lantern.IsVisible);
    }

    [Fact]
    public async Task ExecuteAsync_AddsItemToCurrentRoom()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);

        // Add to the state machine's inventory (not the test gameState)
        _stateMachine.CurrentState.Player.Inventory.Items.Add(lantern);
        _stateMachine.CurrentState.World.Objects[lantern.Id] = lantern;

        var command = new ParsedCommand { Verb = "drop", Object = "lantern" };
        var currentRoom = _stateMachine.GetCurrentRoom();
        var initialCount = currentRoom.State.VisibleObjectIds.Count;

        // Act
        var result = await _handler.ExecuteAsync(command, _stateMachine.CurrentState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("lantern", currentRoom.State.VisibleObjectIds);
        Assert.Equal(initialCount + 1, currentRoom.State.VisibleObjectIds.Count);
    }

    [Fact]
    public async Task ExecuteAsync_UsesCustomDropMessage_WhenAvailable()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions["drop"] = new ActionDefinition
        {
            Verb = "drop",
            SuccessMessage = "Custom drop message"
        };
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "drop", Object = "lantern" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Custom drop message", result.Message);
    }

    private GameState CreateTestGameState()
    {
        return new GameState
        {
            Player = new Player
            {
                CurrentLocationId = "test_room",
                Inventory = new Inventory()
            }
        };
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
            }
        };

        return new StoryPack
        {
            Id = "test",
            Theme = "Test",
            Description = "Test story pack",
            StartingRoomId = "test_room",
            Rooms = new List<Room> { room },
            Objects = new Dictionary<string, GameObject>()
        };
    }

    private GameObject CreateTestObject(string id, string name, bool isCollectable = false)
    {
        var obj = new GameObject
        {
            Id = id,
            Name = name,
            Description = $"A test {name}",
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
