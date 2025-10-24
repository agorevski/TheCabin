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

        services.AddSingleton<IStoryPackService>(sp => 
            new StoryPackService(Path.Combine(AppContext.BaseDirectory, "story_packs")));
        services.AddSingleton<IGameSaveRepository, GameSaveRepository>();
        services.AddSingleton<IGameStateService, GameStateService>();
        services.AddSingleton<ILocalCommandParser, LocalCommandParser>();
        services.AddSingleton<ICommandParserService, LocalCommandParserAdapter>();
        services.AddSingleton<IPuzzleEngine, PuzzleEngine>();
        
        // Command handlers
        services.AddTransient<ICommandHandler, MoveCommandHandler>();
        services.AddTransient<ICommandHandler, TakeCommandHandler>();
        services.AddTransient<ICommandHandler, LookCommandHandler>();
        services.AddTransient<ICommandHandler, InventoryCommandHandler>();

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
    private readonly ICommandParserService _commandParser;
    private GameStateMachine? _stateMachine;
    private CommandRouter? _commandRouter;
    private IInventoryManager? _inventoryManager;

    public GameRunner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _storyPackService = serviceProvider.GetRequiredService<IStoryPackService>();
        _gameStateService = serviceProvider.GetRequiredService<IGameStateService>();
        _commandParser = serviceProvider.GetRequiredService<ICommandParserService>();
    }
    
    private void InitializeGameComponents()
    {
        // Create inventory manager with current game state
        _inventoryManager = new InventoryManager(_gameStateService.CurrentState);
        
        // Create game state machine
        _stateMachine = new GameStateMachine(_inventoryManager);
        
        // Create command router with handlers and state machine
        var handlers = _serviceProvider.GetServices<ICommandHandler>();
        _commandRouter = new CommandRouter(handlers, _stateMachine);
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
            var storyPack = await _storyPackService.LoadPackAsync(selectedPack.Id);
            await _gameStateService.InitializeNewGameAsync(storyPack);
            
            // Initialize game components now that we have a game state
            InitializeGameComponents();
            _stateMachine!.Initialize(storyPack);

            System.Console.WriteLine($"\n✓ Loaded '{selectedPack.Theme}'");
            System.Console.WriteLine("\nPress any key to begin your adventure...");
            System.Console.ReadKey();

            await GameLoopAsync();
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
            await _gameStateService.LoadGameAsync(selectedSave.Id);
            var storyPack = await _storyPackService.LoadPackAsync(selectedSave.ThemeId);
            
            // Initialize game components now that we have a game state
            InitializeGameComponents();
            _stateMachine!.Initialize(storyPack);

            System.Console.WriteLine($"\n✓ Loaded game '{selectedSave.Name}'");
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();

            await GameLoopAsync();
        }
    }

    private async Task GameLoopAsync()
    {
        var currentRoom = _stateMachine.GetCurrentRoom();
        
        System.Console.Clear();
        System.Console.WriteLine("\n" + new string('=', 60));
        System.Console.WriteLine(currentRoom.Description);
        System.Console.WriteLine(new string('=', 60));

        while (true)
        {
            System.Console.Write("\n> ");
            var input = System.Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.Write("\nSave before quitting? (y/n): ");
                if (System.Console.ReadLine()?.Trim().ToLower() == "y")
                {
                    System.Console.Write("Enter save name: ");
                    var saveName = System.Console.ReadLine()?.Trim();
                    if (!string.IsNullOrWhiteSpace(saveName))
                    {
                        await _gameStateService.SaveGameAsync(saveName);
                        System.Console.WriteLine("Game saved!");
                    }
                }
                break;
            }

            if (input.Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.Write("Enter save name: ");
                var saveName = System.Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(saveName))
                {
                    await _gameStateService.SaveGameAsync(saveName);
                    System.Console.WriteLine("✓ Game saved!");
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
                var context = new GameContext
                {
                    CurrentLocation = _stateMachine.GetCurrentRoom().Id,
                    VisibleObjects = _stateMachine.GetVisibleObjects().Select(o => o.Id).ToList(),
                    InventoryItems = _gameStateService.CurrentState.Player.Inventory.Items.Select(i => i.Id).ToList(),
                    GameFlags = _gameStateService.CurrentState.Progress.StoryFlags,
                    RecentCommands = new List<string>()
                };

                var parsed = await _commandParser.ParseAsync(input, context);
                var result = await _commandRouter.RouteAsync(parsed);

                System.Console.WriteLine();
                System.Console.WriteLine(result.Message);

                if (result.Success)
                {
                    // State is automatically updated by the command handlers through StateMachine
                    // Just track the narrative for display purposes
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }

    private void ShowHelp()
    {
        System.Console.WriteLine("\nAVAILABLE COMMANDS:");
        System.Console.WriteLine("  Movement:    go [direction], move [direction]");
        System.Console.WriteLine("  Items:       take [item], drop [item], use [item]");
        System.Console.WriteLine("  Exploration: look, examine [object]");
        System.Console.WriteLine("  Inventory:   inventory, i");
        System.Console.WriteLine("  System:      save, help, quit/exit");
        System.Console.WriteLine("\nExamples: 'go north', 'take lantern', 'look around'");
    }
}
