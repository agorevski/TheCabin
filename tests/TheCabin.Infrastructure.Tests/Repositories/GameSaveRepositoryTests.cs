using System.Text.Json.Nodes;
using TheCabin.Core.Models;
using TheCabin.Infrastructure.Repositories;

namespace TheCabin.Infrastructure.Tests.Repositories;

public sealed class GameSaveRepositoryTests : IDisposable
{
    private readonly string _root;
    private readonly string _savesPath;

    public GameSaveRepositoryTests()
    {
        _root = Path.Combine(AppContext.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"));
        _savesPath = Path.Combine(_root, "Saves");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsGameStateAndLeavesNoTempFiles()
    {
        var repository = new GameSaveRepository(_savesPath);
        var id = await repository.SaveAsync("Before the storm", CreateState("Ada", 75));

        var loaded = await repository.LoadAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("Ada", loaded.Player.Name);
        Assert.Equal("cabin_porch", loaded.Player.CurrentLocationId);
        Assert.Equal("the-cabin", loaded.World.CurrentThemeId);
        Assert.Equal(42, loaded.World.TurnNumber);
        Assert.Equal(75, loaded.Progress.Score);
        Assert.True(loaded.Progress.StoryFlags["door_unlocked"]);
        Assert.Equal("The door creaks open.", Assert.Single(loaded.StoryLog).Text);
        Assert.Empty(Directory.GetFiles(_savesPath, "*.tmp"));
    }

    [Fact]
    public async Task SaveAndLoad_RestoresGlobalVariablesToOriginalTypes()
    {
        var repository = new GameSaveRepository(_savesPath);
        var state = CreateState("Ada", 1);
        state.World.GlobalVariables["lamp_lit"] = true;
        state.World.GlobalVariables["matches_left"] = 3;
        state.World.GlobalVariables["visitor_name"] = "Mara";
        var id = await repository.SaveAsync("Globals", state);

        var loaded = await repository.LoadAsync(id);

        Assert.NotNull(loaded);
        var variables = loaded.World.GlobalVariables;
        Assert.True(variables["lamp_lit"] is true);
        Assert.True(variables["matches_left"] is int matches && matches == 3);
        Assert.Equal("Mara", variables["visitor_name"] as string);
    }

    [Fact]
    public async Task LoadAsync_MissingSave_ReturnsNull()
    {
        var repository = new GameSaveRepository(_savesPath);

        var loaded = await repository.LoadAsync(999);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSave_AndIsIdempotent()
    {
        var repository = new GameSaveRepository(_savesPath);
        var id = await repository.SaveAsync("Temporary", CreateState("Bob", 1));

        await repository.DeleteAsync(id);
        await repository.DeleteAsync(id);

        Assert.False(await repository.ExistsAsync(id));
        Assert.Null(await repository.LoadAsync(id));
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task ConcurrentSaves_AssignUniqueIdsAndPersistEveryState()
    {
        var repository = new GameSaveRepository(_savesPath);

        var ids = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => repository.SaveAsync($"Save {i}", CreateState($"Player{i}", i)))));

        Assert.Equal(50, ids.Distinct().Count());
        Assert.Equal(50, (await repository.GetAllAsync()).Count);
        foreach (var id in ids)
        {
            Assert.NotNull(await repository.LoadAsync(id));
        }
    }

    [Fact]
    public async Task NewRepository_WithStaleCounter_DoesNotOverwriteExistingSaves()
    {
        var first = new GameSaveRepository(_savesPath);
        var firstId = await first.SaveAsync("First", CreateState("Original", 10));
        await first.SaveAsync("Second", CreateState("Other", 20));

        // Simulate a crash that left the counter file behind the saves on disk.
        await File.WriteAllTextAsync(Path.Combine(_savesPath, ".nextid"), firstId.ToString());

        var restarted = new GameSaveRepository(_savesPath);
        var newId = await restarted.SaveAsync("After restart", CreateState("Newcomer", 30));

        Assert.True(newId > 2);
        var original = await restarted.LoadAsync(firstId);
        Assert.Equal("Original", original?.Player.Name);
    }

    [Fact]
    public async Task LoadAsync_CorruptSave_ThrowsInvalidDataAndIsSkippedByGetAll()
    {
        var repository = new GameSaveRepository(_savesPath);
        var goodId = await repository.SaveAsync("Good", CreateState("Ada", 5));
        var corruptId = goodId + 1;
        await File.WriteAllTextAsync(Path.Combine(_savesPath, $"save_{corruptId}.json"), "{ \"gameState\": { \"player\":");

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync(corruptId));

        var saves = await repository.GetAllAsync();
        Assert.Equal(goodId, Assert.Single(saves).Id);
    }

