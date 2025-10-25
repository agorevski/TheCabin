using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Core.Services;
using TheCabin.Infrastructure.Repositories;
using TheCabin.Maui.Services;

#if ANDROID
using TheCabin.Maui.Platforms.Android.Services;
#endif

namespace TheCabin.Maui;

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
            });

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

        // Register platform services
        RegisterPlatformServices(builder.Services);
        
        // Register core services
        RegisterCoreServices(builder.Services);
        
        // Register engine components
        RegisterEngineComponents(builder.Services);
        
        // Register data access
        RegisterDataAccess(builder.Services);
        
        // Register ViewModels
        RegisterViewModels(builder.Services);
        
        // Register Views
        RegisterViews(builder.Services);

        var app = builder.Build();
        
        // Register routes with DI-aware factories after app is built
        RegisterRoutesWithDI(app.Services);
        
        return app;
    }
    
    private static void RegisterRoutesWithDI(IServiceProvider services)
    {
        // Register routes - just pass the type, Shell will handle creation
        Routing.RegisterRoute(nameof(Views.StoryPackSelectorPage), typeof(Views.StoryPackSelectorPage));
        Routing.RegisterRoute(nameof(Views.MainPage), typeof(Views.MainPage));
        Routing.RegisterRoute(nameof(Views.InventoryPage), typeof(Views.InventoryPage));
        Routing.RegisterRoute(nameof(Views.SettingsPage), typeof(Views.SettingsPage));
        Routing.RegisterRoute(nameof(Views.LoadGamePage), typeof(Views.LoadGamePage));
        Routing.RegisterRoute(nameof(Views.AchievementsPage), typeof(Views.AchievementsPage));
    }
    
    private static void RegisterPlatformServices(IServiceCollection services)
    {
        // MAUI built-in services
        services.AddSingleton(TextToSpeech.Default);
        services.AddSingleton<ITextToSpeechService, MauiTextToSpeechService>();
        
        // Platform-specific services - register after logging is configured
#if ANDROID
        services.AddSingleton<IVoiceRecognitionService, AndroidVoiceRecognitionService>();
#endif
    }
    
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // Story pack service - will initialize story packs lazily on first access
        services.AddSingleton<IStoryPackService>(sp => 
        {
            var storyPackPath = Path.Combine(FileSystem.AppDataDirectory, "story_packs");
            
            // Ensure directory exists
            if (!Directory.Exists(storyPackPath))
            {
                Directory.CreateDirectory(storyPackPath);
            }
            
            return new StoryPackService(storyPackPath);
        });
        
        // Core services
        services.AddSingleton<IGameStateService, GameStateService>();
        
        // Use LocalCommandParser as the command parser (it implements the simpler interface)
        // We'll create a wrapper to adapt it to ICommandParserService
        services.AddSingleton<ILocalCommandParser, LocalCommandParser>();
        services.AddSingleton<ICommandParserService>(sp =>
        {
            var localParser = sp.GetRequiredService<ILocalCommandParser>();
            // Return a wrapper that adapts ILocalCommandParser to ICommandParserService
            return new LocalCommandParserAdapter(localParser);
        });
        
        // Achievement service (optional dependency for engine components)
        services.AddSingleton<IAchievementService, AchievementService>();
        
        // Puzzle engine with achievement service
        services.AddSingleton<IPuzzleEngine>(sp =>
            new PuzzleEngine(sp.GetService<IAchievementService>()));
        
    }
    
    private static void RegisterEngineComponents(IServiceCollection services)
    {
        // Game state machine - does NOT depend on InventoryManager in constructor
        // We'll need to modify GameStateMachine to not require IInventoryManager
        // For now, let's register it without dependencies
        services.AddSingleton(sp => 
        {
            var achievementService = sp.GetService<IAchievementService>();
            // Create a simple InventoryManager with empty state for GameStateMachine
            var emptyState = new GameState();
            var tempInventoryManager = new InventoryManager(emptyState, achievementService);
            return new GameStateMachine(tempInventoryManager, achievementService);
        });
        
        // Inventory manager - gets the actual GameState from GameStateMachine
        services.AddSingleton<IInventoryManager>(sp =>
        {
            var stateMachine = sp.GetRequiredService<GameStateMachine>();
            var achievementService = sp.GetService<IAchievementService>();
            return new InventoryManager(stateMachine.CurrentState, achievementService);
        });
        
        // Command router with game state machine and achievement service
        services.AddSingleton(sp =>
            new CommandRouter(
                sp.GetServices<ICommandHandler>(),
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetService<IAchievementService>()));
        
        // Command handlers - register with factory methods for proper DI
        services.AddTransient<ICommandHandler>(sp => 
            new MoveCommandHandler(sp.GetRequiredService<GameStateMachine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new TakeCommandHandler(
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IInventoryManager>()));
        services.AddTransient<ICommandHandler>(sp =>
            new DropCommandHandler(
                sp.GetRequiredService<IInventoryManager>(),
                sp.GetRequiredService<GameStateMachine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new UseCommandHandler(
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IInventoryManager>(),
                sp.GetRequiredService<IPuzzleEngine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new ExamineCommandHandler(
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IInventoryManager>()));
        services.AddTransient<ICommandHandler>(sp => 
            new OpenCommandHandler(sp.GetRequiredService<GameStateMachine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new CloseCommandHandler(sp.GetRequiredService<GameStateMachine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new LookCommandHandler(sp.GetRequiredService<GameStateMachine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new InventoryCommandHandler(sp.GetRequiredService<IInventoryManager>()));
    }
    
    private static void RegisterDataAccess(IServiceCollection services)
    {
        services.AddSingleton<IGameSaveRepository, GameSaveRepository>();
    }
    
    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.InventoryViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<ViewModels.StoryPackSelectorViewModel>();
        services.AddTransient<ViewModels.LoadGameViewModel>();
        services.AddTransient<ViewModels.AchievementsPageViewModel>();
        
        // Achievement notification service
        services.AddSingleton<IAchievementNotificationService, AchievementToastService>();
    }
    
    private static void RegisterViews(IServiceCollection services)
    {
        services.AddSingleton<AppShell>();
        services.AddTransient<Views.MainPage>();
        services.AddTransient<Views.InventoryPage>();
        services.AddTransient<Views.SettingsPage>();
        services.AddTransient<Views.StoryPackSelectorPage>();
        services.AddTransient<Views.LoadGamePage>();
        services.AddTransient<Views.AchievementsPage>();
    }
    
}
