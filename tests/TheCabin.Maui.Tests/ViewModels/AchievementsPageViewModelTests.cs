using Xunit;
using Moq;
using FluentAssertions;
using TheCabin.Maui.ViewModels;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using System.Collections.Generic;

namespace TheCabin.Maui.Tests.ViewModels;

public class AchievementsPageViewModelTests
{
    private readonly Mock<IAchievementService> _mockAchievementService;
    private readonly AchievementsPageViewModel _viewModel;
    private readonly List<Achievement> _testAchievements;
    private readonly Dictionary<string, AchievementProgress> _testProgress;

    public AchievementsPageViewModelTests()
    {
        _mockAchievementService = new Mock<IAchievementService>();

        // Setup test achievements
        _testAchievements = new List<Achievement>
        {
            new Achievement
            {
                Id = "first_steps",
                Name = "First Steps",
                Description = "Complete your first action",
                Category = "Exploration",
                Points = 10
            },
            new Achievement
            {
                Id = "locked_one",
                Name = "Locked Achievement",
                Description = "This is locked",
                Category = "Story",
                Points = 15
            },
            new Achievement
            {
                Id = "unlocked_one",
                Name = "Unlocked Achievement",
                Description = "This is unlocked",
                Category = "Combat",
                Points = 20
            }
        };

        // Setup test progress
        _testProgress = new Dictionary<string, AchievementProgress>
        {
            ["unlocked_one"] = new AchievementProgress
            {
                AchievementId = "unlocked_one",
                IsUnlocked = true,
                UnlockedDate = DateTime.Now,
                CurrentProgress = 10,
                RequiredProgress = 10
            }
        };

        _mockAchievementService.Setup(x => x.GetAllAchievements())
            .Returns(_testAchievements);
        _mockAchievementService.Setup(x => x.GetAllProgress())
            .Returns(_testProgress);

        _viewModel = new AchievementsPageViewModel(_mockAchievementService.Object);
    }

