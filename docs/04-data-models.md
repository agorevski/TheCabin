# 04 - Data Models

## Overview

This document defines all data models, entities, and data transfer objects (DTOs) used throughout The Cabin application. Models are organized by layer and purpose.

## Domain Models

### Room Model

Represents a physical location in the game world.

```csharp
namespace TheCabin.Domain.Models
{
    public class Room
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<string> ObjectIds { get; set; }
        public Dictionary<string, string> Exits { get; set; }
        public RoomState State { get; set; }
        public bool IsVisited { get; set; }
        public string AmbientSound { get; set; }
        public LightLevel LightLevel { get; set; }
        
        public Room()
        {
            ObjectIds = new List<string>();
            Exits = new Dictionary<string, string>();
            State = new RoomState();
        }
    }
    
    public class RoomState
    {
        public bool IsLocked { get; set; }
        public Dictionary<string, bool> Flags { get; set; }
        public List<string> VisibleObjectIds { get; set; }
        
        public RoomState()
        {
            Flags = new Dictionary<string, bool>();
            VisibleObjectIds = new List<string>();
        }
    }
    
    public enum LightLevel
    {
        Bright,
        Normal,
        Dim,
        Dark,
        PitchBlack
    }
}
```

**JSON Schema**:

```json
{
  "id": "cabin_main",
  "description": "You stand in a dimly lit wooden cabin...",
  "objects": ["lantern", "door", "table"],
  "exits": {
    "north": "forest_edge",
    "up": "cabin_loft"
  },
  "state": {
    "isLocked": false,
    "flags": {
      "fire_lit": false
    },
    "visibleObjectIds": ["lantern", "door"]
  },
  "isVisited": false,
  "ambientSound": "wind_howling.mp3",
  "lightLevel": "Dim"
}
```

### Object Model

Represents interactive items in the game world.

```csharp
namespace TheCabin.Domain.Models
{
    public class GameObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ObjectType Type { get; set; }
        public ObjectState State { get; set; }
        public Dictionary<string, ActionDefinition> Actions { get; set; }
        public List<string> RequiredItems { get; set; }
        public bool IsCollectable { get; set; }
        public bool IsVisible { get; set; }
        public int Weight { get; set; }
        
        public GameObject()
        {
            Actions = new Dictionary<string, ActionDefinition>();
            RequiredItems = new List<string>();
            State = new ObjectState();
        }
    }
    
    public class ObjectState
    {
        public string CurrentState { get; set; }
        public Dictionary<string, bool> Flags { get; set; }
        public int UsageCount { get; set; }
        
        public ObjectState()
        {
            CurrentState = "default";
            Flags = new Dictionary<string, bool>();
            UsageCount = 0;
        }
    }
    
    public enum ObjectType
    {
        Item,
        Tool,
        Furniture,
        Door,
        Container,
        Light,
        Puzzle,
        Decoration
    }
    
    public class ActionDefinition
    {
        public string Verb { get; set; }
        public string SuccessMessage { get; set; }
        public string FailureMessage { get; set; }
        public List<StateChange> StateChanges { get; set; }
        public List<string> RequiredFlags { get; set; }
        public bool ConsumesItem { get; set; }
        
        public ActionDefinition()
        {
            StateChanges = new List<StateChange>();
            RequiredFlags = new List<string>();
        }
    }
    
    public class StateChange
    {
        public string Target { get; set; }  // "self", "room", or object ID
        public string Property { get; set; }
        public object NewValue { get; set; }
    }
}
```

**JSON Schema**:

```json
{
  "id": "lantern",
  "name": "Rusty Lantern",
  "description": "An old brass lantern covered in rust and cobwebs",
  "type": "Light",
  "state": {
    "currentState": "unlit",
    "flags": {
      "has_oil": true
    },
    "usageCount": 0
  },
  "actions": {
    "take": {
      "verb": "take",
      "successMessage": "You pick up the lantern. It feels heavy with oil.",
      "failureMessage": "You can't take that.",
      "stateChanges": [
        {
          "target": "self",
          "property": "isVisible",
          "newValue": false
        }
      ],
      "requiredFlags": [],
      "consumesItem": false
    },
    "light": {
      "verb": "light",
      "successMessage": "The lantern flickers to life, casting dancing shadows.",
      "failureMessage": "You need something to light it with.",
      "stateChanges": [
        {
          "target": "self",
          "property": "currentState",
          "newValue": "lit"
        },
        {
          "target": "room",
          "property": "lightLevel",
          "newValue": "Bright"
        }
      ],
      "requiredFlags": ["has_oil"],
      "consumesItem": false
    }
  },
  "requiredItems": [],
  "isCollectable": true,
  "isVisible": true,
  "weight": 3
}
```

