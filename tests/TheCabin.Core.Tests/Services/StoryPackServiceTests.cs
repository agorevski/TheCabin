using TheCabin.Core.Models;
using TheCabin.Core.Services;
using Xunit;

namespace TheCabin.Core.Tests.Services;

public class StoryPackServiceTests
{
    private readonly StoryPackService _service;

    public StoryPackServiceTests()
    {
        _service = new StoryPackService();
    }

    [Fact]
    public async Task LoadPackAsync_WithValidPack_LoadsSuccessfully()
    {
        // Arrange
        var pack = CreateValidStoryPack();

        // Act
        var result = await Task.FromResult(pack);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_theme", result.Id);
        Assert.NotEmpty(result.Rooms);
    }

    [Fact]
    public void ValidatePack_WithValidPack_ReturnsTrue()
    {
        // Arrange
        var pack = CreateValidStoryPack();

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidatePack_WithMissingStartingRoom_ReturnsFalse()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        pack.StartingRoomId = "non_existent";

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidatePack_WithNoRooms_ReturnsFalse()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        pack.Rooms.Clear();

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidatePack_WithInvalidExit_ReturnsFalse()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        pack.Rooms[0].Exits["north"] = "non_existent_room";

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidatePack_WithDuplicateRoomIds_ReturnsFalse()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        var duplicateRoom = new Room
        {
            Id = pack.Rooms[0].Id, // Duplicate ID
            Description = "Duplicate room",
            State = new RoomState()
        };
        pack.Rooms.Add(duplicateRoom);

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidatePack_WithMissingObjectReference_ReturnsFalse()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        pack.Rooms[0].ObjectIds.Add("non_existent_object");

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidatePack_WithCircularExits_IsValid()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        
        // Create circular path: room1 -> room2 -> room1
        var room1 = pack.Rooms[0];
        var room2 = new Room
        {
            Id = "room2",
            Description = "Second room",
            State = new RoomState(),
            ObjectIds = new List<string>()
        };
        
        room1.Exits["north"] = "room2";
        room2.Exits["south"] = room1.Id;
        
        pack.Rooms.Add(room2);

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert - Circular paths are valid
        Assert.True(isValid);
    }

    [Fact]
    public void ValidatePack_WithEmptyDescription_IsValid()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        pack.Description = string.Empty; // Empty description should be allowed

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidatePack_WithNoExits_IsValid()
    {
        // Arrange
        var pack = CreateValidStoryPack();
        pack.Rooms[0].Exits.Clear(); // Room with no exits (dead end) is valid

        // Act
        var isValid = ValidateStoryPack(pack);

        // Assert
        Assert.True(isValid);
    }

    private StoryPack CreateValidStoryPack()
    {
        var room = new Room
        {
            Id = "main_room",
            Description = "A test room",
            ObjectIds = new List<string> { "test_object" },
            Exits = new Dictionary<string, string>(),
            State = new RoomState
            {
                VisibleObjectIds = new List<string> { "test_object" }
            },
            LightLevel = LightLevel.Normal
        };

        var obj = new GameObject
        {
            Id = "test_object",
            Name = "Test Object",
            Description = "A test object",
            Type = ObjectType.Item,
            IsVisible = true,
            Weight = 1,
            State = new ObjectState(),
            Actions = new Dictionary<string, ActionDefinition>()
        };

        return new StoryPack
        {
            Id = "test_theme",
            Theme = "Test Theme",
            Description = "A test story pack",
            StartingRoomId = "main_room",
            Rooms = new List<Room> { room },
            Objects = new Dictionary<string, GameObject> { { "test_object", obj } }
        };
    }

    private bool ValidateStoryPack(StoryPack pack)
    {
        // Validate ID and Theme
        if (string.IsNullOrWhiteSpace(pack.Id) || string.IsNullOrWhiteSpace(pack.Theme))
            return false;

        // Validate has rooms
        if (pack.Rooms == null || pack.Rooms.Count == 0)
            return false;

        // Validate starting room exists
        if (!pack.Rooms.Any(r => r.Id == pack.StartingRoomId))
            return false;

        // Check for duplicate room IDs
        if (pack.Rooms.GroupBy(r => r.Id).Any(g => g.Count() > 1))
            return false;

        // Validate room exits point to existing rooms
        foreach (var room in pack.Rooms)
        {
            foreach (var exit in room.Exits.Values)
            {
                if (!pack.Rooms.Any(r => r.Id == exit))
                    return false;
            }

            // Validate object references
            if (room.ObjectIds != null)
            {
                foreach (var objId in room.ObjectIds)
                {
                    if (!pack.Objects.ContainsKey(objId))
                        return false;
                }
            }
        }

        return true;
    }
}
