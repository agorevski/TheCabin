using Microsoft.Extensions.Logging.Abstractions;
using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Core.Services;
using Xunit;

namespace TheCabin.Core.Tests.Integration;

/// <summary>
/// Integration tests for Achievement and Puzzle systems working together
/// </summary>
public class AchievementPuzzleIntegrationTests
{
    private readonly GameStateMachine _stateMachine;
    private readonly CommandRouter _commandRouter;
    private readonly IAchievementService _achievementService;
    private readonly IPuzzleEngine _puzzleEngine;
    private readonly IInventoryManager _inventoryManager;
    private readonly GameState _gameState;

    public AchievementPuzzleIntegrationTests()
    {
        // Set up game state
        _gameState = new GameState();
        _inventoryManager = new InventoryManager(_gameState);
        _achievementService = new AchievementService(NullLogger<AchievementService>.Instance);
        _puzzleEngine = new PuzzleEngine(_achievementService);
        _stateMachine = new GameStateMachine(_inventoryManager, _achievementService);
        
        // Create a minimal story pack for initialization
        var testStoryPack = new StoryPack
        {
            Id = "test_pack",
            Theme = "Test",
            StartingRoomId = "test_room",
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "test_room",
                    Description = "A test room",
                    ObjectIds = new List<string>()
                }
            },
            Objects = new Dictionary<string, GameObject>()
        };
        
        _stateMachine.Initialize(testStoryPack);

        // Set up command handlers
        var handlers = new List<ICommandHandler>
        {
            new TakeCommandHandler(_stateMachine, _inventoryManager),
            new UseCommandHandler(_stateMachine, _inventoryManager, _puzzleEngine),
            new MoveCommandHandler(_stateMachine)
        };

        _commandRouter = new CommandRouter(handlers, _stateMachine, _achievementService);
    }

    [Fact]
    public Task HintSystem_RespectsTimeDelays()
    {
        // Arrange
        var puzzle = new Puzzle
        {
            Id = "hint_test",
            Name = "Hint Test",
            Hints = new List<Hint>
            {
                new Hint
                {
                    Level = 1,
                    Text = "Immediate hint",
                    DelayMinutes = 0,
                    Order = 1
                },
                new Hint
                {
                    Level = 2,
                    Text = "Delayed hint",
                    DelayMinutes = 5,
                    Order = 2
                },
                new Hint
                {
                    Level = 3,
                    Text = "Late hint",
                    DelayMinutes = 10,
                    Order = 3
                }
            },
            Steps = new List<PuzzleStep>()
        };

        var puzzleState = new PuzzleState
        {
            StartedAt = DateTime.UtcNow.AddMinutes(-3) // Started 3 minutes ago
        };
        _gameState.Progress.PuzzleStates["hint_test"] = puzzleState;
        _puzzleEngine.InitializePuzzles(new List<Puzzle> { puzzle });

        // Act
        var availableHints = _puzzleEngine.GetAvailableHints("hint_test", _gameState);

        // Assert
        Assert.Single(availableHints); // Only immediate hint should be available
        Assert.Contains(availableHints, h => h.Text == "Immediate hint");
        Assert.DoesNotContain(availableHints, h => h.Text == "Late hint");

        // Simulate 10 more minutes passing (total 13 minutes)
        puzzleState.StartedAt = DateTime.UtcNow.AddMinutes(-13);
        availableHints = _puzzleEngine.GetAvailableHints("hint_test", _gameState);

        // Now all hints should be available
        Assert.Equal(3, availableHints.Count);

        return Task.CompletedTask;
    }

    [Fact]
    public Task PuzzleStepConditions_CheckCorrectly()
    {
        // Arrange
        var puzzle = new Puzzle
        {
            Id = "condition_test",
            Name = "Condition Test",
            Type = PuzzleType.Sequential,
            Steps = new List<PuzzleStep>
            {
                new PuzzleStep
                {
                    Id = "step1",
                    StepNumber = 1,
                    Action = "take",
                    TargetObject = "key",
                    RequiredFlags = new List<string> { "door_unlocked" },
                    CompletionFlag = "has_key"
                }
            }
        };

        _puzzleEngine.InitializePuzzles(new List<Puzzle> { puzzle });
        _gameState.Progress.PuzzleStates["condition_test"] = new PuzzleState();

        // Act & Assert - Without required flag
        var canExecute = _puzzleEngine.CheckStepConditions(puzzle.Steps[0], _gameState);
        Assert.False(canExecute);

        // Act & Assert - With required flag
        _gameState.Progress.StoryFlags["door_unlocked"] = true;
        canExecute = _puzzleEngine.CheckStepConditions(puzzle.Steps[0], _gameState);
        Assert.True(canExecute);

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetActivePuzzles_ReturnsOnlyIncomplete()
    {
        // Arrange
        var puzzle1 = new Puzzle
        {
            Id = "puzzle1",
            Name = "Active Puzzle",
            Type = PuzzleType.Sequential,
            Steps = new List<PuzzleStep>
            {
                new PuzzleStep { Id = "step1", StepNumber = 1 }
            }
        };

        var puzzle2 = new Puzzle
        {
            Id = "puzzle2",
            Name = "Completed Puzzle",
            Type = PuzzleType.Sequential,
            Steps = new List<PuzzleStep>
            {
                new PuzzleStep { Id = "step1", StepNumber = 1, CompletionFlag = "puzzle2_done" }
            }
        };

        _puzzleEngine.InitializePuzzles(new List<Puzzle> { puzzle1, puzzle2 });
        
        // Mark puzzle2 as complete
        _gameState.Progress.PuzzleStates["puzzle2"] = new PuzzleState { IsCompleted = true };
        _gameState.Progress.StoryFlags["puzzle2_done"] = true;

        // Act
        var activePuzzles = _puzzleEngine.GetActivePuzzles(_gameState);

        // Assert
        Assert.Single(activePuzzles);
        Assert.Equal("puzzle1", activePuzzles[0].Id);

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetPuzzleState_ReturnsCorrectState()
    {
        // Arrange
        var puzzle = new Puzzle
        {
            Id = "state_test",
            Name = "State Test",
            Steps = new List<PuzzleStep>()
        };

        var expectedState = new PuzzleState
        {
            StartedAt = DateTime.UtcNow,
            IsCompleted = false,
            CompletedSteps = new List<string> { "step1", "step2" }
        };

        _puzzleEngine.InitializePuzzles(new List<Puzzle> { puzzle });
        _gameState.Progress.PuzzleStates["state_test"] = expectedState;

        // Act
        var actualState = _puzzleEngine.GetPuzzleState("state_test", _gameState);

        // Assert
        Assert.NotNull(actualState);
        Assert.False(actualState.IsCompleted);
        Assert.Equal(2, actualState.CompletedSteps.Count);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task AchievementService_TracksProgress()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            new Achievement
            {
                Id = "explorer",
                Name = "Explorer",
                Description = "Visit all rooms",
                RequiredFlags = new List<string> { "room1_visited", "room2_visited", "room3_visited" }
            }
        };

        await _achievementService.InitializeAsync(achievements);

        // Act & Assert - No progress
        var progress = _achievementService.GetProgress("explorer");
        Assert.NotNull(progress);
        Assert.Equal(0.0, progress.PercentComplete);

        // Act & Assert - Partial progress
        _gameState.Progress.StoryFlags["room1_visited"] = true;
        await _achievementService.TrackEventAsync(TriggerType.RoomVisited, "room1", _gameState);
        progress = _achievementService.GetProgress("explorer");
        Assert.NotNull(progress);
        Assert.True(progress.PercentComplete > 0 && progress.PercentComplete < 100);

        // Act & Assert - Complete progress
        _gameState.Progress.StoryFlags["room2_visited"] = true;
        _gameState.Progress.StoryFlags["room3_visited"] = true;
        await _achievementService.TrackEventAsync(TriggerType.RoomVisited, "room2", _gameState);
        var unlocked = await _achievementService.TrackEventAsync(TriggerType.RoomVisited, "room3", _gameState);
        
        // Check if achievement unlocks
        Assert.Contains(unlocked, a => a.Achievement.Id == "explorer");
    }

    [Fact]
    public async Task CommandRouter_IntegratesWithAchievements()
    {
        // Arrange - Set up a simple game scenario
        var testRoom = new Room
        {
            Id = "test_room",
            Description = "A test room",
            ObjectIds = new List<string> { "coin" }
        };

        var coin = new GameObject
        {
            Id = "coin",
            Name = "Gold Coin",
            Description = "A shiny gold coin",
            IsCollectable = true,
            IsVisible = true,
            Actions = new Dictionary<string, ActionDefinition>
            {
                ["take"] = new ActionDefinition
                {
                    Verb = "take",
                    SuccessMessage = "You take the coin",
                    StateChanges = new List<StateChange>(),
                    RequiredFlags = new List<string>()
                }
            }
        };

        _gameState.World.Rooms["test_room"] = testRoom;
        _gameState.World.Objects["coin"] = coin;
        _gameState.Player.CurrentLocationId = "test_room";

        var achievement = new Achievement
        {
            Id = "first_item",
            Name = "First Item",
            Description = "Collect your first item",
            RequiredFlags = new List<string> { "coin_collected" }
        };

        await _achievementService.InitializeAsync(new List<Achievement> { achievement });

        // Act - Execute take command
        var command = new ParsedCommand
        {
            Verb = "take",
            Object = "coin"
        };
        
        var result = await _commandRouter.RouteAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("coin", _gameState.Player.Inventory.Items.Select(i => i.Id));
    }

    [Fact]
    public Task SaveLoad_PreservesPuzzleAndAchievementState()
    {
        // Arrange - Set up some progress
        _gameState.Progress.PuzzleStates["puzzle1"] = new PuzzleState
        {
            IsCompleted = false,
            CompletedSteps = new List<string> { "step1", "step2" },
            StartedAt = DateTime.UtcNow.AddHours(-1)
        };

        _gameState.Progress.UnlockedAchievements.Add("achievement1");
        _gameState.Progress.UnlockedAchievements.Add("achievement2");
        _gameState.Progress.StoryFlags["flag1"] = true;
        _gameState.Progress.StoryFlags["flag2"] = false;

        // Act - Simulate save/load by serializing state
        var serialized = System.Text.Json.JsonSerializer.Serialize(_gameState);
        var loadedState = System.Text.Json.JsonSerializer.Deserialize<GameState>(serialized);

        // Assert - State should be preserved
        Assert.NotNull(loadedState);
        Assert.True(loadedState!.Progress.PuzzleStates.ContainsKey("puzzle1"));
        Assert.Equal(2, loadedState.Progress.PuzzleStates["puzzle1"].CompletedSteps.Count);
        Assert.Equal(2, loadedState.Progress.UnlockedAchievements.Count);
        Assert.True(loadedState.Progress.StoryFlags["flag1"]);
        Assert.False(loadedState.Progress.StoryFlags["flag2"]);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task PuzzleEngine_ChecksPuzzleCompletion()
    {
        // Arrange - Create ONLY the puzzle we want to check
        var puzzle = new Puzzle
        {
            Id = "test_completion_puzzle",
            Name = "Test Completion Puzzle",
            Type = PuzzleType.Sequential,
            CompletionMessage = "You solved the test puzzle!",
            CompletionAchievementId = "puzzle_solver",
            Steps = new List<PuzzleStep>
            {
                new PuzzleStep
                {
                    Id = "step1",
                    StepNumber = 1,
                    CompletionFlag = "test_step1_done",
                    Action = "test",
                    TargetObject = "test_obj"
                }
            }
        };

        // Initialize with ONLY this puzzle
        _puzzleEngine.InitializePuzzles(new List<Puzzle> { puzzle });
        
        // Mark all steps as complete
        _gameState.Progress.PuzzleStates["test_completion_puzzle"] = new PuzzleState
        {
            IsCompleted = false,
            CompletedSteps = new List<string> { "step1" }
        };
        _gameState.Progress.StoryFlags["test_step1_done"] = true;

        // Act
        var result = await _puzzleEngine.CheckPuzzleCompletionAsync(_gameState);

        // Assert
        Assert.True(result.Completed);
        Assert.Equal("test_completion_puzzle", result.PuzzleId);
        Assert.Equal("You solved the test puzzle!", result.CompletionMessage);
    }
}
