using Microsoft.Extensions.Logging;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Core.Services;

namespace TheCabin.Core.Engine;

/// <summary>
/// Orchestrates game flow and command processing
/// Shared between Console and MAUI implementations
/// </summary>
public class GameOrchestrator
{
    private readonly IGameStateService _gameStateService;
    private readonly IStoryPackService _storyPackService;
    private readonly ICommandParserService _commandParser;
    private readonly CommandRouter _commandRouter;
    private readonly IGameStateMachine _stateMachine;
    private readonly IPuzzleEngine _puzzleEngine;
    private readonly IAchievementService? _achievementService;
    private readonly IGameDisplay _display;
    private readonly GameInitializationService _initService;
    private readonly ILogger<GameOrchestrator>? _logger;

    public GameOrchestrator(
        IGameStateService gameStateService,
        IStoryPackService storyPackService,
        ICommandParserService commandParser,
        CommandRouter commandRouter,
        IGameStateMachine stateMachine,
        IPuzzleEngine puzzleEngine,
        IGameDisplay display,
        GameInitializationService initService,
        IAchievementService? achievementService = null,
        ILogger<GameOrchestrator>? logger = null)
    {
        _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
        _storyPackService = storyPackService ?? throw new ArgumentNullException(nameof(storyPackService));
        _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _puzzleEngine = puzzleEngine ?? throw new ArgumentNullException(nameof(puzzleEngine));
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _initService = initService ?? throw new ArgumentNullException(nameof(initService));
        _achievementService = achievementService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize a new game with the specified story pack
    /// </summary>
    public async Task<bool> InitializeGameAsync(string storyPackId)
    {
        try
        {
            _logger?.LogInformation("Loading story pack: {PackId}", storyPackId);

            var storyPack = await _storyPackService.LoadPackAsync(storyPackId);

            var result = await _initService.InitializeNewGameAsync(
                storyPack,
                _stateMachine,
                _puzzleEngine,
                _achievementService,
                _gameStateService);

            if (!result.Success)
            {
                await _display.ShowMessageAsync(
                    result.ErrorMessage ?? "Failed to initialize game",
                    MessageType.Failure);
                return false;
            }

            // Format and display initial room description
            var (displayMessage, _) = RoomDescriptionFormatter.FormatRoomDescription(
                result.InitialRoomDescription,
                result.VisibleObjects,
                result.Exits);

            await _display.ShowRoomDescriptionAsync(
                displayMessage,
                result.VisibleObjects,
                result.Exits);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize game");
            await _display.ShowMessageAsync(
                $"Failed to initialize game: {ex.Message}",
                MessageType.Failure);
            return false;
        }
    }

    /// <summary>
    /// Load a saved game
    /// </summary>
    public async Task<bool> LoadGameAsync(int saveId)
    {
        try
        {
            var result = await _initService.LoadSavedGameAsync(
                saveId,
                _stateMachine,
                _puzzleEngine,
                _achievementService,
                _gameStateService,
                _storyPackService);

            if (!result.Success)
            {
                await _display.ShowMessageAsync(
                    result.ErrorMessage ?? "Failed to load game",
                    MessageType.Failure);
                return false;
            }

            // Show current room
            await _display.ShowMessageAsync(
                result.InitialRoomDescription,
                MessageType.Description);

            await _display.ShowMessageAsync(
                "Game loaded successfully!",
                MessageType.SystemMessage);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load game");
            await _display.ShowMessageAsync(
                $"Failed to load game: {ex.Message}",
                MessageType.Failure);
            return false;
        }
    }

    /// <summary>
    /// Save the current game
    /// </summary>
    public async Task<bool> SaveGameAsync(string saveName)
    {
        try
        {
            await _gameStateService.SaveGameAsync(saveName);
            await _display.ShowMessageAsync(
                $"✓ Game saved as '{saveName}'",
                MessageType.SystemMessage);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save game");
            await _display.ShowMessageAsync(
                $"Failed to save game: {ex.Message}",
                MessageType.Failure);
            return false;
        }
    }

    /// <summary>
    /// Process a command from the player
    /// </summary>
    public async Task ProcessCommandAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        try
        {
            var currentState = _gameStateService.CurrentState;
            if (currentState == null || string.IsNullOrEmpty(currentState.Player?.CurrentLocationId))
            {
                await _display.ShowMessageAsync(
                    "Game not properly initialized. Please start a new game.",
                    MessageType.Failure);
                return;
            }

            // Echo the player's command
            await _display.ShowMessageAsync(
                $"▶ \"{input}\"",
                MessageType.PlayerCommand);

            // Build game context
            var context = BuildGameContext(currentState);

            // Parse the command
            var parsed = await _commandParser.ParseAsync(input, context);
            _logger?.LogInformation("Parsed command: {Verb} {Object}", parsed.Verb, parsed.Object);

            // Execute the command
            var result = await _commandRouter.RouteAsync(parsed);

            // Display the result
            var messageType = result.Success ? MessageType.Success : MessageType.Failure;
            await _display.ShowMessageAsync(result.Message, messageType);

            // The CommandRouter already handles achievement tracking via TrackEventAsync
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing command: {Input}", input);
            await _display.ShowMessageAsync(
                $"Error processing command: {ex.Message}",
                MessageType.Failure);
        }
    }

    /// <summary>
    /// Get the current game state
    /// </summary>
    public GameState GetCurrentState()
    {
        return _gameStateService.CurrentState;
    }

    /// <summary>
    /// Build game context for command parsing
    /// </summary>
    private GameContext BuildGameContext(GameState currentState)
    {
        var room = currentState.World.Rooms[currentState.Player.CurrentLocationId];
        var visibleObjects = room.State.VisibleObjectIds
            .Where(id => currentState.World.Objects.ContainsKey(id))
            .Select(id => currentState.World.Objects[id])
            .Where(obj => obj.IsVisible)
            .ToList();

        return new GameContext
        {
            CurrentLocation = room.Id,
            VisibleObjects = visibleObjects.Select(o => o.Id).ToList(),
            InventoryItems = currentState.Player.Inventory.Items.Select(i => i.Id).ToList(),
            GameFlags = currentState.Progress.StoryFlags,
            RecentCommands = new List<string>()
        };
    }
}
