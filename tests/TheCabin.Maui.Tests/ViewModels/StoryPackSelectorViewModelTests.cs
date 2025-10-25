using Xunit;
using Moq;
using FluentAssertions;
using TheCabin.Maui.ViewModels;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using System.Collections.Generic;

namespace TheCabin.Maui.Tests.ViewModels;

public class StoryPackSelectorViewModelTests
{
    private readonly Mock<IStoryPackService> _mockStoryPackService;
    private readonly StoryPackSelectorViewModel _viewModel;
    private readonly List<StoryPackInfo> _testPacks;

    public StoryPackSelectorViewModelTests()
    {
        _mockStoryPackService = new Mock<IStoryPackService>();

        // Setup test story packs
        _testPacks = new List<StoryPackInfo>
        {
            new StoryPackInfo
            {
                Id = "classic_horror",
                Theme = "Classic Horror",
                Description = "A spooky cabin in the woods",
                Difficulty = Difficulty.Medium,
                EstimatedPlayTime = 50,
                Tags = new List<string> { "horror", "classic" }
            },
            new StoryPackInfo
            {
                Id = "arctic_survival",
                Theme = "Arctic Survival",
                Description = "Survive the frozen wasteland",
                Difficulty = Difficulty.Hard,
                EstimatedPlayTime = 75,
                Tags = new List<string> { "survival", "arctic" }
            },
            new StoryPackInfo
            {
                Id = "fantasy_magic",
                Theme = "Fantasy Magic",
                Description = "A magical tower full of mysteries",
                Difficulty = Difficulty.Medium,
                EstimatedPlayTime = 40,
                Tags = new List<string> { "fantasy", "magic" }
            },
            new StoryPackInfo
            {
                Id = "sci_fi_isolation",
                Theme = "Sci-Fi Isolation",
                Description = "Alone on a space station",
                Difficulty = Difficulty.Hard,
                EstimatedPlayTime = 70,
                Tags = new List<string> { "sci-fi", "isolation" }
            },
            new StoryPackInfo
            {
                Id = "cozy_mystery",
                Theme = "Cozy Mystery",
                Description = "A cozy cabin mystery",
                Difficulty = Difficulty.Easy,
                EstimatedPlayTime = 35,
                Tags = new List<string> { "cozy", "mystery" }
            }
        };

        _mockStoryPackService.Setup(x => x.GetAvailablePacksAsync())
            .ReturnsAsync(_testPacks);

        _viewModel = new StoryPackSelectorViewModel(_mockStoryPackService.Object);
    }

