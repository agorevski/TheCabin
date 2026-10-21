using System.Text.Json;
using TheCabin.Core.Engine;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine;

public class PuzzleEngineTests
{
    [Fact]
    public async Task AttemptStepAsync_WithJsonValueInStateChange_SetsTypedProperty()
    {
        // Story pack JSON deserializes StateChange.NewValue as JsonElement; it must still set a bool property
        var hiddenKey = new GameObject { Id = "iron_key", Name = "Iron Key", IsVisible = false };
        var gameState = new GameState { Player = new Player { CurrentLocationId = "cellar" } };
        gameState.World.Objects["iron_key"] = hiddenKey;

        var step = new PuzzleStep
        {
            Id = "open_chest",
            StepNumber = 1,
            Action = "open",
            TargetObject = "chest",
            StateChanges = new List<StateChange>
            {
                new StateChange
                {
                    Target = "iron_key",
                    Property = "IsVisible",
                    NewValue = JsonSerializer.Deserialize<JsonElement>("true")
                }
            }
        };
        var engine = new PuzzleEngine();
        engine.InitializePuzzles(new List<Puzzle>
        {
            new Puzzle { Id = "escape", Type = PuzzleType.Sequential, Steps = new List<PuzzleStep> { step } }
        });

        var result = await engine.AttemptStepAsync("escape", new ParsedCommand { Verb = "open", Object = "chest" }, gameState);

        Assert.True(result.Success);
        Assert.True(hiddenKey.IsVisible);
    }

    [Fact]
    public async Task AttemptStepAsync_SequentialPuzzle_RequiresStepsInStepNumberOrder()
    {
        // Steps are listed out of order in the puzzle definition
        var gameState = new GameState { Player = new Player { CurrentLocationId = "cellar" } };
        var second = new PuzzleStep { Id = "lamp", StepNumber = 2, Action = "take", TargetObject = "lamp" };
        var first = new PuzzleStep { Id = "key", StepNumber = 1, Action = "take", TargetObject = "key" };
        var engine = new PuzzleEngine();
        engine.InitializePuzzles(new List<Puzzle>
        {
            new Puzzle { Id = "ordered", Type = PuzzleType.Sequential, Steps = new List<PuzzleStep> { second, first } }
        });

        var outOfOrder = await engine.AttemptStepAsync("ordered", new ParsedCommand { Verb = "take", Object = "lamp" }, gameState);
        var inOrder = await engine.AttemptStepAsync("ordered", new ParsedCommand { Verb = "take", Object = "key" }, gameState);

        Assert.False(outOfOrder.Success);
        Assert.True(inOrder.Success);
        Assert.Equal("key", inOrder.AttemptedStep?.Id);
    }
}
