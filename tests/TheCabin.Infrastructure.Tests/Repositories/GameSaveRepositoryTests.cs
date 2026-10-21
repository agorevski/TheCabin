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
