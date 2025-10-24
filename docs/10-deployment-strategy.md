# 10 - Deployment Strategy

## Overview

This document outlines the deployment strategy for The Cabin, including build configurations, release management, distribution channels, and update mechanisms.

## Build Configurations

### Debug Build

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <AndroidEnableProfiledAot>false</AndroidEnableProfiledAot>
    <AndroidLinkMode>None</AndroidLinkMode>
    <AndroidUseInterpreter>true</AndroidUseInterpreter>
    <DebugSymbols>true</DebugSymbols>
    <Optimize>false</Optimize>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
</PropertyGroup>
```

**Characteristics**:

- Full debugging support
- No code optimization
- Larger APK size (~60-80 MB)
- Faster build times
- All logging enabled

### Release Build

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidPackageFormat>apk</AndroidPackageFormat>
    <AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>
    <AndroidLinkMode>SdkOnly</AndroidLinkMode>
    <AndroidUseAapt2>true</AndroidUseAapt2>
    <AndroidCreatePackagePerAbi>false</AndroidCreatePackagePerAbi>
    <DebugSymbols>false</DebugSymbols>
    <Optimize>true</Optimize>
    <EnableLLVM>false</EnableLLVM>
    <AndroidEnableMultiDex>true</AndroidEnableMultiDex>
</PropertyGroup>
```

**Characteristics**:

- AOT compilation enabled
- Code optimization
- Smaller APK size (~40-50 MB)
- Production-ready
- Minimal logging

## Version Management

### Versioning Strategy

Following Semantic Versioning (SemVer): `MAJOR.MINOR.PATCH`

```xml
<PropertyGroup>
    <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
</PropertyGroup>
```

- **MAJOR**: Breaking changes, major features
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, minor improvements

### Version Code (Android)

```csharp
public static class VersionInfo
{
    public const string Version = "1.0.0";
    public const int VersionCode = 1;
    public const string BuildDate = "2025-10-23";
    
    public static string GetFullVersion()
    {
        return $"{Version} ({VersionCode}) - {BuildDate}";
    }
}
```

## Code Signing

### Android App Signing

#### Development/Debug Signing

```bash
# Uses debug keystore automatically
# Location: %USERPROFILE%\.android\debug.keystore
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey
```

#### Production Signing

```bash
# Generate release keystore
keytool -genkey -v -keystore thecabin-release.keystore -alias thecabin -keyalg RSA -keysize 2048 -validity 10000

# Sign APK
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore thecabin-release.keystore app-release-unsigned.apk thecabin

# Verify signature
jarsigner -verify -verbose -certs app-release.apk
```

#### Keystore Configuration

```xml
<!-- In .csproj for automated signing -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidKeyStore>true</AndroidKeyStore>
    <AndroidSigningKeyStore>thecabin-release.keystore</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>thecabin</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass>$(KeystorePassword)</AndroidSigningKeyPass>
    <AndroidSigningStorePass>$(KeystorePassword)</AndroidSigningStorePass>
</PropertyGroup>
```

**IMPORTANT**: Never commit keystore files or passwords to source control!

## Build Pipeline

### GitHub Actions Workflow

```yaml
name: Android Build

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  release:
    types: [ published ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Install MAUI Workloads
      run: dotnet workload install maui
    
    - name: Restore Dependencies
      run: dotnet restore TheCabin.csproj
    
    - name: Build Debug APK
      if: github.event_name == 'pull_request'
      run: dotnet build TheCabin.csproj -c Debug -f net8.0-android
    
    - name: Build Release APK
      if: github.ref == 'refs/heads/main'
      env:
        KEYSTORE_PASSWORD: ${{ secrets.KEYSTORE_PASSWORD }}
      run: dotnet publish TheCabin.csproj -c Release -f net8.0-android
    
    - name: Upload APK Artifact
      uses: actions/upload-artifact@v3
      with:
        name: android-apk
        path: bin/Release/net8.0-android/publish/*.apk
    
    - name: Run Tests
      run: dotnet test TheCabin.Tests/TheCabin.Tests.csproj
```

### Azure DevOps Pipeline

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'windows-latest'

variables:
  buildConfiguration: 'Release'
  
steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- script: dotnet workload install maui
  displayName: 'Install MAUI Workloads'

- task: DotNetCoreCLI@2
  displayName: 'Restore NuGet Packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build Release APK'
  inputs:
    command: 'publish'
    projects: 'TheCabin.csproj'
    arguments: '-c $(buildConfiguration) -f net8.0-android'
    
- task: CopyFiles@2
  displayName: 'Copy APK to Staging'
  inputs:
    contents: '**/*.apk'
    targetFolder: '$(Build.ArtifactStagingDirectory)'

- task: PublishBuildArtifacts@1
  displayName: 'Publish APK Artifact'
  inputs:
    pathToPublish: '$(Build.ArtifactStagingDirectory)'
    artifactName: 'android-build'
