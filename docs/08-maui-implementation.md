# 08 - MAUI Implementation

## Overview

This document provides detailed implementation guidance for The Cabin using .NET MAUI, including project structure, platform-specific code, and best practices.

## Project Structure

```text
TheCabin/
├── TheCabin.csproj
├── App.xaml
├── App.xaml.cs
├── AppShell.xaml
├── AppShell.xaml.cs
├── MauiProgram.cs
│
├── Models/
│   ├── Domain/
│   │   ├── Room.cs
│   │   ├── GameObject.cs
│   │   ├── Player.cs
│   │   └── GameState.cs
│   ├── Services/
│   │   ├── VoiceRecognitionResult.cs
│   │   └── ParsedCommand.cs
│   └── UI/
│       └── NarrativeEntry.cs
│
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── MainViewModel.cs
│   ├── InventoryViewModel.cs
│   ├── SettingsViewModel.cs
│   └── StoryPackViewModel.cs
│
├── Views/
│   ├── MainPage.xaml
│   ├── MainPage.xaml.cs
│   ├── InventoryPage.xaml
│   ├── InventoryPage.xaml.cs
│   ├── SettingsPage.xaml
│   └── SettingsPage.xaml.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── IVoiceRecognitionService.cs
│   │   ├── ITextToSpeechService.cs
│   │   ├── ICommandParserService.cs
│   │   └── IGameStateService.cs
│   └── Implementation/
│       ├── CommandParserService.cs
│       ├── GameStateService.cs
│       └── StoryPackService.cs
│
├── Engine/
│   ├── GameStateMachine.cs
│   ├── CommandRouter.cs
│   ├── CommandHandlers/
│   │   ├── MoveCommandHandler.cs
│   │   ├── TakeCommandHandler.cs
│   │   └── UseCommandHandler.cs
│   ├── InventoryManager.cs
│   └── PuzzleEngine.cs
│
├── Data/
│   ├── Repositories/
│   │   ├── GameSaveRepository.cs
│   │   ├── StoryPackRepository.cs
│   │   └── CommandCacheRepository.cs
│   └── Entities/
│       ├── GameSaveEntity.cs
│       └── CommandCacheEntity.cs
│
├── Platforms/
│   ├── Android/
│   │   ├── MainActivity.cs
│   │   ├── MainApplication.cs
│   │   ├── AndroidManifest.xml
│   │   └── Services/
│   │       ├── AndroidVoiceRecognitionService.cs
│   │       └── AndroidTtsService.cs
│   ├── iOS/ (future)
│   └── Windows/ (future)
│
├── Resources/
│   ├── Fonts/
│   ├── Images/
│   ├── Raw/
│   └── Styles/
│       ├── Colors.xaml
│       └── Styles.xaml
│
└── Assets/
    └── StoryPacks/
        ├── classic_horror.json
        ├── arctic_survival.json
        └── ...
```

## MauiProgram.cs Setup

