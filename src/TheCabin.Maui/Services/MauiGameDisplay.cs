using System.Collections.ObjectModel;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;
using MauiNarrativeEntry = TheCabin.Maui.Models.NarrativeEntry;

namespace TheCabin.Maui.Services;

/// <summary>
/// MAUI implementation of IGameDisplay
/// </summary>
public class MauiGameDisplay : IGameDisplay
{
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly ObservableCollection<MauiNarrativeEntry> _storyFeed;

    public MauiGameDisplay(
        IMainThreadDispatcher dispatcher,
        ObservableCollection<MauiNarrativeEntry> storyFeed)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _storyFeed = storyFeed ?? throw new ArgumentNullException(nameof(storyFeed));
    }

    public Task ShowMessageAsync(string message, MessageType type)
    {
        var narrativeType = MapMessageTypeToNarrativeType(type);
        var color = GetColorForMessageType(type);

        var entry = new MauiNarrativeEntry
        {
            Text = message,
            Type = narrativeType,
            Timestamp = DateTime.Now,
            TextColor = color,
            IsImportant = type == MessageType.Discovery
        };

        _dispatcher.BeginInvokeOnMainThread(() =>
        {
            _storyFeed.Add(entry);

            // Keep only last 100 entries for performance
            while (_storyFeed.Count > 100)
            {
                _storyFeed.RemoveAt(0);
            }
        });

        return Task.CompletedTask;
    }

    public Task ShowRoomDescriptionAsync(string roomDescription, IEnumerable<string> visibleObjects, IEnumerable<string> exits)
    {
        // Display the formatted room description
        var entry = new MauiNarrativeEntry
        {
            Text = roomDescription,
            Type = NarrativeType.Description,
            Timestamp = DateTime.Now,
            TextColor = Colors.White,
            IsImportant = false
        };

        _dispatcher.BeginInvokeOnMainThread(() =>
        {
            _storyFeed.Add(entry);

            // Keep only last 100 entries for performance
            while (_storyFeed.Count > 100)
            {
                _storyFeed.RemoveAt(0);
            }
        });

        return Task.CompletedTask;
    }

    public Task ShowAchievementUnlockedAsync(string achievementTitle, string achievementDescription)
    {
        var message = $"🏆 ACHIEVEMENT UNLOCKED!\n{achievementTitle}\n{achievementDescription}";

        var entry = new MauiNarrativeEntry
        {
            Text = message,
            Type = NarrativeType.Discovery,
            Timestamp = DateTime.Now,
            TextColor = Color.FromArgb("#FFD700"), // Gold
            IsImportant = true
        };

        _dispatcher.BeginInvokeOnMainThread(() =>
        {
            _storyFeed.Add(entry);
        });

        return Task.CompletedTask;
    }

    public async Task<string?> PromptAsync(string prompt, string? defaultValue = null)
    {
        var tcs = new TaskCompletionSource<string?>();

        _dispatcher.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var result = await Shell.Current.DisplayPromptAsync(
                    "Input Required",
                    prompt,
                    "OK",
                    "Cancel",
                    placeholder: defaultValue);

                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        _dispatcher.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var result = await Shell.Current.DisplayAlert(title, message, "Yes", "No");
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    private NarrativeType MapMessageTypeToNarrativeType(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Description => NarrativeType.Description,
            MessageType.Success => NarrativeType.Success,
            MessageType.Failure => NarrativeType.Failure,
            MessageType.SystemMessage => NarrativeType.SystemMessage,
            MessageType.Discovery => NarrativeType.Discovery,
            MessageType.PlayerCommand => NarrativeType.PlayerCommand,
            _ => NarrativeType.Description
        };
    }

    private Color GetColorForMessageType(MessageType type)
    {
        return type switch
        {
            MessageType.PlayerCommand => Color.FromArgb("#4A90E2"),
            MessageType.Success => Color.FromArgb("#7ED321"),
            MessageType.Failure => Color.FromArgb("#D0021B"),
            MessageType.SystemMessage => Color.FromArgb("#F5A623"),
            MessageType.Discovery => Color.FromArgb("#BD10E0"),
            _ => Colors.White
        };
    }
}
