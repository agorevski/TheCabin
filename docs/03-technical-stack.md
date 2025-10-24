# 03 - Technical Stack

## Overview

This document details the complete technology stack for The Cabin, including frameworks, libraries, tools, and platform-specific components required for Android development using .NET MAUI.

## Core Framework

### .NET MAUI (Multi-platform App UI)

**Version**: .NET 8.0 or higher  
**Target Frameworks**: `net8.0-android`

**Key Features Utilized**:

- Single project structure
- XAML-based UI with C# code-behind
- Platform-specific code via conditional compilation
- Hot reload for rapid development
- Native Android API access via bindings

**MAUI Advantages**:

- Unified codebase with platform customization
- Native performance
- Direct Android API access
- Extensive community and Microsoft support
- Familiar C# development experience

## Development Environment

### Required Tools

| Tool | Version | Purpose |
|------|---------|---------|
| Visual Studio 2022 | 17.8+ | Primary IDE |
| .NET SDK | 8.0+ | Runtime and libraries |
| Android SDK | API 23-34 | Android development |
| Android Emulator | Latest | Testing |
| Git | 2.40+ | Version control |

### Optional Tools

| Tool | Purpose |
|------|---------|
| Visual Studio Code | Alternative editor |
| Android Studio | Advanced Android debugging |
| Postman | API testing |
| DB Browser for SQLite | Database inspection |

### Build Configuration

```xml
<!-- .csproj settings -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0-android</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>TheCabin</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    
    <!-- Android specific -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">23.0</SupportedOSPlatformVersion>
    <AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>
    <AndroidPackageFormat>apk</AndroidPackageFormat>
    
    <!-- App info -->
    <ApplicationTitle>The Cabin</ApplicationTitle>
    <ApplicationId>com.thecabin.voiceadventure</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
  </PropertyGroup>
</Project>
```

## NuGet Packages

### Core MAUI Packages

```xml
<!-- Included by default with MAUI -->
<PackageReference Include="Microsoft.Maui.Controls" Version="8.0.0" />
<PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
```

### Speech Recognition

#### Option 1: Android SpeechRecognizer (Built-in)

```csharp
// Platform API - No package required
// Use: Android.Speech.SpeechRecognizer
```

#### Option 2: Whisper.NET (Offline Alternative)

```xml
<PackageReference Include="Whisper.net" Version="1.5.0" />
<PackageReference Include="Whisper.net.Runtime" Version="1.5.0" />
```

**Whisper Model Options**:

- `tiny`: 75 MB, fastest, lower accuracy
- `base`: 142 MB, good balance
- `small`: 466 MB, better accuracy
- Recommended: **base** model for mobile

### Text-to-Speech

#### MAUI Built-in TTS

```csharp
// Platform API included with MAUI
// Use: Microsoft.Maui.Media.TextToSpeech
```

#### Alternative: Azure Cognitive Services TTS

```xml
<PackageReference Include="Microsoft.CognitiveServices.Speech" Version="1.34.0" />
```

### LLM Integration

#### OpenAI API Client

```xml
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />
```

**Alternative**:

```xml
<PackageReference Include="OpenAI" Version="1.10.0" />
```

#### HTTP Client Extensions

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

### Data Storage

#### SQLite (Recommended)

```xml
<PackageReference Include="sqlite-net-pcl" Version="1.8.116" />
<PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.7" />
```

#### JSON Serialization

```xml
<PackageReference Include="System.Text.Json" Version="8.0.0" />
<!-- or -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### MVVM Framework

#### CommunityToolkit.Mvvm (Recommended)

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
```

**Features**:

- Source generators for MVVM boilerplate
- `ObservableProperty` attribute
- `RelayCommand` attribute
- Messenger for loosely coupled communication

#### Alternative: ReactiveUI

```xml
<PackageReference Include="ReactiveUI.Maui" Version="19.5.31" />
```

### UI Enhancements

```xml
<!-- Additional UI controls -->
<PackageReference Include="CommunityToolkit.Maui" Version="7.0.0" />

<!-- Animations -->
<PackageReference Include="Microsoft.Maui.Graphics" Version="8.0.0" />

<!-- Icons -->
<PackageReference Include="Plugin.Maui.Audio" Version="2.0.0" />
```

### Utilities

```xml
<!-- Dependency Injection -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />

<!-- Configuration -->
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />

<!-- Logging -->
<PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />

<!-- Caching -->
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />

<!-- Polly (Resilience) -->
<PackageReference Include="Polly" Version="8.2.0" />
```

## Speech-to-Text Options

### Option 1: Android SpeechRecognizer (Default)

**Pros**:

- Built into Android platform
- No additional dependencies
- Low memory footprint
- Regular updates from Google
- Supports multiple languages

**Cons**:

