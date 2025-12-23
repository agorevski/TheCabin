using Microsoft.Extensions.Logging;
using Moq;
using TheCabin.Core.Models;
using TheCabin.Core.Services;
using Xunit;

namespace TheCabin.Core.Tests.Services;

public class AchievementServiceTests
{
    private readonly Mock<ILogger<AchievementService>> _mockLogger;
    private readonly AchievementService _service;

    public AchievementServiceTests()
    {
        _mockLogger = new Mock<ILogger<AchievementService>>();
        _service = new AchievementService(_mockLogger.Object);

        // Clean up any existing progress file from previous test runs
        var progressFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheCabin",
            "achievement_progress.json");

        if (File.Exists(progressFilePath))
        {
            File.Delete(progressFilePath);
        }
    }

    [Fact]
    public async Task InitializeAsync_LoadsAchievements_Successfully()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited),
            CreateTestAchievement("test2", "Test 2", TriggerType.ItemCollected)
        };

        // Act
        await _service.InitializeAsync(achievements);

        // Assert
        var allAchievements = _service.GetAllAchievements();
        Assert.Equal(2, allAchievements.Count);
        Assert.Contains(allAchievements, a => a.Id == "test1");
        Assert.Contains(allAchievements, a => a.Id == "test2");
    }

    [Fact]
    public async Task InitializeAsync_CreatesProgressForEachAchievement()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited)
        };

        // Act
        await _service.InitializeAsync(achievements);

        // Assert
        var progress = _service.GetProgress("test1");
        Assert.NotNull(progress);
        Assert.Equal("test1", progress.AchievementId);
        Assert.False(progress.IsUnlocked);
        Assert.Equal(0, progress.CurrentProgress);
    }

    [Fact]
    public async Task TrackEventAsync_RoomVisited_IncrementsProgress()
    {
        // Arrange
        var achievement = CreateTestAchievement("room_visitor", "Room Visitor",
            TriggerType.RoomVisited, "main_room");
        await _service.InitializeAsync(new List<Achievement> { achievement });
        var gameState = CreateTestGameState();

        // Act
        var unlocked = await _service.TrackEventAsync(TriggerType.RoomVisited,
            "main_room", gameState);

        // Assert
        var progress = _service.GetProgress("room_visitor");
        Assert.Equal(1, progress!.CurrentProgress);
        Assert.Single(unlocked);
        Assert.True(progress.IsUnlocked);
    }

    [Fact]
    public async Task TrackEventAsync_MultipleEvents_AccumulatesProgress()
    {
        // Arrange
        var achievement = CreateTestAchievement("collector", "Collector",
            TriggerType.ItemCollected, "", 3);
        await _service.InitializeAsync(new List<Achievement> { achievement });
        var gameState = CreateTestGameState();

        // Act
        await _service.TrackEventAsync(TriggerType.ItemCollected, "item1", gameState);
        await _service.TrackEventAsync(TriggerType.ItemCollected, "item2", gameState);
        var unlocked = await _service.TrackEventAsync(TriggerType.ItemCollected,
            "item3", gameState);

        // Assert
        var progress = _service.GetProgress("collector");
        Assert.Equal(3, progress!.CurrentProgress);
        Assert.True(progress.IsUnlocked);
        Assert.Single(unlocked);
    }

    [Fact]
    public async Task TrackEventAsync_WrongTriggerType_DoesNotIncrementProgress()
    {
        // Arrange
        var achievement = CreateTestAchievement("room_visitor", "Room Visitor",
            TriggerType.RoomVisited);
        await _service.InitializeAsync(new List<Achievement> { achievement });
        var gameState = CreateTestGameState();

        // Act
        await _service.TrackEventAsync(TriggerType.ItemCollected, "item1", gameState);

        // Assert
        var progress = _service.GetProgress("room_visitor");
        Assert.Equal(0, progress!.CurrentProgress);
        Assert.False(progress.IsUnlocked);
    }

    [Fact]
    public async Task TrackEventAsync_WrongTarget_DoesNotIncrementProgress()
    {
        // Arrange
        var achievement = CreateTestAchievement("specific_room", "Specific Room",
            TriggerType.RoomVisited, "target_room");
        await _service.InitializeAsync(new List<Achievement> { achievement });
        var gameState = CreateTestGameState();

        // Act
        await _service.TrackEventAsync(TriggerType.RoomVisited, "other_room", gameState);

        // Assert
        var progress = _service.GetProgress("specific_room");
        Assert.Equal(0, progress!.CurrentProgress);
    }

    [Fact]
    public async Task TrackEventAsync_WithRequiredFlags_OnlyUnlocksWhenFlagsSet()
    {
        // Arrange
        var achievement = CreateTestAchievement("flag_dependent", "Flag Dependent",
            TriggerType.RoomVisited);
        achievement.RequiredFlags.Add("required_flag");
        await _service.InitializeAsync(new List<Achievement> { achievement });

        var gameState = CreateTestGameState();

        // Act - without flag
        var unlocked1 = await _service.TrackEventAsync(TriggerType.RoomVisited,
            "room1", gameState);

        // Assert - not unlocked
        Assert.Empty(unlocked1);
        Assert.False(_service.IsUnlocked("flag_dependent"));

        // Act - with flag
        gameState.Progress.StoryFlags["required_flag"] = true;
        var unlocked2 = await _service.TrackEventAsync(TriggerType.RoomVisited,
            "room1", gameState);

        // Assert - now unlocked
        Assert.Single(unlocked2);
        Assert.True(_service.IsUnlocked("flag_dependent"));
    }

    [Fact]
    public async Task TrackEventAsync_WithMinHealthCondition_OnlyUnlocksWhenMet()
    {
        // Arrange
        var achievement = CreateTestAchievement("healthy", "Healthy",
            TriggerType.CommandExecuted);
        achievement.Trigger.Conditions["minHealth"] = 80;
        await _service.InitializeAsync(new List<Achievement> { achievement });

        var gameState = CreateTestGameState();

        // Act - with low health
        gameState.Player.Health = 50;
        var unlocked1 = await _service.TrackEventAsync(TriggerType.CommandExecuted,
            "test", gameState);

        // Assert - not unlocked
        Assert.Empty(unlocked1);

        // Act - with high health
        gameState.Player.Health = 100;
        var unlocked2 = await _service.TrackEventAsync(TriggerType.CommandExecuted,
            "test", gameState);

        // Assert - now unlocked
        Assert.Single(unlocked2);
    }

    [Fact]
    public async Task TrackEventAsync_WithMinInventoryCondition_OnlyUnlocksWhenMet()
    {
        // Arrange
        var achievement = CreateTestAchievement("hoarder", "Hoarder",
            TriggerType.ItemCollected);
        achievement.Trigger.Conditions["minInventory"] = 3;
        await _service.InitializeAsync(new List<Achievement> { achievement });

        var gameState = CreateTestGameState();

        // Act - with 2 items
        gameState.Player.Inventory.Items.Add(new GameObject { Id = "item1" });
        gameState.Player.Inventory.Items.Add(new GameObject { Id = "item2" });
        var unlocked1 = await _service.TrackEventAsync(TriggerType.ItemCollected,
            "item2", gameState);

        // Assert - not unlocked
        Assert.Empty(unlocked1);

        // Act - with 3 items
        gameState.Player.Inventory.Items.Add(new GameObject { Id = "item3" });
        var unlocked2 = await _service.TrackEventAsync(TriggerType.ItemCollected,
            "item3", gameState);

        // Assert - now unlocked
        Assert.Single(unlocked2);
    }

    [Fact]
    public async Task UnlockAchievementAsync_ManuallyUnlocksAchievement()
    {
        // Arrange
        var achievement = CreateTestAchievement("manual", "Manual",
            TriggerType.RoomVisited);
        await _service.InitializeAsync(new List<Achievement> { achievement });

        // Act
        var notification = await _service.UnlockAchievementAsync("manual");

        // Assert
        Assert.NotNull(notification);
        Assert.Equal("manual", notification.Achievement.Id);
        Assert.True(_service.IsUnlocked("manual"));
    }

    [Fact]
    public async Task UnlockAchievementAsync_AlreadyUnlocked_ReturnsNull()
    {
        // Arrange
        var achievement = CreateTestAchievement("already_unlocked", "Already Unlocked",
            TriggerType.RoomVisited);
        await _service.InitializeAsync(new List<Achievement> { achievement });
        await _service.UnlockAchievementAsync("already_unlocked");

        // Act
        var notification = await _service.UnlockAchievementAsync("already_unlocked");

        // Assert
        Assert.Null(notification);
    }

    [Fact]
    public async Task UnlockAchievementAsync_UnknownAchievement_ReturnsNull()
    {
        // Arrange
        await _service.InitializeAsync(new List<Achievement>());

        // Act
        var notification = await _service.UnlockAchievementAsync("unknown");

        // Assert
        Assert.Null(notification);
    }

    [Fact]
    public async Task IsUnlocked_UnlockedAchievement_ReturnsTrue()
    {
        // Arrange
        var achievement = CreateTestAchievement("test", "Test",
            TriggerType.RoomVisited);
        await _service.InitializeAsync(new List<Achievement> { achievement });
        await _service.UnlockAchievementAsync("test");

        // Act
        var isUnlocked = _service.IsUnlocked("test");

        // Assert
        Assert.True(isUnlocked);
    }

    [Fact]
    public async Task IsUnlocked_LockedAchievement_ReturnsFalse()
    {
        // Arrange
        var achievement = CreateTestAchievement("test", "Test",
            TriggerType.RoomVisited);
        await _service.InitializeAsync(new List<Achievement> { achievement });

        // Act
        var isUnlocked = _service.IsUnlocked("test");

        // Assert
        Assert.False(isUnlocked);
    }

    [Fact]
    public async Task GetStats_CalculatesCorrectStatistics()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievementWithPoints("test1", "Test 1", 10, "Category1"),
            CreateTestAchievementWithPoints("test2", "Test 2", 20, "Category1"),
            CreateTestAchievementWithPoints("test3", "Test 3", 30, "Category2")
        };
        await _service.InitializeAsync(achievements);
        await _service.UnlockAchievementAsync("test1");
        await _service.UnlockAchievementAsync("test2");

        // Act
        var stats = _service.GetStats();

        // Assert
        Assert.Equal(3, stats.TotalAchievements);
        Assert.Equal(2, stats.UnlockedAchievements);
        Assert.Equal(60, stats.TotalPoints);
        Assert.Equal(30, stats.EarnedPoints);
        Assert.Equal(66.67f, stats.CompletionPercentage, 1); // Relaxed precision
    }

    [Fact]
    public async Task GetStats_CalculatesCategoryProgress()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievementWithPoints("test1", "Test 1", 10, "Exploration"),
            CreateTestAchievementWithPoints("test2", "Test 2", 20, "Exploration"),
            CreateTestAchievementWithPoints("test3", "Test 3", 30, "Combat")
        };
        await _service.InitializeAsync(achievements);
        await _service.UnlockAchievementAsync("test1");

        // Act
        var stats = _service.GetStats();

        // Assert
        Assert.Equal(2, stats.CategoryProgress.Count);
        Assert.Equal(1, stats.CategoryProgress["Exploration"]); // 1 out of 2 unlocked
        Assert.Equal(0, stats.CategoryProgress["Combat"]); // 0 out of 1 unlocked
    }

    [Fact]
    public async Task GetByCategory_ReturnsOnlyMatchingCategory()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievementWithPoints("test1", "Test 1", 10, "Exploration"),
            CreateTestAchievementWithPoints("test2", "Test 2", 20, "Exploration"),
            CreateTestAchievementWithPoints("test3", "Test 3", 30, "Combat")
        };
        await _service.InitializeAsync(achievements);

        // Act
        var explorationAchievements = _service.GetByCategory("Exploration");

        // Assert
        Assert.Equal(2, explorationAchievements.Count);
        Assert.All(explorationAchievements, a => Assert.Equal("Exploration", a.Category));
    }

    [Fact]
    public async Task GetUnlockedAchievements_ReturnsOnlyUnlocked()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited),
            CreateTestAchievement("test2", "Test 2", TriggerType.RoomVisited),
            CreateTestAchievement("test3", "Test 3", TriggerType.RoomVisited)
        };
        await _service.InitializeAsync(achievements);
        await _service.UnlockAchievementAsync("test1");
        await _service.UnlockAchievementAsync("test3");

        // Act
        var unlocked = _service.GetUnlockedAchievements();

        // Assert
        Assert.Equal(2, unlocked.Count);
        Assert.Contains(unlocked, a => a.Id == "test1");
        Assert.Contains(unlocked, a => a.Id == "test3");
    }

    [Fact]
    public async Task GetLockedAchievements_ReturnsOnlyLocked()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited),
            CreateTestAchievement("test2", "Test 2", TriggerType.RoomVisited),
            CreateTestAchievement("test3", "Test 3", TriggerType.RoomVisited)
        };
        await _service.InitializeAsync(achievements);
        await _service.UnlockAchievementAsync("test1");

        // Act
        var locked = _service.GetLockedAchievements();

        // Assert - test2 and test3 should be locked
        Assert.Equal(2, locked.Count);
        Assert.Contains(locked, a => a.Id == "test2");
        Assert.Contains(locked, a => a.Id == "test3");
        Assert.DoesNotContain(locked, a => a.Id == "test1");
    }

    [Fact]
    public async Task GetLockedAchievements_ExcludesSecretByDefault()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited),
            CreateSecretAchievement("secret1", "Secret 1", TriggerType.RoomVisited)
        };
        await _service.InitializeAsync(achievements);

        // Act
        var locked = _service.GetLockedAchievements(includeSecret: false);

        // Assert
        Assert.Single(locked);
        Assert.DoesNotContain(locked, a => a.IsSecret);
    }

    [Fact]
    public async Task GetLockedAchievements_IncludesSecretWhenRequested()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited),
            CreateSecretAchievement("secret1", "Secret 1", TriggerType.RoomVisited)
        };
        await _service.InitializeAsync(achievements);

        // Act
        var locked = _service.GetLockedAchievements(includeSecret: true);

        // Assert - both should be locked since we didn't unlock any
        Assert.Equal(2, locked.Count);
        Assert.Contains(locked, a => a.Id == "test1");
        Assert.Contains(locked, a => a.Id == "secret1" && a.IsSecret);
    }

    [Fact]
    public async Task ResetAllProgressAsync_ClearsAllProgress()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited),
            CreateTestAchievement("test2", "Test 2", TriggerType.RoomVisited)
        };
        await _service.InitializeAsync(achievements);
        await _service.UnlockAchievementAsync("test1");
        await _service.UnlockAchievementAsync("test2");

        // Act
        await _service.ResetAllProgressAsync();

        // Assert
        Assert.False(_service.IsUnlocked("test1"));
        Assert.False(_service.IsUnlocked("test2"));
        var stats = _service.GetStats();
        Assert.Equal(0, stats.UnlockedAchievements);
    }

    [Fact]
    public async Task GetProgress_ExistingAchievement_ReturnsProgress()
    {
        // Arrange
        var achievement = CreateTestAchievement("test", "Test",
            TriggerType.RoomVisited, "", 5);
        await _service.InitializeAsync(new List<Achievement> { achievement });

        // Act
        var progress = _service.GetProgress("test");

        // Assert
        Assert.NotNull(progress);
        Assert.Equal("test", progress.AchievementId);
        Assert.Equal(5, progress.RequiredProgress); // Uses the requiredCount from trigger
    }

    [Fact]
    public void GetProgress_NonExistentAchievement_ReturnsNull()
    {
        // Act
        var progress = _service.GetProgress("nonexistent");

        // Assert
        Assert.Null(progress);
    }

    [Fact]
    public async Task GetAllProgress_ReturnsAllProgressEntries()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            CreateTestAchievement("test1", "Test 1", TriggerType.RoomVisited),
            CreateTestAchievement("test2", "Test 2", TriggerType.RoomVisited)
        };
        await _service.InitializeAsync(achievements);

        // Act
        var allProgress = _service.GetAllProgress();

        // Assert
        Assert.Equal(2, allProgress.Count);
        Assert.Contains("test1", allProgress.Keys);
        Assert.Contains("test2", allProgress.Keys);
    }

    // Helper methods
    private Achievement CreateTestAchievement(string id, string name,
        TriggerType triggerType, string targetId = "", int requiredCount = 1)
    {
        return new Achievement
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            Category = "Test",
            Points = 10,
            Trigger = new AchievementTrigger
            {
                Type = triggerType,
                TargetId = targetId,
                RequiredCount = requiredCount
            }
        };
    }

    private Achievement CreateTestAchievementWithPoints(string id, string name,
        int points, string category)
    {
        return new Achievement
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            Category = category,
            Points = points,
            Trigger = new AchievementTrigger
            {
                Type = TriggerType.RoomVisited,
                RequiredCount = 1
            }
        };
    }

    private Achievement CreateSecretAchievement(string id, string name,
        TriggerType triggerType)
    {
        var achievement = CreateTestAchievement(id, name, triggerType);
        achievement.IsSecret = true;
        return achievement;
    }

    private GameState CreateTestGameState()
    {
        return new GameState
        {
            Player = new Player
            {
                Health = 100,
                CurrentLocationId = "main_room",
                Inventory = new Inventory(),
                Stats = new PlayerStats()
            },
            Progress = new ProgressState()
        };
    }
}
