using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine;

/// <summary>
/// Manages puzzle logic and completion checking
/// </summary>
public class PuzzleEngine : IPuzzleEngine
{
    private readonly Dictionary<string, Func<GameState, bool>> _puzzleCheckers = new();
    private readonly IAchievementService? _achievementService;
    
    public PuzzleEngine(IAchievementService? achievementService = null)
    {
        _achievementService = achievementService;
        
        // Register built-in puzzle types
        RegisterBuiltInPuzzles();
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
}
