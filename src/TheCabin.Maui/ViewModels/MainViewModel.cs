using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Engine;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Core.Services;
using TheCabin.Maui.Services;
using NarrativeEntry = TheCabin.Maui.Models.NarrativeEntry;

namespace TheCabin.Maui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IVoiceRecognitionService _voiceService;
    private readonly ITextToSpeechService _ttsService;
    private readonly IGameStateService _gameStateService;
    private readonly GameOrchestrator _orchestrator;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly ILogger<MainViewModel> _logger;
    private readonly MauiGameDisplay _display;
    
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
        ITextToSpeechService ttsService,
        IGameStateService gameStateService,
        IStoryPackService storyPackService,
        ICommandParserService commandParser,
        CommandRouter commandRouter,
        IGameStateMachine stateMachine,
        IPuzzleEngine puzzleEngine,
        GameInitializationService initService,
        IMainThreadDispatcher mainThreadDispatcher,
        IAchievementService achievementService,
        ILogger<MainViewModel> logger,
        ILogger<GameOrchestrator>? orchestratorLogger = null)
    {
        _voiceService = voiceService;
        _ttsService = ttsService;
        _gameStateService = gameStateService;
        _mainThreadDispatcher = mainThreadDispatcher;
        _logger = logger;
        
        // Create MauiGameDisplay with our story feed
        _display = new MauiGameDisplay(_mainThreadDispatcher, StoryFeed);
        
        // Create GameOrchestrator with our display
        _orchestrator = new GameOrchestrator(
            gameStateService,
            storyPackService,
            commandParser,
            commandRouter,
            stateMachine,
            puzzleEngine,
            _display,
            initService,
            achievementService,
            orchestratorLogger);

        Title = "The Cabin";
    }

    public async Task InitializeAsync(string? packId = null)
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                // Use provided packId or default to classic_horror
                var selectedPackId = packId ?? "classic_horror";
                _logger.LogInformation("Initializing game with story pack: {PackId}", selectedPackId);
                
                // Clear story feed for fresh start
                StoryFeed.Clear();
                
                // Initialize game using orchestrator
                var success = await _orchestrator.InitializeGameAsync(selectedPackId);
                
                if (!success)
                {
                    throw new InvalidOperationException("Failed to initialize game");
                }
                
                // Play TTS for initial room description if enabled
                if (TtsEnabled && StoryFeed.Any())
                {
                    var initialDescription = StoryFeed.FirstOrDefault(e => e.Type == NarrativeType.Description)?.Text;
                    if (!string.IsNullOrEmpty(initialDescription))
                    {
                        // Use TTS-specific formatting if available
                        var ttsMessage = initialDescription
                            .Replace("📍 You see:", "You see:")
                            .Replace("🚪 Exits:", "Exits:");
                        await _ttsService.SpeakAsync(ttsMessage);
                    }
                }
                
                // Update UI state
                UpdateUIState();
                
                _logger.LogInformation("Game initialized successfully with pack: {PackId}", selectedPackId);
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
        var currentState = _orchestrator.GetCurrentState();
        if (currentState == null)
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
            var success = await _orchestrator.SaveGameAsync(saveName);
            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Game saved successfully!", "OK");
            }
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
            // Clear story feed
            StoryFeed.Clear();
            
            // Load game using orchestrator
            var success = await _orchestrator.LoadGameAsync(saveId);
            
            if (!success)
            {
                throw new InvalidOperationException("Failed to load game");
            }
            
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
        _display.ShowMessageAsync(message, MessageType.SystemMessage);
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
                await _display.ShowMessageAsync(result.ErrorMessage, MessageType.SystemMessage);
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
        var currentState = _orchestrator.GetCurrentState();
        if (currentState == null || string.IsNullOrEmpty(currentState.Player?.CurrentLocationId))
        {
            await ShowErrorAsync("Game not properly initialized. Please start a new game.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            IsProcessing = true;
            
            // Process command using orchestrator
            await _orchestrator.ProcessCommandAsync(input);
            
            // Update UI state
            UpdateUIState();
            
            // Optional TTS narration for the last message
            if (TtsEnabled && StoryFeed.Any())
            {
                var lastEntry = StoryFeed.Last();
                if (lastEntry.Type == NarrativeType.Success || lastEntry.Type == NarrativeType.Description)
                {
                    // Clean up display formatting for TTS
                    var ttsText = lastEntry.Text
                        .Replace("📍 You see:", "You see:")
                        .Replace("🚪 Exits:", "Exits:");
                    await _ttsService.SpeakAsync(ttsText);
                }
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

    private void UpdateUIState()
    {
        var currentState = _orchestrator.GetCurrentState();
        if (currentState == null)
            return;

        var room = currentState.World.Rooms[currentState.Player.CurrentLocationId];
        
        CurrentLocation = FormatRoomName(room.Id);
        PlayerHealth = currentState.Player.Health;
        LightLevel = room.LightLevel.ToString();
        GameTime = currentState.Player.Stats.PlayTime.ToString(@"h\:mm");
    }

    private string FormatRoomName(string roomId)
    {
        return roomId.Replace("_", " ")
            .Split(' ')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1))
            .Aggregate((a, b) => a + " " + b);
    }
}
