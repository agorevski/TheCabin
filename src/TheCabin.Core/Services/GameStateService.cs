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
    
    internal void UpdateState(CommandResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));
        
        lock (_stateLock)
        {
            if (_currentState == null)
                throw new InvalidOperationException("No active game state");
            
            // Add result message to story log
            _currentState.StoryLog.Add(new NarrativeEntry
            {
                Text = result.Message,
                Type = result.Success ? NarrativeType.Success : NarrativeType.Failure,
                Timestamp = DateTime.Now,
                IsImportant = !result.Success
            });
            
            // Apply state changes if any
            if (result.StateChange != null)
            {
                ApplyStateChanges(result.StateChange);
            }
            
            // Increment turn counter
            _currentState.World.TurnNumber++;
            
            // Update play time
            _currentState.Player.Stats.PlayTime = 
                DateTime.UtcNow - _currentState.Player.Stats.StartTime;
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
    
    private void ApplyStateChanges(GameStateChange changes)
    {
        if (_currentState == null)
            return;
        
        // Location change
        if (!string.IsNullOrEmpty(changes.LocationChanged))
        {
            _currentState.Player.CurrentLocationId = changes.LocationChanged;
            
            // Mark room as visited and increment counter if first visit
            if (_currentState.World.Rooms.TryGetValue(changes.LocationChanged, out var room))
            {
                if (!room.IsVisited)
                {
                    room.IsVisited = true;
                    _currentState.Player.Stats.RoomsExplored++;
                }
            }
        }
        
        // Items added to inventory
        foreach (var itemId in changes.ItemsAdded)
        {
            if (_currentState.World.Objects.TryGetValue(itemId, out var obj))
            {
                _currentState.Player.Inventory.Items.Add(obj);
                _currentState.Player.Inventory.TotalWeight += obj.Weight;
                _currentState.Player.Stats.ItemsCollected++;
            }
        }
        
        // Items removed from inventory
        foreach (var itemId in changes.ItemsRemoved)
        {
            var item = _currentState.Player.Inventory.Items
                .FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                _currentState.Player.Inventory.Items.Remove(item);
                _currentState.Player.Inventory.TotalWeight -= item.Weight;
            }
        }
        
        // Health change
        if (changes.HealthChange != 0)
        {
            _currentState.Player.Health = Math.Max(0, 
                Math.Min(_currentState.Player.MaxHealth, 
                    _currentState.Player.Health + changes.HealthChange));
        }
        
        // Flags changed
        foreach (var flag in changes.FlagsChanged)
        {
            _currentState.Progress.StoryFlags[flag.Key] = flag.Value;
        }
    }
}
