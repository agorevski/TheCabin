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
        
        // Preferences service
        services.AddSingleton<IPreferencesService, MauiPreferencesService>();
        
        // Main thread dispatcher
        services.AddSingleton<IMainThreadDispatcher, MauiMainThreadDispatcher>();
        
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
            
            // Copy story pack files from Resources/Raw to AppDataDirectory synchronously
            CopyStoryPacksFromResourcesSync(storyPackPath);
            
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
        
        // Shared game orchestration services
        services.AddSingleton<GameInitializationService>();
        
    }
    
    private static void RegisterEngineComponents(IServiceCollection services)
    {
        // Register IGameStateMachine and GameStateMachine with empty initial state
        // They will work with GameStateService which manages the actual game state
        services.AddSingleton<IGameStateMachine>(sp =>
        {
            var achievementService = sp.GetService<IAchievementService>();
            
            // Create with empty state - the actual state comes from GameStateService
            var emptyState = new GameState();
            var emptyInventoryManager = new InventoryManager(emptyState, achievementService);
            
            return new GameStateMachine(emptyInventoryManager, achievementService);
        });
        
        // Also register as concrete type for legacy code
        services.AddSingleton<GameStateMachine>(sp =>
            (GameStateMachine)sp.GetRequiredService<IGameStateMachine>());
        
        services.AddSingleton<IInventoryManager>(sp =>
        {
            var achievementService = sp.GetService<IAchievementService>();
            
            // Create with empty state - the actual state comes from GameStateService
            var emptyState = new GameState();
            return new InventoryManager(emptyState, achievementService);
        });
        
        // Command router - needs concrete GameStateMachine for now
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
                sp.GetRequiredService<IInventoryManager>(),
                sp.GetRequiredService<IPuzzleEngine>()));
        services.AddTransient<ICommandHandler>(sp =>
            new DropCommandHandler(
                sp.GetRequiredService<IInventoryManager>(),
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IPuzzleEngine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new UseCommandHandler(
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IInventoryManager>(),
                sp.GetRequiredService<IPuzzleEngine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new ExamineCommandHandler(
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IInventoryManager>(),
                sp.GetRequiredService<IPuzzleEngine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new OpenCommandHandler(
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IPuzzleEngine>()));
        services.AddTransient<ICommandHandler>(sp => 
            new CloseCommandHandler(
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetRequiredService<IPuzzleEngine>()));
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
        // Don't register GameOrchestrator here - MainViewModel will create it with the correct display
        
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
    
    private static void CopyStoryPacksFromResourcesSync(string targetDirectory)
    {
        System.Diagnostics.Debug.WriteLine($"Copying story packs to: {targetDirectory}");
        
        // List of story pack files to copy
        var storyPackFiles = new[]
        {
            "achievements_arctic_survival.json",
            "achievements_classic_horror.json",
            "arctic_survival.json",
            "classic_horror.json",
            "cozy_mystery.json",
            "fantasy_magic.json",
            "puzzles_arctic_survival.json",
            "puzzles_classic_horror.json",
            "sci_fi_isolation.json"
        };
        
        foreach (var fileName in storyPackFiles)
        {
            var targetPath = Path.Combine(targetDirectory, fileName);
            
            // Always copy/overwrite to ensure we have the latest version
            try
            {
                System.Diagnostics.Debug.WriteLine($"Copying {fileName}...");
                
                // Use Task.Run to avoid blocking and get the result synchronously
                var stream = Task.Run(async () => await FileSystem.OpenAppPackageFileAsync(fileName)).Result;
                using (stream)
                using (var fileStream = File.Create(targetPath))
                {
                    stream.CopyTo(fileStream);
                }
                
                System.Diagnostics.Debug.WriteLine($"Successfully copied {fileName}");
            }
            catch (Exception ex)
            {
                // Log error but don't crash - the file might not exist in resources
                System.Diagnostics.Debug.WriteLine($"Failed to copy {fileName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
            }
        }
        
        // Log what files actually exist in the target directory
        if (Directory.Exists(targetDirectory))
        {
            var existingFiles = Directory.GetFiles(targetDirectory);
            System.Diagnostics.Debug.WriteLine($"Files in target directory: {existingFiles.Length}");
            foreach (var file in existingFiles)
            {
                System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
            }
        }
    }
}
