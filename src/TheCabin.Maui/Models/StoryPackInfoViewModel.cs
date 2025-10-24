using TheCabin.Core.Models;
using TheCabin.Maui.Extensions;

namespace TheCabin.Maui.Models;

/// <summary>
/// View model wrapper for StoryPackInfo to provide UI-specific properties
/// </summary>
public class StoryPackInfoViewModel
{
    private readonly StoryPackInfo _info;
    
    public StoryPackInfoViewModel(StoryPackInfo info)
    {
        _info = info;
    }
    
    // Core properties
    public string Id => _info.Id;
    public string Theme => _info.Theme;
    public string Description => _info.Description;
    public Difficulty Difficulty => _info.Difficulty;
    public int EstimatedPlayTime => _info.EstimatedPlayTime;
    public List<string> Tags => _info.Tags;
    public string? CoverImage => _info.CoverImage;
    
    // UI-specific properties
    public string DifficultyColor => _info.GetDifficultyColor();
    public string DifficultyText => _info.Difficulty.ToString();
    public string PlayTimeDisplay => _info.GetPlayTimeDisplay();
    public string TagsDisplay => _info.GetTagsDisplay();
    public string Icon => _info.GetIcon();
    
    // Get the underlying StoryPackInfo
    public StoryPackInfo ToStoryPackInfo() => _info;
}