### Player Model

Represents the player's state and inventory.

```csharp
namespace TheCabin.Domain.Models
{
    public class Player
    {
        public string Name { get; set; }
        public string CurrentLocationId { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public Inventory Inventory { get; set; }
        public PlayerStats Stats { get; set; }
        public List<StatusEffect> StatusEffects { get; set; }
        public int CarryCapacity { get; set; }
        
        public Player()
        {
            Health = 100;
            MaxHealth = 100;
            CarryCapacity = 20;
            Inventory = new Inventory();
            Stats = new PlayerStats();
            StatusEffects = new List<StatusEffect>();
        }
    }
    
    public class Inventory
    {
        public List<GameObject> Items { get; set; }
        public int TotalWeight { get; set; }
        public int MaxCapacity { get; set; }
        
        public Inventory()
        {
            Items = new List<GameObject>();
            MaxCapacity = 20;
        }
        
        public bool CanAdd(GameObject item)
        {
            return TotalWeight + item.Weight <= MaxCapacity;
        }
        
        public bool HasItem(string itemId)
        {
            return Items.Any(i => i.Id == itemId);
        }
        
        public GameObject GetItem(string itemId)
        {
            return Items.FirstOrDefault(i => i.Id == itemId);
        }
    }
    
    public class PlayerStats
    {
        public TimeSpan PlayTime { get; set; }
        public int CommandsExecuted { get; set; }
        public int RoomsExplored { get; set; }
        public int ItemsCollected { get; set; }
        public int PuzzlesSolved { get; set; }
        public DateTime StartTime { get; set; }
    }
    
    public class StatusEffect
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }  // In turns/actions
        public EffectType Type { get; set; }
        public int Magnitude { get; set; }
    }
    
    public enum EffectType
    {
        Health,
        Speed,
        Vision,
        Strength
    }
}
```

### Command Models

Represents player commands and their parsed structure.

```csharp
namespace TheCabin.Domain.Models
{
    public class ParsedCommand
    {
        public string Verb { get; set; }
        public string Object { get; set; }
        public string Target { get; set; }
        public string Context { get; set; }
        public double Confidence { get; set; }
        public string RawInput { get; set; }
        public DateTime Timestamp { get; set; }
        
        public ParsedCommand()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
    
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> AdditionalMessages { get; set; }
        public GameStateChange StateChange { get; set; }
        public CommandResultType Type { get; set; }
        
        public CommandResult()
        {
            AdditionalMessages = new List<string>();
        }
    }
    
    public enum CommandResultType
    {
        Success,
        Failure,
        InvalidCommand,
        RequirementsNotMet,
        AmbiguousCommand,
        SystemMessage
    }
    
    public class GameStateChange
    {
        public string LocationChanged { get; set; }
        public List<string> ItemsAdded { get; set; }
        public List<string> ItemsRemoved { get; set; }
        public int HealthChange { get; set; }
        public Dictionary<string, bool> FlagsChanged { get; set; }
        
        public GameStateChange()
        {
            ItemsAdded = new List<string>();
            ItemsRemoved = new List<string>();
            FlagsChanged = new Dictionary<string, bool>();
        }
    }
}
```

### Story Pack Model

Represents a complete theme/story package.

```csharp
namespace TheCabin.Domain.Models
{
    public class StoryPack
    {
        public string Id { get; set; }
        public string Theme { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public List<Room> Rooms { get; set; }
        public Dictionary<string, GameObject> Objects { get; set; }
        public StoryMetadata Metadata { get; set; }
        public string StartingRoomId { get; set; }
        public List<Achievement> Achievements { get; set; }
        
        public StoryPack()
        {
            Rooms = new List<Room>();
            Objects = new Dictionary<string, GameObject>();
            Achievements = new List<Achievement>();
        }
    }
    
    public class StoryMetadata
    {
        public Difficulty Difficulty { get; set; }
        public int EstimatedPlayTime { get; set; }  // In minutes
        public List<string> Tags { get; set; }
        public string CoverImage { get; set; }
        public string ThemeColor { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        
        public StoryMetadata()
        {
            Tags = new List<string>();
        }
    }
    
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }
    
    public class Achievement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> RequiredFlags { get; set; }
        public bool IsUnlocked { get; set; }
    }
}
```