```csharp
using Microsoft.Extensions.Logging;
using TheCabin.ViewModels;
using TheCabin.Views;
using TheCabin.Services;
using TheCabin.Engine;
using CommunityToolkit.Maui;

namespace TheCabin;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Georgia.ttf", "Georgia");
                fonts.AddFont("Courier.ttf", "Courier");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register Services
        RegisterServices(builder.Services);
        
        // Register ViewModels
        RegisterViewModels(builder.Services);
        
        // Register Views
        RegisterViews(builder.Services);
        
        // Register Engine Components
        RegisterEngineComponents(builder.Services);
        
        // Register Data Access
        RegisterDataAccess(builder.Services);

        return builder.Build();
    }
    
    private static void RegisterServices(IServiceCollection services)
    {
        // Platform Services
#if ANDROID
        services.AddSingleton<IVoiceRecognitionService, 
            Platforms.Android.Services.AndroidVoiceRecognitionService>();
#endif
        
        // Core Services
        services.AddSingleton<ITextToSpeechService, MauiTtsService>();
        services.AddSingleton<ICommandParserService, LlmCommandParserService>();
        services.AddSingleton<IGameStateService, GameStateService>();
        services.AddSingleton<IStoryPackService, StoryPackService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        
        // HTTP Client for LLM API
        services.AddHttpClient<ILlmApiClient, OpenAIApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Memory Cache
        services.AddMemoryCache();
        
        // Logging
        services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Information);
        });
    }
    
    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<StoryPackViewModel>();
        services.AddTransient<LoadGameViewModel>();
    }
    
    private static void RegisterViews(IServiceCollection services)
    {
        services.AddTransient<MainPage>();
        services.AddTransient<InventoryPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<StoryPackSelectorPage>();
        services.AddTransient<LoadGamePage>();
    }
    
    private static void RegisterEngineComponents(IServiceCollection services)
    {
        services.AddSingleton<GameStateMachine>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<IInventoryManager, InventoryManager>();
        services.AddSingleton<IPuzzleEngine, PuzzleEngine>();
        
        // Command Handlers
        services.AddTransient<ICommandHandler, MoveCommandHandler>();
        services.AddTransient<ICommandHandler, TakeCommandHandler>();
        services.AddTransient<ICommandHandler, DropCommandHandler>();
        services.AddTransient<ICommandHandler, UseCommandHandler>();
        services.AddTransient<ICommandHandler, LookCommandHandler>();
        services.AddTransient<ICommandHandler, ExamineCommandHandler>();
        services.AddTransient<ICommandHandler, InventoryCommandHandler>();
    }
    
    private static void RegisterDataAccess(IServiceCollection services)
    {
        services.AddSingleton<IGameSaveRepository, GameSaveRepository>();
        services.AddSingleton<IStoryPackRepository, StoryPackRepository>();
        services.AddSingleton<ICommandCacheRepository, CommandCacheRepository>();
        
        // Initialize Database
        services.AddSingleton(sp =>
        {
            var dbPath = Path.Combine(
                FileSystem.AppDataDirectory, 
                "thecabin.db3");
            return new SQLiteAsyncConnection(dbPath);
        });
    }
}
```

## App.xaml and App.xaml.cs

```xaml
<?xml version="1.0" encoding="UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TheCabin.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

```csharp
namespace TheCabin;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // Set user app theme
        UserAppTheme = AppTheme.Dark;
        
        MainPage = new AppShell();
    }
    
    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        
        // Set window size (for desktop)
        window.Width = 400;
        window.Height = 800;
        window.MinimumWidth = 350;
        window.MinimumHeight = 600;
        
        return window;
    }
}
```

## AppShell.xaml

```xaml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="TheCabin.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:local="clr-namespace:TheCabin.Views"
    Shell.FlyoutBehavior="Disabled"
    Shell.NavBarIsVisible="False">

    <ShellContent
        Title="The Cabin"
        ContentTemplate="{DataTemplate local:MainPage}"
        Route="MainPage" />

</Shell>
```

## BaseViewModel Implementation

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TheCabin.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;
    
    [ObservableProperty]
    private string title;
    
    [ObservableProperty]
    private string errorMessage;
    
    public BaseViewModel()
    {
        title = string.Empty;
        errorMessage = string.Empty;
    }
    
    protected async Task ExecuteAsync(
        Func<Task> operation, 
        string? errorMessage = null)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            
            await operation();
        }
        catch (Exception ex)
        {
            ErrorMessage = errorMessage ?? ex.Message;
            await ShowErrorAsync(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    protected async Task ShowErrorAsync(string message)
    {
        await Shell.Current.DisplayAlert(
            "Error", 
            message, 
            "OK");
    }
    
    protected async Task<bool> ShowConfirmAsync(
        string title,
        string message)
    {
        return await Shell.Current.DisplayAlert(
            title, 
            message, 
            "Yes", 
            "No");
    }
}
```

