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
    private readonly object _idLock = new();
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

        int saveId;
        lock (_idLock)
        {
            saveId = _nextId++;

            // Persist the counter before writing the save so a crash cannot leave a stale counter that reuses this ID.
            SaveNextId();
        }

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

        // Write to a temp file and move it into place so a crash never leaves a truncated save behind.
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);

        return saveId;
    }

    /// <summary>
    /// Loads a game state by ID. Returns null when the save does not exist; throws <see cref="InvalidDataException"/> when it exists but cannot be read.
    /// </summary>
    public async Task<GameState?> LoadAsync(int saveId)
    {
        var fileName = $"save_{saveId}.json";
        var filePath = Path.Combine(_savesPath, fileName);

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        SaveFileData? saveData;
        try
        {
            saveData = JsonSerializer.Deserialize<SaveFileData>(json, options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Save {saveId} is corrupt and cannot be loaded.", ex);
        }

        var gameState = saveData?.GameState ?? throw new InvalidDataException($"Save {saveId} does not contain a game state.");
        RestoreGlobalVariableTypes(gameState.World.GlobalVariables);
        return gameState;
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

        // File.Delete is a no-op for missing files, avoiding a check-then-delete race.
        File.Delete(filePath);

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

    // GlobalVariables is typed as object, so System.Text.Json restores every value as a JsonElement.
    // Scalars are converted back to the CLR types they were saved from so comparisons behave as they did before saving.
    // Nulls, objects, and arrays are left as JsonElement because they have no direct CLR equivalent here.
    private static void RestoreGlobalVariableTypes(Dictionary<string, object> variables)
    {
        foreach (var key in variables.Keys.ToList())
        {
            if (variables[key] is not JsonElement element)
            {
                continue;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.True:
                    variables[key] = true;
                    break;
                case JsonValueKind.False:
                    variables[key] = false;
                    break;
                case JsonValueKind.String:
                    variables[key] = element.GetString() ?? string.Empty;
                    break;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                    {
                        variables[key] = intValue;
                    }
                    else if (element.TryGetInt64(out var longValue))
                    {
                        variables[key] = longValue;
                    }
                    else
                    {
                        variables[key] = element.GetDouble();
                    }
                    break;
            }
        }
    }

    private void LoadNextId()
    {
        // The counter file can lag behind existing saves (e.g. after a crash), so never go below the highest existing ID.
        var nextIdFromFile = 1;
        var idFile = Path.Combine(_savesPath, ".nextid");
        if (File.Exists(idFile) && int.TryParse(File.ReadAllText(idFile), out var storedId))
        {
            nextIdFromFile = storedId;
        }

        var nextIdFromSaves = 1;
        foreach (var file in Directory.GetFiles(_savesPath, "save_*.json"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.StartsWith("save_", StringComparison.Ordinal) &&
                int.TryParse(fileName.Substring(5), out var id))
            {
                nextIdFromSaves = Math.Max(nextIdFromSaves, id + 1);
            }
        }

        _nextId = Math.Max(nextIdFromFile, nextIdFromSaves);
    }

    // Must be called while holding _idLock.
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
