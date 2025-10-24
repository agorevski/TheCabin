using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine;

/// <summary>
/// Manages player inventory operations including capacity and weight limits
/// </summary>
public class InventoryManager : IInventoryManager
{
    private readonly GameState _gameState;
    private readonly IAchievementService? _achievementService;
    
    public InventoryManager(GameState gameState, IAchievementService? achievementService = null)
    {
        _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        _achievementService = achievementService; // Optional dependency
    }
    
    /// <summary>
    /// Checks if an item can be added to inventory based on capacity
    /// </summary>
    public bool CanAdd(GameObject item)
    {
        if (item == null)
            return false;
        
        var inventory = _gameState.Player.Inventory;
        return inventory.TotalWeight + item.Weight <= inventory.MaxCapacity;
    }
    
    /// <summary>
    /// Adds an item to the inventory (synchronous version for backward compatibility)
    /// </summary>
    public void AddItem(GameObject item)
    {
        AddItemAsync(item).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Adds an item to the inventory with achievement tracking
    /// </summary>
    public async Task AddItemAsync(GameObject item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        
        if (!CanAdd(item))
            throw new InvalidOperationException("Cannot add item: inventory is full");
        
        var inventory = _gameState.Player.Inventory;
        inventory.Items.Add(item);
        inventory.TotalWeight += item.Weight;
        
        // Update stats
        _gameState.Player.Stats.ItemsCollected++;
        
        // Track achievement
        if (_achievementService != null)
        {
            await _achievementService.TrackEventAsync(
                TriggerType.ItemCollected,
                item.Id,
                _gameState);
        }
    }
    
    /// <summary>
    /// Removes an item from the inventory by ID (synchronous version)
    /// </summary>
    public void RemoveItem(string itemId)
    {
        RemoveItemAsync(itemId).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Removes an item from the inventory by ID with achievement tracking
    /// </summary>
    public async Task RemoveItemAsync(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("Item ID cannot be empty", nameof(itemId));
        
        var inventory = _gameState.Player.Inventory;
        var item = inventory.Items.FirstOrDefault(i => i.Id == itemId);
        
        if (item != null)
        {
            inventory.Items.Remove(item);
            inventory.TotalWeight -= item.Weight;
            
            // Track achievement
            if (_achievementService != null)
            {
                await _achievementService.TrackEventAsync(
                    TriggerType.ItemDropped,
                    item.Id,
                    _gameState);
            }
        }
    }
    
    /// <summary>
    /// Checks if the inventory contains an item
    /// </summary>
    public bool HasItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;
        
        return _gameState.Player.Inventory.Items
            .Any(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Gets an item from the inventory by ID or partial name match
    /// </summary>
    public GameObject? GetItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;
        
        itemId = itemId.ToLowerInvariant();
        
        // Try exact ID match first
        var exactMatch = _gameState.Player.Inventory.Items
            .FirstOrDefault(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
            return exactMatch;
        
        // Try partial name match
        return _gameState.Player.Inventory.Items
            .FirstOrDefault(i => 
                i.Id.ToLowerInvariant().Contains(itemId) ||
                i.Name.ToLowerInvariant().Contains(itemId));
    }
    
    /// <summary>
    /// Gets all items in the inventory
    /// </summary>
    public List<GameObject> GetAllItems()
    {
        return new List<GameObject>(_gameState.Player.Inventory.Items);
    }
    
    /// <summary>
    /// Gets a formatted description of inventory contents
    /// </summary>
    public string GetInventoryDescription()
    {
        var inventory = _gameState.Player.Inventory;
        
        if (inventory.Items.Count == 0)
        {
            return "You're not carrying anything.";
        }
        
        var itemList = inventory.Items
            .Select(i => $"  - {i.Name} ({i.Weight} kg)")
            .ToList();
        
        var description = "You are carrying:\n" +
                         string.Join("\n", itemList) +
                         $"\n\nTotal weight: {inventory.TotalWeight}/{inventory.MaxCapacity} kg";
        
        return description;
    }
}
