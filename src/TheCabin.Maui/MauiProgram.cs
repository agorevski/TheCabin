using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
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

        return builder.Build();
    }
    
    private static void RegisterPlatformServices(IServiceCollection services)
    {
        // Platform-specific services
#if ANDROID
        services.AddSingleton<IVoiceRecognitionService, AndroidVoiceRecognitionService>();
#endif
        
        // MAUI built-in services
        services.AddSingleton(TextToSpeech.Default);
        services.AddSingleton<ITextToSpeechService, MauiTextToSpeechService>();
    }
    
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // Story pack service with file path
        var storyPackPath = Path.Combine(FileSystem.AppDataDirectory, "story_packs");
        services.AddSingleton<IStoryPackService>(sp => 
            new StoryPackService(storyPackPath));
        
        // Core services
        services.AddSingleton<IGameStateService, GameStateService>();
        services.AddSingleton<ILocalCommandParser, LocalCommandParser>();
        services.AddSingleton<ICommandParserService>(sp =>
            sp.GetRequiredService<ILocalCommandParser>() as ICommandParserService 
            ?? new LocalCommandParser());
        services.AddSingleton<IPuzzleEngine, PuzzleEngine>();
        
        // Memory cache
        services.AddMemoryCache();
    }
    
    private static void RegisterEngineComponents(IServiceCollection services)
    {
        // Command handlers
        services.AddTransient<ICommandHandler, MoveCommandHandler>();
        services.AddTransient<ICommandHandler, TakeCommandHandler>();
        services.AddTransient<ICommandHandler, LookCommandHandler>();
        services.AddTransient<ICommandHandler, InventoryCommandHandler>();
    }
    
    private static void RegisterDataAccess(IServiceCollection services)
    {
        services.AddSingleton<IGameSaveRepository, GameSaveRepository>();
    }
    
    private static void RegisterViewModels(IServiceCollection services)
    {
        // TODO: Add ViewModels as they are created
        // services.AddTransient<MainViewModel>();
    }
    
    private static void RegisterViews(IServiceCollection services)
    {
        // TODO: Add Views as they are created
        // services.AddTransient<MainPage>();
    }
}
