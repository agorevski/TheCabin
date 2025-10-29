using System.Text.Json.Serialization;

namespace StoryPackValidator;

public class StoryPack
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "";
    
    [JsonPropertyName("rooms")]
    public List<Room> Rooms { get; set; } = new();
    
    [JsonPropertyName("objects")]
    public Dictionary<string, GameObject> Objects { get; set; } = new();
}

public class Room
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("objectIds")]
    public List<string> ObjectIds { get; set; } = new();
}

public class GameObject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("isCollectable")]
    public bool IsCollectable { get; set; }
}

public class PuzzleFile
{
    public List<Puzzle> Puzzles { get; set; } = new();
}

public class Puzzle
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
    
    [JsonPropertyName("steps")]
    public List<PuzzleStep> Steps { get; set; } = new();
    
    [JsonPropertyName("achievementId")]
    public string? AchievementId { get; set; }
}

public class PuzzleStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }
    
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";
    
    [JsonPropertyName("targetObject")]
    public string TargetObject { get; set; } = "";
    
    [JsonPropertyName("requiredFlags")]
    public List<string> RequiredFlags { get; set; } = new();
    
    [JsonPropertyName("requiredItems")]
    public List<string> RequiredItems { get; set; } = new();
    
    [JsonPropertyName("requiredLocation")]
    public string RequiredLocation { get; set; } = "";
    
    [JsonPropertyName("completionFlag")]
    public string CompletionFlag { get; set; } = "";
}

public class AchievementFile
{
    public List<Achievement> Achievements { get; set; } = new();
}

public class Achievement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("trigger")]
    public Trigger? Trigger { get; set; }
}

public class Trigger
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
    
    [JsonPropertyName("puzzleId")]
    public string? PuzzleId { get; set; }
}

public class ValidationIssue
{
    public string Severity { get; set; } = ""; // Error, Warning, Info
    public string Category { get; set; } = "";
    public string Message { get; set; } = "";
    public string? File { get; set; }
    public string? Location { get; set; }
}
