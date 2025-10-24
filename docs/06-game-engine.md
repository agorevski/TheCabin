# 06 - Game Engine

## Overview

The game engine is the core logic system that manages world state, processes commands, handles puzzles, and drives the narrative forward. This document details the engine architecture and implementation.

## Engine Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                      GAME ENGINE                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌────────────────┐      ┌────────────────┐                 │
│  │ State Machine  │◄────►│ Command Router │                 │
│  └────────┬───────┘      └────────┬───────┘                 │
│           │                       │                         │
│           ▼                       ▼                         │
│  ┌────────────────┐      ┌────────────────┐                 │
│  │ World Manager  │      │Action Executor │                 │
│  └────────┬───────┘      └────────┬───────┘                 │
│           │                       │                         │
│           ▼                       ▼                         │
│  ┌────────────────┐      ┌────────────────┐                 │
│  │ Inventory Mgr  │      │ Puzzle Engine  │                 │
│  └────────────────┘      └────────────────┘                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Game State Machine

```csharp
namespace TheCabin.Domain.Engine
{
    public class GameStateMachine
    {
        private readonly IWorldManager _worldManager;
        private readonly IInventoryManager _inventoryManager;
        private readonly IPuzzleEngine _puzzleEngine;
        private readonly ILogger _logger;
        
        public GameState CurrentState { get; private set; }
        
        public GameStateMachine(
            IWorldManager worldManager,
            IInventoryManager inventoryManager,
            IPuzzleEngine puzzleEngine,
            ILogger<GameStateMachine> logger)
        {
            _worldManager = worldManager;
            _inventoryManager = inventoryManager;
            _puzzleEngine = puzzleEngine;
            _logger = logger;
        }
        
        public async Task InitializeAsync(StoryPack storyPack)
        {
            CurrentState = new GameState();
            
            // Initialize world from story pack
            await _worldManager.LoadStoryPackAsync(storyPack);
            
            // Set starting location
            CurrentState.Player.CurrentLocationId = storyPack.StartingRoomId;
            
            // Initialize world state
            CurrentState.World = new WorldState
            {
                CurrentThemeId = storyPack.Id,
                Rooms = storyPack.Rooms.ToDictionary(r => r.Id, r => r),
                Objects = storyPack.Objects
            };
            
            _logger.LogInformation(
                "Game initialized with theme: {Theme}", storyPack.Theme);
        }
        
        public Room GetCurrentRoom()
        {
            return CurrentState.World.Rooms[
                CurrentState.Player.CurrentLocationId];
        }
        
        public List<GameObject> GetVisibleObjects()
        {
            var room = GetCurrentRoom();
            return room.State.VisibleObjectIds
                .Select(id => CurrentState.World.Objects[id])
                .Where(obj => obj.IsVisible)
                .ToList();
        }
        
        public bool CanTransitionTo(string roomId)
        {
            var currentRoom = GetCurrentRoom();
            
            // Check if exit exists
            if (!currentRoom.Exits.ContainsValue(roomId))
                return false;
            
            // Check if room is locked
            var targetRoom = CurrentState.World.Rooms[roomId];
            if (targetRoom.State.IsLocked)
            {
                // Check if player has key or unlock conditions met
                return CheckUnlockConditions(targetRoom);
            }
            
            return true;
        }
        
        public void TransitionTo(string roomId)
        {
            if (!CanTransitionTo(roomId))
            {
                throw new InvalidOperationException(
                    $"Cannot transition to room: {roomId}");
            }
            
            var previousLocation = CurrentState.Player.CurrentLocationId;
            CurrentState.Player.CurrentLocationId = roomId;
            
            // Mark room as visited
            var room = CurrentState.World.Rooms[roomId];
            room.IsVisited = true;
            
            // Update stats
            if (!room.IsVisited)
            {
                CurrentState.Player.Stats.RoomsExplored++;
            }
            
            _logger.LogInformation(
                "Player moved from {From} to {To}",
                previousLocation, roomId);
        }
        
        private bool CheckUnlockConditions(Room room)
        {
            // Check required flags
            if (room.State.Flags.TryGetValue("requires_key", out var requiresKey)
                && requiresKey)
            {
                var keyId = room.State.Flags
                    .GetValueOrDefault("key_id")?.ToString();
                return _inventoryManager.HasItem(keyId);
            }
            
            return false;
        }
    }
}
```

### 2. Command Router