## Game State Models

### Complete Game State

```csharp
namespace TheCabin.Domain.Models
{
    public class GameState
    {
        public Guid Id { get; set; }
        public string SaveName { get; set; }
        public Player Player { get; set; }
        public WorldState World { get; set; }
        public ProgressState Progress { get; set; }
        public MetaState Meta { get; set; }
        public List<NarrativeEntry> StoryLog { get; set; }
        
        public GameState()
        {
            Id = Guid.NewGuid();
            Player = new Player();
            World = new WorldState();
            Progress = new ProgressState();
            Meta = new MetaState();
            StoryLog = new List<NarrativeEntry>();
        }
    }
    
    public class WorldState
    {
        public string CurrentThemeId { get; set; }
        public Dictionary<string, Room> Rooms { get; set; }
        public Dictionary<string, GameObject> Objects { get; set; }
        public Dictionary<string, object> GlobalVariables { get; set; }
        public int TurnNumber { get; set; }
        
        public WorldState()
        {
            Rooms = new Dictionary<string, Room>();
            Objects = new Dictionary<string, GameObject>();
            GlobalVariables = new Dictionary<string, object>();
        }
    }
    
    public class ProgressState
    {
        public List<string> CompletedPuzzles { get; set; }
        public List<string> UnlockedAreas { get; set; }
        public List<string> DiscoveredSecrets { get; set; }
        public Dictionary<string, bool> StoryFlags { get; set; }
        public int Score { get; set; }
        public List<string> UnlockedAchievements { get; set; }
        
        public ProgressState()
        {
            CompletedPuzzles = new List<string>();
            UnlockedAreas = new List<string>();
            DiscoveredSecrets = new List<string>();
            StoryFlags = new Dictionary<string, bool>();
            UnlockedAchievements = new List<string>();
        }
    }
    
    public class MetaState
    {
        public DateTime SaveTimestamp { get; set; }
        public string Version { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public int SaveCount { get; set; }
        public string DeviceId { get; set; }
        
        public MetaState()
        {
            SaveTimestamp = DateTime.UtcNow;
            Version = "1.0.0";
        }
    }
}
```

## UI Models

### Narrative Entry

```csharp
namespace TheCabin.Presentation.Models
{
    public class NarrativeEntry
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public NarrativeType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string SpeakerName { get; set; }
        public Color TextColor { get; set; }
        public bool IsImportant { get; set; }
        
        public NarrativeEntry()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }
    }
    
    public enum NarrativeType
    {
        Description,
        PlayerCommand,
        SystemMessage,
        Success,
        Failure,
        Dialogue,
        Discovery
    }
}
```

### Voice Recognition Models

```csharp
namespace TheCabin.Services.Models
{
    public class VoiceRecognitionResult
    {
        public bool Success { get; set; }
        public string TranscribedText { get; set; }
        public double Confidence { get; set; }
        public List<string> Alternatives { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
        
        public VoiceRecognitionResult()
        {
            Alternatives = new List<string>();
        }
    }
    
    public class VoiceSettings
    {
        public bool IsEnabled { get; set; }
        public bool AutoListen { get; set; }
        public bool UsePushToTalk { get; set; }
        public float ConfidenceThreshold { get; set; }
        public string PreferredEngine { get; set; }
        public bool OfflineMode { get; set; }
        
        public VoiceSettings()
        {
            IsEnabled = true;
            AutoListen = false;
            UsePushToTalk = true;
            ConfidenceThreshold = 0.75f;
            PreferredEngine = "Android";
            OfflineMode = false;
        }
    }
}
```

### TTS Models

```csharp
namespace TheCabin.Services.Models
{
    public class TtsSettings
    {
        public bool IsEnabled { get; set; }
        public float SpeechRate { get; set; }
        public float Pitch { get; set; }
        public float Volume { get; set; }
        public string PreferredVoice { get; set; }
        public bool AutoNarrate { get; set; }
        
        public TtsSettings()
        {
            IsEnabled = true;
            SpeechRate = 1.0f;
            Pitch = 1.0f;
            Volume = 1.0f;
            AutoNarrate = false;
        }
    }
}
```

