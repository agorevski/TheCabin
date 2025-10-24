# 09 - Content Management

## Overview

This document details the content management system for The Cabin, including story pack format, content creation workflows, and dynamic content generation using AI.

## Story Pack Format

### Complete Story Pack Structure

```json
{
  "id": "classic_horror",
  "theme": "Classic Horror",
  "description": "A haunted log cabin deep in the woods, decaying interiors lit by candlelight.",
  "version": "1.0.0",
  "author": "The Cabin Team",
  "startingRoomId": "cabin_main",
  "metadata": {
    "difficulty": "Medium",
    "estimatedPlayTime": 45,
    "tags": ["horror", "mystery", "supernatural"],
    "coverImage": "classic_horror_cover.png",
    "themeColor": "#8B0000",
    "createdDate": "2025-01-01T00:00:00Z",
    "modifiedDate": "2025-01-15T00:00:00Z"
  },
  "rooms": [
    {
      "id": "cabin_main",
      "description": "You stand in a dimly lit wooden cabin. Dust motes dance in shafts of pale moonlight streaming through cracked windows. The air smells of old wood and decay.",
      "objects": ["lantern", "door", "table"],
      "exits": {
        "north": "forest_edge",
        "up": "cabin_loft"
      },
      "state": {
        "isLocked": false,
        "flags": {
          "fire_lit": false,
          "candles_lit": false
        },
        "visibleObjectIds": ["lantern", "door", "table"]
      },
      "isVisited": false,
      "ambientSound": "wind_howling.mp3",
      "lightLevel": "Dim"
    }
  ],
  "objects": {
    "lantern": {
      "id": "lantern",
      "name": "Rusty Lantern",
      "description": "An old brass lantern covered in rust and cobwebs. Oil sloshes inside when you shake it.",
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
              "property": "IsVisible",
              "newValue": false
            }
          ],
          "requiredFlags": [],
          "consumesItem": false
        },
        "light": {
          "verb": "light",
          "successMessage": "The lantern flickers to life, casting dancing shadows on the walls.",
          "failureMessage": "You need matches or a lighter.",
          "stateChanges": [
            {
              "target": "self",
              "property": "CurrentState",
              "newValue": "lit"
            },
            {
              "target": "room",
              "property": "LightLevel",
              "newValue": "Bright"
            }
          ],
          "requiredFlags": ["has_matches"],
          "consumesItem": false
        }
      },
      "requiredItems": [],
      "isCollectable": true,
      "isVisible": true,
      "weight": 3
    }
  },
  "achievements": [
    {
      "id": "first_steps",
      "name": "First Steps",
      "description": "Enter the cabin for the first time",
      "requiredFlags": ["cabin_entered"],
      "isUnlocked": false
    },
    {
      "id": "light_bringer",
      "name": "Light Bringer",
      "description": "Light the lantern",
      "requiredFlags": ["lantern_lit"],
      "isUnlocked": false
    }
  ]
}
```

## Story Pack Service

