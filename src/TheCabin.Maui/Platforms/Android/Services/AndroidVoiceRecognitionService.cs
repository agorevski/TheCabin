using Android.Content;
using Android.Speech;
using Microsoft.Extensions.Logging;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Maui.Platforms.Android.Services;

public class AndroidVoiceRecognitionService : IVoiceRecognitionService
{
    private readonly ILogger<AndroidVoiceRecognitionService> _logger;
    private SpeechRecognizer? _recognizer;
    private TaskCompletionSource<VoiceRecognitionResult>? _tcs;
    private System.Diagnostics.Stopwatch? _stopwatch;

    public AndroidVoiceRecognitionService(ILogger<AndroidVoiceRecognitionService> logger)
    {
        _logger = logger;
    }

    public async Task<VoiceRecognitionResult> RecognizeSpeechAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check permissions
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    return new VoiceRecognitionResult
                    {
                        Success = false,
                        ErrorMessage = "Microphone permission denied"
                    };
                }
            }

            _tcs = new TaskCompletionSource<VoiceRecognitionResult>();
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Initialize recognizer on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var context = global::Android.App.Application.Context;
                _recognizer = SpeechRecognizer.CreateSpeechRecognizer(context);
                _recognizer.SetRecognitionListener(new RecognitionListener(this));

                var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                intent.PutExtra(RecognizerIntent.ExtraLanguage, "en-US");
                intent.PutExtra(RecognizerIntent.ExtraMaxResults, 5);
                intent.PutExtra(RecognizerIntent.ExtraPartialResults, true);

                _recognizer.StartListening(intent);
            });

            // Wait for result or cancellation
            using (cancellationToken.Register(() =>
            {
                StopListening();
                _tcs?.TrySetCanceled();
            }))
            {
                return await _tcs.Task;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice recognition failed");
            return new VoiceRecognitionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public void StopListening()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _recognizer?.StopListening();
            _recognizer?.Destroy();
            _recognizer = null;
        });
    }

    private class RecognitionListener : Java.Lang.Object, IRecognitionListener
    {
        private readonly AndroidVoiceRecognitionService _service;

        public RecognitionListener(AndroidVoiceRecognitionService service)
        {
            _service = service;
        }

        public void OnBeginningOfSpeech()
        {
            _service._stopwatch?.Start();
        }

        public void OnEndOfSpeech()
        {
            _service._stopwatch?.Stop();
        }

        public void OnResults(global::Android.OS.Bundle? results)
        {
            if (results == null)
            {
                _service._tcs?.TrySetResult(new VoiceRecognitionResult
                {
                    Success = false,
                    ErrorMessage = "No results"
                });
                return;
            }

            var matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            var confidences = results.GetFloatArray(SpeechRecognizer.ConfidenceScores);

            if (matches?.Count > 0)
            {
                var result = new VoiceRecognitionResult
                {
                    Success = true,
                    TranscribedText = matches[0] ?? "",
                    Confidence = confidences?[0] ?? 0.0,
                    Alternatives = matches.Skip(1).Where(m => m != null).Cast<string>().ToList(),
                    Duration = _service._stopwatch?.Elapsed ?? TimeSpan.Zero
                };

                _service._tcs?.TrySetResult(result);
            }
            else
            {
                _service._tcs?.TrySetResult(new VoiceRecognitionResult
                {
                    Success = false,
                    ErrorMessage = "No speech detected"
                });
            }
        }

        public void OnError(SpeechRecognizerError error)
        {
            var result = new VoiceRecognitionResult
            {
                Success = false,
                ErrorMessage = GetErrorMessage(error)
            };

            _service._tcs?.TrySetResult(result);
        }

        private string GetErrorMessage(SpeechRecognizerError error)
        {
            return error switch
            {
                SpeechRecognizerError.Network => "Network error. Check your connection.",
                SpeechRecognizerError.NoMatch => "Didn't catch that. Please try again.",
                SpeechRecognizerError.Audio => "Audio error. Check microphone.",
                SpeechRecognizerError.InsufficientPermissions => "Microphone permission required.",
                SpeechRecognizerError.Client => "Recognition cancelled.",
                SpeechRecognizerError.Server => "Server error. Please try again.",
                SpeechRecognizerError.SpeechTimeout => "No speech detected. Please try again.",
                SpeechRecognizerError.RecognizerBusy => "Recognizer is busy. Please wait.",
                _ => "Recognition failed. Please try again."
            };
        }

        // Other interface methods
        public void OnReadyForSpeech(global::Android.OS.Bundle? @params) { }
        public void OnRmsChanged(float rmsdB) { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnPartialResults(global::Android.OS.Bundle? partialResults) { }
        public void OnEvent(int eventType, global::Android.OS.Bundle? @params) { }
    }
}
