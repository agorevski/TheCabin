using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Services;

/// <summary>
/// Service for managing game state operations
/// </summary>
public class GameStateService : IGameStateService
{
    private GameState? _currentState;
    private readonly object _stateLock = new();
    private readonly IGameSaveRepository _saveRepository;
    private readonly IStoryPackService _storyPackService;

    public GameStateService(
        IGameSaveRepository saveRepository,
        IStoryPackService storyPackService)
    {
        _saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
        _storyPackService = storyPackService ?? throw new ArgumentNullException(nameof(storyPackService));
    }

    /// <summary>
    /// Gets the current game state
    /// </summary>
    public GameState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                if (_currentState == null)
                    throw new InvalidOperationException("No active game. Start a new game or load a saved game.");
                return _currentState;
            }
        }
    }

    /// <summary>
    /// Initializes a new game with the specified story pack
    /// </summary>
    public Task InitializeNewGameAsync(StoryPack storyPack)
    {
        if (storyPack == null)
            throw new ArgumentNullException(nameof(storyPack));

        lock (_stateLock)
        {
            _currentState = new GameState
            {
                Id = Guid.NewGuid(),
                SaveName = $"New Game - {storyPack.Theme}",
                Player = new Player
                {
                    Name = "Adventurer",
                    CurrentLocationId = storyPack.StartingRoomId,
                    Health = 100,
                    MaxHealth = 100,
                    CarryCapacity = 20,
                    Stats = new PlayerStats
                    {
                        StartTime = DateTime.UtcNow
                    }
                },
                World = new WorldState
                {
                    CurrentThemeId = storyPack.Id,
                    Rooms = storyPack.Rooms.ToDictionary(r => r.Id, r => r),
                    Objects = new Dictionary<string, GameObject>(storyPack.Objects),
                    TurnNumber = 0
                },
                Progress = new ProgressState(),
                Meta = new MetaState
                {
                    Version = "1.0.0"
                }
            };

            // Add initial narrative entry
            var startRoom = GetStartingRoom();
            _currentState.StoryLog.Add(new NarrativeEntry
            {
                Text = startRoom.Description,
                Type = NarrativeType.Description,
                Timestamp = DateTime.Now
            });
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the current game state
    /// </summary>
    public async Task<int> SaveGameAsync(string saveName)
    {
        GameState stateToSave;

        lock (_stateLock)
        {
            if (_currentState == null)
                throw new InvalidOperationException("No active game to save");

            _currentState.Meta.SaveTimestamp = DateTime.UtcNow;
            _currentState.Meta.SaveCount++;
            _currentState.SaveName = saveName;

            stateToSave = _currentState;
        }

        return await _saveRepository.SaveAsync(saveName, stateToSave);
    }

    /// <summary>
    /// Loads a saved game by ID
    /// </summary>
    public async Task LoadGameAsync(int saveId)
    {
        var gameState = await _saveRepository.LoadAsync(saveId);

        if (gameState == null)
            throw new InvalidOperationException($"Save game not found: {saveId}");

        lock (_stateLock)
        {
            _currentState = gameState;
        }
    }

    /// <summary>
    /// Makes <paramref name="state"/> the active game state. Used so saves write the same instance the state machine plays on.
    /// </summary>
    public void SetCurrentState(GameState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        lock (_stateLock)
        {
            _currentState = state;
        }
    }

    /// <summary>
    /// Gets a list of all saved games
    /// </summary>
    public async Task<List<GameSaveInfo>> GetSavedGamesAsync()
    {
        return await _saveRepository.GetAllAsync();
    }

    /// <summary>
    /// Deletes a saved game
    /// </summary>
    public async Task DeleteSaveAsync(int saveId)
    {
        await _saveRepository.DeleteAsync(saveId);
    }

    // Internal helper methods

    internal Room GetCurrentRoom()
    {
        lock (_stateLock)
        {
            if (_currentState == null)
                throw new InvalidOperationException("No active game state");

            var roomId = _currentState.Player.CurrentLocationId;
            if (!_currentState.World.Rooms.TryGetValue(roomId, out var room))
                throw new InvalidOperationException($"Room not found: {roomId}");

            return room;
        }
    }

    internal void AddNarrativeEntry(string text, NarrativeType type)
    {
        lock (_stateLock)
        {
            if (_currentState == null)
                throw new InvalidOperationException("No active game state");

            _currentState.StoryLog.Add(new NarrativeEntry
            {
                Text = text,
                Type = type,
                Timestamp = DateTime.Now
            });

            // Keep only last 100 entries for performance
            if (_currentState.StoryLog.Count > 100)
            {
                _currentState.StoryLog.RemoveAt(0);
            }
        }
    }

    private Room GetStartingRoom()
    {
        if (_currentState == null)
            throw new InvalidOperationException("No active game state");

        var roomId = _currentState.Player.CurrentLocationId;
        return _currentState.World.Rooms[roomId];
    }
}