- Requires internet connection for best accuracy
- Limited customization
- Privacy concerns (data sent to Google)

**Implementation**:

```csharp
using Android.Speech;
using Android.Content;

public class AndroidSpeechRecognizer : IVoiceRecognitionService
{
    private SpeechRecognizer _recognizer;
    private Intent _recognizerIntent;
    
    public async Task<string> RecognizeSpeechAsync()
    {
        _recognizerIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
        _recognizerIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, 
            RecognizerIntent.LanguageModelFreeForm);
        _recognizerIntent.PutExtra(RecognizerIntent.ExtraCallingPackage, 
            Package);
        _recognizerIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
        
        _recognizer.StartListening(_recognizerIntent);
        // Return recognized text
    }
}
```

### Option 2: Whisper.NET (Offline)

**Pros**:

- Fully offline functionality
- High accuracy
- Privacy-focused (on-device)
- Multiple model sizes
- Open source

**Cons**:

- Large model files (75-466 MB)
- Higher CPU/battery usage
- Slower than cloud solutions
- Requires model download/bundling

**Implementation**:

```csharp
using Whisper.net;

public class WhisperRecognitionService : IVoiceRecognitionService
{
    private WhisperFactory _factory;
    private WhisperProcessor _processor;
    
    public async Task InitializeAsync()
    {
        var modelPath = Path.Combine(FileSystem.AppDataDirectory, 
            "ggml-base.bin");
        _factory = WhisperFactory.FromPath(modelPath);
        _processor = _factory.CreateBuilder().Build();
    }
    
    public async Task<string> RecognizeSpeechAsync(string audioFile)
    {
        await foreach (var result in _processor.ProcessAsync(audioFile))
        {
            return result.Text;
        }
    }
}
```

### Option 3: Azure Cognitive Services

**Pros**:

- Excellent accuracy
- Real-time streaming
- Custom models available
- Extensive language support

**Cons**:

- Requires internet
- Usage costs
- Additional SDK size
- Azure account needed

**Cost Estimate**: ~$1 per hour of audio (Standard tier)

## LLM Integration Options

### Option 1: OpenAI GPT-4o-mini (Recommended)

**API Endpoint**: `https://api.openai.com/v1/chat/completions`

**Model**: `gpt-4o-mini`

**Pricing**:

- Input: $0.150 per 1M tokens
- Output: $0.600 per 1M tokens
- Est. cost per command: $0.0001-0.0003

**Estimated Monthly Cost** (1000 active users):

- Avg. 50 commands/user/month = 50,000 commands
- Cost: ~$5-15/month

**Implementation**:

```csharp
using Azure.AI.OpenAI;

public class OpenAICommandParser : ICommandParserService
{
    private readonly OpenAIClient _client;
    
    public async Task<ParsedCommand> ParseAsync(string input, string context)
    {
        var messages = new[]
        {
            new ChatRequestSystemMessage(
                "Parse player commands into JSON: {verb, object, context}"),
            new ChatRequestUserMessage($"Context: {context}\nCommand: {input}")
        };
        
        var response = await _client.GetChatCompletionsAsync(
            new ChatCompletionsOptions("gpt-4o-mini", messages)
            {
                Temperature = 0.3f,
                MaxTokens = 100
            });
        
        return JsonSerializer.Deserialize<ParsedCommand>(
            response.Value.Choices[0].Message.Content);
    }
}
```

### Option 2: Azure OpenAI Service

**Benefits**:

- Enterprise features
- Data residency control
- Private endpoint support
- SLA guarantees

**Pricing**: Similar to OpenAI, with added Azure fees

### Option 3: Local LLM (Phi-3, Llama)

**Options**:

- Phi-3 Mini (3.8B parameters, ~2GB)
- Llama 3.2 (3B parameters, ~2GB)

**Implementation** (using LLamaSharp):

```xml
<PackageReference Include="LLamaSharp" Version="0.10.0" />
<PackageReference Include="LLamaSharp.Backend.Cpu" Version="0.10.0" />
```

**Pros**:

- No API costs
- Full offline support
- Privacy

**Cons**:

- Large app size (2-4GB)
- High memory usage
- Slower inference
- Battery drain

### Option 4: Hybrid Approach (Recommended)

```csharp
public class HybridCommandParser : ICommandParserService
{
    private readonly ILocalCommandParser _localParser;
    private readonly ILlmApiClient _cloudParser;
    private readonly IConnectivity _connectivity;
    
    public async Task<ParsedCommand> ParseAsync(string input, string context)
    {
        // Try cached result first
        if (TryGetCached(input, out var cached))
            return cached;
        
        // Check for simple/common commands
        if (_localParser.CanHandle(input))
            return await _localParser.ParseAsync(input, context);
        
        // Use cloud LLM for complex commands
        if (_connectivity.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                return await _cloudParser.ParseAsync(input, context);
            }
            catch
            {
                // Fallback to local
                return await _localParser.ParseAsync(input, context);
            }
        }
        
        // Offline fallback
        return await _localParser.ParseAsync(input, context);
    }
}
```

