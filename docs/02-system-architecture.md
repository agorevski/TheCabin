# 02 - System Architecture

## High-Level Architecture

The Cabin follows a layered architecture pattern optimized for .NET MAUI mobile applications, emphasizing separation of concerns, testability, and maintainability.

```text
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                       │
│  ┌────────────┐  ┌────────────┐  ┌────────────────────┐     │
│  │   Views    │  │ ViewModels │  │  Value Converters  │     │
│  │  (XAML/C#) │◄─┤   (MVVM)   │  │    Behaviors       │     │
│  └────────────┘  └────────────┘  └────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   APPLICATION LAYER                         │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐    │
│  │   Services   │  │ Orchestrators│  │   State Mgmt    │    │
│  │ (Business    │  │  (Game Flow) │  │   (Game State)  │    │
│  │  Logic)      │  └──────────────┘  └─────────────────┘    │
│  └──────────────┘                                           │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                     DOMAIN LAYER                            │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐    │
│  │ Game Engine  │  │ Command      │  │  Story Pack     │    │
│  │ (Core Logic) │  │ Processor    │  │  Loader         │    │
│  └──────────────┘  └──────────────┘  └─────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                        │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐    │
│  │Voice Services│  │ Data Access  │  │  External APIs  │    │
│  │ (STT/TTS)    │  │ (Repository) │  │  (LLM)          │    │
│  └──────────────┘  └──────────────┘  └─────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    PLATFORM LAYER                           │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐    │
│  │Android APIs  │  │  File System │  │   Database      │    │
│  │(Native)      │  │   Storage    │  │   (SQLite)      │    │
│  └──────────────┘  └──────────────┘  └─────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Presentation Layer

#### Views (XAML + Code-Behind)

- **MainPage**: Primary game interface
- **InventoryPage**: Item management
- **SettingsPage**: Configuration options
- **StoryPackSelectorPage**: Theme selection
- **LoadGamePage**: Save game management

#### ViewModels (MVVM Pattern)

- **MainViewModel**: Orchestrates game flow and UI state
- **InventoryViewModel**: Manages inventory display
- **SettingsViewModel**: Configuration management
- **StoryPackViewModel**: Theme selection logic

#### UI Components

- **VoiceControlPanel**: Microphone button, waveform animation
- **NarrativeTextView**: Story text display with scrolling
- **StatsBar**: Health, light level, time indicators
- **TranscriptPreview**: Real-time speech recognition feedback

### 2. Application Layer

#### Services

```csharp
// Service interfaces
public interface IVoiceRecognitionService
public interface ITextToSpeechService
public interface ICommandParserService
public interface IGameStateService
public interface IStoryPackService
public interface ISettingsService
```

#### Orchestrators

- **GameFlowOrchestrator**: Coordinates voice → parse → execute → respond cycle
- **SaveGameOrchestrator**: Manages game state persistence
- **ThemeOrchestrator**: Handles theme switching

### 3. Domain Layer

#### Game Engine

- **GameStateMachine**: Core state management and room transitions
- **ActionExecutor**: Processes validated commands
- **PuzzleEngine**: Handles puzzle logic and state
- **InventoryManager**: Item collection and usage

#### Command Processing

- **CommandParser**: Converts parsed JSON to domain commands
- **CommandValidator**: Validates commands against game state
- **ContextManager**: Maintains conversation history

#### Models (See Document 04 for details)

- Room, Object, Player, Inventory, Command structures

### 4. Infrastructure Layer

#### Voice Services

- **AndroidSpeechRecognizer**: Platform-specific STT
- **WhisperService**: Offline speech recognition (optional)
- **TtsService**: Text-to-speech implementation
- **AudioManager**: Audio resource management

#### Data Access

- **StoryPackRepository**: Loads theme JSON files
- **GameStateRepository**: Saves/loads game progress
- **CacheRepository**: Stores parsed command cache

#### External Services

- **LlmApiClient**: OpenAI/Azure OpenAI integration
- **LocalParserService**: Fallback rule-based parser

### 5. Platform Layer (Android-Specific)

#### Android Services

- Permission management (RECORD_AUDIO, etc.)
- Background service support
- Notification handling
- Battery optimization handling

## Data Flow Diagrams

### Primary Game Loop Flow

```text
┌─────────────┐
│   Player    │
│   Speaks    │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────┐
│  VoiceRecognitionService            │
│  - Activates microphone             │
│  - Records audio                    │
│  - Calls Android SpeechRecognizer   │
└──────┬──────────────────────────────┘
       │ (Audio → Text)
       ▼