    #region Initialization Tests (S-01 to S-04)

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("Select Your Story");
    }

    [Fact]
    public void Constructor_InitializesAvailablePacksCollection()
    {
        // Assert
        _viewModel.AvailablePacks.Should().NotBeNull();
        _viewModel.AvailablePacks.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPacks_LoadsFromService()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _mockStoryPackService.Verify(x => x.GetAvailablePacksAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadPacks_PopulatesPacksList()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().HaveCount(5);
    }

    [Fact]
    public async Task LoadPacks_SelectsFirstPackByDefault()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.SelectedPack.Should().NotBeNull();
        _viewModel.SelectedPack!.Id.Should().Be("classic_horror");
    }

    #endregion

    #region Story Pack Card Display Tests (S-10 to S-22)

    [Fact]
    public async Task Packs_DisplayThemeName()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().Contain(p => p.Theme == "Classic Horror");
    }

    [Fact]
    public async Task Packs_DisplayDescription()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().Contain(p => p.Description == "A spooky cabin in the woods");
    }

    [Fact]
    public async Task Packs_DisplayDifficulty()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().Contain(p => p.Difficulty == Difficulty.Medium);
        _viewModel.AvailablePacks.Should().Contain(p => p.Difficulty == Difficulty.Hard);
        _viewModel.AvailablePacks.Should().Contain(p => p.Difficulty == Difficulty.Easy);
    }

    [Fact]
    public async Task Packs_DisplayPlayTime()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().Contain(p => p.EstimatedPlayTime == 50);
    }

    [Fact]
    public async Task AllFivePacks_AreLoaded()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().Contain(p => p.Id == "classic_horror");
        _viewModel.AvailablePacks.Should().Contain(p => p.Id == "arctic_survival");
        _viewModel.AvailablePacks.Should().Contain(p => p.Id == "fantasy_magic");
        _viewModel.AvailablePacks.Should().Contain(p => p.Id == "sci_fi_isolation");
        _viewModel.AvailablePacks.Should().Contain(p => p.Id == "cozy_mystery");
    }

    [Fact]
    public async Task Packs_HaveTags()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().AllSatisfy(p => p.Tags.Should().NotBeEmpty());
    }

    #endregion

    #region Pack Selection Tests (S-30 to S-33)

    [Fact]
    public void SelectPackCommand_IsNotNull()
    {
        // Assert
        _viewModel.SelectPackCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SelectPack_UpdatesSelectedPack()
    {
        // Arrange
        await _viewModel.LoadPacksAsync();
        var packToSelect = _viewModel.AvailablePacks.First(p => p.Id == "arctic_survival");

        // Act
        _viewModel.SelectedPack = packToSelect;

        // Assert
        _viewModel.SelectedPack.Should().Be(packToSelect);
        _viewModel.SelectedPack.Id.Should().Be("arctic_survival");
    }

    [Fact]
    public async Task SelectPack_CanSelectDifferentPacks()
    {
        // Arrange
        await _viewModel.LoadPacksAsync();

        // Act & Assert
        _viewModel.SelectedPack = _viewModel.AvailablePacks[0];
        _viewModel.SelectedPack.Id.Should().Be("classic_horror");

        _viewModel.SelectedPack = _viewModel.AvailablePacks[1];
        _viewModel.SelectedPack.Id.Should().Be("arctic_survival");

        _viewModel.SelectedPack = _viewModel.AvailablePacks[2];
        _viewModel.SelectedPack.Id.Should().Be("fantasy_magic");
    }

    #endregion

    #region Navigation Tests (S-32)

    [Fact]
    public void CancelCommand_IsNotNull()
    {
        // Assert
        _viewModel.CancelCommand.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task LoadAndSelect_WorksTogether()
    {
        // Act
        await _viewModel.LoadPacksAsync();
        var selectedPack = _viewModel.AvailablePacks.First(p => p.Id == "fantasy_magic");
        _viewModel.SelectedPack = selectedPack;

        // Assert
        _viewModel.AvailablePacks.Should().HaveCount(5);
        _viewModel.SelectedPack.Id.Should().Be("fantasy_magic");
    }

    [Fact]
    public async Task EmptyPackList_HandledCorrectly()
    {
        // Arrange
        _mockStoryPackService.Setup(x => x.GetAvailablePacksAsync())
            .ReturnsAsync(new List<StoryPackInfo>());
        var vm = new StoryPackSelectorViewModel(_mockStoryPackService.Object);

        // Act
        await vm.LoadPacksAsync();

        // Assert
        vm.AvailablePacks.Should().BeEmpty();
        vm.SelectedPack.Should().BeNull();
    }

    [Fact]
    public async Task ReloadPacks_ClearsExistingList()
    {
        // Arrange
        await _viewModel.LoadPacksAsync();
        var initialCount = _viewModel.AvailablePacks.Count;

        // Setup with fewer packs
        _mockStoryPackService.Setup(x => x.GetAvailablePacksAsync())
            .ReturnsAsync(new List<StoryPackInfo> { _testPacks[0] });

        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().HaveCount(1);
    }

    #endregion

    #region Difficulty Tests

    [Fact]
    public async Task Packs_ContainVariousDifficulties()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().Contain(p => p.Difficulty == Difficulty.Easy);
        _viewModel.AvailablePacks.Should().Contain(p => p.Difficulty == Difficulty.Medium);
        _viewModel.AvailablePacks.Should().Contain(p => p.Difficulty == Difficulty.Hard);
    }

    [Fact]
    public async Task EasyPack_HasCorrectProperties()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        var easyPack = _viewModel.AvailablePacks.First(p => p.Difficulty == Difficulty.Easy);
        easyPack.Id.Should().Be("cozy_mystery");
        easyPack.Theme.Should().Be("Cozy Mystery");
    }

    [Fact]
    public async Task HardPacks_HaveLongerPlayTime()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        var hardPacks = _viewModel.AvailablePacks.Where(p => p.Difficulty == Difficulty.Hard).ToList();
        hardPacks.Should().HaveCount(2);
        hardPacks.Should().AllSatisfy(p => p.EstimatedPlayTime.Should().BeGreaterOrEqualTo(60));
    }

    #endregion

    #region SelectedPack Tests

    [Fact]
    public void SelectedPack_CanBeNull()
    {
        // Assert
        _viewModel.SelectedPack.Should().BeNull();
    }

    [Fact]
    public async Task SelectedPack_CanBeCleared()
    {
        // Arrange
        await _viewModel.LoadPacksAsync();
        _viewModel.SelectedPack.Should().NotBeNull();

        // Act
        _viewModel.SelectedPack = null;

        // Assert
        _viewModel.SelectedPack.Should().BeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadPacks_HandlesServiceException()
    {
        // Arrange
        _mockStoryPackService.Setup(x => x.GetAvailablePacksAsync())
            .ThrowsAsync(new Exception("Failed to load packs"));
        var vm = new StoryPackSelectorViewModel(_mockStoryPackService.Object);

        // Act
        await vm.LoadPacksAsync();

        // Assert - BaseViewModel catches exceptions and sets ErrorMessage instead of rethrowing
        vm.ErrorMessage.Should().NotBeEmpty();
        vm.ErrorMessage.Should().Contain("Failed to load story packs");
    }

    #endregion

    #region Theme Variety Tests

    [Fact]
    public async Task Packs_CoverDifferentThemes()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert - Verify we have different genre themes
        var themes = _viewModel.AvailablePacks.Select(p => p.Theme).ToList();
        themes.Should().Contain("Classic Horror");
        themes.Should().Contain("Arctic Survival");
        themes.Should().Contain("Fantasy Magic");
        themes.Should().Contain("Sci-Fi Isolation");
        themes.Should().Contain("Cozy Mystery");
    }

    [Fact]
    public async Task AllPacks_HaveRequiredProperties()
    {
        // Act
        await _viewModel.LoadPacksAsync();

        // Assert
        _viewModel.AvailablePacks.Should().AllSatisfy(pack =>
        {
            pack.Id.Should().NotBeNullOrEmpty();
            pack.Theme.Should().NotBeNullOrEmpty();
            pack.Description.Should().NotBeNullOrEmpty();
            pack.EstimatedPlayTime.Should().BeGreaterThan(0);
        });
    }

    #endregion
}