```csharp
namespace TheCabin.Services
{
    public class StoryPackService : IStoryPackService
    {
        private readonly IStoryPackRepository _repository;
        private readonly ILogger<StoryPackService> _logger;
        private Dictionary<string, StoryPack> _loadedPacks;
        
        public StoryPackService(
            IStoryPackRepository repository,
            ILogger<StoryPackService> logger)
        {
            _repository = repository;
            _logger = logger;
            _loadedPacks = new Dictionary<string, StoryPack>();
        }
        
        public async Task<List<StoryPackInfo>> GetAvailablePacksAsync()
        {
            var packs = await _repository.GetAllPacksAsync();
            
            return packs.Select(p => new StoryPackInfo
            {
                Id = p.Id,
                Theme = p.Theme,
                Description = p.Description,
                Difficulty = p.Metadata.Difficulty,
                EstimatedPlayTime = p.Metadata.EstimatedPlayTime,
                Tags = p.Metadata.Tags,
                CoverImage = p.Metadata.CoverImage
            }).ToList();
        }
        
        public async Task<StoryPack> LoadPackAsync(string packId)
        {
            // Check if already loaded
            if (_loadedPacks.TryGetValue(packId, out var cached))
            {
                return cached;
            }
            
            // Load from repository
            var pack = await _repository.GetPackAsync(packId);
            
            if (pack == null)
            {
                throw new FileNotFoundException(
                    $"Story pack not found: {packId}");
            }
            
            // Validate pack
            ValidatePack(pack);
            
            // Cache and return
            _loadedPacks[packId] = pack;
            
            _logger.LogInformation(
                "Loaded story pack: {Theme} ({RoomCount} rooms, {ObjectCount} objects)",
                pack.Theme, pack.Rooms.Count, pack.Objects.Count);
            
            return pack;
        }
        
        private void ValidatePack(StoryPack pack)
        {
            // Validate starting room exists
            if (!pack.Rooms.Any(r => r.Id == pack.StartingRoomId))
            {
                throw new InvalidDataException(
                    $"Starting room not found: {pack.StartingRoomId}");
            }
            
            // Validate all room exits point to existing rooms
            foreach (var room in pack.Rooms)
            {
                foreach (var exit in room.Exits.Values)
                {
                    if (!pack.Rooms.Any(r => r.Id == exit))
                    {
                        _logger.LogWarning(
                            "Room {RoomId} has exit to non-existent room: {ExitId}",
                            room.Id, exit);
                    }
                }
                
                // Validate all object references exist
                foreach (var objId in room.ObjectIds)
                {
                    if (!pack.Objects.ContainsKey(objId))
                    {
                        _logger.LogWarning(
                            "Room {RoomId} references non-existent object: {ObjectId}",
                            room.Id, objId);
                    }
                }
            }
        }
        
        public async Task<StoryPack> CreatePackFromTemplateAsync(
            string templateId,
            Dictionary<string, string> parameters)
        {
            // Load template
            var template = await _repository.GetTemplateAsync(templateId);
            
            // Apply parameters (e.g., theme-specific replacements)
            var pack = ApplyTemplate(template, parameters);
            
            return pack;
        }
        
        public void UnloadPack(string packId)
        {
            _loadedPacks.Remove(packId);
            _logger.LogInformation("Unloaded story pack: {PackId}", packId);
        }
    }
}
```

## Content Creation Workflow

### 1. Manual Content Creation

```csharp
public class StoryPackBuilder
{
    private StoryPack _pack;
    
    public StoryPackBuilder(string id, string theme)
    {
        _pack = new StoryPack
        {
            Id = id,
            Theme = theme,
            Metadata = new StoryMetadata
            {
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };
    }
    
    public StoryPackBuilder WithDescription(string description)
    {
        _pack.Description = description;
        return this;
    }
    
    public StoryPackBuilder WithDifficulty(Difficulty difficulty)
    {
        _pack.Metadata.Difficulty = difficulty;
        return this;
    }
    
    public StoryPackBuilder AddRoom(Room room)
    {
        _pack.Rooms.Add(room);
        return this;
    }
    
    public StoryPackBuilder AddObject(GameObject obj)
    {
        _pack.Objects[obj.Id] = obj;
        return this;
    }
    
    public StoryPackBuilder SetStartingRoom(string roomId)
    {
        _pack.StartingRoomId = roomId;
        return this;
    }
    
    public StoryPack Build()
    {
        // Validate before building
        if (string.IsNullOrEmpty(_pack.StartingRoomId))
        {
            throw new InvalidOperationException(
                "Starting room must be set");
        }
        
        if (_pack.Rooms.Count == 0)
        {
            throw new InvalidOperationException(
                "Pack must contain at least one room");
        }
        
        return _pack;
    }
}
```

### 2. AI-Assisted Content Generation