```csharp
namespace TheCabin.Domain.Engine
{
    public class CommandRouter
    {
        private readonly Dictionary<string, ICommandHandler> _handlers;
        private readonly ILogger _logger;
        
        public CommandRouter(
            IEnumerable<ICommandHandler> handlers,
            ILogger<CommandRouter> logger)
        {
            _handlers = handlers.ToDictionary(h => h.Verb, h => h);
            _logger = logger;
        }
        
        public async Task<CommandResult> RouteAsync(
            ParsedCommand command,
            GameState gameState)
        {
            _logger.LogInformation(
                "Routing command: {Verb} {Object}",
                command.Verb, command.Object);
            
            // Find appropriate handler
            if (!_handlers.TryGetValue(command.Verb, out var handler))
            {
                return new CommandResult
                {
                    Success = false,
                    Type = CommandResultType.InvalidCommand,
                    Message = $"I don't understand '{command.Verb}'."
                };
            }
            
            // Validate command
            var validation = await handler.ValidateAsync(command, gameState);
            if (!validation.IsValid)
            {
                return new CommandResult
                {
                    Success = false,
                    Type = CommandResultType.RequirementsNotMet,
                    Message = validation.Message
                };
            }
            
            // Execute command
            try
            {
                var result = await handler.ExecuteAsync(command, gameState);
                
                // Update game stats
                gameState.Player.Stats.CommandsExecuted++;
                gameState.World.TurnNumber++;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command execution failed");
                return new CommandResult
                {
                    Success = false,
                    Type = CommandResultType.Failure,
                    Message = "Something went wrong. Please try again."
                };
            }
        }
    }
}
```

### 3. Command Handlers

#### Movement Handler

```csharp
public class MoveCommandHandler : ICommandHandler
{
    public string Verb => "go";
    
    private readonly IGameStateMachine _stateMachine;
    
    public async Task<ValidationResult> ValidateAsync(
        ParsedCommand command,
        GameState gameState)
    {
        if (string.IsNullOrEmpty(command.Object))
        {
            return ValidationResult.Invalid(
                "Go where? Specify a direction.");
        }
        
        var currentRoom = _stateMachine.GetCurrentRoom();
        var direction = NormalizeDirection(command.Object);
        
        if (!currentRoom.Exits.TryGetValue(direction, out var targetRoomId))
        {
            return ValidationResult.Invalid(
                $"You can't go {direction} from here.");
        }
        
        if (!_stateMachine.CanTransitionTo(targetRoomId))
        {
            return ValidationResult.Invalid(
                "The way is blocked.");
        }
        
        return ValidationResult.Valid();
    }
    
    public async Task<CommandResult> ExecuteAsync(
        ParsedCommand command,
        GameState gameState)
    {
        var currentRoom = _stateMachine.GetCurrentRoom();
        var direction = NormalizeDirection(command.Object);
        var targetRoomId = currentRoom.Exits[direction];
        
        _stateMachine.TransitionTo(targetRoomId);
        
        var newRoom = _stateMachine.GetCurrentRoom();
        
        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = $"You move {direction}.\n\n{newRoom.Description}",
            StateChange = new GameStateChange
            {
                LocationChanged = targetRoomId
            }
        };
    }
    
    private string NormalizeDirection(string input)
    {
        return input.ToLower() switch
        {
            "n" => "north",
            "s" => "south",
            "e" => "east",
            "w" => "west",
            "u" or "upstairs" => "up",
            "d" or "downstairs" => "down",
            _ => input.ToLower()
        };
    }
}
```

#### Take Item Handler