┌─────────────────────────────────────┐
│  TranscriptPreview (UI Update)      │
│  "You said: take the lantern"       │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  CommandParserService               │
│  - Checks cache first               │
│  - Falls back to LLM API            │
│  - Returns structured JSON          │
└──────┬──────────────────────────────┘
       │ (Text → Command)
       ▼
┌─────────────────────────────────────┐
│  GameStateMachine                   │
│  - Validates command                │
│  - Checks game state                │
│  - Executes action                  │
│  - Updates state                    │
└──────┬──────────────────────────────┘
       │ (Command → Result)
       ▼
┌─────────────────────────────────────┐
│  NarrativeTextView (UI Update)      │
│  "You pick up the dusty lantern"    │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  TextToSpeechService (Optional)     │
│  - Synthesizes narration            │
│  - Plays audio                      │
└─────────────────────────────────────┘
```

### Command Parsing Flow

```text
Raw Text Input: "Pick up the lantern on the table"
       │
       ▼
┌─────────────────────────────────────┐
│  Cache Check                        │
│  Key: hash(text + context)          │
└──────┬──────────────────────────────┘
       │
       ├─ Cache Hit ─────────┐
       │                     │
       ├─ Cache Miss         │
       │      ▼              │
       │  ┌────────────┐     │
       │  │ LLM API    │     │
       │  │ Call       │     │
       │  └─────┬──────┘     │
       │        │            │
       ▼        ▼            ▼
┌─────────────────────────────────────┐
│  Parsed Command JSON                │
│  {                                  │
│    "verb": "take",                  │
│    "object": "lantern",             │
│    "context": "table",              │
│    "confidence": 0.95               │
│  }                                  │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  Command Validator                  │
│  - Object exists in current room?   │
│  - Action allowed in current state? │
│  - Required items in inventory?     │
└──────┬──────────────────────────────┘
       │
       ├─ Valid ──────────────┐
       │                      │
       ├─ Invalid             │
       │      ▼               │
       │  ┌────────────┐      │
       │  │ Error      │      │
       │  │ Response   │      │
       │  └────────────┘      │
       │                      │
       ▼                      ▼
┌─────────────────────────────────────┐
│  ActionExecutor                     │
│  - Execute game logic               │
│  - Update game state                │
│  - Generate response text           │
└─────────────────────────────────────┘
```

## MVVM Pattern Implementation

### ViewModel Structure

```csharp
public class MainViewModel : BaseViewModel
{
    // Services (injected)
    private readonly IVoiceRecognitionService _voiceService;
    private readonly ICommandParserService _parserService;
    private readonly IGameStateService _gameStateService;
    private readonly ITextToSpeechService _ttsService;
    
    // Observable Properties
    public ObservableCollection<NarrativeEntry> StoryFeed { get; }
    public string CurrentLocation { get; set; }
    public int PlayerHealth { get; set; }
    public string LightLevel { get; set; }
    public string TranscriptText { get; set; }
    public bool IsListening { get; set; }
    public bool IsProcessing { get; set; }
    
    // Commands (ICommand)
    public ICommand StartListeningCommand { get; }
    public ICommand StopListeningCommand { get; }
    public ICommand ShowInventoryCommand { get; }
    public ICommand SaveGameCommand { get; }
    
    // Methods
    public async Task ProcessVoiceCommandAsync();
    private async Task ExecuteGameCommandAsync(ParsedCommand cmd);
    private void UpdateUIState();
}
```

### Dependency Injection Setup

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { /* ... */ });
        
        // Register Services
        builder.Services.AddSingleton<IVoiceRecognitionService, AndroidSpeechRecognizer>();
        builder.Services.AddSingleton<ITextToSpeechService, MauiTtsService>();
        builder.Services.AddSingleton<ICommandParserService, LlmCommandParser>();
        builder.Services.AddSingleton<IGameStateService, GameStateService>();
        builder.Services.AddSingleton<IStoryPackService, StoryPackService>();
        
        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<InventoryViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        
        // Register Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<InventoryPage>();
        builder.Services.AddTransient<SettingsPage>();
        
        return builder.Build();
    }
}
```