## Text-to-Speech Options

### Option 1: MAUI TextToSpeech (Default)

**Implementation**:

```csharp
using Microsoft.Maui.Media;

public class MauiTtsService : ITextToSpeechService
{
    public async Task SpeakAsync(string text)
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var settings = new SpeechOptions
        {
            Pitch = 1.0f,
            Volume = 1.0f,
            Locale = locales.FirstOrDefault()
        };
        
        await TextToSpeech.Default.SpeakAsync(text, settings);
    }
}
```

**Pros**:

- Built-in, no extra packages
- Works offline
- Low battery usage

**Cons**:

- Robotic voice quality
- Limited voice options
- No emotion/expression

### Option 2: Azure Cognitive Services TTS

**Best Voices**: Neural TTS with SSML support

**Implementation**:

```csharp
using Microsoft.CognitiveServices.Speech;

public class AzureTtsService : ITextToSpeechService
{
    private SpeechSynthesizer _synthesizer;
    
    public async Task SpeakAsync(string text)
    {
        var ssml = $@"
            <speak version='1.0' xml:lang='en-US'>
                <voice name='en-US-JennyNeural'>
                    <prosody rate='0.9' pitch='0%'>
                        {text}
                    </prosody>
                </voice>
            </speak>";
        
        await _synthesizer.SpeakSsmlAsync(ssml);
    }
}
```

**Pricing**: $16 per 1M characters (Neural voices)

### Option 3: ElevenLabs (Premium)

**Pros**:

- Most realistic voices
- Emotion control
- Voice cloning

**Cons**:

- Expensive ($5-330/month)
- Requires internet
- Latency

## Database Options

### SQLite - Recommended

**Why SQLite**:

- Embedded database
- No server required
- Small footprint (~1MB)
- ACID compliant
- Wide platform support

**Schema Example**:

```sql
-- Game saves
CREATE TABLE GameSaves (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    ThemeId TEXT NOT NULL,
    GameStateJson TEXT NOT NULL,
    Timestamp INTEGER NOT NULL
);

-- Command cache
CREATE TABLE CommandCache (
    CommandHash TEXT PRIMARY KEY,
    ContextHash TEXT NOT NULL,
    ParsedJson TEXT NOT NULL,
    Confidence REAL,
    Timestamp INTEGER
);

-- Settings
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
```

### Alternative: LiteDB

```xml
<PackageReference Include="LiteDB" Version="5.0.17" />
```

**Pros**:

- NoSQL document database
- No SQL required
- Simple API
- Good for JSON storage

## Android Permissions

### Required Permissions

```xml
<!-- AndroidManifest.xml -->
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
<uses-permission android:name="android.permission.WAKE_LOCK" />

<!-- Optional -->
<uses-permission android:name="android.permission.VIBRATE" />
```

### Runtime Permission Handling

```csharp
public async Task<bool> RequestMicrophonePermissionAsync()
{
    var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
    
    if (status != PermissionStatus.Granted)
    {
        status = await Permissions.RequestAsync<Permissions.Microphone>();
    }
    
    return status == PermissionStatus.Granted;
}
```

## Build and Deployment

### Android Build Configurations

**Debug**:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <AndroidEnableProfiledAot>false</AndroidEnableProfiledAot>
    <AndroidLinkMode>None</AndroidLinkMode>
    <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

**Release**:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>
    <AndroidLinkMode>SdkOnly</AndroidLinkMode>
    <AndroidUseAapt2>true</AndroidUseAapt2>
    <AndroidCreatePackagePerAbi>false</AndroidCreatePackagePerAbi>
    <EnableLLVM>false</EnableLLVM>
</PropertyGroup>
```

### APK Size Optimization

1. **Enable ProGuard/R8 shrinking**
2. **Use APK splits for different architectures**
3. **Compress assets**
4. **Remove unused resources**
5. **Use vector drawables instead of PNGs**

**Target Size**: < 50 MB (without Whisper model)

## Development Workflow

### Recommended Git Workflow

```bash
main (production)
  ├─ develop (integration)
  │   ├─ feature/voice-pipeline
  │   ├─ feature/game-engine
  │   └─ feature/ui-components
  └─ hotfix/* (urgent fixes)
```

### CI/CD Considerations

- GitHub Actions or Azure DevOps
- Automated testing on every PR
- APK builds on merge to develop
- Release builds tagged in main

## Performance Profiling Tools

- Android Studio Profiler
- Visual Studio Performance Profiler
- dotTrace (JetBrains)
- MAUI Blazor DevTools

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 02-system-architecture.md, 05-voice-pipeline.md, 08-maui-implementation.md