```csharp
public class AiContentGenerator
{
    private readonly ILlmApiClient _llmClient;
    private readonly ILogger _logger;
    
    public async Task<StoryPack> GenerateStoryPackAsync(
        string theme,
        ContentGenerationOptions options)
    {
        _logger.LogInformation(
            "Generating story pack for theme: {Theme}", theme);
        
        // Step 1: Generate pack metadata
        var metadata = await GenerateMetadataAsync(theme, options);
        
        // Step 2: Generate rooms
        var rooms = await GenerateRoomsAsync(theme, options.RoomCount);
        
        // Step 3: Generate objects for each room
        var objects = new Dictionary<string, GameObject>();
        foreach (var room in rooms)
        {
            var roomObjects = await GenerateRoomObjectsAsync(
                theme, room, options.ObjectsPerRoom);
            
            foreach (var obj in roomObjects)
            {
                objects[obj.Id] = obj;
                room.ObjectIds.Add(obj.Id);
            }
        }
        
        // Step 4: Create interconnections
        ConnectRooms(rooms, options.Connectivity);
        
        // Step 5: Add puzzles and special items
        await AddPuzzlesAsync(rooms, objects, options.PuzzleCount);
        
        var pack = new StoryPack
        {
            Id = GenerateId(theme),
            Theme = theme,
            Description = metadata.Description,
            Version = "1.0.0",
            Author = "AI Generated",
            StartingRoomId = rooms.First().Id,
            Rooms = rooms,
            Objects = objects,
            Metadata = metadata
        };
        
        _logger.LogInformation(
            "Generated story pack: {Rooms} rooms, {Objects} objects",
            pack.Rooms.Count, pack.Objects.Count);
        
        return pack;
    }
    
    private async Task<StoryMetadata> GenerateMetadataAsync(
        string theme,
        ContentGenerationOptions options)
    {
        var prompt = $@"
Generate metadata for a text adventure game with theme: '{theme}'.
Include a compelling description (2-3 sentences), difficulty level, 
estimated play time, and 3-5 relevant tags.

Output as JSON:
{{
  ""description"": ""..."",
  ""difficulty"": ""Easy|Medium|Hard"",
  ""estimatedPlayTime"": 30,
  ""tags"": [""tag1"", ""tag2"", ""tag3""]
}}";
        
        var response = await _llmClient.GenerateAsync(prompt);
        return JsonSerializer.Deserialize<StoryMetadata>(response);
    }
    
    private async Task<List<Room>> GenerateRoomsAsync(
        string theme,
        int count)
    {
        var prompt = $@"
Generate {count} interconnected rooms for a '{theme}' themed adventure game.
Each room should have:
- Unique ID (lowercase, underscores)
- Vivid description (2-3 sentences)
- Light level (Bright, Normal, Dim, Dark, PitchBlack)
- Suggested ambient sound

Output as JSON array.";
        
        var response = await _llmClient.GenerateAsync(prompt);
        return JsonSerializer.Deserialize<List<Room>>(response);
    }
    
    private async Task<List<GameObject>> GenerateRoomObjectsAsync(
        string theme,
        Room room,
        int count)
    {
        var prompt = $@"
Generate {count} interactive objects for this room:
Theme: {theme}
Room: {room.Description}

Objects should include:
- Mix of collectible items and furniture
- Appropriate actions (take, use, examine, etc.)
- Success/failure messages
- State changes where relevant

Output as JSON array.";
        
        var response = await _llmClient.GenerateAsync(prompt);
        return JsonSerializer.Deserialize<List<GameObject>>(response);
    }
    
    private void ConnectRooms(
        List<Room> rooms,
        double connectivity)
    {
        // Create a network of room connections
        // Higher connectivity = more paths between rooms
        
        var random = new Random();
        
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            // Always connect to next room (minimum connectivity)
            var direction = GetRandomDirection();
            rooms[i].Exits[direction] = rooms[i + 1].Id;
            rooms[i + 1].Exits[GetOppositeDirection(direction)] = 
                rooms[i].Id;
            
            // Add extra connections based on connectivity factor
            if (random.NextDouble() < connectivity)
            {
                var targetIndex = random.Next(rooms.Count);
                if (targetIndex != i)
                {
                    direction = GetRandomDirection();
                    if (!rooms[i].Exits.ContainsKey(direction))
                    {
                        rooms[i].Exits[direction] = rooms[targetIndex].Id;
                    }
                }
            }
        }
    }
    
    private async Task AddPuzzlesAsync(
        List<Room> rooms,
        Dictionary<string, GameObject> objects,
        int puzzleCount)
    {
        // Generate puzzle scenarios
        for (int i = 0; i < puzzleCount; i++)
        {
            var prompt = $@"
Generate a puzzle for a text adventure game.
Include:
- Locked door or container
- Key or solution item
- Clues in room descriptions
- Satisfying resolution

Output as JSON with room IDs and object definitions.";
            
            var response = await _llmClient.GenerateAsync(prompt);
            // Parse and integrate puzzle into rooms and objects
        }
    }
    
    private string GetRandomDirection()
    {
        var directions = new[] { "north", "south", "east", "west", 
                                "up", "down" };
        return directions[new Random().Next(directions.Length)];
    }
    
    private string GetOppositeDirection(string direction)
    {
        return direction switch
        {
            "north" => "south",
            "south" => "north",
            "east" => "west",
            "west" => "east",
            "up" => "down",
            "down" => "up",
            _ => direction
        };
    }
    
    private string GenerateId(string theme)
    {
        return theme.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("-", "_");
    }
}

public class ContentGenerationOptions
{
    public int RoomCount { get; set; } = 6;
    public int ObjectsPerRoom { get; set; } = 3;
    public int PuzzleCount { get; set; } = 2;
    public double Connectivity { get; set; } = 0.3;
}
```

