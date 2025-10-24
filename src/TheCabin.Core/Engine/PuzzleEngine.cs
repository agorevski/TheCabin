using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine;

/// <summary>
/// Manages puzzle logic and completion checking
/// </summary>
public class PuzzleEngine : IPuzzleEngine
{
    private readonly Dictionary<string, Func<GameState, bool>> _puzzleCheckers = new();
    private readonly Dictionary<string, Puzzle> _puzzles = new();
    private readonly IAchievementService? _achievementService;
    
    public PuzzleEngine(IAchievementService? achievementService = null)
    {
        _achievementService = achievementService;
        
        // Register built-in puzzle types
        RegisterBuiltInPuzzles();
    }
    
    /// <summary>
    /// Initializes puzzles from a story pack
    /// </summary>
    public void InitializePuzzles(List<Puzzle> puzzles)
    {
        _puzzles.Clear();
        foreach (var puzzle in puzzles)
        {
            _puzzles[puzzle.Id] = puzzle;
        }
    }
    
    /// <summary>
    /// Checks if any puzzle conditions have been met
    /// </summary>
    public async Task<PuzzleResult> CheckPuzzleCompletionAsync(GameState gameState)
    {
        foreach (var puzzle in _puzzleCheckers)
        {
            // Skip if already completed
            if (gameState.Progress.CompletedPuzzles.Contains(puzzle.Key))
                continue;
            
            // Check if puzzle is solved
            if (puzzle.Value(gameState))
            {
                // Mark as completed
                gameState.Progress.CompletedPuzzles.Add(puzzle.Key);
                gameState.Player.Stats.PuzzlesSolved++;
                
                // Track achievement
                if (_achievementService != null)
                {
                    await _achievementService.TrackEventAsync(
                        TriggerType.PuzzleSolved,
                        puzzle.Key,
                        gameState);
                }
                
                return new PuzzleResult
                {
                    Completed = true,
                    PuzzleId = puzzle.Key,
                    CompletionMessage = GetCompletionMessage(puzzle.Key),
                    Reward = GetReward(puzzle.Key)
                };
            }
        }
        
        return new PuzzleResult { Completed = false };
    }
    
    /// <summary>
    /// Registers a custom puzzle checker
    /// </summary>
    public void RegisterPuzzle(string puzzleId, Func<GameState, bool> checker)
    {
        _puzzleCheckers[puzzleId] = checker ?? throw new ArgumentNullException(nameof(checker));
    }
    
    /// <summary>
    /// Registers built-in puzzle types
    /// </summary>
    private void RegisterBuiltInPuzzles()
    {
        // Lantern puzzle - light source in dark room
        RegisterPuzzle("lantern_puzzle", state =>
        {
            var currentRoom = state.World.Rooms.GetValueOrDefault(state.Player.CurrentLocationId);
            if (currentRoom == null || currentRoom.LightLevel != LightLevel.Dark)
                return false;
            
            // Check if player has a lit light source
            return state.Player.Inventory.Items.Any(i =>
                i.Type == ObjectType.Light &&
                i.State.CurrentState == "lit");
        });
        
        // Collection puzzle - gather specific items
        RegisterPuzzle("collection_puzzle", state =>
        {
            var requiredItems = new[] { "key", "map", "compass" };
            return requiredItems.All(itemId =>
                state.Player.Inventory.Items.Any(i => i.Id.Contains(itemId)));
        });
        
        // Room exploration puzzle - visit all rooms
        RegisterPuzzle("exploration_puzzle", state =>
        {
            return state.World.Rooms.Values.All(r => r.IsVisited);
        });
    }
    
    /// <summary>
    /// Gets completion message for a puzzle
    /// </summary>
    private string GetCompletionMessage(string puzzleId)
    {
        return puzzleId switch
        {
            "lantern_puzzle" => "The light reveals hidden details you couldn't see before!",
            "collection_puzzle" => "You've gathered all the essential items!",
            "exploration_puzzle" => "You've thoroughly explored every corner of this place.",
            _ => "You've solved a puzzle!"
        };
    }
    
    /// <summary>
    /// Gets reward for completing a puzzle
    /// </summary>
    private string? GetReward(string puzzleId)
    {
        return puzzleId switch
        {
            "lantern_puzzle" => "hidden_passage_revealed",
            "collection_puzzle" => "final_door_unlocked",
            "exploration_puzzle" => "achievement_unlocked",
            _ => null
        };
    }
    
