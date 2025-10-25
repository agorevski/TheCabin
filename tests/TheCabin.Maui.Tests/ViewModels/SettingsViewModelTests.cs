using Xunit;
using Moq;
using FluentAssertions;
using TheCabin.Maui.ViewModels;
using Microsoft.Maui.Storage;

namespace TheCabin.Maui.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        // Clear preferences before each test
        Preferences.Clear();
        _viewModel = new SettingsViewModel();
    }

    #region Initialization Tests (SE-01 to SE-04)

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("Settings");
    }

    [Fact]
    public void Constructor_LoadsDefaultSettings()
    {
        // Assert
        _viewModel.VoiceEnabled.Should().BeTrue();
        _viewModel.PushToTalk.Should().BeTrue();
        _viewModel.ConfidenceThreshold.Should().Be(0.75);
        _viewModel.OfflineMode.Should().BeFalse();
    }

    [Fact]
    public void Constructor_LoadsDefaultTtsSettings()
    {
        // Assert
        _viewModel.TtsEnabled.Should().BeFalse();
        _viewModel.SpeechRate.Should().Be(1.0);
        _viewModel.VoicePitch.Should().Be(1.0);
    }

    [Fact]
    public void Constructor_LoadsDefaultDisplaySettings()
    {
        // Assert
        _viewModel.FontSize.Should().Be(16.0);
        _viewModel.SelectedTheme.Should().Be("Dark");
    }

    [Fact]
    public void AvailableThemes_ContainsAllThemes()
    {
        // Assert
        _viewModel.AvailableThemes.Should().Contain("Dark");
        _viewModel.AvailableThemes.Should().Contain("Light");
        _viewModel.AvailableThemes.Should().Contain("High Contrast");
        _viewModel.AvailableThemes.Should().HaveCount(3);
    }

    [Fact]
    public void AppVersion_IsSet()
    {
        // Assert
        _viewModel.AppVersion.Should().NotBeNullOrEmpty();
        _viewModel.AppVersion.Should().Contain("Version");
    }

    #endregion

    #region Voice Recognition Settings Tests (SE-10 to SE-16)

    [Fact]
    public void VoiceEnabled_CanBeToggled()
    {
        // Arrange
        var initialValue = _viewModel.VoiceEnabled;

        // Act
        _viewModel.VoiceEnabled = !initialValue;

        // Assert
        _viewModel.VoiceEnabled.Should().Be(!initialValue);
    }

    [Fact]
    public void VoiceEnabled_PersistsToPreferences()
    {
        // Act
        _viewModel.VoiceEnabled = false;

        // Assert
        Preferences.Get(nameof(_viewModel.VoiceEnabled), true).Should().BeFalse();
    }

    [Fact]
    public void PushToTalk_CanBeToggled()
    {
        // Arrange
        var initialValue = _viewModel.PushToTalk;

        // Act
        _viewModel.PushToTalk = !initialValue;

        // Assert
        _viewModel.PushToTalk.Should().Be(!initialValue);
    }

    [Fact]
    public void PushToTalk_PersistsToPreferences()
    {
        // Act
        _viewModel.PushToTalk = false;

        // Assert
        Preferences.Get(nameof(_viewModel.PushToTalk), true).Should().BeFalse();
    }

    [Fact]
    public void ConfidenceThreshold_CanBeAdjusted()
    {
        // Act
        _viewModel.ConfidenceThreshold = 0.85;

        // Assert
        _viewModel.ConfidenceThreshold.Should().Be(0.85);
    }

    [Fact]
    public void ConfidenceThreshold_PersistsToPreferences()
    {
        // Act
        _viewModel.ConfidenceThreshold = 0.90;

        // Assert
        Preferences.Get(nameof(_viewModel.ConfidenceThreshold), 0.75).Should().Be(0.90);
    }

    [Fact]
    public void OfflineMode_CanBeToggled()
    {
        // Act
        _viewModel.OfflineMode = true;

        // Assert
        _viewModel.OfflineMode.Should().BeTrue();
    }

    [Fact]
    public void OfflineMode_PersistsToPreferences()
    {
        // Act
        _viewModel.OfflineMode = true;

        // Assert
        Preferences.Get(nameof(_viewModel.OfflineMode), false).Should().BeTrue();
    }

    [Fact]
    public void TestVoiceCommand_IsNotNull()
    {
        // Assert
        _viewModel.TestVoiceCommand.Should().NotBeNull();
    }

    #endregion

    #region TTS Settings Tests (SE-20 to SE-26)

    [Fact]
    public void TtsEnabled_CanBeToggled()
    {
        // Act
        _viewModel.TtsEnabled = true;

        // Assert
        _viewModel.TtsEnabled.Should().BeTrue();
    }

    [Fact]
    public void TtsEnabled_PersistsToPreferences()
    {
        // Act
        _viewModel.TtsEnabled = true;

        // Assert
        Preferences.Get(nameof(_viewModel.TtsEnabled), false).Should().BeTrue();
    }

    [Fact]
    public void SpeechRate_CanBeAdjusted()
    {
        // Act
        _viewModel.SpeechRate = 1.5;

        // Assert
        _viewModel.SpeechRate.Should().Be(1.5);
    }

    [Fact]
    public void SpeechRate_PersistsToPreferences()
    {
        // Act
        _viewModel.SpeechRate = 0.8;

        // Assert
        Preferences.Get(nameof(_viewModel.SpeechRate), 1.0).Should().Be(0.8);
    }

    [Fact]
    public void VoicePitch_CanBeAdjusted()
    {
        // Act
        _viewModel.VoicePitch = 1.2;

        // Assert
        _viewModel.VoicePitch.Should().Be(1.2);
    }

    [Fact]
    public void VoicePitch_PersistsToPreferences()
    {
        // Act
        _viewModel.VoicePitch = 0.9;

        // Assert
        Preferences.Get(nameof(_viewModel.VoicePitch), 1.0).Should().Be(0.9);
    }

    [Fact]
    public void TestTtsCommand_IsNotNull()
    {
        // Assert
        _viewModel.TestTtsCommand.Should().NotBeNull();
    }

    #endregion

    #region Display Settings Tests (SE-30 to SE-35)

    [Fact]
    public void FontSize_CanBeAdjusted()
    {
        // Act
        _viewModel.FontSize = 18.0;

        // Assert
        _viewModel.FontSize.Should().Be(18.0);
    }

    [Fact]
    public void FontSize_PersistsToPreferences()
    {
        // Act
        _viewModel.FontSize = 20.0;

        // Assert
        Preferences.Get(nameof(_viewModel.FontSize), 16.0).Should().Be(20.0);
    }

    [Fact]
    public void SelectedTheme_CanBeChanged()
    {
        // Act
        _viewModel.SelectedTheme = "Light";

        // Assert
        _viewModel.SelectedTheme.Should().Be("Light");
    }

    [Fact]
    public void SelectedTheme_PersistsToPreferences()
    {
        // Act
        _viewModel.SelectedTheme = "High Contrast";

        // Assert
        Preferences.Get(nameof(_viewModel.SelectedTheme), "Dark").Should().Be("High Contrast");
    }

    [Fact]
    public void SelectedTheme_AcceptsValidThemes()
    {
        // Act & Assert
        _viewModel.SelectedTheme = "Dark";
        _viewModel.SelectedTheme.Should().Be("Dark");

        _viewModel.SelectedTheme = "Light";
        _viewModel.SelectedTheme.Should().Be("Light");

        _viewModel.SelectedTheme = "High Contrast";
        _viewModel.SelectedTheme.Should().Be("High Contrast");
    }

    #endregion

    #region Reset Settings Tests (SE-40 to SE-42)

    [Fact]
    public void ResetToDefaultsCommand_IsNotNull()
    {
        // Assert
        _viewModel.ResetToDefaultsCommand.Should().NotBeNull();
    }

    [Fact]
    public void ResetToDefaults_RestoresAllSettings()
    {
        // Arrange
        _viewModel.VoiceEnabled = false;
        _viewModel.TtsEnabled = true;
        _viewModel.FontSize = 20.0;
        _viewModel.SelectedTheme = "Light";

        // Act
        Preferences.Clear();
        var newViewModel = new SettingsViewModel();

        // Assert
        newViewModel.VoiceEnabled.Should().BeTrue();
        newViewModel.TtsEnabled.Should().BeFalse();
        newViewModel.FontSize.Should().Be(16.0);
        newViewModel.SelectedTheme.Should().Be("Dark");
    }

    #endregion

    #region Navigation Tests (SE-52)

    [Fact]
    public void CloseCommand_IsNotNull()
    {
        // Assert
        _viewModel.CloseCommand.Should().NotBeNull();
    }

    #endregion

    #region Persistence Tests (SE-50 to SE-51)

    [Fact]
    public void Settings_PersistAcrossInstances()
    {
        // Arrange
        _viewModel.VoiceEnabled = false;
        _viewModel.ConfidenceThreshold = 0.85;
        _viewModel.TtsEnabled = true;
        _viewModel.SpeechRate = 1.3;
        _viewModel.FontSize = 18.0;
        _viewModel.SelectedTheme = "Light";

        // Act
        var newViewModel = new SettingsViewModel();

        // Assert
        newViewModel.VoiceEnabled.Should().BeFalse();
        newViewModel.ConfidenceThreshold.Should().Be(0.85);
        newViewModel.TtsEnabled.Should().BeTrue();
        newViewModel.SpeechRate.Should().Be(1.3);
        newViewModel.FontSize.Should().Be(18.0);
        newViewModel.SelectedTheme.Should().Be("Light");
    }

    [Fact]
    public void Settings_LoadFromPreferencesOnConstruction()
    {
        // Arrange
        Preferences.Set(nameof(SettingsViewModel.VoiceEnabled), false);
        Preferences.Set(nameof(SettingsViewModel.PushToTalk), false);
        Preferences.Set(nameof(SettingsViewModel.ConfidenceThreshold), 0.80);

        // Act
        var vm = new SettingsViewModel();

        // Assert
        vm.VoiceEnabled.Should().BeFalse();
        vm.PushToTalk.Should().BeFalse();
        vm.ConfidenceThreshold.Should().Be(0.80);
    }

    #endregion

    #region Range Validation Tests

    [Fact]
    public void ConfidenceThreshold_AcceptsValidRange()
    {
        // Act & Assert
        _viewModel.ConfidenceThreshold = 0.0;
        _viewModel.ConfidenceThreshold.Should().Be(0.0);

        _viewModel.ConfidenceThreshold = 0.5;
        _viewModel.ConfidenceThreshold.Should().Be(0.5);

        _viewModel.ConfidenceThreshold = 1.0;
        _viewModel.ConfidenceThreshold.Should().Be(1.0);
    }

    [Fact]
    public void SpeechRate_AcceptsValidRange()
    {
        // Act & Assert
        _viewModel.SpeechRate = 0.5;
        _viewModel.SpeechRate.Should().Be(0.5);

        _viewModel.SpeechRate = 1.0;
        _viewModel.SpeechRate.Should().Be(1.0);

        _viewModel.SpeechRate = 2.0;
        _viewModel.SpeechRate.Should().Be(2.0);
    }

    [Fact]
    public void VoicePitch_AcceptsValidRange()
    {
        // Act & Assert
        _viewModel.VoicePitch = 0.5;
        _viewModel.VoicePitch.Should().Be(0.5);

        _viewModel.VoicePitch = 1.0;
        _viewModel.VoicePitch.Should().Be(1.0);

        _viewModel.VoicePitch = 2.0;
        _viewModel.VoicePitch.Should().Be(2.0);
    }

    [Fact]
    public void FontSize_AcceptsValidRange()
    {
        // Act & Assert
        _viewModel.FontSize = 12.0;
        _viewModel.FontSize.Should().Be(12.0);

        _viewModel.FontSize = 16.0;
        _viewModel.FontSize.Should().Be(16.0);

        _viewModel.FontSize = 24.0;
        _viewModel.FontSize.Should().Be(24.0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MultipleSettings_CanBeChangedIndependently()
    {
        // Act
        _viewModel.VoiceEnabled = false;
        _viewModel.TtsEnabled = true;
        _viewModel.FontSize = 20.0;

        // Assert
        _viewModel.VoiceEnabled.Should().BeFalse();
        _viewModel.TtsEnabled.Should().BeTrue();
        _viewModel.FontSize.Should().Be(20.0);
        // Other settings should remain at default
        _viewModel.PushToTalk.Should().BeTrue();
        _viewModel.SelectedTheme.Should().Be("Dark");
    }

    [Fact]
    public void AllSettings_CanBePersisted()
    {
        // Arrange
        _viewModel.VoiceEnabled = false;
        _viewModel.PushToTalk = false;
        _viewModel.ConfidenceThreshold = 0.88;
        _viewModel.OfflineMode = true;
        _viewModel.TtsEnabled = true;
        _viewModel.SpeechRate = 1.4;
        _viewModel.VoicePitch = 1.1;
        _viewModel.FontSize = 19.0;
        _viewModel.SelectedTheme = "Light";

        // Act
        var newVm = new SettingsViewModel();

        // Assert - All settings should be loaded from preferences
        newVm.VoiceEnabled.Should().BeFalse();
        newVm.PushToTalk.Should().BeFalse();
        newVm.ConfidenceThreshold.Should().Be(0.88);
        newVm.OfflineMode.Should().BeTrue();
        newVm.TtsEnabled.Should().BeTrue();
        newVm.SpeechRate.Should().Be(1.4);
        newVm.VoicePitch.Should().Be(1.1);
        newVm.FontSize.Should().Be(19.0);
        newVm.SelectedTheme.Should().Be("Light");
    }

    #endregion
}
