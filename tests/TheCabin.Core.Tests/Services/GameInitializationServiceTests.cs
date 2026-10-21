using Moq;
using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
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

    [Fact]
    public async Task SaveAfterCommands_CapturesLocationInventoryVisitsTurnsAndFlags()
    {
        var saveRepository = new Mock<IGameSaveRepository>();
        GameState? saved = null;
        saveRepository.Setup(r => r.SaveAsync(It.IsAny<string>(), It.IsAny<GameState>()))
            .Callback<string, GameState>((_, state) => saved = state)
            .ReturnsAsync(1);
        var gameStateService = new GameStateService(saveRepository.Object, Mock.Of<IStoryPackService>());
        var stateMachine = new GameStateMachine(new InventoryManager(new GameState()));
        var router = CreateRouter(stateMachine);

        await new GameInitializationService().InitializeNewGameAsync(
            CreatePack(), stateMachine, Mock.Of<IPuzzleEngine>(), null, gameStateService);
        await router.RouteAsync(new ParsedCommand { Verb = "take", Object = "lamp" });
        await router.RouteAsync(new ParsedCommand { Verb = "go", Object = "down" });
        stateMachine.SetStoryFlag("cellar_entered", true);
        await gameStateService.SaveGameAsync("slot");

        Assert.NotNull(saved);
        Assert.Equal("cellar", saved!.Player.CurrentLocationId);
        Assert.Contains(saved.Player.Inventory.Items, i => i.Id == "lamp");
        Assert.True(saved.World.Rooms["cellar"].IsVisited);
        Assert.Equal(2, saved.World.TurnNumber);
        Assert.True(saved.Progress.StoryFlags["cellar_entered"]);
    }

    [Fact]
    public async Task LoadSavedGame_ThenCommands_SaveWritesResumedProgressPlusNewCommands()
    {
        var pack = CreatePack();
        var saved = new GameState
        {
            Player = new Player { CurrentLocationId = "main_room" },
            World = new WorldState
            {
                CurrentThemeId = pack.Id,
                Rooms = pack.Rooms.ToDictionary(r => r.Id, r => r),
                Objects = new Dictionary<string, GameObject> { ["lamp"] = pack.Objects["lamp"] },
                TurnNumber = 7
            }
        };
        saved.Progress.StoryFlags["lamp_lit"] = true;

        var saveRepository = new Mock<IGameSaveRepository>();
        saveRepository.Setup(r => r.LoadAsync(3)).ReturnsAsync(saved);
        GameState? resaved = null;
        saveRepository.Setup(r => r.SaveAsync(It.IsAny<string>(), It.IsAny<GameState>()))
            .Callback<string, GameState>((_, state) => resaved = state)
            .ReturnsAsync(4);
        var gameStateService = new GameStateService(saveRepository.Object, Mock.Of<IStoryPackService>());
        var storyPackService = new Mock<IStoryPackService>();
        storyPackService.Setup(s => s.LoadPackAsync(pack.Id)).ReturnsAsync(pack);
        var stateMachine = new GameStateMachine(new InventoryManager(new GameState()));
        var router = CreateRouter(stateMachine);

        var result = await new GameInitializationService().LoadSavedGameAsync(
            3, stateMachine, Mock.Of<IPuzzleEngine>(), null, gameStateService, storyPackService.Object);
        await router.RouteAsync(new ParsedCommand { Verb = "take", Object = "lamp" });
        await router.RouteAsync(new ParsedCommand { Verb = "go", Object = "down" });
        await gameStateService.SaveGameAsync("slot");

        Assert.True(result.Success);
        Assert.NotNull(resaved);
        Assert.Equal("cellar", resaved!.Player.CurrentLocationId);
        Assert.Contains(resaved.Player.Inventory.Items, i => i.Id == "lamp");
        Assert.True(resaved.Progress.StoryFlags["lamp_lit"]);
        Assert.Equal(9, resaved.World.TurnNumber);
    }

    [Fact]
    public async Task LoadSavedGameAsync_DoesNotCountStartingRoomVisitAgain()
    {
        var pack = CreatePack();
        var achievements = new Mock<IAchievementService>();
        achievements
            .Setup(a => a.TrackEventAsync(It.IsAny<TriggerType>(), It.IsAny<string>(), It.IsAny<GameState>()))
            .ReturnsAsync(new List<AchievementUnlocked>());

        var saveRepository = new Mock<IGameSaveRepository>();
        GameState? saved = null;
        saveRepository.Setup(r => r.SaveAsync(It.IsAny<string>(), It.IsAny<GameState>()))
            .Callback<string, GameState>((_, state) => saved = state)
            .ReturnsAsync(1);
        var storyPackService = new Mock<IStoryPackService>();
        storyPackService.Setup(s => s.LoadPackAsync(pack.Id)).ReturnsAsync(pack);

        var gameStateService = new GameStateService(saveRepository.Object, Mock.Of<IStoryPackService>());
        await new GameInitializationService().InitializeNewGameAsync(
            pack, new GameStateMachine(new InventoryManager(new GameState()), achievements.Object),
            Mock.Of<IPuzzleEngine>(), achievements.Object, gameStateService);
        await gameStateService.SaveGameAsync("slot");
        achievements.Verify(
            a => a.TrackEventAsync(TriggerType.RoomVisited, "main_room", It.IsAny<GameState>()),
            Times.Once);

        saveRepository.Setup(r => r.LoadAsync(1)).ReturnsAsync(saved);
        var loadingService = new GameStateService(saveRepository.Object, Mock.Of<IStoryPackService>());
        await new GameInitializationService().LoadSavedGameAsync(
            1, new GameStateMachine(new InventoryManager(new GameState()), achievements.Object),
            Mock.Of<IPuzzleEngine>(), achievements.Object, loadingService, storyPackService.Object);

        achievements.Verify(
            a => a.TrackEventAsync(It.IsAny<TriggerType>(), It.IsAny<string>(), It.IsAny<GameState>()),
            Times.Once);
    }

    /// <summary>
    /// Builds a router the same way the apps do: inventory commands act on the state machine's live state.
    /// </summary>
    private static CommandRouter CreateRouter(GameStateMachine stateMachine)
    {
        var inventoryManager = new InventoryManager(() => stateMachine.CurrentState);
        return new CommandRouter(
            new ICommandHandler[]
            {
                new TakeCommandHandler(stateMachine, inventoryManager),
                new MoveCommandHandler(stateMachine)
            },
            stateMachine);
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
            IsCollectable = true,
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
                    Exits = new Dictionary<string, string> { ["down"] = "cellar" },
                    State = new RoomState { VisibleObjectIds = new List<string> { "lamp" } }
                },
                new Room
                {
                    Id = "cellar",
                    Description = "A damp cellar",
                    Exits = new Dictionary<string, string> { ["up"] = "main_room" },
                    State = new RoomState()
                }
            },
            Objects = new Dictionary<string, GameObject> { ["lamp"] = lamp }
        };
    }
}
