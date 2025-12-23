using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheCabin.Core.Interfaces;
using TheCabin.Maui.Models;

namespace TheCabin.Maui.ViewModels;

public partial class StoryPackSelectorViewModel : BaseViewModel
{
    private readonly IStoryPackService _storyPackService;

    [ObservableProperty]
    private ObservableCollection<StoryPackInfoViewModel> availablePacks = new();

    [ObservableProperty]
    private StoryPackInfoViewModel? selectedPack;

    public StoryPackSelectorViewModel(IStoryPackService storyPackService)
    {
        _storyPackService = storyPackService;
        Title = "Select Your Story";
    }

    public async Task LoadPacksAsync()
    {
        await ExecuteAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine("LoadPacksAsync: Starting to load packs...");

            var packs = await _storyPackService.GetAvailablePacksAsync();

            System.Diagnostics.Debug.WriteLine($"LoadPacksAsync: Found {packs.Count} packs");

            AvailablePacks.Clear();
            foreach (var pack in packs)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPacksAsync: Adding pack: {pack.Id} - {pack.Theme}");
                AvailablePacks.Add(new StoryPackInfoViewModel(pack));
            }

            System.Diagnostics.Debug.WriteLine($"LoadPacksAsync: Total packs in collection: {AvailablePacks.Count}");

            // Select first pack by default
            if (AvailablePacks.Count > 0)
            {
                SelectedPack = AvailablePacks[0];
                System.Diagnostics.Debug.WriteLine($"LoadPacksAsync: Selected default pack: {SelectedPack.Id}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("LoadPacksAsync: No packs available to select");
            }
        }, "Failed to load story packs");
    }

    [RelayCommand]
    private async Task SelectPackAsync(StoryPackInfoViewModel pack)
    {
        if (pack == null) return;

        SelectedPack = pack;

        // Navigate to main page with selected pack ID
        await Shell.Current.GoToAsync("MainPage", new Dictionary<string, object>
        {
            { "SelectedPackId", pack.Id }
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
