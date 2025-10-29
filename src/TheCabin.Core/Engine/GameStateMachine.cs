using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine;

/// <summary>
/// Core game state machine that manages world state, room transitions, and game flow
/// </summary>
public class GameStateMachine : IGameStateMachine
{
    private readonly IInventoryManager _inventoryManager;
    private readonly IAchievementService? _achievementService;
    
    /// <summary>
    /// Current game state
    /// </summary>
    public GameState CurrentState { get; private set; }
    
    public GameStateMachine(
        IInventoryManager inventoryManager, 
        IAchievementService? achievementService = null)
    {
        _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
        _achievementService = achievementService; // Optional dependency
        CurrentState = new GameState();
    }
    
    /// <summary>
    /// Initializes a new game from a story pack (synchronous version for backward compatibility)
    /// </summary>
    public void Initialize(StoryPack storyPack)
    {
        InitializeAsync(storyPack).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Initializes a new game from a story pack with achievement tracking
    /// </summary>
    public async Task InitializeAsync(StoryPack storyPack)
    {
        if (storyPack == null)
            throw new ArgumentNullException(nameof(storyPack));
        
        if (string.IsNullOrEmpty(storyPack.StartingRoomId))
            throw new InvalidOperationException("Story pack must have a starting room ID");
        
        if (!storyPack.Rooms.Any(r => r.Id == storyPack.StartingRoomId))
            throw new InvalidOperationException($"Starting room '{storyPack.StartingRoomId}' not found in story pack");
        
        CurrentState = new GameState
        {
            SaveName = $"New Game - {storyPack.Theme}",
            Player = new Player
            {
                CurrentLocationId = storyPack.StartingRoomId,
                Health = 100,
                MaxHealth = 100
            },
            World = new WorldState
            {
                CurrentThemeId = storyPack.Id,
                Rooms = storyPack.Rooms.ToDictionary(r => r.Id, r => 
                {
                    // Initialize room state with visible objects from story pack
                    if (r.State.VisibleObjectIds == null || !r.State.VisibleObjectIds.Any())
                    {
                        r.State.VisibleObjectIds = new List<string>(r.ObjectIds ?? new List<string>());
                    }
                    return r;
                }),
                Objects = new Dictionary<string, GameObject>(storyPack.Objects),
                TurnNumber = 0
            },
            Progress = new ProgressState(),
            Meta = new MetaState
            {
                Version = "1.0.0",
                SaveTimestamp = DateTime.UtcNow
            }
        };
        
        // Initialize achievements if available
        if (_achievementService != null && storyPack.Achievements != null && storyPack.Achievements.Any())
        {
            await _achievementService.InitializeAsync(storyPack.Achievements);
        }
        
        // Add initial narrative entry
        var startingRoom = GetCurrentRoom();
        CurrentState.StoryLog.Add(new NarrativeEntry
        {
            Text = startingRoom.Description,
            Type = NarrativeType.Description
        });
        
        // Mark starting room as visited
        startingRoom.IsVisited = true;
        
        // Track initial room visit
        if (_achievementService != null)
        {
            await _achievementService.TrackEventAsync(
                TriggerType.RoomVisited, 
                storyPack.StartingRoomId, 
                CurrentState);
        }
    }
    
    /// <summary>
    /// Gets the room the player is currently in
    /// </summary>
    public Room GetCurrentRoom()
    {
        if (CurrentState.World.Rooms.TryGetValue(CurrentState.Player.CurrentLocationId, out var room))
        {
            return room;
        }
        
        throw new InvalidOperationException($"Current location '{CurrentState.Player.CurrentLocationId}' not found");
    }
    
    /// <summary>
    /// Gets all objects visible in the current room
    /// </summary>
    public List<GameObject> GetVisibleObjects()
    {
        var room = GetCurrentRoom();
        var visibleObjects = new List<GameObject>();
        
        foreach (var objectId in room.State.VisibleObjectIds)
        {
            if (CurrentState.World.Objects.TryGetValue(objectId, out var obj) && obj.IsVisible)
            {
                visibleObjects.Add(obj);
            }
        }
        
        return visibleObjects;
    }
    
    /// <summary>
    /// Gets all available exits from the current room
    /// </summary>
    public Dictionary<string, string> GetAvailableExits()
    {
        var room = GetCurrentRoom();
        return new Dictionary<string, string>(room.Exits);
    }
    
    /// <summary>
    /// Checks if the player can transition to the specified room
    /// </summary>
    public bool CanTransitionTo(string roomId)
    {
        var currentRoom = GetCurrentRoom();
        
        // Check if exit exists
        if (!currentRoom.Exits.ContainsValue(roomId))
            return false;
        
        // Check if target room exists
        if (!CurrentState.World.Rooms.TryGetValue(roomId, out var targetRoom))
            return false;
        
        // Check if room is locked
        if (targetRoom.State.IsLocked)
        {
            return CheckUnlockConditions(targetRoom);
        }
        
        return true;
    }
    
    /// <summary>
    /// Transitions the player to a new room (synchronous version for backward compatibility)
    /// </summary>
    public void TransitionTo(string roomId)
    {
        TransitionToAsync(roomId).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Transitions the player to a new room with achievement tracking
    /// </summary>
    public async Task TransitionToAsync(string roomId)
    {
        if (!CanTransitionTo(roomId))
        {
            throw new InvalidOperationException($"Cannot transition to room '{roomId}'");
        }
        
        var previousLocation = CurrentState.Player.CurrentLocationId;
        CurrentState.Player.CurrentLocationId = roomId;
        
        var newRoom = CurrentState.World.Rooms[roomId];
        
        // Mark room as visited and update stats
        if (!newRoom.IsVisited)
        {
            newRoom.IsVisited = true;
            CurrentState.Player.Stats.RoomsExplored++;
            
            // Track room visited achievement
            if (_achievementService != null)
            {
                await _achievementService.TrackEventAsync(
                    TriggerType.RoomVisited, 
                    roomId, 
                    CurrentState);
            }
        }
        
        // Increment turn counter
        CurrentState.World.TurnNumber++;
        
        // Update play time
        CurrentState.Player.Stats.PlayTime = DateTime.UtcNow - CurrentState.Meta.SaveTimestamp;
    }
    
    /// <summary>
    /// Finds an object by ID or partial name match
    /// </summary>
    public GameObject? FindObject(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;
        
        identifier = identifier.ToLowerInvariant();
        
        // Try exact ID match first
        if (CurrentState.World.Objects.TryGetValue(identifier, out var exactMatch))
        {
            return exactMatch;
        }
        
        // Try partial name match
        return CurrentState.World.Objects.Values
            .FirstOrDefault(obj => 
                obj.Id.ToLowerInvariant().Contains(identifier) ||
                obj.Name.ToLowerInvariant().Contains(identifier));
    }
    
    /// <summary>
    /// Finds a visible object in the current room
    /// </summary>
    public GameObject? FindVisibleObject(string identifier)
    {
        var visibleObjects = GetVisibleObjects();
        identifier = identifier.ToLowerInvariant();
        
        return visibleObjects.FirstOrDefault(obj =>
        {
            var objId = obj.Id.ToLowerInvariant();
            var objName = obj.Name.ToLowerInvariant();
            
            // Check both directions for flexible matching:
            // 1. Object ID/name contains the identifier (e.g., "fuel_can" contains "fuel")
            // 2. Identifier contains the object ID/name (e.g., "insulated coat" contains "coat")
            return objId.Contains(identifier) || 
                   objName.Contains(identifier) ||
                   identifier.Contains(objId) ||
                   identifier.Contains(objName);
        });
    }
    
    /// <summary>
    /// Checks if conditions are met to unlock a room
    /// </summary>
    private bool CheckUnlockConditions(Room room)
    {
        // Check if a key is required
        if (room.State.Flags.GetValueOrDefault("requires_key", false))
        {
            // Key ID would be stored in a separate metadata dictionary on the room
            // For now, check if player has any key-type item
            var keyItems = _inventoryManager.GetAllItems()
                .Where(i => i.Type == ObjectType.Item && i.Id.Contains("key"))
                .ToList();
            
            return keyItems.Any();
        }
        
        // Check for other unlock conditions via story flags
        if (room.State.Flags.GetValueOrDefault("requires_flag", false))
        {
            // The flag name would need to be in metadata - simplified for now
            return true; // Placeholder - would check specific flag
        }
        
        return false;
    }
    
    /// <summary>
    /// Adds a narrative entry to the story log
    /// </summary>
    public void AddNarrativeEntry(string text, NarrativeType type, bool isImportant = false)
    {
        var entry = new NarrativeEntry
        {
            Text = text,
            Type = type,
            IsImportant = isImportant,
            Timestamp = DateTime.Now
        };
        
        CurrentState.StoryLog.Add(entry);
        
        // Keep log size manageable (last 100 entries)
        while (CurrentState.StoryLog.Count > 100)
        {
            CurrentState.StoryLog.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Updates player health
    /// </summary>
    public void ModifyHealth(int amount)
    {
        CurrentState.Player.Health = Math.Clamp(
            CurrentState.Player.Health + amount,
            0,
            CurrentState.Player.MaxHealth
        );
    }
    
    /// <summary>
    /// Sets a story flag
    /// </summary>
    public void SetStoryFlag(string flagName, bool value)
    {
        CurrentState.Progress.StoryFlags[flagName] = value;
    }
    
    /// <summary>
    /// Gets a story flag value
    /// </summary>
    public bool GetStoryFlag(string flagName)
    {
        return CurrentState.Progress.StoryFlags.GetValueOrDefault(flagName, false);
    }
}
