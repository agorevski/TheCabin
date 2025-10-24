# 05 - Voice Pipeline

## Overview

The voice pipeline is the core feature of The Cabin, enabling hands-free gameplay through speech recognition and natural language processing. This document details the complete voice-to-command workflow.

## Voice Pipeline Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                     VOICE PIPELINE                          │
└─────────────────────────────────────────────────────────────┘

1. CAPTURE                    5. VALIDATION
   ┌──────────┐                 ┌──────────┐
   │Microphone│                 │Validator │
   │  Input   │                 │  Engine  │
   └────┬─────┘                 └────┬─────┘
        │                             │
2. TRANSCRIBE              6. EXECUTION
   ┌────▼─────┐                 ┌────▼─────┐
   │   STT    │                 │  Game    │
   │  Engine  │                 │  Engine  │
   └────┬─────┘                 └────┬─────┘
        │                             │
3. PARSE                   7. RESPONSE
   ┌────▼─────┐                 ┌────▼─────┐
   │   LLM    │                 │Narrative │
   │  Parser  │                 │Generator │
   └────┬─────┘                 └────┬─────┘
        │                             │
4. CACHE CHECK             8. NARRATION
   ┌────▼─────┐                 ┌────▼─────┐
   │  Cache   │                 │   TTS    │
   │  Layer   │                 │  Engine  │
   └──────────┘                 └──────────┘
```

## Component Details

### 1. Voice Capture

#### Android Speech Recognizer Implementation

```csharp
namespace TheCabin.Platforms.Android.Services
{
    public class AndroidVoiceRecognitionService : IVoiceRecognitionService
    {
        private readonly SpeechRecognizer _recognizer;
        private readonly TaskCompletionSource<VoiceRecognitionResult> _tcs;
        private readonly ILogger<AndroidVoiceRecognitionService> _logger;
        
        public AndroidVoiceRecognitionService(ILogger<AndroidVoiceRecognitionService> logger)
        {
            _logger = logger;
            _recognizer = SpeechRecognizer.CreateSpeechRecognizer(
                Platform.AppContext);
            _recognizer.SetRecognitionListener(new RecognitionListener(this));
        }
        
        public async Task<VoiceRecognitionResult> RecognizeSpeechAsync(
            CancellationToken cancellationToken = default)
        {
            _tcs = new TaskCompletionSource<VoiceRecognitionResult>();
            
            // Check permissions
            var permissionStatus = await Permissions
                .CheckStatusAsync<Permissions.Microphone>();
                
            if (permissionStatus != PermissionStatus.Granted)
            {
                permissionStatus = await Permissions
                    .RequestAsync<Permissions.Microphone>();
                    
                if (permissionStatus != PermissionStatus.Granted)
                {
                    return new VoiceRecognitionResult
                    {
                        Success = false,
                        ErrorMessage = "Microphone permission denied"
                    };
                }
            }
            
            // Configure intent
            var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel,
                RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraLanguage, "en-US");
            intent.PutExtra(RecognizerIntent.ExtraMaxResults, 5);
            intent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
            
            // Start listening
            _recognizer.StartListening(intent);
            
            // Wait for result or cancellation
            using (cancellationToken.Register(() => 
            {
                _recognizer.StopListening();
                _tcs?.TrySetCanceled();
            }))
            {
                return await _tcs.Task;
            }
        }
        
        public void StopListening()
        {
            _recognizer.StopListening();
        }
        
