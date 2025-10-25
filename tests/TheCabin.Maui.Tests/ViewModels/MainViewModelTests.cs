using Xunit;
using Moq;
using FluentAssertions;
using TheCabin.Maui.ViewModels;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Core.Engine;
using TheCabin.Maui.Services;
using Microsoft.Extensions.Logging;
using TheCabin.Maui.Models;

namespace TheCabin.Maui.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<IVoiceRecognitionService> _mockVoiceService;
    private readonly Mock<ICommandParserService> _mockParserService;
    private readonly Mock<IGameStateService> _mockGameStateService;
    private readonly Mock<ITextToSpeechService> _mockTtsService;
    private readonly Mock<IStoryPackService> _mockStoryPackService;
    private readonly Mock<IAchievementService> _mockAchievementService;
    private readonly Mock<IAchievementNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<MainViewModel>> _mockLogger;
    private readonly Mock<CommandRouter> _mockCommandRouter;
    private readonly MainViewModel _viewModel;
    private readonly GameState _testGameState;
    private readonly StoryPack _testStoryPack;

    public MainViewModelTests()
    {
        _mockVoiceService = new Mock<IVoiceRecognitionService>();
        _mockParserService = new Mock<ICommandParserService>();
        _mockGameStateService = new Mock<IGameStateService>();
        _mockTtsService = new Mock<ITextToSpeechService>();
        _mockStoryPackService = new Mock<IStoryPackService>();
        _mockAchievementService = new Mock<IAchievementService>();
        _mockNotificationService = new Mock<IAchievementNotificationService>();
        _mockLogger = new Mock<ILogger<MainViewModel>>();
        _mockCommandRouter = new Mock<CommandRouter>(MockBehavior.Loose, null, null);

        // Setup test story pack
        _testStoryPack = new StoryPack
        {
            Id = "test_pack",
            Theme = "Test Theme",
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "test_room",
                    Description = "A test room for testing.",
                    LightLevel = LightLevel.Bright
                }
            }
        };

        // Setup test game state
        _testGameState = new GameState();
        _testGameState.Player.CurrentLocationId = "test_room";
        _testGameState.Player.Health = 100;
        _testGameState.Player.Stats.PlayTime = TimeSpan.FromMinutes(30);
        foreach (var room in _testStoryPack.Rooms)
        {
            _testGameState.World.Rooms[room.Id] = room;
        }
        
        _mockGameStateService.Setup(x => x.CurrentState).Returns(_testGameState);
        _mockStoryPackService.Setup(x => x.LoadPackAsync(It.IsAny<string>()))
            .ReturnsAsync(_testStoryPack);

        _viewModel = new MainViewModel(
            _mockVoiceService.Object,
            _mockParserService.Object,
            _mockGameStateService.Object,
            _mockTtsService.Object,
            _mockStoryPackService.Object,
            _mockAchievementService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object,
            _mockCommandRouter.Object
        );
    }

    #region Initialization & Properties Tests (M-01 to M-06)

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("The Cabin");
        _viewModel.StoryFeed.Should().NotBeNull();
        _viewModel.StoryFeed.Should().BeEmpty();
        _viewModel.IsListening.Should().BeFalse();
        _viewModel.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesAllObservableProperties()
    {
        // Assert
        _viewModel.CurrentLocation.Should().Be("Unknown");
        _viewModel.PlayerHealth.Should().Be(100);
        _viewModel.LightLevel.Should().Be("Normal");
        _viewModel.GameTime.Should().Be("0:00");
        _viewModel.TranscriptText.Should().BeEmpty();
        _viewModel.TtsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_LoadsDefaultStoryPack()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _mockStoryPackService.Verify(x => x.LoadPackAsync("classic_horror"), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_LoadsSpecifiedStoryPack()
    {
        // Act
        await _viewModel.InitializeAsync("test_pack");

        // Assert
        _mockStoryPackService.Verify(x => x.LoadPackAsync("test_pack"), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_InitializesGameState()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _mockGameStateService.Verify(x => x.InitializeNewGameAsync(It.IsAny<StoryPack>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_AddsInitialRoomDescriptionToStoryFeed()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.StoryFeed.Should().HaveCount(1);
        _viewModel.StoryFeed[0].Text.Should().Contain("test room");
    }

    [Fact]
    public async Task InitializeAsync_UpdatesUIState()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.CurrentLocation.Should().NotBe("Unknown");
        _viewModel.PlayerHealth.Should().Be(100);
    }

    #endregion

    #region Stats Bar Tests (M-10 to M-17)

    [Fact]
    public async Task StatsBar_DisplaysLocationName()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.CurrentLocation.Should().NotBeEmpty();
    }

    [Fact]
    public async Task StatsBar_DisplaysHealthValue()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.PlayerHealth.Should().Be(100);
    }

    [Fact]
    public async Task StatsBar_DisplaysLightLevel()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.LightLevel.Should().Be("Bright");
    }

    [Fact]
    public async Task StatsBar_DisplaysGameTime()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.GameTime.Should().Be("0:30");
    }

    [Fact]
    public async Task StatsBar_UpdatesAfterCommand()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var initialLocation = _viewModel.CurrentLocation;

        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Test" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("look around");

        // Assert - stats should remain valid
        _viewModel.CurrentLocation.Should().NotBeEmpty();
        _viewModel.PlayerHealth.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Story Feed Tests (M-20 to M-29)

    [Fact]
    public void StoryFeed_InitiallyEmpty()
    {
        // Assert
        _viewModel.StoryFeed.Should().BeEmpty();
    }

    [Fact]
    public async Task StoryFeed_AddsPlayerCommand()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Test" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("look around");

        // Assert
        _viewModel.StoryFeed.Should().Contain(e => e.Type == NarrativeType.PlayerCommand);
    }

    [Fact]
    public async Task StoryFeed_AddsSuccessMessage()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Success message" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("look around");

        // Assert
        _viewModel.StoryFeed.Should().Contain(e => e.Type == NarrativeType.Success && e.Text == "Success message");
    }

    [Fact]
    public async Task StoryFeed_AddsFailureMessage()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var parsedCommand = new ParsedCommand { Verb = "invalid", Object = "" };
        var commandResult = new CommandResult { Success = false, Message = "Failure message" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("invalid command");

        // Assert
        _viewModel.StoryFeed.Should().Contain(e => e.Type == NarrativeType.Failure && e.Text == "Failure message");
    }

    [Fact]
    public async Task StoryFeed_LimitsTo100Entries()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Test" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act - Add more than 100 entries
        for (int i = 0; i < 110; i++)
        {
            await _viewModel.ProcessTextCommandCommand.ExecuteAsync($"command {i}");
        }

        // Assert
        _viewModel.StoryFeed.Count.Should().BeLessOrEqualTo(101); // +1 for initial room description
    }

    #endregion

    #region Voice Control Tests (M-30 to M-37)

    [Fact]
    public async Task ToggleListeningCommand_WhenNotListening_StartsListening()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var voiceResult = new VoiceRecognitionResult
        {
            Success = true,
            TranscribedText = "look around",
            Confidence = 0.95
        };

        _mockVoiceService.Setup(x => x.RecognizeSpeechAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(voiceResult);

        var parsedCommand = new ParsedCommand
        {
            Verb = "look",
            Object = "around",
            Confidence = 0.95
        };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);

        var commandResult = new CommandResult
        {
            Success = true,
            Message = "You look around the room."
        };

        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ToggleListeningCommand.ExecuteAsync(null);

        // Assert
        _mockVoiceService.Verify(x => x.RecognizeSpeechAsync(It.IsAny<CancellationToken>()), Times.Once);
        _viewModel.StoryFeed.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task VoiceRecognition_SetsTranscriptText()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var voiceResult = new VoiceRecognitionResult
        {
            Success = true,
            TranscribedText = "test command",
            Confidence = 0.90
        };

        _mockVoiceService.Setup(x => x.RecognizeSpeechAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(voiceResult);

        var parsedCommand = new ParsedCommand { Verb = "test", Object = "command" };
        var commandResult = new CommandResult { Success = true, Message = "Done" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ToggleListeningCommand.ExecuteAsync(null);

        // Assert - TranscriptText should be cleared after processing
        _viewModel.TranscriptText.Should().BeEmpty();
    }

    [Fact]
    public async Task VoiceRecognition_HandlesFailure()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var voiceResult = new VoiceRecognitionResult
        {
            Success = false,
            ErrorMessage = "Recognition failed"
        };

        _mockVoiceService.Setup(x => x.RecognizeSpeechAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(voiceResult);

        // Act
        await _viewModel.ToggleListeningCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StoryFeed.Should().Contain(e => e.Type == NarrativeType.SystemMessage);
    }

    #endregion

    #region Command Tests (M-50 to M-58)

    [Fact]
    public void SaveGameCommand_IsNotNull()
    {
        // Assert
        _viewModel.SaveGameCommand.Should().NotBeNull();
    }

    [Fact]
    public void LoadGameCommand_IsNotNull()
    {
        // Assert
        _viewModel.LoadGameCommand.Should().NotBeNull();
    }

    // Note: ShowInventoryCommand navigation happens through Shell navigation, not a command

    [Fact]
    public void ShowAchievementsCommand_IsNotNull()
    {
        // Assert
        _viewModel.ShowAchievementsCommand.Should().NotBeNull();
    }

    [Fact]
    public void ShowSettingsCommand_IsNotNull()
    {
        // Assert
        _viewModel.ShowSettingsCommand.Should().NotBeNull();
    }

    [Fact]
    public void SelectStoryPackCommand_IsNotNull()
    {
        // Assert
        _viewModel.SelectStoryPackCommand.Should().NotBeNull();
    }

    [Fact]
    public void ShowHelpCommand_IsNotNull()
    {
        // Assert
        _viewModel.ShowHelpCommand.Should().NotBeNull();
    }

    [Fact]
    public void NewGameCommand_IsNotNull()
    {
        // Assert
        _viewModel.NewGameCommand.Should().NotBeNull();
    }

    [Fact]
    public void ToggleTtsCommand_TogglesIsTtsEnabled()
    {
        // Arrange
        var initialState = _viewModel.TtsEnabled;

        // Act
        _viewModel.ToggleTtsCommand.Execute(null);

        // Assert
        _viewModel.TtsEnabled.Should().Be(!initialState);
    }

    [Fact]
    public void ToggleTtsCommand_AddsSystemMessage()
    {
        // Act
        _viewModel.ToggleTtsCommand.Execute(null);

        // Assert
        _viewModel.StoryFeed.Should().Contain(e => e.Type == NarrativeType.SystemMessage);
    }

    #endregion

    #region TTS Integration Tests

    [Fact]
    public async Task ProcessCommand_CallsTtsWhenEnabled()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        _viewModel.TtsEnabled = true;

        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Test message" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("look");

        // Assert
        _mockTtsService.Verify(x => x.SpeakAsync(It.Is<string>(s => !string.IsNullOrEmpty(s)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCommand_DoesNotCallTtsWhenDisabled()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        _viewModel.TtsEnabled = false;

        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Test message" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("look");

        // Assert
        _mockTtsService.Verify(x => x.SpeakAsync(It.Is<string>(s => !string.IsNullOrEmpty(s)), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCommand_DoesNotCallTtsOnFailure()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        _viewModel.TtsEnabled = true;

        var parsedCommand = new ParsedCommand { Verb = "invalid", Object = "" };
        var commandResult = new CommandResult { Success = false, Message = "Failure" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("invalid");

        // Assert
        _mockTtsService.Verify(x => x.SpeakAsync(It.Is<string>(s => !string.IsNullOrEmpty(s)), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Game State & Loading Tests

    [Fact]
    public async Task LoadSavedGameAsync_LoadsGameState()
    {
        // Arrange
        var saveId = 42;
        _mockGameStateService.Setup(x => x.LoadGameAsync(saveId))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.LoadSavedGameAsync(saveId);

        // Assert
        _mockGameStateService.Verify(x => x.LoadGameAsync(saveId), Times.Once);
    }

    [Fact]
    public async Task LoadSavedGameAsync_UpdatesStoryFeed()
    {
        // Arrange
        var saveId = 42;
        _mockGameStateService.Setup(x => x.LoadGameAsync(saveId))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.LoadSavedGameAsync(saveId);

        // Assert
        _viewModel.StoryFeed.Should().Contain(e => e.Text.Contains("loaded successfully"));
    }

    #endregion

    #region Command Processing Tests

    [Fact]
    public async Task ProcessTextCommand_IgnoresEmptyInput()
    {
        // Arrange
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("");

        // Assert
        _mockParserService.Verify(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTextCommand_IgnoresWhitespaceInput()
    {
        // Arrange
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("   ");

        // Assert
        _mockParserService.Verify(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCommand_SetsIsProcessingFlag()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Test" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("look");

        // Assert - IsProcessing should be false after completion
        _viewModel.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessCommand_ClearsTranscriptText()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var parsedCommand = new ParsedCommand { Verb = "look", Object = "" };
        var commandResult = new CommandResult { Success = true, Message = "Test" };

        _mockParserService.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<GameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedCommand);
        _mockCommandRouter.Setup(x => x.RouteAsync(It.IsAny<ParsedCommand>()))
            .ReturnsAsync(commandResult);

        // Act
        await _viewModel.ProcessTextCommandCommand.ExecuteAsync("look");

        // Assert
        _viewModel.TranscriptText.Should().BeEmpty();
    }

    #endregion
}
