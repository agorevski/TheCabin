using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Engine;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Maui.Services;
using NarrativeEntry = TheCabin.Maui.Models.NarrativeEntry;

namespace TheCabin.Maui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IVoiceRecognitionService _voiceService;
    private readonly ICommandParserService _parserService;
    private readonly IGameStateService _gameStateService;
    private readonly ITextToSpeechService _ttsService;
    private readonly IStoryPackService _storyPackService;
    private readonly IAchievementService _achievementService;
    private readonly IAchievementNotificationService _notificationService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly CommandRouter _commandRouter;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly GameStateMachine _stateMachine;
    private readonly IPuzzleEngine _puzzleEngine;
    
    private GameState? _currentGameState;
    private CancellationTokenSource? _listeningCts;

    [ObservableProperty]
    private ObservableCollection<NarrativeEntry> storyFeed = new();

    [ObservableProperty]
    private string currentLocation = "Unknown";

    [ObservableProperty]
    private int playerHealth = 100;

    [ObservableProperty]
    private string lightLevel = "Normal";

    [ObservableProperty]
    private string gameTime = "0:00";

    [ObservableProperty]
    private string transcriptText = string.Empty;

    [ObservableProperty]
    private bool isListening;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private bool ttsEnabled = true;

    public MainViewModel(
        IVoiceRecognitionService voiceService,
        ICommandParserService parserService,
        IGameStateService gameStateService,
        ITextToSpeechService ttsService,
        IStoryPackService storyPackService,
        IAchievementService achievementService,
        IAchievementNotificationService notificationService,
        ILogger<MainViewModel> logger,
        CommandRouter commandRouter,
        IMainThreadDispatcher mainThreadDispatcher,
        GameStateMachine stateMachine,
        IPuzzleEngine puzzleEngine)
    {
        _voiceService = voiceService;
        _parserService = parserService;
        _gameStateService = gameStateService;
        _ttsService = ttsService;
        _storyPackService = storyPackService;
        _achievementService = achievementService;
        _notificationService = notificationService;
        _logger = logger;
        _commandRouter = commandRouter;
        _mainThreadDispatcher = mainThreadDispatcher;
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _puzzleEngine = puzzleEngine ?? throw new ArgumentNullException(nameof(puzzleEngine));

        Title = "The Cabin";
    }

    public async Task InitializeAsync(string? packId = null)
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                // Load a story pack (use provided packId or default to classic_horror)
                var selectedPackId = packId ?? "classic_horror";
                _logger.LogInformation("Attempting to load story pack: {PackId}", selectedPackId);
                
                var storyPack = await _storyPackService.LoadPackAsync(selectedPackId);
                _logger.LogInformation("Story pack loaded successfully: {Theme}", storyPack.Theme);
                
                // Initialize puzzle engine with loaded puzzles
                if (storyPack.Puzzles != null && storyPack.Puzzles.Any())
                {
                    _puzzleEngine.InitializePuzzles(storyPack.Puzzles);
                    _logger.LogInformation("Initialized {Count} puzzles", storyPack.Puzzles.Count);
                }
                
                // Initialize game state in both services
                await _gameStateService.InitializeNewGameAsync(storyPack);
                await _stateMachine.InitializeAsync(storyPack);
                _currentGameState = _gameStateService.CurrentState;
                _logger.LogInformation("Game state initialized");
                
                // Clear story feed for fresh start
                StoryFeed.Clear();
                
                // Show initial room description with objects and exits (like "look around")
                var initialRoom = _currentGameState.World.Rooms[_currentGameState.Player.CurrentLocationId];
                
                // Get visible objects
                var visibleObjects = initialRoom.State.VisibleObjectIds
                    .Where(id => _currentGameState.World.Objects.ContainsKey(id))
                    .Select(id => _currentGameState.World.Objects[id])
                    .Where(obj => obj.IsVisible)
                    .Select(obj => obj.Name);
                
                // Get exits
                var exits = initialRoom.Exits.Keys;
                
                // Format with separate display and TTS messages
                var (displayMessage, ttsMessage) = Core.Engine.RoomDescriptionFormatter.FormatRoomDescription(
                    initialRoom.Description,
                    visibleObjects,
                    exits);
                
                AddNarrativeEntry(displayMessage, NarrativeType.Success);
                
                // Play TTS for initial room description if enabled (using TTS-specific message)
                if (TtsEnabled)
                {
                    await _ttsService.SpeakAsync(ttsMessage);
                }
                
                // Update UI state
                UpdateUIState();
                
                _logger.LogInformation("Game initialized with {Theme} (Pack: {PackId})", storyPack.Theme, selectedPackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize game with packId: {PackId}", packId);
                throw new InvalidOperationException($"Failed to initialize game: {ex.Message}", ex);
            }
        }, "Failed to initialize game");
    }

    [RelayCommand]
    private async Task ShowSettingsAsync()
    {
        await Shell.Current.GoToAsync("//SettingsPage");
    }
    
    [RelayCommand]
    private async Task ShowAchievementsAsync()
    {
        await Shell.Current.GoToAsync("//AchievementsPage");
    }
    
    [RelayCommand]
    private async Task SelectStoryPackAsync()
    {
        await Shell.Current.GoToAsync("//StoryPackSelectorPage");
    }
    
    [RelayCommand]
    private async Task StartNewGameAsync()
    {
        // This will be called when returning from story selector
        // For now, just show a message
        await Shell.Current.DisplayAlert("New Game", "Story pack selected! Ready to start.", "OK");
    }
    
    [RelayCommand]
    private async Task ShowHelpAsync()
    {
        await Shell.Current.DisplayAlert(
            "Help",
            "🎙️ Voice Commands:\n\n" +
            "• Look around\n" +
            "• Inventory\n" +
            "• [Take | Use | Examine | Drop | Open] [object]\n" +
            "• [Go | Move] [direction]\n\n" +
            "Tap the microphone to speak!",
            "OK");
    }

    [RelayCommand]
    private async Task NewGameAsync()
    {
        var confirm = await ShowConfirmAsync(
            "New Game",
            "Start a new game? Current progress will be lost.");
        
        if (confirm)
        {
            StoryFeed.Clear();
            await InitializeAsync();
        }
    }
    
    [RelayCommand]
    private async Task SaveGameAsync()
    {
        if (_currentGameState == null)
        {
            await ShowErrorAsync("No active game to save");
            return;
        }
        
        var saveName = await Shell.Current.DisplayPromptAsync(
            "Save Game",
            "Enter a name for this save:",
            "Save",
            "Cancel",
            placeholder: $"Save {DateTime.Now:yyyy-MM-dd HH:mm}");
        
        if (string.IsNullOrWhiteSpace(saveName))
            return;
        
        await ExecuteAsync(async () =>
        {
            await _gameStateService.SaveGameAsync(saveName);
            AddNarrativeEntry($"Game saved as '{saveName}'", NarrativeType.SystemMessage);
            await Shell.Current.DisplayAlert("Success", "Game saved successfully!", "OK");
        }, "Failed to save game");
    }
    
    [RelayCommand]
    private async Task LoadGameAsync()
    {
        await Shell.Current.GoToAsync("//LoadGamePage");
    }
    
    public async Task LoadSavedGameAsync(int saveId)
    {
        await ExecuteAsync(async () =>
        {
            await _gameStateService.LoadGameAsync(saveId);
            _currentGameState = _gameStateService.CurrentState;
            
            // Re-initialize GameStateMachine and PuzzleEngine with loaded state
            var storyPack = await _storyPackService.LoadPackAsync(_currentGameState.World.CurrentThemeId);
            
            // Re-initialize puzzle engine
            if (storyPack.Puzzles != null && storyPack.Puzzles.Any())
            {
                _puzzleEngine.InitializePuzzles(storyPack.Puzzles);
                _logger.LogInformation("Re-initialized {Count} puzzles for loaded game", storyPack.Puzzles.Count);
            }
            
            await _stateMachine.InitializeAsync(storyPack);
            
            // Restore the player's current location in the state machine
            if (!string.IsNullOrEmpty(_currentGameState.Player.CurrentLocationId))
            {
                _stateMachine.CurrentState.Player.CurrentLocationId = _currentGameState.Player.CurrentLocationId;
            }
            
            // Clear and reload story feed
            StoryFeed.Clear();
            
            // Show current location
            var room = _currentGameState.World.Rooms[_currentGameState.Player.CurrentLocationId];
            AddNarrativeEntry(room.Description, NarrativeType.Description);
            AddNarrativeEntry("Game loaded successfully!", NarrativeType.SystemMessage);
            
            // Update UI
            UpdateUIState();
            
            _logger.LogInformation("Game loaded from save {SaveId}", saveId);
        }, "Failed to load game");
    }

    [RelayCommand]
    private void ToggleTts()
    {
        TtsEnabled = !TtsEnabled;
        var message = TtsEnabled ? "Text-to-speech enabled" : "Text-to-speech disabled";
        AddNarrativeEntry(message, NarrativeType.SystemMessage);
    }

    [RelayCommand]
    private async Task ToggleListeningAsync()
    {
        if (IsListening)
        {
            StopListening();
        }
        else
        {
            await StartListeningAsync();
        }
    }

    private async Task StartListeningAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsListening = true;
            TranscriptText = "Listening...";
            
            _listeningCts = new CancellationTokenSource();
            
            var result = await _voiceService.RecognizeSpeechAsync(_listeningCts.Token);
            
            IsListening = false;
            
            if (result.Success)
            {
                TranscriptText = result.TranscribedText;
                await ProcessCommandAsync(result.TranscribedText);
            }
            else
            {
                TranscriptText = string.Empty;
                AddNarrativeEntry(result.ErrorMessage, NarrativeType.SystemMessage);
            }
        }, "Voice recognition failed");
    }

    private void StopListening()
    {
        _listeningCts?.Cancel();
        _voiceService.StopListening();
        IsListening = false;
        TranscriptText = string.Empty;
    }

    private async Task ProcessCommandAsync(string input)
    {
        if (_currentGameState == null || string.IsNullOrEmpty(_currentGameState.Player?.CurrentLocationId))
        {
            await ShowErrorAsync("Game not properly initialized. Please start a new game.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            IsProcessing = true;
            
            // Add player command to story feed
            AddNarrativeEntry($"▶ \"{input}\"", NarrativeType.PlayerCommand);
            
            // Parse command
            var context = BuildGameContext();
            var parsed = await _parserService.ParseAsync(input, context);
            
            _logger.LogInformation("Parsed command: {Verb} {Object}", parsed.Verb, parsed.Object);
            
            // Execute command
            var result = await _commandRouter.RouteAsync(parsed);
            
            // Add result to story feed
            var entryType = result.Success ? NarrativeType.Success : NarrativeType.Failure;
            AddNarrativeEntry(result.Message, entryType);
            
            // Check for achievement unlocks - the command router already handles this via TrackEventAsync
            // No need to manually check achievements here
            
            // Update UI state
            UpdateUIState();
            
            // Optional TTS narration (use TTS-specific message if available)
            if (TtsEnabled && result.Success)
            {
                var ttsText = result.TtsMessage ?? result.Message;
                await _ttsService.SpeakAsync(ttsText);
            }
            
            IsProcessing = false;
            TranscriptText = string.Empty;
        }, "Command processing failed");
    }

    [RelayCommand]
    private async Task ProcessTextCommandAsync(string input)
    {
        if (!string.IsNullOrWhiteSpace(input))
        {
            await ProcessCommandAsync(input);
        }
    }

    private GameContext BuildGameContext()
    {
        if (_currentGameState == null)
            return new GameContext();

        var room = _currentGameState.World.Rooms[_currentGameState.Player.CurrentLocationId];
        var visibleObjects = room.State.VisibleObjectIds
            .Where(id => _currentGameState.World.Objects.ContainsKey(id))
            .Select(id => _currentGameState.World.Objects[id])
            .Where(obj => obj.IsVisible)
            .ToList();

        return new GameContext
        {
            CurrentLocation = room.Id,
            VisibleObjects = visibleObjects.Select(o => o.Id).ToList(),
            InventoryItems = _currentGameState.Player.Inventory.Items.Select(i => i.Id).ToList(),
            RecentCommands = StoryFeed
                .Where(e => e.Type == NarrativeType.PlayerCommand)
                .TakeLast(3)
                .Select(e => e.Text.TrimStart('▶', ' ', '"').TrimEnd('"'))
                .ToList()
        };
    }

    private void UpdateUIState()
    {
        if (_currentGameState == null)
            return;

        var room = _currentGameState.World.Rooms[_currentGameState.Player.CurrentLocationId];
        
        CurrentLocation = FormatRoomName(room.Id);
        PlayerHealth = _currentGameState.Player.Health;
        LightLevel = room.LightLevel.ToString();
        GameTime = _currentGameState.Player.Stats.PlayTime.ToString(@"h\:mm");
    }

    private string FormatRoomName(string roomId)
    {
        return roomId.Replace("_", " ")
            .Split(' ')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1))
            .Aggregate((a, b) => a + " " + b);
    }

    private void AddNarrativeEntry(string text, NarrativeType type)
    {
        var color = type switch
        {
            NarrativeType.PlayerCommand => Color.FromArgb("#4A90E2"),
            NarrativeType.Success => Color.FromArgb("#7ED321"),
            NarrativeType.Failure => Color.FromArgb("#D0021B"),
            NarrativeType.SystemMessage => Color.FromArgb("#F5A623"),
            NarrativeType.Discovery => Color.FromArgb("#BD10E0"),
            _ => Colors.White
        };

        var entry = new NarrativeEntry
        {
            Text = text,
            Type = type,
            Timestamp = DateTime.Now,
            TextColor = color,
            IsImportant = type == NarrativeType.Discovery
        };

        _mainThreadDispatcher.BeginInvokeOnMainThread(() =>
        {
            StoryFeed.Add(entry);
            
            // Keep only last 100 entries for performance
            while (StoryFeed.Count > 100)
            {
                StoryFeed.RemoveAt(0);
            }
        });
    }

}