## MainViewModel Implementation

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TheCabin.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IVoiceRecognitionService _voiceService;
    private readonly ICommandParserService _parserService;
    private readonly IGameStateService _gameStateService;
    private readonly ITextToSpeechService _ttsService;
    private readonly GameStateMachine _stateMachine;
    private readonly CommandRouter _commandRouter;
    
    [ObservableProperty]
    private ObservableCollection<NarrativeEntry> storyFeed;
    
    [ObservableProperty]
    private string currentLocation;
    
    [ObservableProperty]
    private int playerHealth;
    
    [ObservableProperty]
    private string lightLevel;
    
    [ObservableProperty]
    private string gameTime;
    
    [ObservableProperty]
    private string transcriptText;
    
    [ObservableProperty]
    private bool isListening;
    
    [ObservableProperty]
    private bool isProcessing;
    
    public MainViewModel(
        IVoiceRecognitionService voiceService,
        ICommandParserService parserService,
        IGameStateService gameStateService,
        ITextToSpeechService ttsService,
        GameStateMachine stateMachine,
        CommandRouter commandRouter)
    {
        _voiceService = voiceService;
        _parserService = parserService;
        _gameStateService = gameStateService;
        _ttsService = ttsService;
        _stateMachine = stateMachine;
        _commandRouter = commandRouter;
        
        StoryFeed = new ObservableCollection<NarrativeEntry>();
        
        Title = "The Cabin";
        CurrentLocation = "Unknown";
        PlayerHealth = 100;
        LightLevel = "Normal";
        GameTime = "0:00";
    }
    
    [RelayCommand]
    private async Task ToggleListeningAsync()
    {
        if (IsListening)
        {
            _voiceService.StopListening();
            IsListening = false;
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
            
            var result = await _voiceService.RecognizeSpeechAsync();
            
            IsListening = false;
            
            if (result.Success)
            {
                TranscriptText = result.TranscribedText;
                await ProcessCommandAsync(result.TranscribedText);
            }
            else
            {
                TranscriptText = string.Empty;
                AddNarrativeEntry(
                    result.ErrorMessage, 
                    NarrativeType.SystemMessage);
            }
        }, "Voice recognition failed");
    }
    
    private async Task ProcessCommandAsync(string input)
    {
        await ExecuteAsync(async () =>
        {
            IsProcessing = true;
            
            // Add player command to story feed
            AddNarrativeEntry(
                $"▶ \"{input}\"", 
                NarrativeType.PlayerCommand);
            
            // Parse command
            var context = BuildGameContext();
            var parsed = await _parserService.ParseAsync(input, context);
            
            // Execute command
            var result = await _commandRouter.RouteAsync(
                parsed, 
                _stateMachine.CurrentState);
            
            // Add result to story feed
            AddNarrativeEntry(
                result.Message, 
                result.Success 
                    ? NarrativeType.Success 
                    : NarrativeType.Failure);
            
            // Update UI state
            UpdateUIState();
            
            // Optional TTS narration
            if (_ttsService != null && result.Success)
            {
                await _ttsService.SpeakAsync(result.Message);
            }
            
            IsProcessing = false;
        }, "Command processing failed");
    }
    
    private GameContext BuildGameContext()
    {
        var room = _stateMachine.GetCurrentRoom();
        var visibleObjects = _stateMachine.GetVisibleObjects();
        
        return new GameContext
        {
            CurrentLocation = room.Id,
            VisibleObjects = visibleObjects.Select(o => o.Id).ToList(),
            InventoryItems = _stateMachine.CurrentState.Player.Inventory
                .Items.Select(i => i.Id).ToList(),
            GameFlags = _stateMachine.CurrentState.Progress.StoryFlags
        };
    }
    
    private void UpdateUIState()
    {
        var state = _stateMachine.CurrentState;
        var room = _stateMachine.GetCurrentRoom();
        
        CurrentLocation = room.Id.Replace("_", " ");
        PlayerHealth = state.Player.Health;
        LightLevel = room.LightLevel.ToString();
        GameTime = state.Player.Stats.PlayTime.ToString(@"h\:mm");
    }
    
    private void AddNarrativeEntry(string text, NarrativeType type)
    {
        var entry = new NarrativeEntry
        {
            Text = text,
            Type = type,
            Timestamp = DateTime.Now
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
        await Shell.Current.GoToAsync(nameof(InventoryPage));
    }
    
    [RelayCommand]
    private async Task ShowMenuAsync()
    {
        // Show menu options
    }
    
    [RelayCommand]
    private async Task ShowSettingsAsync()
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
```

## Platform-Specific: Android MainActivity

```csharp
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace TheCabin.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true, 
    ConfigurationChanges = ConfigChanges.ScreenSize | 
                          ConfigChanges.Orientation | 
                          ConfigChanges.UiMode | 
                          ConfigChanges.ScreenLayout | 
                          ConfigChanges.SmallestScreenSize | 
                          ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Portrait)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Request permissions
        RequestPermissions();
    }
    
    private async void RequestPermissions()
    {
        await Permissions.RequestAsync<Permissions.Microphone>();
        await Permissions.RequestAsync<Permissions.StorageWrite>();
    }
}
```

## Platform-Specific: AndroidManifest.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application 
        android:allowBackup="true" 
        android:icon="@mipmap/appicon" 
        android:roundIcon="@mipmap/appicon_round" 
        android:supportsRtl="true"
        android:label="The Cabin">
    </application>
    
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.RECORD_AUDIO" />
    <uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
    <uses-permission android:name="android.permission.WAKE_LOCK" />
    <uses-permission android:name="android.permission.VIBRATE" />
    
    <uses-sdk android:minSdkVersion="23" android:targetSdkVersion="34" />
</manifest>
```

