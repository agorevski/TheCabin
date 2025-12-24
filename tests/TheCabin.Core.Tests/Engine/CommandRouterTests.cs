using Moq;
using TheCabin.Core.Engine;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine;

/// <summary>
/// Unit tests for CommandRouter
/// </summary>
public class CommandRouterTests
{
    private readonly Mock<ICommandHandler> _mockHandler;
    private readonly GameStateMachine _stateMachine;
    private readonly CommandRouter _router;
    private readonly GameState _gameState;

    public CommandRouterTests()
    {
        // Create mock handler
        _mockHandler = new Mock<ICommandHandler>();
        _mockHandler.Setup(h => h.Verb).Returns("test");

        // Create test story pack
        var storyPack = CreateTestStoryPack();

        // Create state machine
        var tempGameState = new GameState();
        var tempInventoryManager = new InventoryManager(tempGameState);
        _stateMachine = new GameStateMachine(tempInventoryManager);
        _stateMachine.Initialize(storyPack);

        // Get the actual game state
        _gameState = _stateMachine.CurrentState;

        // Create router with mock handler
        _router = new CommandRouter(new[] { _mockHandler.Object }, _stateMachine);
    }

    [Fact]
    public async Task RouteAsync_WithValidCommand_CallsHandler()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "test", Object = "item" };
        _mockHandler.Setup(h => h.ValidateAsync(command, _gameState))
            .ReturnsAsync(CommandValidationResult.Valid());
        _mockHandler.Setup(h => h.ExecuteAsync(command, _gameState))
            .ReturnsAsync(new CommandResult { Success = true, Message = "Success" });

        // Act
        var result = await _router.RouteAsync(command);

        // Assert
        Assert.True(result.Success);
        _mockHandler.Verify(h => h.ExecuteAsync(command, _gameState), Times.Once);
    }

    [Fact]
    public async Task RouteAsync_WithUnknownVerb_ReturnsInvalidCommand()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "unknown" };

        // Act
        var result = await _router.RouteAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CommandResultType.InvalidCommand, result.Type);
        Assert.Contains("don't understand", result.Message.ToLower());
    }

    [Fact]
    public async Task RouteAsync_WhenValidationFails_ReturnsValidationMessage()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "test", Object = "item" };
        _mockHandler.Setup(h => h.ValidateAsync(command, _gameState))
            .ReturnsAsync(CommandValidationResult.Invalid("Validation failed"));

        // Act
        var result = await _router.RouteAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CommandResultType.RequirementsNotMet, result.Type);
        Assert.Equal("Validation failed", result.Message);
        _mockHandler.Verify(h => h.ExecuteAsync(command, _gameState), Times.Never);
    }

    [Fact]
    public async Task RouteAsync_WhenHandlerThrows_ReturnsErrorResult()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "test", Object = "item" };
        _mockHandler.Setup(h => h.ValidateAsync(command, _gameState))
            .ReturnsAsync(CommandValidationResult.Valid());
        _mockHandler.Setup(h => h.ExecuteAsync(command, _gameState))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _router.RouteAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CommandResultType.Failure, result.Type);
        Assert.Contains("error occurred", result.Message.ToLower());
    }

    [Fact]
    public async Task RouteAsync_IncrementsGameStats()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "test" };
        _mockHandler.Setup(h => h.ValidateAsync(command, _gameState))
            .ReturnsAsync(CommandValidationResult.Valid());
        _mockHandler.Setup(h => h.ExecuteAsync(command, _gameState))
            .ReturnsAsync(new CommandResult { Success = true, Message = "Success" });

        var initialTurnNumber = _gameState.World.TurnNumber;
        var initialCommandsExecuted = _gameState.Player.Stats.CommandsExecuted;

        // Act
        await _router.RouteAsync(command);

        // Assert
        Assert.Equal(initialTurnNumber + 1, _gameState.World.TurnNumber);
        Assert.Equal(initialCommandsExecuted + 1, _gameState.Player.Stats.CommandsExecuted);
    }

    [Fact]
    public async Task RouteAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _router.RouteAsync(null!));
    }

    [Fact]
    public void GetAvailableVerbs_ReturnsRegisteredVerbs()
    {
        // Act
        var verbs = _router.GetAvailableVerbs();

        // Assert
        Assert.Single(verbs);
        Assert.Contains("test", verbs);
    }

    [Fact]
    public void GetVerbHelp_WithKnownVerb_ReturnsHelp()
    {
        // Act
        var help = _router.GetVerbHelp("test");

        // Assert
        Assert.Contains("test", help.ToLower());
        Assert.Contains("available", help.ToLower());
    }

    [Fact]
    public void GetVerbHelp_WithUnknownVerb_ReturnsUnknownMessage()
    {
        // Act
        var help = _router.GetVerbHelp("fly");

        // Assert
        Assert.Contains("unknown", help.ToLower());
    }

    [Fact]
    public void Constructor_WithNullHandlers_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommandRouter(null!, _stateMachine));
    }

    [Fact]
    public void Constructor_WithNullStateMachine_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommandRouter(new[] { _mockHandler.Object }, null!));
    }

    [Fact]
    public async Task RouteAsync_VerbIsCaseInsensitive()
    {
        // Arrange
        var command = new ParsedCommand { Verb = "TEST" };
        _mockHandler.Setup(h => h.ValidateAsync(It.IsAny<ParsedCommand>(), _gameState))
            .ReturnsAsync(CommandValidationResult.Valid());
        _mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<ParsedCommand>(), _gameState))
            .ReturnsAsync(new CommandResult { Success = true, Message = "Success" });

        // Act
        var result = await _router.RouteAsync(command);

        // Assert
        Assert.True(result.Success);
    }

    private StoryPack CreateTestStoryPack()
    {
        var room = new Room
        {
            Id = "test_room",
            Description = "Test room",
            State = new RoomState
            {
                VisibleObjectIds = new List<string>()
            },
            LightLevel = LightLevel.Normal
        };

        return new StoryPack
        {
            Id = "test",
            Theme = "Test Theme",
            StartingRoomId = "test_room",
            Rooms = new List<Room> { room },
            Objects = new Dictionary<string, GameObject>()
        };
    }
}
