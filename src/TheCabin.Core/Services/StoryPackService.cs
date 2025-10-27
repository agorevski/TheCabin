using System.Text.Json;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Services;

/// <summary>
/// Service for managing story packs
/// </summary>
public class StoryPackService : IStoryPackService
{
    private readonly Dictionary<string, StoryPack> _loadedPacks = new();
    private readonly string _storyPacksPath;
    
    public StoryPackService(string? storyPacksPath = null)
    {
        // Default to story_packs directory
        _storyPacksPath = storyPacksPath ?? Path.Combine(
            AppContext.BaseDirectory, 
            "story_packs");
    }
    
    /// <summary>
    /// Gets all available story packs
    /// </summary>
    public async Task<List<StoryPackInfo>> GetAvailablePacksAsync()
    {
        var packs = new List<StoryPackInfo>();
        
        if (!Directory.Exists(_storyPacksPath))
        {
            return packs;
        }
        
        var jsonFiles = Directory.GetFiles(_storyPacksPath, "*.json");
        
        foreach (var file in jsonFiles)
        {
            try
            {
                var fileName = Path.GetFileName(file);
                System.Diagnostics.Debug.WriteLine($"GetAvailablePacksAsync: Processing file: {fileName}");
                
                // Skip achievement and puzzle files - they're loaded separately
                if (fileName.StartsWith("achievements_") || fileName.StartsWith("puzzles_"))
                {
                    System.Diagnostics.Debug.WriteLine($"GetAvailablePacksAsync: Skipping {fileName} (achievements/puzzles file)");
                    continue;
                }
                
                System.Diagnostics.Debug.WriteLine($"GetAvailablePacksAsync: Loading pack from {fileName}");
                var pack = await LoadPackFromFileAsync(file);
                
                System.Diagnostics.Debug.WriteLine($"GetAvailablePacksAsync: Loaded pack - Id: {pack.Id}, Theme: {pack.Theme}");
                
                // Validate that pack has required fields
                if (string.IsNullOrEmpty(pack.Id) || string.IsNullOrEmpty(pack.Theme))
                {
                    System.Diagnostics.Debug.WriteLine($"GetAvailablePacksAsync: Skipping {fileName} - missing Id or Theme");
                    continue;
                }
                
                packs.Add(new StoryPackInfo
                {
                    Id = pack.Id,
                    Theme = pack.Theme,
                    Description = pack.Description,
                    Difficulty = pack.Metadata?.Difficulty ?? Difficulty.Medium,
                    EstimatedPlayTime = pack.Metadata?.EstimatedPlayTime ?? 30,
                    Tags = pack.Metadata?.Tags ?? new List<string>()
                });
                
                System.Diagnostics.Debug.WriteLine($"GetAvailablePacksAsync: Added pack: {pack.Id}");
            }
            catch (Exception ex)
            {
                // Skip invalid files
                System.Diagnostics.Debug.WriteLine($"GetAvailablePacksAsync: Error loading {Path.GetFileName(file)}: {ex.Message}");
                continue;
            }
        }
        
        return packs;
    }
    
    /// <summary>
    /// Loads a story pack by ID
    /// </summary>
    public async Task<StoryPack> LoadPackAsync(string packId)
    {
        if (string.IsNullOrEmpty(packId))
            throw new ArgumentException("Pack ID cannot be null or empty", nameof(packId));
        
        // Check if already loaded
        if (_loadedPacks.TryGetValue(packId, out var cachedPack))
        {
            return cachedPack;
        }
        
        // Find the file
        var fileName = $"{packId}.json";
        var filePath = Path.Combine(_storyPacksPath, fileName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Story pack not found: {packId}");
        }
        
        var pack = await LoadPackFromFileAsync(filePath);
        
        // Validate the pack
        ValidatePack(pack);
        
        // Cache it
        _loadedPacks[packId] = pack;
        
        return pack;
    }
    
    /// <summary>
    /// Unloads a story pack from memory
    /// </summary>
    public void UnloadPack(string packId)
    {
        _loadedPacks.Remove(packId);
    }
    
