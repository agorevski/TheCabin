using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine;

/// <summary>
/// Manages player inventory operations including capacity and weight limits
/// </summary>
public class InventoryManager : IInventoryManager
{
    private readonly GameState _gameState;
    
    public InventoryManager(GameState gameState)
    {
        _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
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
    /// Adds an item to the inventory
    /// </summary>
    public void AddItem(GameObject item)
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
    }
    
    /// <summary>
    /// Removes an item from the inventory by ID
    /// </summary>
    public void RemoveItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("Item ID cannot be empty", nameof(itemId));
        
        var inventory = _gameState.Player.Inventory;
        var item = inventory.Items.FirstOrDefault(i => i.Id == itemId);
        
        if (item != null)
        {
            inventory.Items.Remove(item);
            inventory.TotalWeight -= item.Weight;
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
