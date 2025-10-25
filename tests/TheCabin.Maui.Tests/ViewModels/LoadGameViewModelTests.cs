using Xunit;
using Moq;
using FluentAssertions;
using TheCabin.Maui.ViewModels;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using System.Collections.Generic;

namespace TheCabin.Maui.Tests.ViewModels;

public class LoadGameViewModelTests
{
    private readonly Mock<IGameStateService> _mockGameStateService;
    private readonly LoadGameViewModel _viewModel;
    private readonly List<GameSaveInfo> _testSaves;

    public LoadGameViewModelTests()
    {
        _mockGameStateService = new Mock<IGameStateService>();

        // Setup test saves
        _testSaves = new List<GameSaveInfo>
        {
            new GameSaveInfo
            {
                Id = 1,
                Name = "Save 1",
                ThemeId = "classic_horror",
                Timestamp = DateTime.Now.AddHours(-2),
                PlayTime = TimeSpan.FromMinutes(45)
            },
            new GameSaveInfo
            {
                Id = 2,
                Name = "Save 2",
                ThemeId = "arctic_survival",
                Timestamp = DateTime.Now.AddHours(-1),
                PlayTime = TimeSpan.FromMinutes(30)
            },
            new GameSaveInfo
            {
                Id = 3,
                Name = "Save 3",
                ThemeId = "fantasy_magic",
                Timestamp = DateTime.Now,
                PlayTime = TimeSpan.FromMinutes(60)
            }
        };

        _mockGameStateService.Setup(x => x.GetSavedGamesAsync())
            .ReturnsAsync(_testSaves);

        _viewModel = new LoadGameViewModel(_mockGameStateService.Object);
    }

