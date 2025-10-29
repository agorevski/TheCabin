using Microsoft.Extensions.Logging;
using TheCabin.Core.Engine;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Services;

/// <summary>
/// Result of game initialization
/// </summary>
public class InitializationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string InitialRoomDescription { get; set; } = string.Empty;
    public IEnumerable<string> VisibleObjects { get; set; } = new List<string>();
    public IEnumerable<string> Exits { get; set; } = new List<string>();
}

/// <summary>
/// Centralizes game initialization logic shared between Console and MAUI
/// </summary>
public class GameInitializationService
{
    private readonly ILogger<GameInitializationService>? _logger;

    public GameInitializationService(ILogger<GameInitializationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize a new game with the provided story pack
    /// </summary>
    public async Task<InitializationResult> InitializeNewGameAsync(
        StoryPack storyPack,
        IGameStateMachine stateMachine,
        IPuzzleEngine puzzleEngine,
        IAchievementService? achievementService,
        IGameStateService gameStateService)
    {
        try
        {
            _logger?.LogInformation("Initializing new game with story pack: {Theme}", storyPack.Theme);

            // Initialize puzzle engine with puzzles from story pack
            if (storyPack.Puzzles != null && storyPack.Puzzles.Any())
            {
                puzzleEngine.InitializePuzzles(storyPack.Puzzles);
                _logger?.LogInformation("✓ Initialized {Count} puzzle(s)", storyPack.Puzzles.Count);
            }
            else
            {
                _logger?.LogInformation("No puzzles defined in story pack");
            }

            // Initialize achievements if available
            if (achievementService != null && storyPack.Achievements != null && storyPack.Achievements.Any())
            {
                await achievementService.InitializeAsync(storyPack.Achievements);
                _logger?.LogInformation("✓ Initialized {Count} achievement(s)", storyPack.Achievements.Count);
            }
            else
            {
                _logger?.LogInformation("No achievements or achievement service unavailable");
            }

            // Initialize game state
            await gameStateService.InitializeNewGameAsync(storyPack);
            _logger?.LogInformation("✓ Game state initialized");

            // Initialize state machine
            await stateMachine.InitializeAsync(storyPack);
            _logger?.LogInformation("✓ State machine initialized");

            // Get initial room information
            var currentState = gameStateService.CurrentState;
            var initialRoom = currentState.World.Rooms[currentState.Player.CurrentLocationId];

            // Get visible objects
            var visibleObjects = initialRoom.State.VisibleObjectIds
                .Where(id => currentState.World.Objects.ContainsKey(id))
                .Select(id => currentState.World.Objects[id])
                .Where(obj => obj.IsVisible)
                .Select(obj => obj.Name)
                .ToList();

            // Get exits
            var exits = initialRoom.Exits.Keys.ToList();

            _logger?.LogInformation("Game initialized successfully: {Theme} (Pack: {PackId})", 
                storyPack.Theme, storyPack.Id);

            return new InitializationResult
            {
                Success = true,
                InitialRoomDescription = initialRoom.Description,
                VisibleObjects = visibleObjects,
                Exits = exits
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize game with story pack: {Theme}", storyPack.Theme);
            return new InitializationResult
            {
                Success = false,
                ErrorMessage = $"Failed to initialize game: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Load a saved game
    /// </summary>
    public async Task<InitializationResult> LoadSavedGameAsync(
        int saveId,
        IGameStateMachine stateMachine,
        IPuzzleEngine puzzleEngine,
        IAchievementService? achievementService,
        IGameStateService gameStateService,
        IStoryPackService storyPackService)
    {
        try
        {
            _logger?.LogInformation("Loading saved game: {SaveId}", saveId);

            // Load the saved game state
            await gameStateService.LoadGameAsync(saveId);
            var currentState = gameStateService.CurrentState;

            // Re-load the story pack
            var storyPack = await storyPackService.LoadPackAsync(currentState.World.CurrentThemeId);
            _logger?.LogInformation("✓ Story pack loaded: {Theme}", storyPack.Theme);

            // Re-initialize puzzle engine with puzzles
            if (storyPack.Puzzles != null && storyPack.Puzzles.Any())
            {
                puzzleEngine.InitializePuzzles(storyPack.Puzzles);
                _logger?.LogInformation("✓ Re-initialized {Count} puzzle(s)", storyPack.Puzzles.Count);
            }

            // Re-initialize achievements if available
            if (achievementService != null && storyPack.Achievements != null && storyPack.Achievements.Any())
            {
                await achievementService.InitializeAsync(storyPack.Achievements);
                _logger?.LogInformation("✓ Re-initialized {Count} achievement(s)", storyPack.Achievements.Count);
            }

            // Re-initialize state machine
            await stateMachine.InitializeAsync(storyPack);

            // Restore player location in state machine
            if (!string.IsNullOrEmpty(currentState.Player.CurrentLocationId))
            {
                stateMachine.CurrentState.Player.CurrentLocationId = currentState.Player.CurrentLocationId;
            }

            _logger?.LogInformation("✓ State machine restored");

            // Get current room information
            var currentRoom = currentState.World.Rooms[currentState.Player.CurrentLocationId];

            // Get visible objects
            var visibleObjects = currentRoom.State.VisibleObjectIds
                .Where(id => currentState.World.Objects.ContainsKey(id))
                .Select(id => currentState.World.Objects[id])
                .Where(obj => obj.IsVisible)
                .Select(obj => obj.Name)
                .ToList();

            // Get exits
            var exits = currentRoom.Exits.Keys.ToList();

            _logger?.LogInformation("Game loaded successfully from save {SaveId}", saveId);

            return new InitializationResult
            {
                Success = true,
                InitialRoomDescription = currentRoom.Description,
                VisibleObjects = visibleObjects,
                Exits = exits
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load game from save {SaveId}", saveId);
            return new InitializationResult
            {
                Success = false,
                ErrorMessage = $"Failed to load game: {ex.Message}"
            };
        }
    }
}