## State Management

### Game State Hierarchy

```text
GameState (Root)
├── PlayerState
│   ├── CurrentLocation
│   ├── Health
│   ├── Inventory[]
│   └── StatusEffects[]
├── WorldState
│   ├── CurrentTheme
│   ├── Rooms[]
│   │   ├── RoomState
│   │   └── Objects[]
│   └── GlobalVariables{}
├── ProgressState
│   ├── CompletedPuzzles[]
│   ├── UnlockedAreas[]
│   └── StoryFlags{}
└── MetaState
    ├── PlayTime
    ├── SaveTimestamp
    └── Version
```

### State Persistence Strategy

1. **In-Memory State**: Active game state held in `GameStateService`
2. **Auto-Save**: State persisted every significant action
3. **Manual Save**: Player-triggered save to named slots
4. **Checkpoint System**: Automatic checkpoints at major story points

## Communication Patterns

### Event Aggregator Pattern

```csharp
public interface IEventAggregator
{
    void Subscribe<T>(Action<T> handler);
    void Publish<T>(T eventData);
}

// Events
public class GameStateChangedEvent { }
public class InventoryUpdatedEvent { }
public class LocationChangedEvent { }
public class CommandProcessedEvent { }
```

### Message Flow

```text
View → ViewModel → Service → Domain → Repository
  ↑                                        ↓
  └──────── Events/Notifications ──────────┘
```

## Error Handling Strategy

### Layered Error Handling

1. **UI Layer**: User-friendly error messages
2. **Service Layer**: Retry logic and fallbacks
3. **Domain Layer**: Business rule validation
4. **Infrastructure Layer**: Exception logging

### Specific Strategies

```csharp
// Voice Recognition Errors
try
{
    var result = await _voiceService.RecognizeSpeechAsync();
}
catch (PermissionException ex)
{
    // Prompt user for microphone permission
}
catch (SpeechRecognitionException ex)
{
    // Show "Didn't catch that, please try again"
}

// LLM API Errors
try
{
    var parsed = await _llmService.ParseCommandAsync(text);
}
catch (ApiException ex)
{
    // Fall back to local parser
    var parsed = await _localParser.ParseCommandAsync(text);
}
catch (NetworkException ex)
{
    // Queue command for retry or offer offline mode
}
```

## Performance Considerations

### Optimization Strategies

1. **Lazy Loading**: Load story packs on demand
2. **Object Pooling**: Reuse audio buffers and UI elements
3. **Async/Await**: Non-blocking operations for all I/O
4. **Caching**: Command cache, TTS audio cache, image cache
5. **Virtual Scrolling**: For long story text feeds
6. **Background Processing**: Parse commands off UI thread

### Memory Management

- Dispose audio resources properly
- Clear old story text beyond certain threshold
- Unload unused theme assets
- Monitor memory pressure events

## Security Considerations

### Data Protection

1. **API Keys**: Stored in secure storage, never in code
2. **Permissions**: Runtime permission requests
3. **Network Security**: HTTPS only for API calls
4. **Data Encryption**: Encrypt save files (optional)

### Privacy

- Voice data processed locally or transmitted securely
- No persistent audio recording storage
- Clear privacy policy for any telemetry

## Testing Architecture

### Test Pyramid

```text
        ┌───────────┐
        │    E2E    │  (10%)
        ├───────────┤
        │Integration│  (20%)
        ├───────────┤
        │   Unit    │  (70%)
        └───────────┘
```

### Test Strategy

1. **Unit Tests**: Services, ViewModels, Domain Logic
2. **Integration Tests**: Service interactions, Data access
3. **UI Tests**: Critical user flows
4. **Manual Tests**: Voice recognition accuracy, Device compatibility

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 03-technical-stack.md, 04-data-models.md, 08-maui-implementation.md