## Build Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net8.0-android</TargetFrameworks>
        <OutputType>Exe</OutputType>
        <RootNamespace>TheCabin</RootNamespace>
        <UseMaui>true</UseMaui>
        <SingleProject>true</SingleProject>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <!-- Display name -->
        <ApplicationTitle>The Cabin</ApplicationTitle>

        <!-- App Identifier -->
        <ApplicationId>com.thecabin.voiceadventure</ApplicationId>

        <!-- Versions -->
        <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
        <ApplicationVersion>1</ApplicationVersion>

        <!-- Required for C# Hot Reload -->
        <UseInterpreter Condition="'$(Configuration)' == 'Debug'">True</UseInterpreter>

        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">23.0</SupportedOSPlatformVersion>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)' == 'Release'">
        <AndroidPackageFormat>apk</AndroidPackageFormat>
        <AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>
        <AndroidLinkMode>SdkOnly</AndroidLinkMode>
        <AndroidUseAapt2>true</AndroidUseAapt2>
        <RunAOTCompilation>false</RunAOTCompilation>
    </PropertyGroup>

    <ItemGroup>
        <!-- App Icon -->
        <MauiIcon Include="Resources\AppIcon\appicon.svg" 
                  ForegroundFile="Resources\AppIcon\appiconfg.svg" 
                  Color="#512BD4" />

        <!-- Splash Screen -->
        <MauiSplashScreen Include="Resources\Splash\splash.svg" 
                          Color="#512BD4" 
                          BaseSize="128,128" />

        <!-- Images -->
        <MauiImage Include="Resources\Images\*" />

        <!-- Custom Fonts -->
        <MauiFont Include="Resources\Fonts\*" />

        <!-- Raw Assets (also remove the "Resources\Raw" prefix) -->
        <MauiAsset Include="Resources\Raw\**" 
                   LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.0" />
        <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="8.0.0" />
        <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
        <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
        <PackageReference Include="CommunityToolkit.Maui" Version="7.0.0" />
        <PackageReference Include="sqlite-net-pcl" Version="1.8.116" />
        <PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.7" />
        <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />
        <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
    </ItemGroup>

    <ItemGroup>
        <!-- Story Pack JSON files -->
        <MauiAsset Include="Assets\StoryPacks\*.json" />
    </ItemGroup>
</Project>
```

## Hot Reload Support

```csharp
#if DEBUG
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(
    typeof(TheCabin.HotReloadService))]

namespace TheCabin;

public static class HotReloadService
{
    public static void ClearCache(Type[]? types)
    {
        // Clear any cached data that might prevent hot reload
    }
    
    public static void UpdateApplication(Type[]? types)
    {
        // Refresh application state after hot reload
    }
}
#endif
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 02-system-architecture.md, 03-technical-stack.md, 07-ui-ux-design.md
