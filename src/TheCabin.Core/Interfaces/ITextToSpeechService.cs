namespace TheCabin.Core.Interfaces;

/// <summary>
/// Service for converting text to speech
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// Speaks the provided text using text-to-speech
    /// </summary>
    /// <param name="text">Text to speak</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops any currently playing speech
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets whether TTS is currently speaking
    /// </summary>
    bool IsSpeaking { get; }
}
