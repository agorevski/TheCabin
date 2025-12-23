using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Manages player inventory operations
/// </summary>
public interface IInventoryManager
{
    /// <summary>
    /// Checks if an item can be added to inventory
    /// </summary>
    bool CanAdd(GameObject item);

    /// <summary>
    /// Adds an item to the inventory
    /// </summary>
    void AddItem(GameObject item);

    /// <summary>
    /// Removes an item from the inventory by ID
    /// </summary>
    void RemoveItem(string itemId);

    /// <summary>
    /// Checks if the inventory contains an item
    /// </summary>
    bool HasItem(string itemId);

    /// <summary>
    /// Gets an item from the inventory
    /// </summary>
    GameObject? GetItem(string itemId);

    /// <summary>
    /// Gets all items in the inventory
    /// </summary>
    List<GameObject> GetAllItems();

    /// <summary>
    /// Gets a formatted description of inventory contents
    /// </summary>
    string GetInventoryDescription();
}
