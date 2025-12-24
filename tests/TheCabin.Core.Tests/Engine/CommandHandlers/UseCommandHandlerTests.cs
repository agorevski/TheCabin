using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine.CommandHandlers;

public class UseCommandHandlerTests
{
    private readonly GameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly IPuzzleEngine _puzzleEngine;
    private readonly UseCommandHandler _handler;
    private readonly GameState _gameState;

    public UseCommandHandlerTests()
    {
        // Create test story pack first
        var storyPack = CreateTestStoryPack();

        // Create state machine first with a placeholder inventory manager
        var tempGameState = new GameState();
        var tempInventoryManager = new InventoryManager(tempGameState);
        _stateMachine = new GameStateMachine(tempInventoryManager);
        _stateMachine.Initialize(storyPack);

        // Get the actual game state created by Initialize
        _gameState = _stateMachine.CurrentState;

        // Create the actual inventory manager with the correct game state
        _inventoryManager = new InventoryManager(_gameState);

        // Create puzzle engine
        _puzzleEngine = new PuzzleEngine();

        // Create handler
        _handler = new UseCommandHandler(_stateMachine, _inventoryManager, _puzzleEngine);
    }

    [Fact]
    public async Task ValidateAsync_WithNoObject_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "use", Object = null };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Use what?", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithItemNotInInventory_ReturnsInvalid()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "use", Object = "sword" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("don't see", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithItemWithoutUseAction_ReturnsInvalid()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions.Clear(); // No use action
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("can't use", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithValidUsableItem_ReturnsValid()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions["use"] = CreateUseAction("Light the lantern");
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };

        // Act
        var result = await _handler.ValidateAsync(command, _gameState);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExecuteAsync_UsesItemSuccessfully()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions["use"] = CreateUseAction("The lantern flickers to life.");
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("flickers to life", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_IncrementsUsageCount()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions["use"] = CreateUseAction("The lantern flickers to life.");
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };
        var initialCount = lantern.State.UsageCount;

        // Act
        await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.Equal(initialCount + 1, lantern.State.UsageCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithConsumableItem_RemovesFromInventory()
    {
        // Arrange
        var potion = CreateTestObject("potion", "Health Potion", isCollectable: true);
        potion.Actions["use"] = new ActionDefinition
        {
            Verb = "use",
            SuccessMessage = "You drink the potion.",
            ConsumesItem = true,
            RequiredFlags = new List<string>(),
            StateChanges = new List<StateChange>()
        };
        _inventoryManager.AddItem(potion);
        var command = new ParsedCommand { Verb = "use", Object = "potion" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.False(_inventoryManager.HasItem("potion"));
        Assert.Contains("used up", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithRequiredFlagsMissing_ReturnsFailure()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions["use"] = new ActionDefinition
        {
            Verb = "use",
            SuccessMessage = "The lantern lights up.",
            FailureMessage = "You need matches to light it.",
            RequiredFlags = new List<string> { "has_matches" },
            StateChanges = new List<StateChange>()
        };
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CommandResultType.RequirementsNotMet, result.Type);
        Assert.Contains("matches", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithRequiredFlagsPresent_Succeeds()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions["use"] = new ActionDefinition
        {
            Verb = "use",
            SuccessMessage = "The lantern lights up.",
            FailureMessage = "You need matches to light it.",
            RequiredFlags = new List<string> { "has_matches" },
            StateChanges = new List<StateChange>()
        };
        _inventoryManager.AddItem(lantern);
        _gameState.Progress.StoryFlags["has_matches"] = true;
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };

        // Act
        var result = await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("lights up", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesStateChangesToSelf()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.State.CurrentState = "unlit";
        lantern.Actions["use"] = new ActionDefinition
        {
            Verb = "use",
            SuccessMessage = "The lantern lights up.",
            RequiredFlags = new List<string>(),
            StateChanges = new List<StateChange>
            {
                new StateChange
                {
                    Target = "self",
                    Property = "CurrentState",
                    NewValue = "lit"
                }
            }
        };
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };

        // Act
        await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.Equal("lit", lantern.State.CurrentState);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesStateChangesToRoom()
    {
        // Arrange
        var lantern = CreateTestObject("lantern", "Lantern", isCollectable: true);
        lantern.Actions["use"] = new ActionDefinition
        {
            Verb = "use",
            SuccessMessage = "The lantern illuminates the room.",
            RequiredFlags = new List<string>(),
            StateChanges = new List<StateChange>
            {
                new StateChange
                {
                    Target = "room",
                    Property = "LightLevel",
                    NewValue = LightLevel.Bright
                }
            }
        };
        _inventoryManager.AddItem(lantern);
        var command = new ParsedCommand { Verb = "use", Object = "lantern" };
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.LightLevel = LightLevel.Dark;

        // Act
        await _handler.ExecuteAsync(command, _gameState);

        // Assert
        Assert.Equal(LightLevel.Bright, currentRoom.LightLevel);
    }

    private GameState CreateTestGameState()
    {
        return new GameState
        {
            Player = new Player
            {
                CurrentLocationId = "test_room",
                Inventory = new Inventory(),
                Stats = new PlayerStats()
            },
            World = new WorldState
            {
                CurrentThemeId = "test",
                Rooms = new Dictionary<string, Room>(),
                Objects = new Dictionary<string, GameObject>()
            },
            Progress = new ProgressState
            {
                StoryFlags = new Dictionary<string, bool>()
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

    private ActionDefinition CreateUseAction(string successMessage)
    {
        return new ActionDefinition
        {
            Verb = "use",
            SuccessMessage = successMessage,
            RequiredFlags = new List<string>(),
            StateChanges = new List<StateChange>()
        };
    }
}
