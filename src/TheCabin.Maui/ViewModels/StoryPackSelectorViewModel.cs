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
            var packs = await _storyPackService.GetAvailablePacksAsync();
            
            AvailablePacks.Clear();
            foreach (var pack in packs)
            {
                AvailablePacks.Add(new StoryPackInfoViewModel(pack));
            }
            
            // Select first pack by default
            if (AvailablePacks.Count > 0)
            {
                SelectedPack = AvailablePacks[0];
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