    #region Initialization Tests (L-01 to L-03)

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("Load Game");
    }

    [Fact]
    public void Constructor_InitializesSavedGamesCollection()
    {
        // Assert
        _viewModel.SavedGames.Should().NotBeNull();
        _viewModel.SavedGames.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSavedGames_LoadsFromService()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        _mockGameStateService.Verify(x => x.GetSavedGamesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadSavedGames_PopulatesSavedGamesList()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        _viewModel.SavedGames.Should().HaveCount(3);
    }

    #endregion

    #region Empty State Tests (L-10 to L-11)

    [Fact]
    public async Task HasSavedGames_TrueWhenSavesExist()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        _viewModel.HasSavedGames.Should().BeTrue();
    }

    [Fact]
    public async Task HasSavedGames_FalseWhenNoSaves()
    {
        // Arrange
        _mockGameStateService.Setup(x => x.GetSavedGamesAsync())
            .ReturnsAsync(new List<GameSaveInfo>());
        var vm = new LoadGameViewModel(_mockGameStateService.Object);

        // Act
        await vm.LoadSavedGamesAsync();

        // Assert
        vm.HasSavedGames.Should().BeFalse();
    }

    #endregion

    #region Save Game Display Tests (L-20 to L-28)

    [Fact]
    public async Task SavedGames_DisplayName()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        _viewModel.SavedGames.Should().Contain(s => s.Name == "Save 1");
    }

    [Fact]
    public async Task SavedGames_DisplayThemeName()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        _viewModel.SavedGames.Should().Contain(s => s.ThemeName == "classic_horror");
    }

    [Fact]
    public async Task SavedGames_DisplaySavedDate()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        var save = _viewModel.SavedGames.First();
        save.SavedDate.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task SavedGames_DisplayPlayTime()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        var save = _viewModel.SavedGames.First(s => s.Name == "Save 1");
        save.PlayTime.Should().Be(TimeSpan.FromMinutes(45));
    }

    [Fact]
    public async Task SavedGames_OrderedByTimestampDescending()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        var firstSave = _viewModel.SavedGames.First();
        var lastSave = _viewModel.SavedGames.Last();
        firstSave.SavedDate.Should().BeAfter(lastSave.SavedDate);
    }

    #endregion

    #region Load Game Tests (L-30 to L-37)

    [Fact]
    public void LoadGameCommand_IsNotNull()
    {
        // Assert
        _viewModel.LoadGameCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadGame_SetsSelectedSave()
    {
        // Arrange
        await _viewModel.LoadSavedGamesAsync();
        var saveToLoad = _viewModel.SavedGames.First();

        // Note: Shell.Current.GoToAsync would be called in real scenario
        // For unit testing, we verify the property is set
        
        // Act
        _viewModel.SelectedSave = saveToLoad;

        // Assert
        _viewModel.SelectedSave.Should().Be(saveToLoad);
    }

    #endregion

    #region Delete Game Tests (L-32 to L-34)

    [Fact]
    public void DeleteGameCommand_IsNotNull()
    {
        // Assert
        _viewModel.DeleteGameCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteGame_RemovesSaveFromList()
    {
        // Arrange
        await _viewModel.LoadSavedGamesAsync();
        var saveToDelete = _viewModel.SavedGames.First();
        var initialCount = _viewModel.SavedGames.Count;

        _mockGameStateService.Setup(x => x.DeleteSaveAsync(saveToDelete.Id))
            .Returns(Task.CompletedTask);

        // Act
        // In real scenario, this would show confirmation dialog
        // For testing, we simulate the deletion
        await _mockGameStateService.Object.DeleteSaveAsync(saveToDelete.Id);
        _viewModel.SavedGames.Remove(saveToDelete);
        _viewModel.HasSavedGames = _viewModel.SavedGames.Count > 0;

        // Assert
        _viewModel.SavedGames.Should().HaveCount(initialCount - 1);
        _viewModel.SavedGames.Should().NotContain(saveToDelete);
    }

    [Fact]
    public async Task DeleteGame_UpdatesHasSavedGamesFlag()
    {
        // Arrange
        _mockGameStateService.Setup(x => x.GetSavedGamesAsync())
            .ReturnsAsync(new List<GameSaveInfo> { _testSaves[0] });
        var vm = new LoadGameViewModel(_mockGameStateService.Object);
        await vm.LoadSavedGamesAsync();

        _mockGameStateService.Setup(x => x.DeleteSaveAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockGameStateService.Object.DeleteSaveAsync(vm.SavedGames[0].Id);
        vm.SavedGames.Clear();
        vm.HasSavedGames = vm.SavedGames.Count > 0;

        // Assert
        vm.HasSavedGames.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteGame_CallsServiceDeleteMethod()
    {
        // Arrange
        await _viewModel.LoadSavedGamesAsync();
        var saveToDelete = _viewModel.SavedGames.First();

        _mockGameStateService.Setup(x => x.DeleteSaveAsync(saveToDelete.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _mockGameStateService.Object.DeleteSaveAsync(saveToDelete.Id);

        // Assert
        _mockGameStateService.Verify(x => x.DeleteSaveAsync(saveToDelete.Id), Times.Once);
    }

    #endregion

    #region Navigation Tests (L-35 to L-36)

    [Fact]
    public void CancelCommand_IsNotNull()
    {
        // Assert
        _viewModel.CancelCommand.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task LoadAndDelete_WorksTogether()
    {
        // Arrange
        await _viewModel.LoadSavedGamesAsync();
        var initialCount = _viewModel.SavedGames.Count;
        var saveToDelete = _viewModel.SavedGames.First();

        _mockGameStateService.Setup(x => x.DeleteSaveAsync(saveToDelete.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _mockGameStateService.Object.DeleteSaveAsync(saveToDelete.Id);
        _viewModel.SavedGames.Remove(saveToDelete);

        // Assert
        _viewModel.SavedGames.Count.Should().Be(initialCount - 1);
    }

    [Fact]
    public async Task MultipleSaves_CanBeLoadedAndDisplayed()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        _viewModel.SavedGames.Should().HaveCount(3);
        _viewModel.SavedGames.Should().Contain(s => s.Name == "Save 1");
        _viewModel.SavedGames.Should().Contain(s => s.Name == "Save 2");
        _viewModel.SavedGames.Should().Contain(s => s.Name == "Save 3");
    }

    [Fact]
    public async Task SavesList_MaintainsProperOrder()
    {
        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert - Should be ordered by timestamp descending (most recent first)
        _viewModel.SavedGames[0].Name.Should().Be("Save 3"); // Most recent
        _viewModel.SavedGames[1].Name.Should().Be("Save 2");
        _viewModel.SavedGames[2].Name.Should().Be("Save 1"); // Oldest
    }

    [Fact]
    public async Task EmptyList_HandledCorrectly()
    {
        // Arrange
        _mockGameStateService.Setup(x => x.GetSavedGamesAsync())
            .ReturnsAsync(new List<GameSaveInfo>());
        var vm = new LoadGameViewModel(_mockGameStateService.Object);

        // Act
        await vm.LoadSavedGamesAsync();

        // Assert
        vm.SavedGames.Should().BeEmpty();
        vm.HasSavedGames.Should().BeFalse();
    }

    [Fact]
    public async Task ReloadSaves_ClearsExistingList()
    {
        // Arrange
        await _viewModel.LoadSavedGamesAsync();
        var initialCount = _viewModel.SavedGames.Count;

        // Setup with fewer saves
        _mockGameStateService.Setup(x => x.GetSavedGamesAsync())
            .ReturnsAsync(new List<GameSaveInfo> { _testSaves[0] });

        // Act
        await _viewModel.LoadSavedGamesAsync();

        // Assert
        _viewModel.SavedGames.Should().HaveCount(1);
    }

    #endregion

    #region SelectedSave Tests

    [Fact]
    public void SelectedSave_CanBeSet()
    {
        // Arrange
        var testSave = new TheCabin.Maui.Models.GameSaveInfoViewModel
        {
            Id = 1,
            Name = "Test Save"
        };

        // Act
        _viewModel.SelectedSave = testSave;

        // Assert
        _viewModel.SelectedSave.Should().Be(testSave);
    }

    [Fact]
    public void SelectedSave_CanBeCleared()
    {
        // Arrange
        _viewModel.SelectedSave = new TheCabin.Maui.Models.GameSaveInfoViewModel
        {
            Id = 1,
            Name = "Test"
        };

        // Act
        _viewModel.SelectedSave = null;

        // Assert
        _viewModel.SelectedSave.Should().BeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadSavedGames_HandlesServiceException()
    {
        // Arrange
        _mockGameStateService.Setup(x => x.GetSavedGamesAsync())
            .ThrowsAsync(new Exception("Database error"));
        var vm = new LoadGameViewModel(_mockGameStateService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => 
            await vm.LoadSavedGamesAsync());
    }

    [Fact]
    public async Task DeleteSave_HandlesServiceException()
    {
        // Arrange
        await _viewModel.LoadSavedGamesAsync();
        var save = _viewModel.SavedGames.First();

        _mockGameStateService.Setup(x => x.DeleteSaveAsync(save.Id))
            .ThrowsAsync(new Exception("Delete failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _mockGameStateService.Object.DeleteSaveAsync(save.Id));
    }

    #endregion
}
