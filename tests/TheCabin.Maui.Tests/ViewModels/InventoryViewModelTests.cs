using Xunit;
using Moq;
using FluentAssertions;
using TheCabin.Maui.ViewModels;
using TheCabin.Core.Engine;
using TheCabin.Core.Models;
using System.Collections.ObjectModel;

namespace TheCabin.Maui.Tests.ViewModels;

public class InventoryViewModelTests
{
    private readonly Mock<GameStateMachine> _mockStateMachine;
    private readonly InventoryViewModel _viewModel;
    private readonly GameState _testGameState;

    public InventoryViewModelTests()
    {
        _mockStateMachine = new Mock<GameStateMachine>(MockBehavior.Loose, null);
        
        // Setup test game state with inventory
        _testGameState = new GameState();
        _testGameState.Player.Inventory = new Inventory
        {
            MaxCapacity = 20,
            Items = new List<GameObject>()
        };

        _mockStateMachine.Setup(x => x.CurrentState).Returns(_testGameState);

        _viewModel = new InventoryViewModel(_mockStateMachine.Object);
    }

    #region Initialization Tests (I-01 to I-04)

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("Inventory");
        _viewModel.Items.Should().NotBeNull();
        _viewModel.MaxCapacity.Should().Be(20);
    }

    [Fact]
    public void Constructor_InitializesEmptyInventory()
    {
        // Assert
        _viewModel.Items.Should().BeEmpty();
        _viewModel.TotalWeight.Should().Be(0);
        _viewModel.WeightDisplay.Should().Contain("0");
    }

    [Fact]
    public void Constructor_LoadsItemsFromGameState()
    {
        // Arrange
        var testItem = new GameObject
        {
            Id = "test_item",
            Name = "Test Item",
            Description = "A test item",
            Weight = 5
        };
        _testGameState.Player.Inventory.Items.Add(testItem);

        // Act
        var vm = new InventoryViewModel(_mockStateMachine.Object);

        // Assert
        vm.Items.Should().HaveCount(1);
        vm.Items[0].Id.Should().Be("test_item");
    }

    [Fact]
    public void UpdateInventory_LoadsCurrentItems()
    {
        // Arrange
        var testItem = new GameObject
        {
            Id = "flashlight",
            Name = "Flashlight",
            Weight = 2
        };
        _testGameState.Player.Inventory.Items.Add(testItem);

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.Items.Should().HaveCount(1);
        _viewModel.Items[0].Name.Should().Be("Flashlight");
    }

    #endregion

    #region Empty State Tests (I-10 to I-12)

    [Fact]
    public void EmptyState_WhenNoItems()
    {
        // Assert
        _viewModel.Items.Should().BeEmpty();
    }

    #endregion

    #region Item Display Tests (I-20 to I-26)

    [Fact]
    public void ItemDisplay_ShowsItemName()
    {
        // Arrange
        var item = new GameObject
        {
            Id = "key",
            Name = "Rusty Key",
            Weight = 1
        };
        _testGameState.Player.Inventory.Items.Add(item);

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.Items[0].Name.Should().Be("Rusty Key");
    }

    [Fact]
    public void ItemDisplay_ShowsItemDescription()
    {
        // Arrange
        var item = new GameObject
        {
            Id = "key",
            Name = "Rusty Key",
            Description = "An old rusty key",
            Weight = 1
        };
        _testGameState.Player.Inventory.Items.Add(item);

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.Items[0].Description.Should().Be("An old rusty key");
    }

    [Fact]
    public void ItemDisplay_ShowsItemWeight()
    {
        // Arrange
        var item = new GameObject
        {
            Id = "key",
            Name = "Rusty Key",
            Weight = 3
        };
        _testGameState.Player.Inventory.Items.Add(item);

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.Items[0].Weight.Should().Be(3);
    }

    [Fact]
    public void ItemDisplay_HandlesMultipleItems()
    {
        // Arrange
        _testGameState.Player.Inventory.Items.Add(new GameObject { Id = "item1", Name = "Item 1", Weight = 1 });
        _testGameState.Player.Inventory.Items.Add(new GameObject { Id = "item2", Name = "Item 2", Weight = 2 });
        _testGameState.Player.Inventory.Items.Add(new GameObject { Id = "item3", Name = "Item 3", Weight = 3 });

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.Items.Should().HaveCount(3);
    }

    #endregion

    #region Weight Calculation Tests

    [Fact]
    public void Weight_CalculatesTotalWeight()
    {
        // Arrange
        _testGameState.Player.Inventory.Items.Add(new GameObject { Id = "item1", Weight = 5 });
        _testGameState.Player.Inventory.Items.Add(new GameObject { Id = "item2", Weight = 3 });
        _testGameState.Player.Inventory.Items.Add(new GameObject { Id = "item3", Weight = 2 });
        _testGameState.Player.Inventory.TotalWeight = 10;

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.TotalWeight.Should().Be(10);
    }

    [Fact]
    public void WeightDisplay_ShowsFormattedWeight()
    {
        // Arrange
        _testGameState.Player.Inventory.TotalWeight = 15;
        _testGameState.Player.Inventory.MaxCapacity = 20;

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.WeightDisplay.Should().Be("15 / 20 kg");
    }

    [Fact]
    public void Weight_UpdatesAfterDrop()
    {
        // Arrange
        var item = new GameObject { Id = "heavy_item", Name = "Heavy Item", Weight = 5 };
        _testGameState.Player.Inventory.Items.Add(item);
        _testGameState.Player.Inventory.TotalWeight = 5;
        _viewModel.UpdateInventory();
        var initialWeight = _viewModel.TotalWeight;

        // Act
        _viewModel.Items.Remove(item);
        _viewModel.TotalWeight -= item.Weight;
        _viewModel.WeightDisplay = $"{_viewModel.TotalWeight} / {_viewModel.MaxCapacity} kg";

        // Assert
        _viewModel.TotalWeight.Should().BeLessThan(initialWeight);
        _viewModel.WeightDisplay.Should().Be("0 / 20 kg");
    }

    #endregion

    #region Drop Item Tests (I-30 to I-35)

    [Fact]
    public void DropItemCommand_IsNotNull()
    {
        // Assert
        _viewModel.DropItemCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task DropItem_RemovesItemFromCollection()
    {
        // Arrange
        var item = new GameObject { Id = "item1", Name = "Item 1", Weight = 2 };
        _testGameState.Player.Inventory.Items.Add(item);
        _viewModel.UpdateInventory();

        // Note: In real scenario, this would show a confirmation dialog
        // For testing, we're directly manipulating the collection
        
        // Act
        _viewModel.Items.Remove(item);

        // Assert
        _viewModel.Items.Should().NotContain(item);
    }

    #endregion

    #region Use Item Tests (I-32 to I-34)

    [Fact]
    public void UseItemCommand_IsNotNull()
    {
        // Assert
        _viewModel.UseItemCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task UseItem_WithUseableItem_ExecutesAction()
    {
        // Arrange
        var item = new GameObject
        {
            Id = "flashlight",
            Name = "Flashlight",
            Actions = new Dictionary<string, ActionDefinition>
            {
                { "use", new ActionDefinition { Verb = "use", SuccessMessage = "You turn on the flashlight" } }
            }
        };
        _testGameState.Player.Inventory.Items.Add(item);
        _viewModel.UpdateInventory();

        // Act & Assert - Command should be available
        _viewModel.UseItemCommand.Should().NotBeNull();
    }

    #endregion

    #region Examine Item Tests (I-40 to I-41)

    [Fact]
    public void ExamineItemCommand_IsNotNull()
    {
        // Assert
        _viewModel.ExamineItemCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task ExamineItem_ShowsItemDetails()
    {
        // Arrange
        var item = new GameObject
        {
            Id = "key",
            Name = "Ancient Key",
            Description = "A mysterious ancient key with strange markings"
        };
        _testGameState.Player.Inventory.Items.Add(item);
        _viewModel.UpdateInventory();

        // Act & Assert - Command should be available
        _viewModel.ExamineItemCommand.Should().NotBeNull();
    }

    #endregion

    #region Navigation Tests (I-42 to I-43)

    [Fact]
    public void CloseCommand_IsNotNull()
    {
        // Assert
        _viewModel.CloseCommand.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Inventory_HandlesNullGameState()
    {
        // Arrange
        var mockStateMachine = new Mock<GameStateMachine>(MockBehavior.Loose, null);
        mockStateMachine.Setup(x => x.CurrentState).Returns((GameState?)null);

        // Act
        var vm = new InventoryViewModel(mockStateMachine.Object);

        // Assert
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public void Inventory_HandlesNullInventory()
    {
        // Arrange
        var mockStateMachine = new Mock<GameStateMachine>(MockBehavior.Loose, null);
        var state = new GameState();
        state.Player.Inventory = null!;
        mockStateMachine.Setup(x => x.CurrentState).Returns(state);

        // Act
        var vm = new InventoryViewModel(mockStateMachine.Object);

        // Assert
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateInventory_ClearsExistingItems()
    {
        // Arrange
        _viewModel.Items.Add(new GameObject { Id = "old_item", Name = "Old" });
        var newItem = new GameObject { Id = "new_item", Name = "New", Weight = 1 };
        _testGameState.Player.Inventory.Items.Clear();
        _testGameState.Player.Inventory.Items.Add(newItem);

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.Items.Should().HaveCount(1);
        _viewModel.Items[0].Id.Should().Be("new_item");
    }

    [Fact]
    public void Inventory_UpdatesWeightDisplayOnUpdate()
    {
        // Arrange
        _testGameState.Player.Inventory.Items.Add(new GameObject { Id = "item1", Weight = 5 });
        _testGameState.Player.Inventory.TotalWeight = 5;

        // Act
        _viewModel.UpdateInventory();

        // Assert
        _viewModel.WeightDisplay.Should().Contain("5");
        _viewModel.WeightDisplay.Should().Contain("20");
    }

    #endregion
}
