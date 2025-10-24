using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Engine;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Maui.Models;

namespace TheCabin.Maui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IVoiceRecognitionService _voiceService;
    private readonly ICommandParserService _parserService;
    private readonly IGameStateService _gameStateService;
    private readonly ITextToSpeechService _ttsService;
    private readonly IStoryPackService _storyPackService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly CommandRouter _commandRouter;
    
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
        ILogger<MainViewModel> logger,
        CommandRouter commandRouter)
    {
        _voiceService = voiceService;
        _parserService = parserService;
        _gameStateService = gameStateService;
        _ttsService = ttsService;
        _storyPackService = storyPackService;
        _logger = logger;
        _commandRouter = commandRouter;

        Title = "The Cabin";
    }

    public async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load a story pack (default to classic_horror)
            var storyPack = await _storyPackService.LoadStoryPackAsync("classic_horror");
            
            // Initialize game state
            _currentGameState = await _gameStateService.NewGameAsync(storyPack);
            
            // Show initial room description
            var initialRoom = _currentGameState.World.Rooms[_currentGameState.Player.CurrentLocationId];
            AddNarrativeEntry(initialRoom.Description, NarrativeType.Description);
            
            // Update UI state
            UpdateUIState();
            
            _logger.LogInformation("Game initialized with {Theme}", storyPack.Theme);
        }, "Failed to initialize game");
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
        if (_currentGameState == null)
        {
            await ShowErrorAsync("Game not initialized");
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
            var result = await _commandRouter.RouteAsync(parsed, _currentGameState);
            
            // Add result to story feed
            var entryType = result.Success ? NarrativeType.Success : NarrativeType.Failure;
            AddNarrativeEntry(result.Message, entryType);
            
            // Update UI state
            UpdateUIState();
            
            // Optional TTS narration
            if (TtsEnabled && result.Success)
            {
                await _ttsService.SpeakAsync(result.Message);
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

        MainThread.BeginInvokeOnMainThread(() =>
        {
            StoryFeed.Add(entry);
            
            // Keep only last 100 entries for performance
            while (StoryFeed.Count > 100)
            {
                StoryFeed.RemoveAt(0);
            }
        });
    }

    [RelayCommand]
    private async Task ShowInventoryAsync()
    {
        if (_currentGameState == null)
            return;

        var items = _currentGameState.Player.Inventory.Items;
        if (items.Count == 0)
        {
            await ShowMessageAsync("Inventory", "You're not carrying anything.");
            return;
        }

        var itemList = string.Join("\n", items.Select(i => $"• {i.Name}"));
        var weight = _currentGameState.Player.Inventory.TotalWeight;
        var capacity = _currentGameState.Player.Inventory.MaxCapacity;
        
        await ShowMessageAsync(
            "Inventory",
            $"{itemList}\n\nWeight: {weight}/{capacity} kg");
    }

    [RelayCommand]
    private async Task ShowHelpAsync()
    {
        var helpText = @"Voice Commands:
• Look / Look around
• Go [direction] (north, south, east, west, up, down)
• Take [object]
• Use [object]
• Examine [object]
• Inventory

Example: ""Take the lantern"" or ""Go north""

Tap the microphone button and speak your command clearly.";

        await ShowMessageAsync("Help", helpText);
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
    private void ToggleTts()
    {
        TtsEnabled = !TtsEnabled;
        var message = TtsEnabled ? "Text-to-speech enabled" : "Text-to-speech disabled";
        AddNarrativeEntry(message, NarrativeType.SystemMessage);
    }
}