    /// <summary>
    /// Validates a story pack structure
    /// </summary>
    public ValidationResult ValidatePack(StoryPack pack)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            ValidatePackInternal(pack);
        }
        catch (InvalidDataException ex)
        {
            result.IsValid = false;
            result.Errors.Add(ex.Message);
        }
        
        return result;
    }
    
    private async Task<StoryPack> LoadPackFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        
        System.Diagnostics.Debug.WriteLine($"LoadPackFromFileAsync: JSON content preview: {json.Substring(0, Math.Min(200, json.Length))}");
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        
        var pack = JsonSerializer.Deserialize<StoryPack>(json, options);
        
        if (pack == null)
        {
            throw new InvalidDataException($"Failed to deserialize story pack from {filePath}");
        }
        
        System.Diagnostics.Debug.WriteLine($"LoadPackFromFileAsync: After deserialization - Id='{pack.Id}', Theme='{pack.Theme}', Description='{pack.Description}'");
        
        // Try to load achievements from separate file
        await LoadAchievementsAsync(pack, filePath);
        
        return pack;
    }
    
    private async Task LoadAchievementsAsync(StoryPack pack, string packFilePath)
    {
        // Look for achievements file: achievements_{packId}.json
        var directory = Path.GetDirectoryName(packFilePath);
        var achievementsFileName = $"achievements_{pack.Id}.json";
        var achievementsPath = Path.Combine(directory ?? "", achievementsFileName);
        
        if (File.Exists(achievementsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(achievementsPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                
                var achievements = JsonSerializer.Deserialize<List<Achievement>>(json, options);
                
                if (achievements != null && achievements.Count > 0)
                {
                    pack.Achievements = achievements;
                }
            }
            catch
            {
                // If achievements file is invalid, just continue without them
                pack.Achievements = new List<Achievement>();
            }
        }
        else
        {
            // No achievements file, use empty list
            pack.Achievements = new List<Achievement>();
        }
    }
    
    private void ValidatePackInternal(StoryPack pack)
    {
        if (string.IsNullOrEmpty(pack.Id))
            throw new InvalidDataException("Pack ID is required");
        
        if (string.IsNullOrEmpty(pack.Theme))
            throw new InvalidDataException("Pack theme is required");
        
        if (pack.Rooms == null || pack.Rooms.Count == 0)
            throw new InvalidDataException("Pack must contain at least one room");
        
        if (string.IsNullOrEmpty(pack.StartingRoomId))
            throw new InvalidDataException("Starting room ID is required");
        
        // Validate starting room exists
        if (!pack.Rooms.Any(r => r.Id == pack.StartingRoomId))
        {
            throw new InvalidDataException($"Starting room not found: {pack.StartingRoomId}");
        }
        
        // Validate room exits point to existing rooms
        foreach (var room in pack.Rooms)
        {
            if (string.IsNullOrEmpty(room.Id))
                throw new InvalidDataException("Room ID is required");
            
            foreach (var exit in room.Exits.Values)
            {
                if (!pack.Rooms.Any(r => r.Id == exit))
                {
                    throw new InvalidDataException(
                        $"Room {room.Id} has exit to non-existent room: {exit}");
                }
            }
            
            // Validate object references
            foreach (var objId in room.ObjectIds)
            {
                if (pack.Objects == null || !pack.Objects.ContainsKey(objId))
                {
                    throw new InvalidDataException(
                        $"Room {room.Id} references non-existent object: {objId}");
                }
            }
        }
        
        // Validate objects
        if (pack.Objects != null)
        {
            foreach (var kvp in pack.Objects)
            {
                if (string.IsNullOrEmpty(kvp.Value.Id))
                    throw new InvalidDataException("Object ID is required");
                
                if (string.IsNullOrEmpty(kvp.Value.Name))
                    throw new InvalidDataException($"Object {kvp.Value.Id} must have a name");
            }
        }
    }
}
