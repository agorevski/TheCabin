using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TheCabin.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool voiceEnabled;
    
    [ObservableProperty]
    private bool pushToTalk;
    
    [ObservableProperty]
    private double confidenceThreshold;
    
    [ObservableProperty]
    private bool offlineMode;
    
    [ObservableProperty]
    private bool ttsEnabled;
    
    [ObservableProperty]
    private double speechRate;
    
    [ObservableProperty]
    private double voicePitch;
    
    [ObservableProperty]
    private double fontSize;
    
    [ObservableProperty]
    private string selectedTheme = string.Empty;
    
    [ObservableProperty]
    private string appVersion = string.Empty;
    
    public List<string> AvailableThemes { get; } = new()
    {
        "Dark",
        "Light",
        "High Contrast"
    };
    
    public SettingsViewModel()
    {
        Title = "Settings";
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        // Load from preferences
        VoiceEnabled = Preferences.Get(nameof(VoiceEnabled), true);
        PushToTalk = Preferences.Get(nameof(PushToTalk), true);
        ConfidenceThreshold = Preferences.Get(nameof(ConfidenceThreshold), 0.75);
        OfflineMode = Preferences.Get(nameof(OfflineMode), false);
        
        TtsEnabled = Preferences.Get(nameof(TtsEnabled), false);
        SpeechRate = Preferences.Get(nameof(SpeechRate), 1.0);
        VoicePitch = Preferences.Get(nameof(VoicePitch), 1.0);
        
        FontSize = Preferences.Get(nameof(FontSize), 16.0);
        SelectedTheme = Preferences.Get(nameof(SelectedTheme), "Dark");
        
        AppVersion = $"Version 1.0.0 (Build 1)";
    }
    
    partial void OnVoiceEnabledChanged(bool value)
    {
        Preferences.Set(nameof(VoiceEnabled), value);
    }
    
    partial void OnPushToTalkChanged(bool value)
    {
        Preferences.Set(nameof(PushToTalk), value);
    }
    
    partial void OnConfidenceThresholdChanged(double value)
    {
        Preferences.Set(nameof(ConfidenceThreshold), value);
    }
    
    partial void OnOfflineModeChanged(bool value)
    {
        Preferences.Set(nameof(OfflineMode), value);
    }
    
    partial void OnTtsEnabledChanged(bool value)
    {
        Preferences.Set(nameof(TtsEnabled), value);
    }
    
    partial void OnSpeechRateChanged(double value)
    {
        Preferences.Set(nameof(SpeechRate), value);
    }
    
    partial void OnVoicePitchChanged(double value)
    {
        Preferences.Set(nameof(VoicePitch), value);
    }
    
    partial void OnFontSizeChanged(double value)
    {
        Preferences.Set(nameof(FontSize), value);
    }
    
    partial void OnSelectedThemeChanged(string value)
    {
        Preferences.Set(nameof(SelectedTheme), value);
        ApplyTheme(value);
    }
    
    private void ApplyTheme(string theme)
    {
        // Theme switching logic
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = theme switch
        {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Dark
            };
        }
    }
    
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var confirm = await ShowConfirmAsync(
            "Reset Settings",
            "Reset all settings to default values?");
        
        if (confirm)
        {
            Preferences.Clear();
            LoadSettings();
        }
    }
    
    [RelayCommand]
    private async Task TestVoiceAsync()
    {
        await Shell.Current.DisplayAlert(
            "Test Voice",
            "Voice recognition test would start here.",
            "OK");
    }
    
    [RelayCommand]
    private async Task TestTtsAsync()
    {
        await Shell.Current.DisplayAlert(
            "Test TTS",
            "Text-to-speech test: 'Welcome to The Cabin'",
            "OK");
    }
    
    [RelayCommand]
    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
