using TheCabin.Core.Interfaces;

namespace TheCabin.Console;

/// <summary>
/// Console implementation of IGameDisplay
/// </summary>
public class ConsoleGameDisplay : IGameDisplay
{
    public Task ShowMessageAsync(string message, MessageType type)
    {
        var originalColor = System.Console.ForegroundColor;

        // Set color based on message type
        System.Console.ForegroundColor = type switch
        {
            MessageType.Success => ConsoleColor.Green,
            MessageType.Failure => ConsoleColor.Red,
            MessageType.SystemMessage => ConsoleColor.Yellow,
            MessageType.Discovery => ConsoleColor.Magenta,
            MessageType.PlayerCommand => ConsoleColor.Cyan,
            MessageType.Description => ConsoleColor.White,
            _ => ConsoleColor.White
        };

        System.Console.WriteLine(message);
        System.Console.ForegroundColor = originalColor;

        return Task.CompletedTask;
    }

    public Task ShowRoomDescriptionAsync(string roomDescription, IEnumerable<string> visibleObjects, IEnumerable<string> exits)
    {
        // Display the formatted room description (already includes objects and exits)
        System.Console.WriteLine();
        System.Console.WriteLine(new string('=', 60));
        System.Console.ForegroundColor = ConsoleColor.White;
        System.Console.WriteLine(roomDescription);
        System.Console.ForegroundColor = ConsoleColor.Gray;
        System.Console.WriteLine(new string('=', 60));

        return Task.CompletedTask;
    }

    public Task ShowAchievementUnlockedAsync(string achievementTitle, string achievementDescription)
    {
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║                    🏆 ACHIEVEMENT UNLOCKED! 🏆               ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        System.Console.ForegroundColor = ConsoleColor.White;
        System.Console.WriteLine($"  {achievementTitle}");
        System.Console.ForegroundColor = ConsoleColor.Gray;
        System.Console.WriteLine($"  {achievementDescription}");
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Gray;

        return Task.CompletedTask;
    }

    public Task<string?> PromptAsync(string prompt, string? defaultValue = null)
    {
        System.Console.Write($"{prompt}: ");
        var input = System.Console.ReadLine()?.Trim();
        return Task.FromResult(string.IsNullOrWhiteSpace(input) ? defaultValue : input);
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        System.Console.WriteLine($"\n{title}");
        System.Console.Write($"{message} (y/n): ");
        var response = System.Console.ReadLine()?.Trim().ToLower();
        return Task.FromResult(response == "y" || response == "yes");
    }
}