        private class RecognitionListener : Java.Lang.Object, 
            IRecognitionListener
        {
            private readonly AndroidVoiceRecognitionService _service;
            private readonly Stopwatch _stopwatch;
            
            public RecognitionListener(AndroidVoiceRecognitionService service)
            {
                _service = service;
                _stopwatch = new Stopwatch();
            }
            
            public void OnBeginningOfSpeech()
            {
                _stopwatch.Start();
            }
            
            public void OnEndOfSpeech()
            {
                _stopwatch.Stop();
            }
            
            public void OnResults(Bundle results)
            {
                var matches = results
                    .GetStringArrayList(SpeechRecognizer.ResultsRecognition);
                var confidences = results
                    .GetFloatArray(SpeechRecognizer.ConfidenceScores);
                
                if (matches?.Count > 0)
                {
                    var result = new VoiceRecognitionResult
                    {
                        Success = true,
                        TranscribedText = matches[0],
                        Confidence = confidences?[0] ?? 0.0,
                        Alternatives = matches.Skip(1).ToList(),
                        Duration = _stopwatch.Elapsed
                    };
                    
                    _service._tcs.TrySetResult(result);
                }
            }
            
            public void OnError(SpeechRecognizerError error)
            {
                var result = new VoiceRecognitionResult
                {
                    Success = false,
                    ErrorMessage = GetErrorMessage(error)
                };
                
                _service._tcs.TrySetResult(result);
            }
            
            private string GetErrorMessage(SpeechRecognizerError error)
            {
                return error switch
                {
                    SpeechRecognizerError.Network => 
                        "Network error. Check your connection.",
                    SpeechRecognizerError.NoMatch => 
                        "Didn't catch that. Please try again.",
                    SpeechRecognizerError.Audio => 
                        "Audio error. Check microphone.",
                    SpeechRecognizerError.InsufficientPermissions => 
                        "Microphone permission required.",
                    _ => "Recognition failed. Please try again."
                };
            }
            
            // Other interface methods (OnReadyForSpeech, OnRmsChanged, etc.)
            public void OnReadyForSpeech(Bundle @params) { }
            public void OnRmsChanged(float rmsdB) { }
            public void OnBufferReceived(byte[] buffer) { }
            public void OnPartialResults(Bundle partialResults) { }
            public void OnEvent(int eventType, Bundle @params) { }
        }
    }
}
```

#### Audio Quality Settings

```csharp
public class AudioSettings
{
    // Microphone sensitivity (0.0 - 1.0)
    public float Sensitivity { get; set; } = 0.7f;
    
    // Noise cancellation
    public bool NoiseReduction { get; set; } = true;
    
    // Automatic gain control
    public bool AutoGainControl { get; set; } = true;
    
    // Sample rate (Hz)
    public int SampleRate { get; set; } = 16000;
    
    // Timeout for silence (ms)
    public int SilenceTimeout { get; set; } = 2000;
    
    // Maximum recording duration (ms)
    public int MaxDuration { get; set; } = 10000;
}
```

### 2. Speech-to-Text (STT)

#### Confidence Threshold Handler

```csharp
public class SttConfidenceHandler
{
    private readonly float _minConfidence;
    private readonly ILogger _logger;
    
    public SttConfidenceHandler(
        IOptions<VoiceSettings> settings,
        ILogger<SttConfidenceHandler> logger)
    {
        _minConfidence = settings.Value.ConfidenceThreshold;
        _logger = logger;
    }
    
    public async Task<VoiceRecognitionResult> HandleLowConfidence(
        VoiceRecognitionResult result)
    {
        if (result.Confidence < _minConfidence)
        {
            _logger.LogWarning(
                "Low confidence: {Confidence:P} for '{Text}'",
                result.Confidence, result.TranscribedText);
            
            // Try alternatives if available
            if (result.Alternatives?.Count > 0)
            {
                foreach (var alt in result.Alternatives)
                {
                    // Could implement fuzzy matching here
                    // For now, just log alternatives
                    _logger.LogInformation(
                        "Alternative: {Alternative}", alt);
                }
            }
            
            // Request user confirmation
            result.ErrorMessage = 
                $"Did you say '{result.TranscribedText}'? " +
                $"Please confirm or try again.";
        }
        
        return result;
    }
}
```

### 3. Command Parsing

#### LLM-Based Parser

```csharp
namespace TheCabin.Services
{
    public class LlmCommandParserService : ICommandParserService
    {
        private readonly OpenAIClient _client;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;
        