```csharp
public class TakeCommandHandler : ICommandHandler
{
    public string Verb => "take";
    
    private readonly IGameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    
    public async Task<ValidationResult> ValidateAsync(
        ParsedCommand command,
        GameState gameState)
    {
        if (string.IsNullOrEmpty(command.Object))
        {
            return ValidationResult.Invalid("Take what?");
        }
        
        var visibleObjects = _stateMachine.GetVisibleObjects();
        var targetObject = visibleObjects
            .FirstOrDefault(o => o.Id.Contains(command.Object) ||
                               o.Name.Contains(command.Object, 
                                   StringComparison.OrdinalIgnoreCase));
        
        if (targetObject == null)
        {
            return ValidationResult.Invalid(
                $"You don't see a {command.Object} here.");
        }
        
        if (!targetObject.IsCollectable)
        {
            return ValidationResult.Invalid(
                $"You can't take the {targetObject.Name}.");
        }
        
        if (!_inventoryManager.CanAdd(targetObject))
        {
            return ValidationResult.Invalid(
                "You're carrying too much.");
        }
        
        return ValidationResult.Valid();
    }
    
    public async Task<CommandResult> ExecuteAsync(
        ParsedCommand command,
        GameState gameState)
    {
        var visibleObjects = _stateMachine.GetVisibleObjects();
        var targetObject = visibleObjects
            .First(o => o.Id.Contains(command.Object) ||
                       o.Name.Contains(command.Object,
                           StringComparison.OrdinalIgnoreCase));
        
        // Add to inventory
        _inventoryManager.AddItem(targetObject);
        
        // Remove from room
        var currentRoom = _stateMachine.GetCurrentRoom();
        currentRoom.State.VisibleObjectIds.Remove(targetObject.Id);
        targetObject.IsVisible = false;
        
        // Get action message
        var message = targetObject.Actions
            .GetValueOrDefault("take")?.SuccessMessage
            ?? $"You take the {targetObject.Name}.";
        
        // Update stats
        gameState.Player.Stats.ItemsCollected++;
        
        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = message,
            StateChange = new GameStateChange
            {
                ItemsAdded = new List<string> { targetObject.Id }
            }
        };
    }
}
```

#### Use Item Handler

```csharp
public class UseCommandHandler : ICommandHandler
{
    public string Verb => "use";
    
    private readonly IGameStateMachine _stateMachine;
    private readonly IInventoryManager _inventoryManager;
    private readonly IPuzzleEngine _puzzleEngine;
    
    public async Task<ValidationResult> ValidateAsync(
        ParsedCommand command,
        GameState gameState)
    {
        if (string.IsNullOrEmpty(command.Object))
        {
            return ValidationResult.Invalid("Use what?");
        }
        
        var item = _inventoryManager.GetItem(command.Object);
        if (item == null)
        {
            return ValidationResult.Invalid(
                $"You don't have a {command.Object}.");
        }
        
        if (!item.Actions.ContainsKey("use"))
        {
            return ValidationResult.Invalid(
                $"You can't use the {item.Name} like that.");
        }
        
        return ValidationResult.Valid();
    }
    
    public async Task<CommandResult> ExecuteAsync(
        ParsedCommand command,
        GameState gameState)
    {
        var item = _inventoryManager.GetItem(command.Object);
        var useAction = item.Actions["use"];
        
        // Check required flags
        if (!CheckRequiredFlags(useAction, gameState))
        {
            return new CommandResult
            {
                Success = false,
                Type = CommandResultType.RequirementsNotMet,
                Message = useAction.FailureMessage
            };
        }
        
        // Apply state changes
        foreach (var stateChange in useAction.StateChanges)
        {
            ApplyStateChange(stateChange, item, gameState);
        }
        
        // Check for puzzle completion
        var puzzleResult = await _puzzleEngine
            .CheckPuzzleCompletionAsync(gameState);
        
        var messages = new List<string> { useAction.SuccessMessage };
        if (puzzleResult.Completed)
        {
            messages.Add(puzzleResult.CompletionMessage);
            gameState.Player.Stats.PuzzlesSolved++;
        }
        
        // Consume item if needed
        if (useAction.ConsumesItem)
        {
            _inventoryManager.RemoveItem(item.Id);
            messages.Add($"The {item.Name} is used up.");
        }
        else
        {
            item.State.UsageCount++;
        }
        
        return new CommandResult
        {
            Success = true,
            Type = CommandResultType.Success,
            Message = string.Join("\n\n", messages)
        };
    }
    
    private bool CheckRequiredFlags(
        ActionDefinition action,
        GameState gameState)
    {
        foreach (var flag in action.RequiredFlags)
        {
            if (!gameState.Progress.StoryFlags
                .GetValueOrDefault(flag, false))
            {
                return false;
            }
        }
        return true;
    }
    
    private void ApplyStateChange(
        StateChange change,
        GameObject item,
        GameState gameState)
    {
        switch (change.Target)
        {
            case "self":
                ApplyToObject(item, change);
                break;
            case "room":
                ApplyToRoom(
                    _stateMachine.GetCurrentRoom(),
                    change);
                break;
            default:
                var targetObj = gameState.World.Objects[change.Target];
                ApplyToObject(targetObj, change);
                break;
        }
    }
    
    private void ApplyToObject(GameObject obj, StateChange change)
    {
        var property = obj.GetType().GetProperty(change.Property);
        property?.SetValue(obj, change.NewValue);
    }
    
    private void ApplyToRoom(Room room, StateChange change)
    {
        var property = room.GetType().GetProperty(change.Property);
        property?.SetValue(room, change.NewValue);
    }
}
```