```

## Distribution Channels

### 1. Google Play Store

#### Preparation Checklist

- [x] Create Google Play Console account ($25 one-time fee)
- [x] Prepare app listing materials:
  - App name, short description, full description
  - Screenshots (phone, tablet, 7-inch tablet, 10-inch tablet)
  - Feature graphic (1024 x 500 px)
  - App icon (512 x 512 px)
  - Promo video (optional)
- [x] Set content rating questionnaire
- [x] Set target audience and content
- [x] Prepare privacy policy URL
- [x] Configure app pricing (Free)

#### Play Store Metadata

```text
Title: The Cabin - Voice Adventure
Short Description: 
Experience immersive voice-controlled text adventures in The Cabin.

Full Description:
🎙️ VOICE-CONTROLLED ADVENTURE
The Cabin brings classic text adventures into the modern era with cutting-edge voice recognition. Speak your commands naturally and watch the story unfold.

✨ KEY FEATURES
• Hands-free gameplay with voice commands
• Multiple atmospheric story packs
• Dynamic AI-powered narrative
• Save your progress and return anytime
• Offline play mode available

🎭 STORY PACKS INCLUDED
• Classic Horror: Haunted cabin mystery
• Arctic Survival: Frozen isolation
• Fantasy Magic: Wizard's workshop
• Sci-Fi Isolation: Derelict space module
• Cozy Mystery: Snowbound secrets

🎮 HOW TO PLAY
Simply tap the microphone and speak your commands: "look around", "take lantern", "go north". The game understands natural language, so talk as you would to a friend.

Privacy Policy: https://thecabin.app/privacy
```

#### Release Tracks

1. **Internal Testing**: Team members only (1-100 testers)
2. **Closed Testing**: Limited audience (up to 100,000 testers)
3. **Open Testing**: Public beta (unlimited)
4. **Production**: Full release

#### Staged Rollout

```text
Day 1:  5% of users
Day 3:  20% of users
Day 5:  50% of users
Day 7:  100% of users
```

### 2. Direct APK Distribution (Side-loading)

For beta testers or regions without Play Store access:

```bash
# Generate signed APK
dotnet publish -c Release -f net8.0-android

# APK location
bin/Release/net8.0-android/publish/com.thecabin.voiceadventure-Signed.apk
```

**Distribution Methods**:

- GitHub Releases
- Direct download from website
- Enterprise MDM systems

### 3. Third-Party App Stores

Consider additional distribution through:

- Amazon Appstore
- Samsung Galaxy Store
- Huawei AppGallery

## Update Mechanism

### In-App Update Implementation

```csharp
public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    
    private const string UpdateCheckUrl = 
        "https://api.thecabin.app/version/check";
    
    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = VersionInfo.Version;
            var response = await _httpClient.GetAsync(
                $"{UpdateCheckUrl}?current={currentVersion}&platform=android");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UpdateInfo>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
        }
        
        return new UpdateInfo { UpdateAvailable = false };
    }
    
    public async Task<bool> PromptUpdateAsync(UpdateInfo update)
    {
        if (!update.UpdateAvailable)
            return false;
        
        var message = update.IsCritical
            ? $"Critical update available: {update.NewVersion}\n\n" +
              $"{update.ReleaseNotes}\n\nThis update is required to continue."
            : $"Update available: {update.NewVersion}\n\n" +
              $"{update.ReleaseNotes}\n\nWould you like to update now?";
        
        var result = await Application.Current.MainPage.DisplayAlert(
            "Update Available",
            message,
            update.IsCritical ? "Update Now" : "Update",
            update.IsCritical ? null : "Later");
        
        if (result)
        {
            await OpenUpdateLinkAsync(update.UpdateUrl);
            return true;
        }
        
        return false;
    }
    
    private async Task OpenUpdateLinkAsync(string url)
    {
        await Launcher.OpenAsync(new Uri(url));
    }
}

public class UpdateInfo
{
    public bool UpdateAvailable { get; set; }
    public string NewVersion { get; set; }
    public string ReleaseNotes { get; set; }
    public string UpdateUrl { get; set; }
    public bool IsCritical { get; set; }
}
```

### Content Pack Updates

```csharp
public class ContentUpdateService
{
    private const string ContentManifestUrl = 
        "https://cdn.thecabin.app/content/manifest.json";
    
    public async Task<List<StoryPackUpdate>> CheckContentUpdatesAsync()
    {
        var manifest = await FetchManifestAsync();
        var updates = new List<StoryPackUpdate>();
        
        foreach (var pack in manifest.Packs)
        {
            var localVersion = await GetLocalPackVersionAsync(pack.Id);
            if (localVersion == null || 
                Version.Parse(pack.Version) > Version.Parse(localVersion))
            {
                updates.Add(new StoryPackUpdate
                {
                    PackId = pack.Id,
                    NewVersion = pack.Version,
                    DownloadUrl = pack.DownloadUrl,
                    SizeInMb = pack.SizeInMb
                });
            }
        }
        
        return updates;
    }
    
