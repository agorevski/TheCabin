namespace TheCabin.Core.Engine;

/// <summary>
/// Helper class for formatting room descriptions with separate display and TTS text
/// </summary>
public static class RoomDescriptionFormatter
{
    /// <summary>
    /// Formats room description with items and exits
    /// </summary>
    /// <param name="description">The room description text</param>
    /// <param name="visibleObjects">List of visible object names</param>
    /// <param name="exits">List of exit direction names</param>
    /// <returns>Tuple with display message (with emojis) and TTS message (description only)</returns>
    public static (string DisplayMessage, string TtsMessage) FormatRoomDescription(
        string description,
        IEnumerable<string>? visibleObjects = null,
        IEnumerable<string>? exits = null)
    {
        var displayMessage = description;
        var ttsMessage = description;

        // Add visible objects with emoji to display message
        if (visibleObjects != null && visibleObjects.Any())
        {
            var objectNames = string.Join(", ", visibleObjects);
            displayMessage += $"\n\n👀 {objectNames}";
        }

        // Add available exits with emoji to display message
        if (exits != null && exits.Any())
        {
            var exitList = string.Join(", ", exits);
            displayMessage += $"\n\n🚪 {exitList}";
        }

        return (displayMessage, ttsMessage);
    }

    /// <summary>
    /// Formats room description with movement prefix
    /// </summary>
    /// <param name="direction">Direction of movement</param>
    /// <param name="description">The room description text</param>
    /// <param name="visibleObjects">List of visible object names</param>
    /// <param name="exits">List of exit direction names</param>
    /// <returns>Tuple with display message (with emojis) and TTS message (description only)</returns>
    public static (string DisplayMessage, string TtsMessage) FormatMovementDescription(
        string direction,
        string description,
        IEnumerable<string>? visibleObjects = null,
        IEnumerable<string>? exits = null)
    {
        var movementPrefix = $"You move {direction}.\n\n";
        var fullDescription = movementPrefix + description;

        var displayMessage = fullDescription;
        var ttsMessage = fullDescription;

        // Add visible objects with emoji to display message
        if (visibleObjects != null && visibleObjects.Any())
        {
            var objectNames = string.Join(", ", visibleObjects);
            displayMessage += $"\n\n👀 {objectNames}";
        }

        // Add available exits with emoji to display message
        if (exits != null && exits.Any())
        {
            var exitList = string.Join(", ", exits);
            displayMessage += $"\n\n🚪 {exitList}";
        }

        return (displayMessage, ttsMessage);
    }
}
