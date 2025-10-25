using Microsoft.Extensions.Logging;
using TheCabin.Core.Interfaces;

namespace TheCabin.Maui.Services;

public class MauiTextToSpeechService : ITextToSpeechService
{
    private readonly ILogger<MauiTextToSpeechService> _logger;
    private readonly ITextToSpeech _tts;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;

    public MauiTextToSpeechService(
        ILogger<MauiTextToSpeechService> logger,
        ITextToSpeech tts)
    {
        _logger = logger;
        _tts = tts;
    }

    public async Task SpeakAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Cancel any ongoing speech
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _isSpeaking = true;

            var locales = await _tts.GetLocalesAsync();
            var locale = locales?.FirstOrDefault(l => l.Language.StartsWith("en"));

            var options = new SpeechOptions
            {
                Pitch = 1.0f,
                Volume = 1.0f,
                Locale = locale
            };

            await _tts.SpeakAsync(text, options, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Speech cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text-to-speech failed");
        }
        finally
        {
            _isSpeaking = false;
        }
    }

    public async Task StopAsync()
    {
        _cancellationTokenSource?.Cancel();
        _isSpeaking = false;
        await Task.CompletedTask;
    }
}
