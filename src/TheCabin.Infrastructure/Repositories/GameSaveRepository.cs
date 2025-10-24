using System.Text.Json;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Infrastructure.Repositories;

/// <summary>
/// File-based repository for game saves
/// </summary>
public class GameSaveRepository : IGameSaveRepository
{
    private readonly string _savesPath;
    private int _nextId = 1;
    
    public GameSaveRepository(string? savesPath = null)
    {
        _savesPath = savesPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheCabin",
            "Saves");
        
        // Ensure directory exists
        Directory.CreateDirectory(_savesPath);
        
        // Initialize next ID
        LoadNextId();
    }
    
    /// <summary>
    /// Saves a game state
    /// </summary>
    public async Task<int> SaveAsync(string saveName, GameState gameState)
    {
        if (string.IsNullOrEmpty(saveName))
            throw new ArgumentException("Save name cannot be null or empty", nameof(saveName));
        
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState));
        
        var saveId = _nextId++;
        var fileName = $"save_{saveId}.json";
        var filePath = Path.Combine(_savesPath, fileName);
        
        var saveData = new SaveFileData
        {
            Id = saveId,
            Name = saveName,
            ThemeId = gameState.World.CurrentThemeId,
            Timestamp = DateTime.UtcNow,
            PlayTime = gameState.Player.Stats.PlayTime,
            GameState = gameState
        };
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(saveData, options);
        await File.WriteAllTextAsync(filePath, json);
        
        // Save next ID
        SaveNextId();
        
        return saveId;
    }
    
    /// <summary>
    /// Loads a game state by ID
    /// </summary>
    public async Task<GameState?> LoadAsync(int saveId)
    {
        var fileName = $"save_{saveId}.json";
        var filePath = Path.Combine(_savesPath, fileName);
        
        if (!File.Exists(filePath))
            return null;
        
        var json = await File.ReadAllTextAsync(filePath);
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var saveData = JsonSerializer.Deserialize<SaveFileData>(json, options);
        
        return saveData?.GameState;
    }
    
    /// <summary>
    /// Gets all saved games
    /// </summary>
    public async Task<List<GameSaveInfo>> GetAllAsync()
    {
        var saves = new List<GameSaveInfo>();
        
        if (!Directory.Exists(_savesPath))
            return saves;
        
        var files = Directory.GetFiles(_savesPath, "save_*.json");
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var saveData = JsonSerializer.Deserialize<SaveFileData>(json, options);
                
                if (saveData != null)
                {
                    saves.Add(new GameSaveInfo
                    {
                        Id = saveData.Id,
                        Name = saveData.Name,
                        ThemeId = saveData.ThemeId,
                        Timestamp = saveData.Timestamp,
                        PlayTime = saveData.PlayTime,
                        Thumbnail = saveData.Thumbnail
                    });
                }
            }
            catch
            {
                // Skip invalid files
                continue;
            }
        }
        
        return saves.OrderByDescending(s => s.Timestamp).ToList();
    }
    
    /// <summary>
    /// Deletes a saved game
    /// </summary>
    public Task DeleteAsync(int saveId)
    {
        var fileName = $"save_{saveId}.json";
        var filePath = Path.Combine(_savesPath, fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Checks if a save exists
    /// </summary>
    public Task<bool> ExistsAsync(int saveId)
    {
        var fileName = $"save_{saveId}.json";
        var filePath = Path.Combine(_savesPath, fileName);
        
        return Task.FromResult(File.Exists(filePath));
    }
    
    private void LoadNextId()
    {
        var idFile = Path.Combine(_savesPath, ".nextid");
        if (File.Exists(idFile))
        {
            var content = File.ReadAllText(idFile);
            if (int.TryParse(content, out var id))
            {
                _nextId = id;
                return;
            }
        }
        
        // Find highest ID from existing saves
        var files = Directory.GetFiles(_savesPath, "save_*.json");
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.StartsWith("save_") && 
                int.TryParse(fileName.Substring(5), out var id))
            {
                _nextId = Math.Max(_nextId, id + 1);
            }
        }
    }
    
    private void SaveNextId()
    {
        var idFile = Path.Combine(_savesPath, ".nextid");
        File.WriteAllText(idFile, _nextId.ToString());
    }
    
    private class SaveFileData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ThemeId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan PlayTime { get; set; }
        public byte[]? Thumbnail { get; set; }
        public GameState GameState { get; set; } = null!;
    }
}