## Content Validation

```csharp
public class StoryPackValidator
{
    public ValidationResult Validate(StoryPack pack)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Check required fields
        if (string.IsNullOrEmpty(pack.Id))
            errors.Add("Pack ID is required");
        
        if (string.IsNullOrEmpty(pack.Theme))
            errors.Add("Theme is required");
        
        if (pack.Rooms.Count == 0)
            errors.Add("Pack must contain at least one room");
        
        // Check starting room
        if (!pack.Rooms.Any(r => r.Id == pack.StartingRoomId))
            errors.Add($"Starting room not found: {pack.StartingRoomId}");
        
        // Validate rooms
        foreach (var room in pack.Rooms)
        {
            ValidateRoom(room, pack, errors, warnings);
        }
        
        // Validate objects
        foreach (var obj in pack.Objects.Values)
        {
            ValidateObject(obj, errors, warnings);
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
    
    private void ValidateRoom(
        Room room,
        StoryPack pack,
        List<string> errors,
        List<string> warnings)
    {
        // Check description
        if (string.IsNullOrWhiteSpace(room.Description))
            errors.Add($"Room {room.Id} has no description");
        
        // Check exits
        foreach (var exit in room.Exits)
        {
            if (!pack.Rooms.Any(r => r.Id == exit.Value))
            {
                warnings.Add(
                    $"Room {room.Id} exit '{exit.Key}' " +
                    $"points to non-existent room: {exit.Value}");
            }
        }
        
        // Check object references
        foreach (var objId in room.ObjectIds)
        {
            if (!pack.Objects.ContainsKey(objId))
            {
                errors.Add(
                    $"Room {room.Id} references " +
                    $"non-existent object: {objId}");
            }
        }
    }
    
    private void ValidateObject(
        GameObject obj,
        List<string> errors,
        List<string> warnings)
    {
        if (string.IsNullOrEmpty(obj.Name))
            errors.Add($"Object {obj.Id} has no name");
        
        if (string.IsNullOrEmpty(obj.Description))
            warnings.Add($"Object {obj.Id} has no description");
        
        if (obj.Actions.Count == 0)
            warnings.Add($"Object {obj.Id} has no actions");
    }
}
```

## Content Import/Export

```csharp
public class ContentExporter
{
    public async Task ExportPackAsync(
        StoryPack pack,
        string outputPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(pack, options);
        await File.WriteAllTextAsync(outputPath, json);
    }
    
    public async Task<StoryPack> ImportPackAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        return JsonSerializer.Deserialize<StoryPack>(json, options);
    }
}
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 04-data-models.md, 06-game-engine.md
