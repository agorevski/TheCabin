using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheCabin.Core.Interfaces;
using TheCabin.Maui.Models;

namespace TheCabin.Maui.ViewModels;

public partial class LoadGameViewModel : BaseViewModel
{
    private readonly IGameStateService _gameStateService;
    
    [ObservableProperty]
    private ObservableCollection<GameSaveInfoViewModel> savedGames = new();
    
    [ObservableProperty]
    private GameSaveInfoViewModel? selectedSave;
    
    [ObservableProperty]
    private bool hasSavedGames;
    
    public LoadGameViewModel(IGameStateService gameStateService)
    {
        _gameStateService = gameStateService;
        Title = "Load Game";
    }
    
    public async Task LoadSavedGamesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var saves = await _gameStateService.GetSavedGamesAsync();
            
            SavedGames.Clear();
            foreach (var save in saves.OrderByDescending(s => s.Timestamp))
            {
                SavedGames.Add(new GameSaveInfoViewModel
                {
                    Id = save.Id,
                    Name = save.Name,
                    ThemeName = save.ThemeId,
                    SavedDate = save.Timestamp,
                    PlayTime = save.PlayTime,
                    PlayerLocation = "Unknown", // This info is not in GameSaveInfo
                    PlayerHealth = 100 // This info is not in GameSaveInfo
                });
            }
            
            HasSavedGames = SavedGames.Count > 0;
        }, "Failed to load saved games");
    }
    
    [RelayCommand]
    private async Task LoadGameAsync(GameSaveInfoViewModel save)
    {
        if (save == null) return;
        
        var confirm = await ShowConfirmAsync(
            "Load Game",
            $"Load '{save.Name}'? Current progress will be lost.");
        
        if (!confirm) return;
        
        SelectedSave = save;
        
        // Return to main page with selected save ID
        await Shell.Current.GoToAsync("..", new Dictionary<string, object>
        {
            { "LoadSaveId", save.Id }
        });
    }
    
    [RelayCommand]
    private async Task DeleteGameAsync(GameSaveInfoViewModel save)
    {
        if (save == null) return;
        
        var confirm = await ShowConfirmAsync(
            "Delete Save",
            $"Permanently delete '{save.Name}'?");
        
        if (!confirm) return;
        
        await ExecuteAsync(async () =>
        {
            await _gameStateService.DeleteSaveAsync(save.Id);
            SavedGames.Remove(save);
            HasSavedGames = SavedGames.Count > 0;
            
            await Shell.Current.DisplayAlert("Deleted", $"'{save.Name}' has been deleted.", "OK");
        }, "Failed to delete save");
    }
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