    [Fact]
    public async Task SaveAsync_PathLikeSaveName_IsStoredAsDataAndNeverEscapesSaveDirectory()
    {
        var repository = new GameSaveRepository(_savesPath);
        const string maliciousName = "../../escaped";

        var id = await repository.SaveAsync(maliciousName, CreateState("Ada", 1));

        Assert.Equal(maliciousName, Assert.Single(await repository.GetAllAsync()).Name);
        Assert.NotNull(await repository.LoadAsync(id));
        var allFiles = Directory.GetFiles(_root, "*", SearchOption.AllDirectories);
        Assert.All(allFiles, file => Assert.StartsWith(_savesPath, file, StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(_root, "escaped")));
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsAchievementProgressAndUnlocks()
    {
        var repository = new GameSaveRepository(_savesPath);
        var state = CreateState("Ada", 1);
        var unlockedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        state.Progress.AchievementProgress["explorer"] = new AchievementProgress
        {
            AchievementId = "explorer",
            CurrentProgress = 3,
            RequiredProgress = 3,
            IsUnlocked = true,
            UnlockedDate = unlockedAt
        };
        state.Progress.AchievementProgress["collector"] = new AchievementProgress
        {
            AchievementId = "collector",
            CurrentProgress = 1,
            RequiredProgress = 4
        };
        state.Progress.UnlockedAchievements.Add("explorer");
        var id = await repository.SaveAsync("Achievements", state);

        var loaded = await repository.LoadAsync(id);

        Assert.NotNull(loaded);
        var explorer = loaded.Progress.AchievementProgress["explorer"];
        Assert.True(explorer.IsUnlocked);
        Assert.Equal(unlockedAt, explorer.UnlockedDate);
        Assert.Equal(3, explorer.CurrentProgress);
        var collector = loaded.Progress.AchievementProgress["collector"];
        Assert.False(collector.IsUnlocked);
        Assert.Equal(1, collector.CurrentProgress);
        Assert.Equal(4, collector.RequiredProgress);
        Assert.Equal(new[] { "explorer" }, loaded.Progress.UnlockedAchievements);
    }

    [Fact]
    public async Task LoadAsync_SaveWrittenBeforeAchievementProgress_LoadsWithEmptyProgress()
    {
        var repository = new GameSaveRepository(_savesPath);
        var id = await repository.SaveAsync("Old", CreateState("Ada", 5));

        // Remove the progress key to match a save written before achievement progress was stored with the game
        var filePath = Path.Combine(_savesPath, $"save_{id}.json");
        var root = JsonNode.Parse(await File.ReadAllTextAsync(filePath))!;
        Assert.True(root["gameState"]!["progress"]!.AsObject().Remove("achievementProgress"));
        await File.WriteAllTextAsync(filePath, root.ToJsonString());

        var loaded = await repository.LoadAsync(id);

        Assert.NotNull(loaded);
        Assert.Empty(loaded.Progress.AchievementProgress);
        Assert.Equal(5, loaded.Progress.Score);
        Assert.True(loaded.Progress.StoryFlags["door_unlocked"]);
    }

    private static GameState CreateState(string playerName, int score)
    {
        var state = new GameState();
        state.Player.Name = playerName;
        state.Player.CurrentLocationId = "cabin_porch";
        state.World.CurrentThemeId = "the-cabin";
        state.World.TurnNumber = 42;
        state.Progress.Score = score;
        state.Progress.StoryFlags["door_unlocked"] = true;
        state.StoryLog.Add(new NarrativeEntry { Text = "The door creaks open.", Type = NarrativeType.Description });
        return state;
    }
}
