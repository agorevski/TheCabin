namespace TheCabin.Maui.Models;

public class NarrativeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public NarrativeType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public Color TextColor { get; set; } = Colors.White;
    public bool IsImportant { get; set; }
}

public enum NarrativeType
{
    Description,
    PlayerCommand,
    SystemMessage,
    Success,
    Failure,
    Discovery
}
