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
    private readonly GameStateMachine? _stateMachine;
    
    public PuzzleEngine(IAchievementService? achievementService = null, GameStateMachine? stateMachine = null)
    {
        _achievementService = achievementService;
        _stateMachine = stateMachine;
        
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
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] === AttemptStepAsync Called ===");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Puzzle ID: {puzzleId}");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Command Verb: '{command.Verb}'");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Command Object: '{command.Object}'");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Command Target: '{command.Target}'");
        
        if (!_puzzles.TryGetValue(puzzleId, out var puzzle))
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✗ Puzzle '{puzzleId}' not found in puzzle dictionary");
            return new PuzzleStepResult
            {
                Success = false,
                Message = "Puzzle not found."
            };
        }
        
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Puzzle Type: {puzzle.Type}");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Total Steps: {puzzle.Steps.Count}");
        
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
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Created new puzzle state");
        }
        
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Completed Steps: {puzzleState.CompletedSteps.Count}");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Completed Step IDs: [{string.Join(", ", puzzleState.CompletedSteps)}]");
        
        // Check if already completed
        if (puzzleState.IsCompleted)
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✗ Puzzle already completed");
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
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Sequential puzzle - checking step {nextStepIndex + 1} of {puzzle.Steps.Count}");
            
            if (nextStepIndex < puzzle.Steps.Count)
            {
                var nextStep = puzzle.Steps[nextStepIndex];
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Next expected step: '{nextStep.Id}' (Step {nextStep.StepNumber})");
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine]   Expected Action: '{nextStep.Action}'");
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine]   Expected Target: '{nextStep.TargetObject}'");
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine]   Required Location: '{nextStep.RequiredLocation}'");
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine]   Required Flags: [{string.Join(", ", nextStep.RequiredFlags)}]");
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine]   Required Items: [{string.Join(", ", nextStep.RequiredItems)}]");
                
                if (MatchesStep(nextStep, command))
                {
                    System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✓ Step matches command!");
                    matchingStep = nextStep;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✗ Step does not match command");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✗ No more steps to complete (index {nextStepIndex} >= count {puzzle.Steps.Count})");
            }
        }
        else
        {
            // Combinatorial: any order
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Combinatorial puzzle - checking all incomplete steps");
            var incompleteSteps = puzzle.Steps.Where(s => !puzzleState.CompletedSteps.Contains(s.Id)).ToList();
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Incomplete steps: {incompleteSteps.Count}");
            
            foreach (var step in incompleteSteps)
            {
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Checking step '{step.Id}': action='{step.Action}', target='{step.TargetObject}'");
                if (MatchesStep(step, command))
                {
                    System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✓ Step '{step.Id}' matches!");
                    matchingStep = step;
                    break;
                }
            }
        }
        
        if (matchingStep == null)
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✗ No matching step found for command '{command.Verb} {command.Object}'");
            return new PuzzleStepResult
            {
                Success = false,
                Message = "That doesn't seem to help with this puzzle."
            };
        }
        
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Found matching step: '{matchingStep.Id}'");
        
        // Check step conditions
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Checking conditions for step '{matchingStep.Id}'");
        if (!CheckStepConditions(matchingStep, gameState))
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✗ Step conditions not met");
            return new PuzzleStepResult
            {
                Success = false,
                Message = matchingStep.FailureMessage ?? "You can't do that yet.",
                AttemptedStep = matchingStep
            };
        }
        
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✓ All step conditions met");
        
        // Complete the step
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✓ Completing step '{matchingStep.Id}'");
        puzzleState.CompletedSteps.Add(matchingStep.Id);
        puzzleState.LastActivityAt = DateTime.UtcNow;
        
        // Check if puzzle is complete
        var puzzleCompleted = puzzleState.CompletedSteps.Count == puzzle.Steps.Count;
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] Steps completed: {puzzleState.CompletedSteps.Count}/{puzzle.Steps.Count}");
        
        if (puzzleCompleted)
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine] ✓✓✓ PUZZLE COMPLETED! ✓✓✓");
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
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] === Checking Conditions ===");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Step ID: '{step.Id}'");
        
        // Check required items
        if (step.RequiredItems.Any())
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Required Items: [{string.Join(", ", step.RequiredItems)}]");
            var playerItems = gameState.Player.Inventory.Items.Select(i => i.Id).ToList();
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Player Inventory: [{string.Join(", ", playerItems)}]");
            
            foreach (var itemId in step.RequiredItems)
            {
                var hasItem = gameState.Player.Inventory.Items.Any(i => i.Id == itemId);
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions]   Item '{itemId}': {(hasItem ? "✓ FOUND" : "✗ MISSING")}");
                
                if (!hasItem)
                {
                    System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] ✗ Missing required item '{itemId}' - returning false");
                    return false;
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] No required items");
        }
        
        // Check required flags
        if (step.RequiredFlags.Any())
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Required Flags: [{string.Join(", ", step.RequiredFlags)}]");
            var activeFlags = gameState.Progress.StoryFlags.Where(f => f.Value).Select(f => f.Key).ToList();
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Active Story Flags: [{string.Join(", ", activeFlags)}]");
            
            foreach (var flag in step.RequiredFlags)
            {
                var hasFlag = gameState.Progress.StoryFlags.GetValueOrDefault(flag, false);
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions]   Flag '{flag}': {(hasFlag ? "✓ SET" : "✗ NOT SET")}");
                
                if (!hasFlag)
                {
                    System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] ✗ Missing required flag '{flag}' - returning false");
                    return false;
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] No required flags");
        }
        
        // Check required location
        if (!string.IsNullOrEmpty(step.RequiredLocation))
        {
            var isInCorrectLocation = gameState.Player.CurrentLocationId == step.RequiredLocation;
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Required Location: '{step.RequiredLocation}'");
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Current Location: '{gameState.Player.CurrentLocationId}'");
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] Location Check: {(isInCorrectLocation ? "✓ CORRECT" : "✗ WRONG")}");
            
            if (!isInCorrectLocation)
            {
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] ✗ Wrong location - returning false");
                return false;
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] No location requirement");
        }
        
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.CheckStepConditions] ✓ All conditions met - returning true");
        return true;
    }
    
    /// <summary>
    /// Checks if a command matches a puzzle step
    /// </summary>
    private bool MatchesStep(PuzzleStep step, ParsedCommand command)
    {
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] === Matching Step ===");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Step ID: '{step.Id}'");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Step Action: '{step.Action}'");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Step TargetObject: '{step.TargetObject}'");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Command Verb: '{command.Verb}'");
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Command Object: '{command.Object}'");
        
        // Check verb
        if (!string.IsNullOrEmpty(step.Action))
        {
            var verbMatches = step.Action.Equals(command.Verb, StringComparison.OrdinalIgnoreCase);
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Verb comparison: '{step.Action}' == '{command.Verb}' ? {verbMatches}");
            
            if (!verbMatches)
            {
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] ✗ Verb mismatch - returning false");
                return false;
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Step action is empty/null - skipping verb check");
        }
        
        // Check target object with flexible matching (similar to FindVisibleObject)
        if (!string.IsNullOrEmpty(step.TargetObject) && !string.IsNullOrEmpty(command.Object))
        {
            var stepTarget = step.TargetObject.ToLowerInvariant();
            var commandObj = command.Object.ToLowerInvariant();
            
            // Try exact match first
            var exactMatch = stepTarget == commandObj;
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Exact match: '{stepTarget}' == '{commandObj}' ? {exactMatch}");
            
            // Try partial match: does the step's target object ID contain the command object?
            var partialMatch = stepTarget.Contains(commandObj);
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Partial match: '{stepTarget}'.Contains('{commandObj}') ? {partialMatch}");
            
            // Also try reverse: does the command object contain the step target? (for cases like "fuel_can" contains "fuel")
            var reverseMatch = commandObj.Contains(stepTarget);
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Reverse match: '{commandObj}'.Contains('{stepTarget}') ? {reverseMatch}");
            
            var objectMatches = exactMatch || partialMatch || reverseMatch;
            
            if (!objectMatches)
            {
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] ✗ Object mismatch - returning false");
                System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] ISSUE: No match between '{step.TargetObject}' and '{command.Object}'");
                return false;
            }
            
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] ✓ Object matched (exact={exactMatch}, partial={partialMatch}, reverse={reverseMatch})");
        }
        else if (!string.IsNullOrEmpty(step.TargetObject))
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] ✗ Step requires target object but command has none");
            return false;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] Step targetObject is empty/null - skipping object check");
        }
        
        System.Diagnostics.Debug.WriteLine($"[PuzzleEngine.MatchesStep] ✓ All checks passed - returning true");
        return true;
    }
}