        private const string SystemPrompt = @"
You are a command parser for a text adventure game. 
Parse the player's input into a structured JSON command.

Available verbs: go, take, drop, use, open, close, examine, look, 
read, push, pull, inventory, help, save, load

Output format:
{
  ""verb"": ""action verb"",
  ""object"": ""target object or null"",
  ""target"": ""secondary object or null"",
  ""context"": ""additional context"",
  ""confidence"": 0.0-1.0
}

Examples:
Input: 'go north' → {""verb"":""go"",""object"":""north"",""confidence"":0.95}
Input: 'pick up the lantern' → {""verb"":""take"",""object"":""lantern"",""confidence"":0.9}
Input: 'use key on door' → {""verb"":""use"",""object"":""key"",""target"":""door"",""confidence"":0.85}
";
        
        public async Task<ParsedCommand> ParseAsync(
            string input,
            GameContext context,
            CancellationToken cancellationToken = default)
        {
            // Build cache key
            var cacheKey = GenerateCacheKey(input, context);
            
            // Check cache
            if (_cache.TryGetValue<ParsedCommand>(cacheKey, out var cached))
            {
                _logger.LogInformation(
                    "Cache hit for command: {Input}", input);
                return cached;
            }
            
            // Prepare context information
            var contextInfo = BuildContextInfo(context);
            
            // Build user prompt
            var userPrompt = $@"
Context:
- Location: {context.CurrentLocation}
- Visible objects: {string.Join(", ", context.VisibleObjects)}
- Inventory: {string.Join(", ", context.InventoryItems)}
- Recent actions: {string.Join(", ", context.RecentCommands.TakeLast(3))}

Player input: ""{input}""

Parse this command into JSON.";
            
            try
            {
                var chatCompletion = await _client.GetChatCompletionsAsync(
                    new ChatCompletionsOptions
                    {
                        DeploymentName = "gpt-4o-mini",
                        Messages =
                        {
                            new ChatRequestSystemMessage(SystemPrompt),
                            new ChatRequestUserMessage(userPrompt)
                        },
                        Temperature = 0.3f,
                        MaxTokens = 150,
                        ResponseFormat = ChatCompletionsResponseFormat.JsonObject
                    },
                    cancellationToken);
                
                var response = chatCompletion.Value.Choices[0].Message.Content;
                var parsed = JsonSerializer.Deserialize<ParsedCommand>(response);
                
                if (parsed != null)
                {
                    parsed.RawInput = input;
                    
                    // Cache successful parse
                    _cache.Set(cacheKey, parsed, TimeSpan.FromHours(24));
                    
                    _logger.LogInformation(
                        "Parsed '{Input}' → Verb: {Verb}, Object: {Object}, Confidence: {Confidence:P}",
                        input, parsed.Verb, parsed.Object, parsed.Confidence);
                    
                    return parsed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "LLM parsing failed for input: {Input}", input);
            }
            
            // Fallback to local parser
            return await FallbackParse(input);
        }
        
        private string GenerateCacheKey(string input, GameContext context)
        {
            var contextHash = HashCode.Combine(
                context.CurrentLocation,
                string.Join(",", context.VisibleObjects.OrderBy(x => x)),
                string.Join(",", context.InventoryItems.OrderBy(x => x))
            );
            
            return $"cmd:{input.ToLowerInvariant()}:ctx:{contextHash}";
        }
        
        private string BuildContextInfo(GameContext context)
        {
            return JsonSerializer.Serialize(new
            {
                location = context.CurrentLocation,
                visible = context.VisibleObjects,
                inventory = context.InventoryItems,
                flags = context.GameFlags
            });
        }
        