    public async Task DownloadContentPackAsync(
        StoryPackUpdate update,
        IProgress<double> progress)
    {
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(
            update.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead);
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;
        
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = File.Create(
            GetContentPackPath(update.PackId));
        
        var buffer = new byte[8192];
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            downloadedBytes += bytesRead;
            
            if (totalBytes > 0)
            {
                progress?.Report((double)downloadedBytes / totalBytes);
            }
        }
    }
    
    private string GetContentPackPath(string packId)
    {
        return Path.Combine(
            FileSystem.AppDataDirectory,
            "StorePacks",
            $"{packId}.json");
    }
}
```

## Monitoring and Analytics

### Crash Reporting

```csharp
// Using Microsoft App Center
public static class AppCenterConfig
{
    public static void Initialize()
    {
        AppCenter.Start(
            "android={APPCENTER_SECRET}",
            typeof(Analytics),
            typeof(Crashes));
        
        Crashes.GetErrorAttachments = (ErrorReport report) =>
        {
            return new[]
            {
                ErrorAttachmentLog.AttachmentWithText(
                    GetDiagnosticInfo(),
                    "diagnostic_info.txt")
            };
        };
    }
    
    private static string GetDiagnosticInfo()
    {
        return $@"
App Version: {VersionInfo.Version}
Device: {DeviceInfo.Model}
OS: {DeviceInfo.Platform} {DeviceInfo.VersionString}
Memory: {DeviceInfo.Idiom}
        ";
    }
}
```

### Usage Analytics

```csharp
public class AnalyticsService
{
    public void TrackEvent(string eventName, 
        Dictionary<string, string> properties = null)
    {
        Analytics.TrackEvent(eventName, properties);
    }
    
    public void TrackGameSession(GameSession session)
    {
        TrackEvent("game_session_completed", new Dictionary<string, string>
        {
            { "theme", session.ThemeId },
            { "duration_minutes", session.Duration.TotalMinutes.ToString("F0") },
            { "commands_executed", session.CommandsExecuted.ToString() },
            { "puzzles_solved", session.PuzzlesSolved.ToString() }
        });
    }
    
    public void TrackVoiceCommand(string command, bool success)
    {
        TrackEvent("voice_command", new Dictionary<string, string>
        {
            { "command_type", command },
            { "success", success.ToString() }
        });
    }
}
```

## Rollback Strategy

### Detecting Issues

```csharp
public class RollbackDetector
{
    private readonly IAnalyticsService _analytics;
    
    public async Task<RollbackRecommendation> AnalyzeDeploymentAsync(
        string version)
    {
        var metrics = await _analytics.GetMetricsAsync(version);
        
        var crashRate = metrics.Crashes / (double)metrics.Sessions;
        var errorRate = metrics.Errors / (double)metrics.Commands;
        
        if (crashRate > 0.05) // 5% crash rate
        {
            return new RollbackRecommendation
            {
                ShouldRollback = true,
                Reason = $"High crash rate: {crashRate:P}"
            };
        }
        
        if (errorRate > 0.20) // 20% error rate
        {
            return new RollbackRecommendation
            {
                ShouldRollback = true,
                Reason = $"High error rate: {errorRate:P}"
            };
        }
        
        return new RollbackRecommendation
        {
            ShouldRollback = false
        };
    }
}
```

### Rollback Process

1. **Halt Production Rollout**: Pause staged rollout at current percentage
2. **Investigate**: Review crash logs and user feedback
3. **Fix or Revert**: Either hotfix and re-release, or revert to previous version
4. **Communicate**: Notify users of issues and resolution timeline

## Security Considerations

### API Key Management

```csharp
// Store sensitive keys in secure storage
public class SecureConfigService
{
    public async Task<string> GetApiKeyAsync(string keyName)
    {
        return await SecureStorage.GetAsync(keyName);
    }
    
    public async Task SetApiKeyAsync(string keyName, string value)
    {
        await SecureStorage.SetAsync(keyName, value);
    }
}
```

### ProGuard/R8 Configuration

```text
# Keep model classes
-keep class TheCabin.Models.** { *; }

# Keep API client interfaces
-keep interface TheCabin.Services.I** { *; }

# Keep custom exceptions
-keep class TheCabin.Exceptions.** { *; }
```

## Release Checklist

### Pre-Release

- [ ] All tests passing
- [ ] Code review completed
- [ ] Version numbers updated
- [ ] Release notes prepared
- [ ] Screenshots updated
- [ ] Privacy policy current
- [ ] Terms of service current
- [ ] APK signed with release key
- [ ] APK tested on multiple devices
- [ ] Analytics configured
- [ ] Crash reporting tested

### Release

- [ ] Upload to Play Console
- [ ] Set release notes
- [ ] Configure rollout percentage
- [ ] Submit for review

### Post-Release

- [ ] Monitor crash reports
- [ ] Monitor user reviews
- [ ] Track key metrics
- [ ] Prepare hotfix if needed

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 03-technical-stack.md, 08-maui-implementation.md
