using TheCabin.Core.Interfaces;

namespace TheCabin.Maui.Services;

/// <summary>
/// Fallback speech recognizer for platforms without a native implementation (e.g. Windows).
/// Lets MainViewModel resolve from DI; recognition always reports failure.
/// </summary>
public class UnsupportedVoiceRecognitionService : IVoiceRecognitionService
{
    public Task<VoiceRecognitionResult> RecognizeSpeechAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new VoiceRecognitionResult
        {
            Success = false,
            ErrorMessage = "Voice recognition is not available on this platform."
        });
    }

    public void StopListening()
    {
    }
}
