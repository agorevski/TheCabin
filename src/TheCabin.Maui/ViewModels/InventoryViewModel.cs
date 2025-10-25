using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TheCabin.Core.Models;

namespace TheCabin.Maui.ViewModels;

public partial class InventoryViewModel : BaseViewModel
{
    private readonly Core.Engine.GameStateMachine _stateMachine;
    
    [ObservableProperty]
    private ObservableCollection<GameObject> items;
    
    [ObservableProperty]
    private int totalWeight;
    
    [ObservableProperty]
    private int maxCapacity;
    
    [ObservableProperty]
    private string weightDisplay = string.Empty;
    
    public InventoryViewModel(Core.Engine.GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        Items = new ObservableCollection<GameObject>();
        Title = "Inventory";
        MaxCapacity = 20;
        UpdateInventory();
    }
    
    public void UpdateInventory()
    {
        if (_stateMachine?.CurrentState?.Player?.Inventory != null)
        {
            var inventory = _stateMachine.CurrentState.Player.Inventory;
            
            Items.Clear();
            foreach (var item in inventory.Items)
            {
                Items.Add(item);
            }
            
            TotalWeight = inventory.TotalWeight;
            MaxCapacity = inventory.MaxCapacity;
            WeightDisplay = $"{TotalWeight} / {MaxCapacity} kg";
        }
    }
    
    [RelayCommand]
    private async Task DropItemAsync(GameObject item)
    {
        if (item == null) return;
        
        var confirm = await ShowConfirmAsync(
            "Drop Item",
            $"Drop {item.Name}? You can pick it up again later.");
        
        if (confirm)
        {
            // TODO: Implement drop logic through game state
            Items.Remove(item);
            TotalWeight -= item.Weight;
            WeightDisplay = $"{TotalWeight} / {MaxCapacity} kg";
        }
    }
    
    [RelayCommand]
    private async Task UseItemAsync(GameObject item)
    {
        if (item == null) return;
        
        if (!item.Actions.ContainsKey("use"))
        {
            await Shell.Current.DisplayAlert(
                "Cannot Use",
                $"You can't use the {item.Name} right now.",
                "OK");
            return;
        }
        
        // TODO: Implement use logic through game state
        await Shell.Current.DisplayAlert(
            "Item Used",
            $"You used the {item.Name}.",
            "OK");
    }
    
    [RelayCommand]
    private async Task ExamineItemAsync(GameObject item)
    {
        if (item == null) return;
        
        await Shell.Current.DisplayAlert(
            item.Name,
            item.Description,
            "OK");
    }
    
    [RelayCommand]
    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