### 4. Inventory Manager

```csharp
namespace TheCabin.Domain.Engine
{
    public class InventoryManager : IInventoryManager
    {
        private readonly GameState _gameState;
        private readonly ILogger _logger;
        
        public InventoryManager(
            GameState gameState,
            ILogger<InventoryManager> logger)
        {
            _gameState = gameState;
            _logger = logger;
        }
        
        public bool CanAdd(GameObject item)
        {
            var inventory = _gameState.Player.Inventory;
            return inventory.TotalWeight + item.Weight 
                <= inventory.MaxCapacity;
        }
        
        public void AddItem(GameObject item)
        {
            var inventory = _gameState.Player.Inventory;
            
            if (!CanAdd(item))
            {
                throw new InvalidOperationException(
                    "Inventory is full");
            }
            
            inventory.Items.Add(item);
            inventory.TotalWeight += item.Weight;
            
            _logger.LogInformation(
                "Added {Item} to inventory (Weight: {Weight}/{Max})",
                item.Name, inventory.TotalWeight, inventory.MaxCapacity);
        }
        
        public void RemoveItem(string itemId)
        {
            var inventory = _gameState.Player.Inventory;
            var item = inventory.Items.FirstOrDefault(i => i.Id == itemId);
            
            if (item != null)
            {
                inventory.Items.Remove(item);
                inventory.TotalWeight -= item.Weight;
                
                _logger.LogInformation(
                    "Removed {Item} from inventory", item.Name);
            }
        }
        
        public bool HasItem(string itemId)
        {
            return _gameState.Player.Inventory.Items
                .Any(i => i.Id == itemId);
        }
        
        public GameObject GetItem(string itemId)
        {
            return _gameState.Player.Inventory.Items
                .FirstOrDefault(i => i.Id.Contains(itemId) ||
                    i.Name.Contains(itemId, 
                        StringComparison.OrdinalIgnoreCase));
        }
        
        public List<GameObject> GetAllItems()
        {
            return _gameState.Player.Inventory.Items.ToList();
        }
        
        public string GetInventoryDescription()
        {
            var inventory = _gameState.Player.Inventory;
            
            if (inventory.Items.Count == 0)
            {
                return "You're not carrying anything.";
            }
            
            var items = inventory.Items
                .Select(i => $"- {i.Name}")
                .ToList();
            
            return $"You are carrying:\n" +
                   $"{string.Join("\n", items)}\n\n" +
                   $"Total weight: {inventory.TotalWeight}/" +
                   $"{inventory.MaxCapacity}";
        }
    }
}
```

### 5. Puzzle Engine

```csharp
namespace TheCabin.Domain.Engine
{
    public class PuzzleEngine : IPuzzleEngine
    {
        private readonly ILogger _logger;
        
        public async Task<PuzzleResult> CheckPuzzleCompletionAsync(
            GameState gameState)
        {
            // Check if any puzzle conditions are met
            var completedPuzzles = new List<string>();
            
            // Example: Check if lantern lit in dark room
            if (CheckLanternPuzzle(gameState))
            {
                completedPuzzles.Add("lantern_puzzle");
            }
            
            // Example: Check if all items collected
            if (CheckCollectionPuzzle(gameState))
            {
                completedPuzzles.Add("collection_puzzle");
            }
            
            // Add newly completed puzzles to progress
            var newlyCompleted = completedPuzzles
                .Where(p => !gameState.Progress.CompletedPuzzles.Contains(p))
                .ToList();
            
            if (newlyCompleted.Any())
            {
                gameState.Progress.CompletedPuzzles.AddRange(newlyCompleted);
                
                _logger.LogInformation(
                    "Puzzles completed: {Puzzles}",
                    string.Join(", ", newlyCompleted));
                
                return new PuzzleResult
                {
                    Completed = true,
                    PuzzleId = newlyCompleted.First(),
                    CompletionMessage = GetCompletionMessage(
                        newlyCompleted.First())
                };
            }
            
            return new PuzzleResult { Completed = false };
        }
        
        private bool CheckLanternPuzzle(GameState gameState)
        {
            var currentRoom = gameState.World.Rooms[
                gameState.Player.CurrentLocationId];
            
            if (currentRoom.LightLevel != LightLevel.Dark)
                return false;
            
            var lantern = gameState.Player.Inventory.Items
                .FirstOrDefault(i => i.Type == ObjectType.Light);
            
            return lantern?.State.CurrentState == "lit";
        }
        
        private bool CheckCollectionPuzzle(GameState gameState)
        {
            // Example: collect 3 specific items
            var requiredItems = new[] { "key", "map", "compass" };
            var hasAllItems = requiredItems.All(item =>
                gameState.Player.Inventory.Items.Any(i => 
                    i.Id.Contains(item)));
            
            return hasAllItems;
        }
        
        private string GetCompletionMessage(string puzzleId)
        {
            return puzzleId switch
            {
                "lantern_puzzle" => 
                    "The light reveals a hidden passage!",
                "collection_puzzle" => 
                    "You've gathered everything you need!",
                _ => "You've solved a puzzle!"
            };
        }
    }
    
    public class PuzzleResult
    {
        public bool Completed { get; set; }
        public string PuzzleId { get; set; }
        public string CompletionMessage { get; set; }
    }
}
```

