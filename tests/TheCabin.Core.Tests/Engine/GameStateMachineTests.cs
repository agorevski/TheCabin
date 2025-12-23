using TheCabin.Core.Engine;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine;

public class GameStateMachineTests
{
    private GameStateMachine CreateStateMachine(out IInventoryManager inventoryManager)
    {
        var gameState = new GameState();
        inventoryManager = new InventoryManager(gameState);
        return new GameStateMachine(inventoryManager);
    }

    private StoryPack CreateTestStoryPack()
    {
        var room1 = new Room
        {
            Id = "room1",
            Description = "A small room",
            Exits = new Dictionary<string, string> { { "north", "room2" } },
            State = new RoomState
            {
                VisibleObjectIds = new List<string> { "key" }
            }
        };

        var room2 = new Room
        {
            Id = "room2",
            Description = "A larger room",
            Exits = new Dictionary<string, string> { { "south", "room1" } }
        };

        var keyObject = new GameObject
        {
            Id = "key",
            Name = "Rusty Key",
            Description = "An old rusty key",
            Type = ObjectType.Item,
            IsCollectable = true,
            IsVisible = true,
            Weight = 1
        };

        return new StoryPack
        {
            Id = "test_pack",
            Theme = "Test Theme",
            StartingRoomId = "room1",
            Rooms = new List<Room> { room1, room2 },
            Objects = new Dictionary<string, GameObject>
            {
                { "key", keyObject }
            }
        };
    }

    [Fact]
    public void Initialize_SetsUpGameStateCorrectly()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();

        // Act
        stateMachine.Initialize(storyPack);

        // Assert
        Assert.NotNull(stateMachine.CurrentState);
        Assert.Equal("room1", stateMachine.CurrentState.Player.CurrentLocationId);
        Assert.Equal(100, stateMachine.CurrentState.Player.Health);
        Assert.Equal(2, stateMachine.CurrentState.World.Rooms.Count);
        Assert.Single(stateMachine.CurrentState.StoryLog);
    }

    [Fact]
    public void GetCurrentRoom_ReturnsCorrectRoom()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        var room = stateMachine.GetCurrentRoom();

        // Assert
        Assert.NotNull(room);
        Assert.Equal("room1", room.Id);
        Assert.Equal("A small room", room.Description);
    }

    [Fact]
    public void GetVisibleObjects_ReturnsVisibleObjectsInRoom()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        var visibleObjects = stateMachine.GetVisibleObjects();

        // Assert
        Assert.Single(visibleObjects);
        Assert.Equal("key", visibleObjects[0].Id);
    }

    [Fact]
    public void CanTransitionTo_ReturnsTrueForValidExit()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        var canTransition = stateMachine.CanTransitionTo("room2");

        // Assert
        Assert.True(canTransition);
    }

    [Fact]
    public void CanTransitionTo_ReturnsFalseForInvalidExit()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        var canTransition = stateMachine.CanTransitionTo("nonexistent");

        // Assert
        Assert.False(canTransition);
    }

    [Fact]
    public void TransitionTo_MovesPlayerToNewRoom()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        stateMachine.TransitionTo("room2");

        // Assert
        Assert.Equal("room2", stateMachine.CurrentState.Player.CurrentLocationId);
        Assert.Equal(1, stateMachine.CurrentState.Player.Stats.RoomsExplored);
        Assert.True(stateMachine.CurrentState.World.Rooms["room2"].IsVisited);
    }

    [Fact]
    public void FindObject_FindsObjectById()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        var obj = stateMachine.FindObject("key");

        // Assert
        Assert.NotNull(obj);
        Assert.Equal("key", obj.Id);
        Assert.Equal("Rusty Key", obj.Name);
    }

    [Fact]
    public void FindVisibleObject_FindsVisibleObjectInCurrentRoom()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        var obj = stateMachine.FindVisibleObject("key");

        // Assert
        Assert.NotNull(obj);
        Assert.Equal("key", obj.Id);
    }

    [Fact]
    public void ModifyHealth_ClampsHealthCorrectly()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act & Assert - Reduce health
        stateMachine.ModifyHealth(-30);
        Assert.Equal(70, stateMachine.CurrentState.Player.Health);

        // Act & Assert - Can't go below 0
        stateMachine.ModifyHealth(-100);
        Assert.Equal(0, stateMachine.CurrentState.Player.Health);

        // Act & Assert - Heal
        stateMachine.ModifyHealth(50);
        Assert.Equal(50, stateMachine.CurrentState.Player.Health);

        // Act & Assert - Can't exceed max health
        stateMachine.ModifyHealth(100);
        Assert.Equal(100, stateMachine.CurrentState.Player.Health);
    }

    [Fact]
    public void SetStoryFlag_AndGetStoryFlag_WorkCorrectly()
    {
        // Arrange
        var stateMachine = CreateStateMachine(out _);
        var storyPack = CreateTestStoryPack();
        stateMachine.Initialize(storyPack);

        // Act
        stateMachine.SetStoryFlag("test_flag", true);
        var flagValue = stateMachine.GetStoryFlag("test_flag");

        // Assert
        Assert.True(flagValue);
    }
}