        private async Task<ParsedCommand> FallbackParse(string input)
        {
            // Simple rule-based parser as fallback
            var words = input.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
            {
                return new ParsedCommand
                {
                    Verb = "help",
                    Confidence = 0.5,
                    RawInput = input
                };
            }
            
            var verb = words[0];
            var obj = words.Length > 1 ? words[1] : null;
            
            // Map common variations
            verb = verb switch
            {
                "grab" or "get" or "pickup" => "take",
                "leave" or "discard" => "drop",
                "inspect" or "check" => "examine",
                "see" or "view" => "look",
                _ => verb
            };
            
            return new ParsedCommand
            {
                Verb = verb,
                Object = obj,
                Confidence = 0.6,
                RawInput = input
            };
        }
    }
}
```

#### Local Rule-Based Parser (Fallback)

```csharp
public class LocalCommandParser : ILocalCommandParser
{
    private static readonly Dictionary<string, List<string>> VerbSynonyms = new()
    {
        ["take"] = new() { "get", "grab", "pickup", "pick", "collect" },
        ["drop"] = new() { "leave", "discard", "put", "place" },
        ["use"] = new() { "activate", "employ", "utilize" },
        ["open"] = new() { "unlock", "unseals" },
        ["close"] = new() { "shut", "lock", "seal" },
        ["examine"] = new() { "inspect", "check", "study", "investigate" },
        ["look"] = new() { "see", "view", "observe", "peek" },
        ["go"] = new() { "move", "walk", "run", "travel", "head" }
    };
    
    public bool CanHandle(string input)
    {
        var words = input.ToLower().Split(' ');
        var firstWord = words.FirstOrDefault();
        
        // Check if first word matches any known verb or synonym
        return CommandVerbs.AllVerbs.Contains(firstWord) ||
               VerbSynonyms.Values.Any(synonyms => 
                   synonyms.Contains(firstWord));
    }
    
    public Task<ParsedCommand> ParseAsync(
        string input, 
        GameContext context)
    {
        var words = input.ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        
        if (words.Count == 0)
            return Task.FromResult(CreateHelpCommand(input));
        
        // Normalize verb
        var verb = NormalizeVerb(words[0]);
        
        // Extract object and target
        string obj = null;
        string target = null;
        
        if (words.Count > 1)
        {
            // Remove common filler words
            var filtered = words.Skip(1)
                .Where(w => !IsFillerWord(w))
                .ToList();
            
            if (filtered.Count > 0)
            {
                obj = filtered[0];
                
                // Check for "use X on Y" pattern
                if (filtered.Count > 2 && filtered[1] == "on")
                {
                    target = filtered[2];
                }
            }
        }
        
        return Task.FromResult(new ParsedCommand
        {
            Verb = verb,
            Object = obj,
            Target = target,
            Confidence = 0.7,
            RawInput = input
        });
    }
    
    private string NormalizeVerb(string word)
    {
        // Direct match
        if (CommandVerbs.AllVerbs.Contains(word))
            return word;
        
        // Find in synonyms
        foreach (var kvp in VerbSynonyms)
        {
            if (kvp.Value.Contains(word))
                return kvp.Key;
        }
        
        return word;
    }
    
    private bool IsFillerWord(string word)
    {
        return new[] { "the", "a", "an", "to", "at", "in", "on" }
            .Contains(word);
    }
    
    private ParsedCommand CreateHelpCommand(string input)
    {
        return new ParsedCommand
        {
            Verb = "help",
            Confidence = 0.5,
            RawInput = input
        };
    }
}
```

### 4. Command Caching

```csharp
public class CommandCacheService
{
    private readonly ICommandCacheRepository _repository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger _logger;
    
    public async Task<ParsedCommand> GetCachedAsync(
        string commandHash,
        string contextHash)
    {
        // Check memory cache first
        var key = $"{commandHash}:{contextHash}";
        if (_memoryCache.TryGetValue<ParsedCommand>(key, out var cached))
        {
            return cached;
        }
        
        // Check database cache
        var entity = await _repository.GetAsync(commandHash, contextHash);
        if (entity != null && !IsExpired(entity))
        {
            var parsed = JsonSerializer
                .Deserialize<ParsedCommand>(entity.ParsedJson);
            
            // Update memory cache
            _memoryCache.Set(key, parsed, TimeSpan.FromMinutes(30));
            
            // Update hit count
            await _repository.IncrementHitCountAsync(commandHash);
            
            return parsed;
        }
        
        return null;
    }
    
