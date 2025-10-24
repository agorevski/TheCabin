using TheCabin.Core.Models;
using TheCabin.Core.Services;
using Xunit;

namespace TheCabin.Core.Tests.Services;

public class LocalCommandParserTests
{
    private readonly LocalCommandParser _parser;
    private readonly GameContext _context;
    
    public LocalCommandParserTests()
    {
        _parser = new LocalCommandParser();
        _context = new GameContext
        {
            CurrentLocation = "cabin_main",
            VisibleObjects = new List<string> { "lantern", "door", "table" },
            InventoryItems = new List<string> { "key", "map" },
            GameFlags = new Dictionary<string, bool>()
        };
    }
    
    [Fact]
    public async Task ParseAsync_SimpleMovement_ReturnsGoCommand()
    {
        // Act
        var result = await _parser.ParseAsync("go north", _context);
        
        // Assert
        Assert.Equal("go", result.Verb);
        Assert.Equal("north", result.Object);
        Assert.Null(result.Target);
        Assert.True(result.Confidence > 0.8);
    }
    
    [Fact]
    public async Task ParseAsync_DirectionAbbreviation_ParsesAsDirection()
    {
        // Act
        var result = await _parser.ParseAsync("n", _context);
        
        // Assert
        Assert.Equal("n", result.Verb); // Single letter treated as verb itself
        Assert.Null(result.Object);
    }
    
    [Fact]
    public async Task ParseAsync_TakeWithFillerWords_ReturnsTakeCommand()
    {
        // Act
        var result = await _parser.ParseAsync("take the lantern", _context);
        
        // Assert
        Assert.Equal("take", result.Verb);
        Assert.Equal("lantern", result.Object);
        Assert.True(result.Confidence > 0.7); // Boosted by visible object match
    }
    
    [Fact]
    public async Task ParseAsync_UseSynonym_NormalizesVerb()
    {
        // Act
        var result = await _parser.ParseAsync("grab the key", _context);
        
        // Assert
        Assert.Equal("take", result.Verb); // grab -> take
        Assert.Equal("key", result.Object);
    }
    
    [Fact]
    public async Task ParseAsync_UseOnPattern_ExtractsTarget()
    {
        // Act
        var result = await _parser.ParseAsync("use key on door", _context);
        
        // Assert
        Assert.Equal("use", result.Verb);
        Assert.Equal("key", result.Object);
        Assert.Equal("door", result.Target);
    }
    
    [Fact]
    public async Task ParseAsync_LookAround_ReturnsLookCommand()
    {
        // Act
        var result = await _parser.ParseAsync("look around", _context);
        
        // Assert
        Assert.Equal("look", result.Verb);
        Assert.Equal("around", result.Object);
        Assert.True(result.Confidence > 0.8); // System command
    }
    
    [Fact]
    public async Task ParseAsync_InventoryShortcut_ReturnsInventoryCommand()
    {
        // Act
        var result = await _parser.ParseAsync("i", _context);
        
        // Assert
        Assert.Equal("inventory", result.Verb);
        Assert.True(result.Confidence > 0.8);
    }
    
    [Fact]
    public async Task ParseAsync_EmptyInput_ReturnsHelpCommand()
    {
        // Act
        var result = await _parser.ParseAsync("", _context);
        
        // Assert
        Assert.Equal("help", result.Verb);
        Assert.True(result.Confidence < 0.6);
    }
    
    [Fact]
    public async Task ParseAsync_ExamineWithSynonym_NormalizesVerb()
    {
        // Act
        var result = await _parser.ParseAsync("inspect the table", _context);
        
        // Assert
        Assert.Equal("examine", result.Verb);
        Assert.Equal("table", result.Object);
    }
    
    [Fact]
    public void CanHandle_ValidCommand_ReturnsTrue()
    {
        // Act
        var result = _parser.CanHandle("take lantern");
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void CanHandle_InvalidCommand_ReturnsFalse()
    {
        // Act
        var result = _parser.CanHandle("blahblah nonsense");
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void CanHandle_EmptyInput_ReturnsFalse()
    {
        // Act
        var result = _parser.CanHandle("");
        
        // Assert
        Assert.False(result);
    }
    
    [Theory]
    [InlineData("north")]
    [InlineData("south")]
    [InlineData("east")]
    [InlineData("west")]
    [InlineData("up")]
    [InlineData("down")]
    public async Task ParseAsync_Directions_HighConfidence(string direction)
    {
        // Act
        var result = await _parser.ParseAsync($"go {direction}", _context);
        
        // Assert
        Assert.Equal("go", result.Verb);
        Assert.Equal(direction, result.Object);
        Assert.True(result.Confidence > 0.85);
    }
    
    [Fact]
    public async Task ParseAsync_ObjectInContext_BoostsConfidence()
    {
        // Act
        var resultVisible = await _parser.ParseAsync("take lantern", _context);
        var resultNotVisible = await _parser.ParseAsync("take sword", _context);
        
        // Assert
        Assert.True(resultVisible.Confidence > resultNotVisible.Confidence);
    }
}
