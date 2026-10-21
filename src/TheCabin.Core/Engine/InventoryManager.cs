using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Engine;

/// <summary>
/// Manages player inventory operations including capacity and weight limits
/// </summary>
public class InventoryManager : IInventoryManager
{
    private readonly Func<GameState> _stateProvider;
    private readonly IAchievementService? _achievementService;

    public InventoryManager(GameState gameState, IAchievementService? achievementService = null)
        : this(FixedState(gameState), achievementService)
    {
    }

    /// <summary>
    /// Creates an inventory manager that always works on the state returned by <paramref name="stateProvider"/>,
    /// so it follows the live game state when a new game or save is loaded.
    /// </summary>
    public InventoryManager(Func<GameState> stateProvider, IAchievementService? achievementService = null)
    {
        _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        _achievementService = achievementService; // Optional dependency
    }

    private GameState State => _stateProvider();

    private static Func<GameState> FixedState(GameState gameState)
    {
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState));

        return () => gameState;
    }

    /// <summary>
    /// Checks if an item can be added to inventory based on capacity
    /// </summary>
    public bool CanAdd(GameObject item)
    {
        if (item == null)
            return false;

        var inventory = State.Player.Inventory;

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

        var inventory = State.Player.Inventory;
        if (inventory.Items.Any(i => i.Id == item.Id))
            return; // Already carried; adding again would duplicate the item and its weight

        if (!CanAdd(item))
            throw new InvalidOperationException("Cannot add item: inventory is full");

        inventory.Items.Add(item);
        inventory.TotalWeight += item.Weight;

        // Update stats
        State.Player.Stats.ItemsCollected++;

        // Track achievement
        if (_achievementService != null)
        {
            await _achievementService.TrackEventAsync(
                TriggerType.ItemCollected,
                item.Id,
                State);
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

        var inventory = State.Player.Inventory;
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
                    State);
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

        return State.Player.Inventory.Items
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
        var exactMatch = State.Player.Inventory.Items
            .FirstOrDefault(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
            return exactMatch;

        // Try partial name match
        return State.Player.Inventory.Items
            .FirstOrDefault(i =>
                i.Id.ToLowerInvariant().Contains(itemId) ||
                i.Name.ToLowerInvariant().Contains(itemId));
    }

    /// <summary>
    /// Gets all items in the inventory
    /// </summary>
    public List<GameObject> GetAllItems()
    {
        return new List<GameObject>(State.Player.Inventory.Items);
    }

    /// <summary>
    /// Gets a formatted description of inventory contents
    /// </summary>
    public string GetInventoryDescription()
    {
        var inventory = State.Player.Inventory;

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