## Database Models (SQLite)

### Save Game Table

```csharp
namespace TheCabin.Data.Models
{
    [Table("GameSaves")]
    public class GameSaveEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(50)]
        public string ThemeId { get; set; }
        
        public string GameStateJson { get; set; }
        
        public long Timestamp { get; set; }
        
        [MaxLength(20)]
        public string Version { get; set; }
        
        public int PlayTimeMinutes { get; set; }
        
        public byte[] Thumbnail { get; set; }
    }
}
```

### Command Cache Table

```csharp
namespace TheCabin.Data.Models
{
    [Table("CommandCache")]
    public class CommandCacheEntity
    {
        [PrimaryKey, MaxLength(64)]
        public string CommandHash { get; set; }
        
        [MaxLength(64)]
        public string ContextHash { get; set; }
        
        public string ParsedJson { get; set; }
        
        public float Confidence { get; set; }
        
        public long Timestamp { get; set; }
        
        public int HitCount { get; set; }
        
        [Indexed]
        public long ExpiryTimestamp { get; set; }
    }
}
```

### Settings Table

```csharp
namespace TheCabin.Data.Models
{
    [Table("Settings")]
    public class SettingEntity
    {
        [PrimaryKey, MaxLength(50)]
        public string Key { get; set; }
        
        public string Value { get; set; }
        
        public string Type { get; set; }
        
        public long ModifiedTimestamp { get; set; }
    }
}
```

## API Models (LLM Integration)

### LLM Request

```csharp
namespace TheCabin.Services.Models
{
    public class LlmParseRequest
    {
        public string Input { get; set; }
        public string CurrentLocation { get; set; }
        public List<string> VisibleObjects { get; set; }
        public List<string> InventoryItems { get; set; }
        public List<string> RecentCommands { get; set; }
        public Dictionary<string, bool> GameFlags { get; set; }
        
        public LlmParseRequest()
        {
            VisibleObjects = new List<string>();
            InventoryItems = new List<string>();
            RecentCommands = new List<string>();
            GameFlags = new Dictionary<string, bool>();
        }
    }
    
    public class LlmParseResponse
    {
        public string Verb { get; set; }
        public string Object { get; set; }
        public string Target { get; set; }
        public string Context { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; }
        public List<string> AlternativeInterpretations { get; set; }
        
        public LlmParseResponse()
        {
            AlternativeInterpretations = new List<string>();
        }
    }
}
```

## Validation and Constraints

### Command Verbs (Standard Set)

```csharp
namespace TheCabin.Domain.Constants
{
    public static class CommandVerbs
    {
        // Movement
        public const string Go = "go";
        public const string Move = "move";
        public const string Walk = "walk";
        public const string Run = "run";
        
        // Interaction
        public const string Take = "take";
        public const string Drop = "drop";
        public const string Use = "use";
        public const string Open = "open";
        public const string Close = "close";
        public const string Push = "push";
        public const string Pull = "pull";
        
        // Examination
        public const string Look = "look";
        public const string Examine = "examine";
        public const string Search = "search";
        public const string Read = "read";
        
        // Inventory
        public const string Inventory = "inventory";
        public const string Equipment = "equipment";
        
        // System
        public const string Help = "help";
        public const string Save = "save";
        public const string Load = "load";
        public const string Quit = "quit";
        
        public static readonly HashSet<string> AllVerbs = new()
        {
            Go, Move, Walk, Run,
            Take, Drop, Use, Open, Close, Push, Pull,
            Look, Examine, Search, Read,
            Inventory, Equipment,
            Help, Save, Load, Quit
        };
    }
}
```

## Model Relationships Diagram

```text
StoryPack
    ├─ Rooms[]
    │   ├─ Objects[] (references)
    │   └─ Exits{} → Room IDs
    └─ Objects{} (dictionary)

GameState
    ├─ Player
    │   ├─ Inventory
    │   │   └─ Items[] (GameObject references)
    │   └─ StatusEffects[]
    ├─ WorldState
    │   ├─ Rooms{}
    │   └─ Objects{}
    ├─ ProgressState
    └─ MetaState

ParsedCommand → CommandResult → GameStateChange
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 02-system-architecture.md, 06-game-engine.md, 09-content-management.md
