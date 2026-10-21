using Moq;
using TheCabin.Core.Engine;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Core.Services;
using Xunit;

namespace TheCabin.Core.Tests.Services;

public class GameInitializationServiceTests
{
    [Fact]
    public async Task LoadSavedGameAsync_RestoresInventoryFlagsAndTurnCount()
    {
        var pack = CreatePack();
        var lamp = pack.Objects["lamp"];
        var saved = new GameState
        {
            Player = new Player { CurrentLocationId = "main_room" },
            World = new WorldState
            {
                CurrentThemeId = pack.Id,
                Rooms = pack.Rooms.ToDictionary(r => r.Id, r => r),
                Objects = new Dictionary<string, GameObject> { ["lamp"] = lamp },
                TurnNumber = 7
            }
        };
        saved.Player.Inventory.Items.Add(lamp);
        saved.Progress.StoryFlags["lamp_lit"] = true;

        var saveRepository = new Mock<IGameSaveRepository>();
        saveRepository.Setup(r => r.LoadAsync(3)).ReturnsAsync(saved);
        var gameStateService = new GameStateService(saveRepository.Object, Mock.Of<IStoryPackService>());
        var storyPackService = new Mock<IStoryPackService>();
        storyPackService.Setup(s => s.LoadPackAsync(pack.Id)).ReturnsAsync(pack);
        var stateMachine = new GameStateMachine(new InventoryManager(new GameState()));

        var result = await new GameInitializationService().LoadSavedGameAsync(
            3, stateMachine, Mock.Of<IPuzzleEngine>(), null, gameStateService, storyPackService.Object);

        Assert.True(result.Success);
        Assert.Contains(stateMachine.CurrentState.Player.Inventory.Items, i => i.Id == "lamp");
        Assert.True(stateMachine.CurrentState.Progress.StoryFlags["lamp_lit"]);
        Assert.Equal(7, stateMachine.CurrentState.World.TurnNumber);
    }

    [Fact]
    public async Task InitializeNewGameAsync_SaveCapturesProgressMadeThroughStateMachine()
    {
        var saveRepository = new Mock<IGameSaveRepository>();
        saveRepository.Setup(r => r.SaveAsync(It.IsAny<string>(), It.IsAny<GameState>())).ReturnsAsync(1);
        var gameStateService = new GameStateService(saveRepository.Object, Mock.Of<IStoryPackService>());
        var stateMachine = new GameStateMachine(new InventoryManager(new GameState()));

        var result = await new GameInitializationService().InitializeNewGameAsync(
            CreatePack(), stateMachine, Mock.Of<IPuzzleEngine>(), null, gameStateService);
        stateMachine.CurrentState.World.TurnNumber = 5;
        await gameStateService.SaveGameAsync("slot");

        Assert.True(result.Success);
        saveRepository.Verify(
            r => r.SaveAsync("slot", It.Is<GameState>(s => s.World.TurnNumber == 5)),
            Times.Once);
    }

    private static StoryPack CreatePack()
    {
        var lamp = new GameObject
        {
            Id = "lamp",
            Name = "Lamp",
            Description = "A lamp",
            Type = ObjectType.Item,
            IsVisible = true,
            Weight = 1,
            State = new ObjectState(),
            Actions = new Dictionary<string, ActionDefinition>()
        };

        return new StoryPack
        {
            Id = "test_theme",
            Theme = "Test Theme",
            StartingRoomId = "main_room",
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "main_room",
                    Description = "A test room",
                    ObjectIds = new List<string> { "lamp" },
                    Exits = new Dictionary<string, string>(),
                    State = new RoomState { VisibleObjectIds = new List<string> { "lamp" } }
                }
            },
            Objects = new Dictionary<string, GameObject> { ["lamp"] = lamp }
        };
    }
}
