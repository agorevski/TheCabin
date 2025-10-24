using TheCabin.Core.Models;

namespace TheCabin.Core.Interfaces;

/// <summary>
/// Service for converting speech to text
/// </summary>
public interface IVoiceRecognitionService
{
    /// <summary>
    /// Starts listening and recognizes speech from the microphone
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Result containing transcribed text and metadata</returns>
    Task<VoiceRecognitionResult> RecognizeSpeechAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the current listening session
    /// </summary>
    void StopListening();
}

/// <summary>
/// Result from voice recognition
/// </summary>
public class VoiceRecognitionResult
{
    /// <summary>
    /// Whether recognition was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The transcribed text
    /// </summary>
    public string TranscribedText { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Alternative interpretations
    /// </summary>
    public List<string> Alternatives { get; set; } = new();
    
    /// <summary>
    /// Duration of the recording
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Error message if recognition failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