### 6. Save/Load System

```csharp
namespace TheCabin.Domain.Engine
{
    public class SaveGameService
    {
        private readonly IGameSaveRepository _repository;
        private readonly ILogger _logger;
        
        public async Task<int> SaveGameAsync(
            GameState gameState,
            string saveName)
        {
            var saveEntity = new GameSaveEntity
            {
                Name = saveName,
                ThemeId = gameState.World.CurrentThemeId,
                GameStateJson = JsonSerializer.Serialize(gameState),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Version = gameState.Meta.Version,
                PlayTimeMinutes = (int)gameState.Player.Stats.PlayTime
                    .TotalMinutes,
                Thumbnail = await GenerateThumbnailAsync(gameState)
            };
            
            var saveId = await _repository.SaveAsync(saveEntity);
            
            _logger.LogInformation(
                "Game saved: {Name} (ID: {Id})", saveName, saveId);
            
            return saveId;
        }
        
        public async Task<GameState> LoadGameAsync(int saveId)
        {
            var saveEntity = await _repository.GetAsync(saveId);
            
            if (saveEntity == null)
            {
                throw new FileNotFoundException(
                    $"Save game {saveId} not found");
            }
            
            var gameState = JsonSerializer.Deserialize<GameState>(
                saveEntity.GameStateJson);
            
            _logger.LogInformation(
                "Game loaded: {Name} (ID: {Id})",
                saveEntity.Name, saveId);
            
            return gameState;
        }
        
        public async Task<List<GameSaveInfo>> GetSaveListAsync()
        {
            var saves = await _repository.GetAllAsync();
            
            return saves.Select(s => new GameSaveInfo
            {
                Id = s.Id,
                Name = s.Name,
                ThemeId = s.ThemeId,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(s.Timestamp)
                    .DateTime,
                PlayTime = TimeSpan.FromMinutes(s.PlayTimeMinutes),
                Thumbnail = s.Thumbnail
            }).ToList();
        }
        
        public async Task DeleteSaveAsync(int saveId)
        {
            await _repository.DeleteAsync(saveId);
            _logger.LogInformation("Save game deleted: {Id}", saveId);
        }
        
        private async Task<byte[]> GenerateThumbnailAsync(
            GameState gameState)
        {
            // TODO: Generate screenshot or render current scene
            return Array.Empty<byte>();
        }
    }
}
```

## Performance Considerations

### Command Processing Optimization

```csharp
// Cache frequently accessed game state
public class GameStateCache
{
    private Room _cachedCurrentRoom;
    private List<GameObject> _cachedVisibleObjects;
    private string _cachedLocationId;
    
    public Room GetCurrentRoom(GameState state)
    {
        if (_cachedLocationId != state.Player.CurrentLocationId)
        {
            _cachedCurrentRoom = state.World.Rooms[
                state.Player.CurrentLocationId];
            _cachedLocationId = state.Player.CurrentLocationId;
        }
        return _cachedCurrentRoom;
    }
    
    public void InvalidateCache()
    {
        _cachedCurrentRoom = null;
        _cachedVisibleObjects = null;
        _cachedLocationId = null;
    }
}
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 04-data-models.md, 05-voice-pipeline.md, 09-content-management.md
