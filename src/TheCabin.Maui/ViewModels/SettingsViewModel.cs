using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheCabin.Core.Interfaces;

namespace TheCabin.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IPreferencesService _preferencesService;
    
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
    
    public SettingsViewModel(IPreferencesService preferencesService)
    {
        _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        Title = "Settings";
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        // Load from preferences
        VoiceEnabled = _preferencesService.Get(nameof(VoiceEnabled), true);
        PushToTalk = _preferencesService.Get(nameof(PushToTalk), true);
        ConfidenceThreshold = _preferencesService.Get(nameof(ConfidenceThreshold), 0.75);
        OfflineMode = _preferencesService.Get(nameof(OfflineMode), false);
        
        TtsEnabled = _preferencesService.Get(nameof(TtsEnabled), false);
        SpeechRate = _preferencesService.Get(nameof(SpeechRate), 1.0);
        VoicePitch = _preferencesService.Get(nameof(VoicePitch), 1.0);
        
        FontSize = _preferencesService.Get(nameof(FontSize), 16.0);
        SelectedTheme = _preferencesService.Get(nameof(SelectedTheme), "Dark");
        
        AppVersion = $"Version 1.0.0 (Build 1)";
    }
    
    partial void OnVoiceEnabledChanged(bool value)
    {
        _preferencesService.Set(nameof(VoiceEnabled), value);
    }
    
    partial void OnPushToTalkChanged(bool value)
    {
        _preferencesService.Set(nameof(PushToTalk), value);
    }
    
    partial void OnConfidenceThresholdChanged(double value)
    {
        _preferencesService.Set(nameof(ConfidenceThreshold), value);
    }
    
    partial void OnOfflineModeChanged(bool value)
    {
        _preferencesService.Set(nameof(OfflineMode), value);
    }
    
    partial void OnTtsEnabledChanged(bool value)
    {
        _preferencesService.Set(nameof(TtsEnabled), value);
    }
    
    partial void OnSpeechRateChanged(double value)
    {
        _preferencesService.Set(nameof(SpeechRate), value);
    }
    
    partial void OnVoicePitchChanged(double value)
    {
        _preferencesService.Set(nameof(VoicePitch), value);
    }
    
    partial void OnFontSizeChanged(double value)
    {
        _preferencesService.Set(nameof(FontSize), value);
    }
    
    partial void OnSelectedThemeChanged(string value)
    {
        _preferencesService.Set(nameof(SelectedTheme), value);
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
            _preferencesService.Clear();
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