    /// <summary>
    /// Attempts to complete a puzzle step
    /// </summary>
    public async Task<PuzzleStepResult> AttemptStepAsync(string puzzleId, ParsedCommand command, GameState gameState)
    {
        if (!_puzzles.TryGetValue(puzzleId, out var puzzle))
        {
            return new PuzzleStepResult
            {
                Success = false,
                Message = "Puzzle not found."
            };
        }
        
        // Get or create puzzle state
        if (!gameState.Progress.PuzzleStates.TryGetValue(puzzleId, out var puzzleState))
        {
            puzzleState = new PuzzleState
            {
                PuzzleId = puzzleId,
                IsActive = true,
                StartedAt = DateTime.UtcNow
            };
            gameState.Progress.PuzzleStates[puzzleId] = puzzleState;
        }
        
        // Check if already completed
        if (puzzleState.IsCompleted)
        {
            return new PuzzleStepResult
            {
                Success = false,
                Message = "This puzzle has already been solved."
            };
        }
        
        // Find matching step
        PuzzleStep? matchingStep = null;
        
        if (puzzle.Type == PuzzleType.Sequential)
        {
            // Sequential: must complete in order
            var nextStepIndex = puzzleState.CompletedSteps.Count;
            if (nextStepIndex < puzzle.Steps.Count)
            {
                var nextStep = puzzle.Steps[nextStepIndex];
                if (MatchesStep(nextStep, command))
                {
                    matchingStep = nextStep;
                }
            }
        }
        else
        {
            // Combinatorial: any order
            matchingStep = puzzle.Steps
                .Where(s => !puzzleState.CompletedSteps.Contains(s.Id))
                .FirstOrDefault(s => MatchesStep(s, command));
        }
        
        if (matchingStep == null)
        {
            return new PuzzleStepResult
            {
                Success = false,
                Message = "That doesn't seem to help with this puzzle."
            };
        }
        
        // Check step conditions
        if (!CheckStepConditions(matchingStep, gameState))
        {
            return new PuzzleStepResult
            {
                Success = false,
                Message = matchingStep.FailureMessage ?? "You can't do that yet.",
                AttemptedStep = matchingStep
            };
        }
        
        // Complete the step
        puzzleState.CompletedSteps.Add(matchingStep.Id);
        puzzleState.LastActivityAt = DateTime.UtcNow;
        
        // Check if puzzle is complete
        var puzzleCompleted = puzzleState.CompletedSteps.Count == puzzle.Steps.Count;
        
        if (puzzleCompleted)
        {
            puzzleState.IsCompleted = true;
            puzzleState.CompletedAt = DateTime.UtcNow;
            gameState.Progress.CompletedPuzzles.Add(puzzleId);
            gameState.Player.Stats.PuzzlesSolved++;
            
            // Track achievement
            if (_achievementService != null)
            {
                await _achievementService.TrackEventAsync(
                    TriggerType.PuzzleSolved,
                    puzzleId,
                    gameState);
            }
        }
        
        return new PuzzleStepResult
        {
            Success = true,
            Message = matchingStep.SuccessMessage ?? "Progress made on the puzzle.",
            AttemptedStep = matchingStep,
            PuzzleCompleted = puzzleCompleted,
            AchievementUnlocked = puzzleCompleted ? puzzle.AchievementId : null
        };
    }
    
    /// <summary>
    /// Gets the current state of a puzzle
    /// </summary>
    public PuzzleState? GetPuzzleState(string puzzleId, GameState gameState)
    {
        return gameState.Progress.PuzzleStates.GetValueOrDefault(puzzleId);
    }
    
    /// <summary>
    /// Gets all active puzzles
    /// </summary>
    public List<Puzzle> GetActivePuzzles(GameState gameState)
    {
        return _puzzles.Values
            .Where(p => !gameState.Progress.CompletedPuzzles.Contains(p.Id))
            .ToList();
    }
    
    /// <summary>
    /// Gets available hints for a puzzle
    /// </summary>
    public List<Hint> GetAvailableHints(string puzzleId, GameState gameState)
    {
        if (!_puzzles.TryGetValue(puzzleId, out var puzzle))
            return new List<Hint>();
        
        var puzzleState = GetPuzzleState(puzzleId, gameState);
        if (puzzleState == null)
            return new List<Hint>();
        
        var elapsedTime = DateTime.UtcNow - puzzleState.StartedAt;
        
        return puzzle.Hints
            .Where(h => h.DelayMinutes == 0 || elapsedTime.TotalMinutes >= h.DelayMinutes)
            .OrderBy(h => h.Order)
            .ToList();
    }
    
    /// <summary>
    /// Checks if a puzzle step's conditions are met
    /// </summary>
    public bool CheckStepConditions(PuzzleStep step, GameState gameState)
    {
        // Check required items
        foreach (var itemId in step.RequiredItems)
        {
            if (!gameState.Player.Inventory.Items.Any(i => i.Id == itemId))
                return false;
        }
        
        // Check required flags
        foreach (var flag in step.RequiredFlags)
        {
            if (!gameState.Progress.StoryFlags.GetValueOrDefault(flag, false))
                return false;
        }
        
        // Check required location
        if (!string.IsNullOrEmpty(step.RequiredLocation) &&
            gameState.Player.CurrentLocationId != step.RequiredLocation)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if a command matches a puzzle step
    /// </summary>
    private bool MatchesStep(PuzzleStep step, ParsedCommand command)
    {
        // Check verb
        if (!string.IsNullOrEmpty(step.Action) &&
            !step.Action.Equals(command.Verb, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        
        // Check target object
        if (!string.IsNullOrEmpty(step.TargetObject) &&
            !step.TargetObject.Equals(command.Object, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        
        return true;
    }
}
