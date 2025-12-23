using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Engine;
using TheCabin.Core.Engine.CommandHandlers;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using TheCabin.Core.Services;
using TheCabin.Infrastructure.Repositories;

namespace TheCabin.Console;

class Program
{
    static async Task Main(string[] args)
    {
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();
        var game = new GameRunner(serviceProvider);
        await game.RunAsync();
    }

    static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Core services
        services.AddSingleton<IStoryPackService>(sp =>
            new StoryPackService(Path.Combine(AppContext.BaseDirectory, "story_packs")));
        services.AddSingleton<IGameSaveRepository, GameSaveRepository>();
        services.AddSingleton<IGameStateService, GameStateService>();
        services.AddSingleton<ILocalCommandParser, LocalCommandParser>();
        services.AddSingleton<ICommandParserService, LocalCommandParserAdapter>();

        // Achievement service
        services.AddSingleton<IAchievementService, AchievementService>();

        // Puzzle engine with achievement service
        services.AddSingleton<IPuzzleEngine>(sp =>
            new PuzzleEngine(sp.GetService<IAchievementService>()));

        // Game state machine with achievement service
        services.AddSingleton<IGameStateMachine>(sp =>
        {
            var achievementService = sp.GetService<IAchievementService>();
            var emptyState = new GameState();
            var emptyInventoryManager = new InventoryManager(emptyState, achievementService);
            return new GameStateMachine(emptyInventoryManager, achievementService);
        });

        services.AddSingleton<GameStateMachine>(sp =>
            (GameStateMachine)sp.GetRequiredService<IGameStateMachine>());

        services.AddSingleton<IInventoryManager>(sp =>
        {
            var achievementService = sp.GetService<IAchievementService>();
            var emptyState = new GameState();
            return new InventoryManager(emptyState, achievementService);
        });

        // Command handlers
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

        // Command router
        services.AddSingleton(sp =>
            new CommandRouter(
                sp.GetServices<ICommandHandler>(),
                sp.GetRequiredService<GameStateMachine>(),
                sp.GetService<IAchievementService>()));

        // Console-specific services
        services.AddSingleton<IGameDisplay, ConsoleGameDisplay>();
        services.AddSingleton<GameInitializationService>();
        services.AddSingleton<GameOrchestrator>();

        return services;
    }
}

class LocalCommandParserAdapter : ICommandParserService
{
    private readonly ILocalCommandParser _localParser;

    public LocalCommandParserAdapter(ILocalCommandParser localParser)
    {
        _localParser = localParser;
    }

    public Task<ParsedCommand> ParseAsync(string input, GameContext context, CancellationToken cancellationToken = default)
    {
        return _localParser.ParseAsync(input, context);
    }
}

class GameRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStoryPackService _storyPackService;
    private readonly IGameStateService _gameStateService;
    private readonly GameOrchestrator _orchestrator;
    private readonly IGameDisplay _display;

    public GameRunner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _storyPackService = serviceProvider.GetRequiredService<IStoryPackService>();
        _gameStateService = serviceProvider.GetRequiredService<IGameStateService>();
        _orchestrator = serviceProvider.GetRequiredService<GameOrchestrator>();
        _display = serviceProvider.GetRequiredService<IGameDisplay>();
    }

    public async Task RunAsync()
    {
        ShowWelcome();

        while (true)
        {
            try
            {
                System.Console.WriteLine("\n" + new string('=', 60));
                System.Console.WriteLine("MAIN MENU");
                System.Console.WriteLine(new string('=', 60));
                System.Console.WriteLine("1. New Game");
                System.Console.WriteLine("2. Load Game");
                System.Console.WriteLine("3. Exit");
                System.Console.Write("\nChoose an option: ");

                var choice = System.Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await StartNewGameAsync();
                        break;
                    case "2":
                        await LoadGameAsync();
                        break;
                    case "3":
                        System.Console.WriteLine("\nThanks for playing The Cabin!");
                        return;
                    default:
                        System.Console.WriteLine("\nInvalid choice. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
            }
        }
    }

    private void ShowWelcome()
    {
        System.Console.Clear();
        System.Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                                                              ║
║                    🏚️  THE CABIN 🏚️                         ║
║                                                              ║
║              A Voice-Controlled Text Adventure              ║
║                  (Console Demo Version)                      ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
");
        System.Console.WriteLine("Welcome to The Cabin - an immersive interactive fiction experience.");
        System.Console.WriteLine("Type your commands naturally, and the game will understand!");
    }

    private async Task StartNewGameAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("\n" + new string('=', 60));
        System.Console.WriteLine("SELECT STORY PACK");
        System.Console.WriteLine(new string('=', 60));

        var packs = await _storyPackService.GetAvailablePacksAsync();

        if (packs.Count == 0)
        {
            System.Console.WriteLine("\nNo story packs found!");
            System.Console.WriteLine("Press any key to return to menu...");
            System.Console.ReadKey();
            return;
        }

        for (int i = 0; i < packs.Count; i++)
        {
            var pack = packs[i];
            System.Console.WriteLine($"\n{i + 1}. {pack.Theme}");
            System.Console.WriteLine($"   {pack.Description}");
            System.Console.WriteLine($"   Difficulty: {pack.Difficulty} | Est. Time: {pack.EstimatedPlayTime} min");
        }

        System.Console.Write($"\nChoose a story pack (1-{packs.Count}): ");
        var choice = System.Console.ReadLine()?.Trim();

        if (int.TryParse(choice, out var index) && index >= 1 && index <= packs.Count)
        {
            var selectedPack = packs[index - 1];

            System.Console.WriteLine($"\nLoading '{selectedPack.Theme}'...");

            var success = await _orchestrator.InitializeGameAsync(selectedPack.Id);

            if (success)
            {
                System.Console.WriteLine("\nPress any key to begin your adventure...");
                System.Console.ReadKey();
                await GameLoopAsync();
            }
            else
            {
                System.Console.WriteLine("\nFailed to initialize game. Press any key to return...");
                System.Console.ReadKey();
            }
        }
        else
        {
            System.Console.WriteLine("\nInvalid choice.");
            System.Console.WriteLine("Press any key to return...");
            System.Console.ReadKey();
        }
    }

    private async Task LoadGameAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("\n" + new string('=', 60));
        System.Console.WriteLine("LOAD GAME");
        System.Console.WriteLine(new string('=', 60));

        var saves = await _gameStateService.GetSavedGamesAsync();

        if (saves.Count == 0)
        {
            System.Console.WriteLine("\nNo saved games found.");
            System.Console.WriteLine("Press any key to return...");
            System.Console.ReadKey();
            return;
        }

        for (int i = 0; i < saves.Count; i++)
        {
            var save = saves[i];
            System.Console.WriteLine($"\n{i + 1}. {save.Name}");
            System.Console.WriteLine($"   Theme: {save.ThemeId}");
            System.Console.WriteLine($"   Saved: {save.Timestamp:yyyy-MM-dd HH:mm}");
            System.Console.WriteLine($"   Play Time: {save.PlayTime.Hours}h {save.PlayTime.Minutes}m");
        }

        System.Console.Write($"\nChoose a save (1-{saves.Count}, or 0 to cancel): ");
        var choice = System.Console.ReadLine()?.Trim();

        if (choice == "0") return;

        if (int.TryParse(choice, out var index) && index >= 1 && index <= saves.Count)
        {
            var selectedSave = saves[index - 1];

            System.Console.WriteLine($"\nLoading game '{selectedSave.Name}'...");

            var success = await _orchestrator.LoadGameAsync(selectedSave.Id);

            if (success)
            {
                System.Console.WriteLine("\nPress any key to continue...");
                System.Console.ReadKey();
                await GameLoopAsync();
            }
            else
            {
                System.Console.WriteLine("\nFailed to load game. Press any key to return...");
                System.Console.ReadKey();
            }
        }
    }

    private async Task GameLoopAsync()
    {
        // Don't clear - the initial room description was just shown!
        // Just add a separator
        System.Console.WriteLine();

        while (true)
        {
            System.Console.Write("\n> ");
            var input = System.Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                var shouldSave = await _display.ConfirmAsync(
                    "Quit Game",
                    "Save before quitting?");

                if (shouldSave)
                {
                    var saveName = await _display.PromptAsync(
                        "Enter save name",
                        $"Save {DateTime.Now:yyyy-MM-dd HH:mm}");

                    if (!string.IsNullOrWhiteSpace(saveName))
                    {
                        await _orchestrator.SaveGameAsync(saveName);
                    }
                }
                break;
            }

            if (input.Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                var saveName = await _display.PromptAsync(
                    "Enter save name",
                    $"Save {DateTime.Now:yyyy-MM-dd HH:mm}");

                if (!string.IsNullOrWhiteSpace(saveName))
                {
                    await _orchestrator.SaveGameAsync(saveName);
                }
                continue;
            }

            if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ShowHelp();
                continue;
            }

            try
            {
                await _orchestrator.ProcessCommandAsync(input);
            }
            catch (Exception ex)
            {
                await _display.ShowMessageAsync($"Error: {ex.Message}", MessageType.Failure);
            }
        }
    }

    private void ShowHelp()
    {
        System.Console.WriteLine("\nAVAILABLE COMMANDS:");
        System.Console.WriteLine();
        System.Console.WriteLine("  Movement:");
        System.Console.WriteLine("    go [direction], move [direction]");
        System.Console.WriteLine();
        System.Console.WriteLine("  Items:");
        System.Console.WriteLine("    take [item], drop [item], use [item]");
        System.Console.WriteLine();
        System.Console.WriteLine("  Exploration:");
        System.Console.WriteLine("    look, examine [object], open [object], close [object]");
        System.Console.WriteLine();
        System.Console.WriteLine("  Inventory:");
        System.Console.WriteLine("    inventory, i");
        System.Console.WriteLine();
        System.Console.WriteLine("  System:");
        System.Console.WriteLine("    save, help, quit/exit");
        System.Console.WriteLine();
        System.Console.WriteLine("Examples:");
        System.Console.WriteLine("  'go north', 'take lantern', 'use key', 'examine desk'");
    }
}