    public async Task CacheAsync(
        string commandHash,
        string contextHash,
        ParsedCommand parsed)
    {
        var entity = new CommandCacheEntity
        {
            CommandHash = commandHash,
            ContextHash = contextHash,
            ParsedJson = JsonSerializer.Serialize(parsed),
            Confidence = (float)parsed.Confidence,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ExpiryTimestamp = DateTimeOffset.UtcNow
                .AddDays(30).ToUnixTimeSeconds(),
            HitCount = 0
        };
        
        await _repository.UpsertAsync(entity);
        
        // Also cache in memory
        var key = $"{commandHash}:{contextHash}";
        _memoryCache.Set(key, parsed, TimeSpan.FromMinutes(30));
    }
    
    private bool IsExpired(CommandCacheEntity entity)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return entity.ExpiryTimestamp < now;
    }
    
    public async Task CleanupExpiredAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _repository.DeleteExpiredAsync(now);
    }
}
```

### 5. Error Handling & Retry Logic

```csharp
public class VoiceCommandOrchestrator
{
    private readonly IVoiceRecognitionService _sttService;
    private readonly ICommandParserService _parserService;
    private readonly ITextToSpeechService _ttsService;
    private readonly ILogger _logger;
    
    public async Task<ParsedCommand> ProcessVoiceCommandAsync(
        GameContext context,
        int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Step 1: Voice recognition
                var sttResult = await _sttService.RecognizeSpeechAsync();
                
                if (!sttResult.Success)
                {
                    if (attempt < maxRetries)
                    {
                        await _ttsService.SpeakAsync(
                            sttResult.ErrorMessage ?? 
                            "I didn't catch that. Please try again.");
                        continue;
                    }
                    throw new VoiceRecognitionException(sttResult.ErrorMessage);
                }
                
                // Step 2: Check confidence
                if (sttResult.Confidence < 0.7)
                {
                    // Ask for confirmation
                    var confirmed = await ConfirmTranscription(
                        sttResult.TranscribedText);
                    if (!confirmed && attempt < maxRetries)
                        continue;
                }
                
                // Step 3: Parse command
                var parsed = await _parserService.ParseAsync(
                    sttResult.TranscribedText,
                    context);
                
                // Step 4: Validate parse confidence
                if (parsed.Confidence < 0.5)
                {
                    if (attempt < maxRetries)
                    {
                        await _ttsService.SpeakAsync(
                            "I'm not sure I understood that. " +
                            "Could you rephrase?");
                        continue;
                    }
                }
                
                return parsed;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "Attempt {Attempt} failed, retrying...", attempt);
                
                await _ttsService.SpeakAsync(
                    "Sorry, something went wrong. Let's try again.");
            }
        }
        
        throw new MaxRetriesExceededException(
            "Failed to process voice command after maximum retries");
    }
    
    private async Task<bool> ConfirmTranscription(string text)
    {
        await _ttsService.SpeakAsync($"Did you say '{text}'?");
        
        var response = await _sttService.RecognizeSpeechAsync();
        if (!response.Success)
            return false;
        
        var confirmation = response.TranscribedText.ToLower();
        return confirmation.Contains("yes") || 
               confirmation.Contains("yeah") || 
               confirmation.Contains("correct");
    }
}
```

## Performance Optimization

### Preloading and Warmup

```csharp
public class VoicePipelineWarmup
{
    public async Task WarmupAsync()
    {
        // Initialize speech recognizer
        await InitializeSpeechRecognizer();
        
        // Preload TTS voices
        await PreloadTtsVoices();
        
        // Test LLM connection
        await TestLlmConnection();
        
        // Load command cache
        await LoadCommandCache();
    }
}
```

### Batch Processing

```csharp
// Process multiple cached commands at startup
public async Task PreprocessCommonCommands()
{
    var commonCommands = new[]
    {
        "look", "look around", "inventory", 
        "go north", "go south", "go east", "go west",
        "take all", "help"
    };
    
    foreach (var cmd in commonCommands)
    {
        await _parserService.ParseAsync(cmd, GetDefaultContext());
    }
}
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 03-technical-stack.md, 06-game-engine.md
