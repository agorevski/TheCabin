using TheCabin.Core.Models;

namespace TheCabin.Maui.Extensions;

public static class StoryPackInfoExtensions
{
    public static string GetDifficultyColor(this StoryPackInfo info)
    {
        return info.Difficulty switch
        {
            Difficulty.Easy => "#7ED321",
            Difficulty.Medium => "#F5A623",
            Difficulty.Hard => "#D0021B",
            Difficulty.Expert => "#BD10E0",
            _ => "#4A90E2"
        };
    }
    
    public static string GetPlayTimeDisplay(this StoryPackInfo info)
    {
        return $"~{info.EstimatedPlayTime} min";
    }
    
    public static string GetTagsDisplay(this StoryPackInfo info)
    {
        return info.Tags.Count > 0 ? string.Join(" • ", info.Tags) : "";
    }
    
    public static string GetIcon(this StoryPackInfo info)
    {
        var theme = info.Theme?.ToLower() ?? "";
        
        if (theme.Contains("horror"))
            return "🏚️";
        if (theme.Contains("arctic") || theme.Contains("survival"))
            return "🧊";
        if (theme.Contains("fantasy") || theme.Contains("magic"))
            return "🔮";
        if (theme.Contains("sci-fi") || theme.Contains("space"))
            return "🚀";
        if (theme.Contains("cozy") || theme.Contains("mystery"))
            return "🕵️";
        
        return "📖";
    }
}