    #region Initialization Tests (A-01 to A-04)

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("Achievements");
    }

    [Fact]
    public void Constructor_InitializesCollections()
    {
        // Assert
        _viewModel.Achievements.Should().NotBeNull();
        _viewModel.FilteredAchievements.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesDefaultFilter()
    {
        // Assert
        _viewModel.SelectedFilter.Should().Be("All");
    }

    [Fact]
    public async Task LoadAchievements_LoadsFromService()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _mockAchievementService.Verify(x => x.GetAllAchievements(), Times.Once);
        _mockAchievementService.Verify(x => x.GetAllProgress(), Times.Once);
    }

    [Fact]
    public async Task LoadAchievements_PopulatesAchievementsList()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Achievements.Should().HaveCount(3);
    }

    #endregion

    #region Filter Tests (A-10 to A-16)

    [Fact]
    public void FilterAchievementsCommand_IsNotNull()
    {
        // Assert
        _viewModel.FilterAchievementsCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task FilterAll_ShowsAllAchievements()
    {
        // Arrange
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterAchievementsCommand.Execute("All");

        // Assert
        _viewModel.FilteredAchievements.Should().HaveCount(3);
        _viewModel.SelectedFilter.Should().Be("All");
    }

    [Fact]
    public async Task FilterUnlocked_ShowsOnlyUnlockedAchievements()
    {
        // Arrange
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterAchievementsCommand.Execute("Unlocked");

        // Assert
        _viewModel.FilteredAchievements.Should().HaveCount(1);
        _viewModel.FilteredAchievements.Should().OnlyContain(a => a.IsUnlocked);
        _viewModel.SelectedFilter.Should().Be("Unlocked");
    }

    [Fact]
    public async Task FilterLocked_ShowsOnlyLockedAchievements()
    {
        // Arrange
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterAchievementsCommand.Execute("Locked");

        // Assert
        _viewModel.FilteredAchievements.Should().HaveCount(2);
        _viewModel.FilteredAchievements.Should().OnlyContain(a => !a.IsUnlocked);
        _viewModel.SelectedFilter.Should().Be("Locked");
    }

    [Fact]
    public async Task Filter_UpdatesSelectedFilter()
    {
        // Arrange
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterAchievementsCommand.Execute("Unlocked");

        // Assert
        _viewModel.SelectedFilter.Should().Be("Unlocked");
    }

    #endregion

    #region Achievement Display Tests (A-20 to A-30)

    [Fact]
    public async Task Achievements_DisplayName()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Achievements.Should().Contain(a => a.Name == "First Steps");
    }

    [Fact]
    public async Task Achievements_DisplayDescription()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Achievements.Should().Contain(a => a.Description == "Complete your first action");
    }

    [Fact]
    public async Task Achievements_DisplayIcon()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Achievements.Should().Contain(a => a.Icon == "🎯");
    }

    [Fact]
    public async Task Achievements_ShowUnlockedStatus()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        var unlockedAchievement = _viewModel.Achievements.FirstOrDefault(a => a.Id == "unlocked_one");
        unlockedAchievement.Should().NotBeNull();
        unlockedAchievement!.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Achievements_ShowLockedStatus()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        var lockedAchievement = _viewModel.Achievements.FirstOrDefault(a => a.Id == "locked_one");
        lockedAchievement.Should().NotBeNull();
        lockedAchievement!.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockedAchievement_HasUnlockDate()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        var unlockedAchievement = _viewModel.Achievements.FirstOrDefault(a => a.Id == "unlocked_one");
        unlockedAchievement.Should().NotBeNull();
        unlockedAchievement!.UnlockDate.Should().NotBeNull();
    }

    #endregion

    #region Statistics Tests (A-43)

    [Fact]
    public async Task Statistics_CalculatesTotalCount()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Statistics_CalculatesUnlockedCount()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.UnlockedCount.Should().Be(1);
    }

    [Fact]
    public async Task Statistics_CalculatesLockedCount()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LockedCount.Should().Be(2);
    }

    [Fact]
    public async Task Statistics_CalculatesCompletionPercentage()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CompletionPercentage.Should().BeApproximately(0.333, 0.01);
    }

    [Fact]
    public async Task Statistics_FormatsCompletionText()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CompletionText.Should().Contain("1/3");
    }

    [Fact]
    public async Task Statistics_HandlesZeroAchievements()
    {
        // Arrange
        _mockAchievementService.Setup(x => x.GetAllAchievements())
            .Returns(new List<Achievement>());
        var vm = new AchievementsPageViewModel(_mockAchievementService.Object);

        // Act
        await vm.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        vm.CompletionPercentage.Should().Be(0);
        vm.CompletionText.Should().Contain("0/0");
    }

    #endregion

    #region Empty State Tests

    [Fact]
    public async Task HasAchievements_TrueWhenAchievementsExist()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.HasAchievements.Should().BeTrue();
    }

    [Fact]
    public async Task HasAchievements_FalseWhenNoAchievements()
    {
        // Arrange
        _mockAchievementService.Setup(x => x.GetAllAchievements())
            .Returns(new List<Achievement>());
        var vm = new AchievementsPageViewModel(_mockAchievementService.Object);

        // Act
        await vm.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        vm.HasAchievements.Should().BeFalse();
    }

    [Fact]
    public void EmptyStateMessage_IsSet()
    {
        // Assert
        _viewModel.EmptyStateMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Refresh Tests (A-42)

    [Fact]
    public void RefreshCommand_IsNotNull()
    {
        // Assert
        _viewModel.RefreshCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_ReloadsAchievements()
    {
        // Arrange
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);
        var initialCount = _viewModel.Achievements.Count;

        // Act
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        // Assert
        _mockAchievementService.Verify(x => x.GetAllAchievements(), Times.AtLeast(2));
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task FilteredAchievements_SortsUnlockedFirst()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);
        _viewModel.FilterAchievementsCommand.Execute("All");

        // Assert
        var firstAchievement = _viewModel.FilteredAchievements.First();
        firstAchievement.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task FilteredAchievements_ThenSortsByName()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            new Achievement { Id = "a", Name = "Zulu", Description = "Last", IconPath = "1" },
            new Achievement { Id = "b", Name = "Alpha", Description = "First", IconPath = "2" }
        };
        var progress = new Dictionary<string, AchievementProgress>();

        _mockAchievementService.Setup(x => x.GetAllAchievements()).Returns(achievements);
        _mockAchievementService.Setup(x => x.GetAllProgress()).Returns(progress);

        var vm = new AchievementsPageViewModel(_mockAchievementService.Object);

        // Act
        await vm.LoadAchievementsCommand.ExecuteAsync(null);
        vm.FilterAchievementsCommand.Execute("All");

        // Assert
        vm.FilteredAchievements.First().Name.Should().Be("Alpha");
        vm.FilteredAchievements.Last().Name.Should().Be("Zulu");
    }

    #endregion

    #region OnAppearing Tests

    [Fact]
    public async Task OnAppearingAsync_LoadsAchievements()
    {
        // Act
        await _viewModel.OnAppearingAsync();

        // Assert
        _mockAchievementService.Verify(x => x.GetAllAchievements(), Times.Once);
        _viewModel.Achievements.Should().HaveCount(3);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task LoadAndFilter_WorksTogether()
    {
        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);
        _viewModel.FilterAchievementsCommand.Execute("Unlocked");

        // Assert
        _viewModel.Achievements.Should().HaveCount(3);
        _viewModel.FilteredAchievements.Should().HaveCount(1);
        _viewModel.UnlockedCount.Should().Be(1);
    }

    [Fact]
    public async Task MultipleFilters_UpdateCorrectly()
    {
        // Arrange
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Act
        _viewModel.FilterAchievementsCommand.Execute("All");
        var allCount = _viewModel.FilteredAchievements.Count;

        _viewModel.FilterAchievementsCommand.Execute("Unlocked");
        var unlockedCount = _viewModel.FilteredAchievements.Count;

        _viewModel.FilterAchievementsCommand.Execute("Locked");
        var lockedCount = _viewModel.FilteredAchievements.Count;

        // Assert
        allCount.Should().Be(3);
        unlockedCount.Should().Be(1);
        lockedCount.Should().Be(2);
    }

    [Fact]
    public async Task Statistics_UpdateAfterLoad()
    {
        // Arrange
        _viewModel.TotalCount.Should().Be(0);

        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.TotalCount.Should().Be(3);
        _viewModel.UnlockedCount.Should().Be(1);
        _viewModel.LockedCount.Should().Be(2);
    }

    #endregion
}
