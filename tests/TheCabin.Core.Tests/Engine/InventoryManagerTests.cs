using TheCabin.Core.Engine;
using TheCabin.Core.Models;
using Xunit;

namespace TheCabin.Core.Tests.Engine;

public class InventoryManagerTests
{
    [Fact]
    public async Task AddItemAsync_WhenItemAlreadyCarried_DoesNotDuplicateItemOrWeight()
    {
        var gameState = new GameState();
        var inventoryManager = new InventoryManager(gameState);
        var lantern = new GameObject { Id = "lantern", Name = "Lantern", Weight = 3 };
        await inventoryManager.AddItemAsync(lantern);

        await inventoryManager.AddItemAsync(lantern);

        Assert.Single(gameState.Player.Inventory.Items);
        Assert.Equal(3, gameState.Player.Inventory.TotalWeight);
    }
}
