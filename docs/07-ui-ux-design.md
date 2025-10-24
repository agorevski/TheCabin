# 07 - UI/UX Design

## Overview

This document outlines the user interface and user experience design for The Cabin, focusing on voice-first interaction patterns, visual hierarchy, and accessibility.

## Design Principles

1. **Voice-First**: UI supports but doesn't require touch interaction
2. **Minimalist**: Clean, distraction-free interface
3. **Feedback-Rich**: Clear visual and audio feedback
4. **Accessible**: Inclusive design for all users
5. **Immersive**: Support for the narrative experience

## Screen Layouts

### Main Game Screen

```text
┌──────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────┐ │
│ │  Stats Bar                                       │ │
│ │  🏠 Cabin Main    ❤️ 100    💡 Dim    🕐 3:45  │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │                                                  │ │
│ │  Story Feed (Scrollable)                         │ │
│ │                                                  │ │
│ │  > You stand in a dimly lit wooden cabin...      │ │
│ │                                                  │ │
│ │  ▶ "look around"                                │ │
│ │                                                  │ │
│ │  > The walls are rough-hewn logs. A lantern      │ │
│ │    hangs from a rusty hook.                      │ │
│ │                                                  │ │
│ │  ▶ "take lantern"                               │ │
│ │                                                  │ │
│ │  > You pick up the lantern. It feels heavy       │ │
│ │    with oil.                                     │ │
│ │                                                  │ │
│ │                                                  │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │  Transcript Preview                              │ │
│ │  🎤 "Take the lantern"                          │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│         ┌──────────────────────────┐                 │
│         │   🎙️ Voice Button       │                 │
│         │   (Tap to Speak)         │                 │
│         │   ∿∿∿ Waveform ∿∿∿     │                 │
│         └──────────────────────────┘                 │
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │  [Inventory]  [Menu]  [Settings]                 │ │
│ └──────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────┘
```

### XAML Implementation

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:TheCabin.ViewModels"
             x:Class="TheCabin.Views.MainPage"
             x:DataType="vm:MainViewModel"
             BackgroundColor="{DynamicResource PageBackgroundColor}">
    
    <Grid RowDefinitions="Auto,*,Auto,Auto,Auto" Padding="16">
        
        <!-- Stats Bar -->
        <Border Grid.Row="0" 
                StyleClass="StatsBar"
                Padding="12,8"
                Margin="0,0,0,8">
            <Grid ColumnDefinitions="*,Auto,Auto,Auto">
                <Label Grid.Column="0" 
                       Text="{Binding CurrentLocation}"
                       FontSize="16"
                       FontAttributes="Bold"/>
                <HorizontalStackLayout Grid.Column="1" Spacing="12">
                    <Label Text="❤️"/>
                    <Label Text="{Binding PlayerHealth}"/>
                </HorizontalStackLayout>
                <HorizontalStackLayout Grid.Column="2" Spacing="12">
                    <Label Text="💡"/>
                    <Label Text="{Binding LightLevel}"/>
                </HorizontalStackLayout>
                <HorizontalStackLayout Grid.Column="3" Spacing="12">
                    <Label Text="🕐"/>
                    <Label Text="{Binding GameTime}"/>
                </HorizontalStackLayout>
            </Grid>
        </Border>
        
        <!-- Story Feed -->
        <Border Grid.Row="1" 
                StyleClass="ContentPanel"
                Padding="16">
            <CollectionView ItemsSource="{Binding StoryFeed}"
                           VerticalScrollBarVisibility="Always">
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:NarrativeEntry">
                        <Grid Padding="0,8">
                            <Label Text="{Binding Text}"
                                   TextColor="{Binding TextColor}"
                                   FontSize="16"
                                   LineBreakMode="WordWrap"
                                   FontAttributes="{Binding IsImportant, 
                                       Converter={StaticResource BoolToFontAttributes}}"/>
                        </Grid>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </Border>
        
        <!-- Transcript Preview -->
        <Border Grid.Row="2"
                StyleClass="TranscriptPanel"
                Padding="12"
                Margin="0,8"
                IsVisible="{Binding IsListening}">
            <HorizontalStackLayout Spacing="8">
                <Label Text="🎤" FontSize="16"/>
                <Label Text="{Binding TranscriptText}"
                       FontSize="14"
                       TextColor="{DynamicResource SecondaryTextColor}"/>
            </HorizontalStackLayout>
        </Border>
        
        <!-- Voice Control -->
        <Grid Grid.Row="3" 
              Padding="0,16"
              HorizontalOptions="Center">
            <Button x:Name="VoiceButton"
                    Command="{Binding ToggleListeningCommand}"
                    HeightRequest="80"
                    WidthRequest="80"
                    CornerRadius="40"
                    BackgroundColor="{Binding IsListening, 
                        Converter={StaticResource BoolToRecordingColor}}"
                    BorderWidth="3"
                    BorderColor="{DynamicResource PrimaryColor}">
                <Button.ImageSource>
                    <FontImageSource 
                        Glyph="🎙️"
                        FontFamily="SegoeUIEmoji"
                        Size="32"
                        Color="White"/>
                </Button.ImageSource>
            </Button>
            
            <!-- Waveform Animation (when listening) -->
            <GraphicsView x:Name="WaveformView"
                         IsVisible="{Binding IsListening}"
                         HeightRequest="80"
                         WidthRequest="200"
                         VerticalOptions="Center"
                         HorizontalOptions="Center"
                         InputTransparent="True"/>
        </Grid>
        
        <!-- Bottom Navigation -->
        <Border Grid.Row="4" 
                StyleClass="BottomNav"
                Padding="8">
            <Grid ColumnDefinitions="*,*,*">
                <Button Grid.Column="0"
                        Text="🎒 Inventory"
                        Command="{Binding ShowInventoryCommand}"
                        StyleClass="NavButton"/>
                <Button Grid.Column="1"
                        Text="📖 Menu"
                        Command="{Binding ShowMenuCommand}"
                        StyleClass="NavButton"/>
                <Button Grid.Column="2"
                        Text="⚙️ Settings"
                        Command="{Binding ShowSettingsCommand}"
                        StyleClass="NavButton"/>
            </Grid>
        </Border>
        
    </Grid>
</ContentPage>
```

## Component Details

### 1. Voice Control Button

```csharp
public partial class MainPage : ContentPage
{
    private readonly WaveformAnimator _waveformAnimator;
    
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        _waveformAnimator = new WaveformAnimator(WaveformView);
        
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }
    
    private void OnViewModelPropertyChanged(object sender, 
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsListening))
        {
            var vm = (MainViewModel)BindingContext;
            if (vm.IsListening)
            {
                _waveformAnimator.Start();
                VoiceButton.Scale = 1.1;
            }
            else
            {
                _waveformAnimator.Stop();
                VoiceButton.Scale = 1.0;
            }
        }
    }
}
```

### 2. Waveform Animation

```csharp
public class WaveformAnimator
{
    private readonly GraphicsView _graphicsView;
    private readonly WaveformDrawable _drawable;
    private IDispatcherTimer _timer;
    
    public WaveformAnimator(GraphicsView graphicsView)
    {
        _graphicsView = graphicsView;
        _drawable = new WaveformDrawable();
        _graphicsView.Drawable = _drawable;
    }
    
    public void Start()
    {
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(50);
        _timer.Tick += (s, e) =>
        {
            _drawable.Update();
            _graphicsView.Invalidate();
        };
        _timer.Start();
    }
    
    public void Stop()
    {
        _timer?.Stop();
        _drawable.Reset();
        _graphicsView.Invalidate();
    }
}

public class WaveformDrawable : IDrawable
{
    private readonly List<float> _amplitudes = new();
    private readonly Random _random = new();
    
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 3;
        
        var centerY = dirtyRect.Height / 2;
        var barWidth = 4f;
        var spacing = 6f;
        var barCount = (int)(dirtyRect.Width / (barWidth + spacing));
        
        for (int i = 0; i < barCount && i < _amplitudes.Count; i++)
        {
            var x = i * (barWidth + spacing);
            var amplitude = _amplitudes[i];
            var height = amplitude * (dirtyRect.Height / 2);
            
            canvas.DrawLine(
                x, centerY - height,
                x, centerY + height);
        }
    }
    
    public void Update()
    {
        // Simulate audio amplitude
        if (_amplitudes.Count > 30)
            _amplitudes.RemoveAt(0);
        
        _amplitudes.Add((float)_random.NextDouble());
    }
    
    public void Reset()
    {
        _amplitudes.Clear();
    }
}
```

### 3. Narrative Text Styling

```xaml
<!-- App.xaml Resource Dictionary -->
<Style TargetType="Label" x:Key="NarrativeText">
    <Setter Property="FontFamily" Value="Georgia"/>
    <Setter Property="FontSize" Value="16"/>
    <Setter Property="LineHeight" Value="1.5"/>
    <Setter Property="TextColor" Value="{DynamicResource PrimaryTextColor}"/>
</Style>

<Style TargetType="Label" x:Key="PlayerCommandText">
    <Setter Property="FontFamily" Value="Courier"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="TextColor" Value="{DynamicResource AccentColor}"/>
    <Setter Property="Margin" Value="16,8,0,8"/>
</Style>

<Style TargetType="Label" x:Key="SystemMessageText">
    <Setter Property="FontFamily" Value="Arial"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontAttributes" Value="Italic"/>
    <Setter Property="TextColor" Value="{DynamicResource SecondaryTextColor}"/>
</Style>
```

### 4. Inventory Screen

```xaml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TheCabin.Views.InventoryPage"
             Title="Inventory">
    
    <Grid RowDefinitions="Auto,*,Auto" Padding="16">
        
        <!-- Header -->
        <Border Grid.Row="0" 
                Padding="16"
                Margin="0,0,0,16">
            <Grid ColumnDefinitions="*,Auto">
                <Label Grid.Column="0"
                       Text="Your Inventory"
                       FontSize="24"
                       FontAttributes="Bold"/>
                <Label Grid.Column="1"
                       Text="{Binding WeightDisplay}"
                       FontSize="14"
                       VerticalOptions="End"/>
            </Grid>
        </Border>
        
        <!-- Item List -->
        <CollectionView Grid.Row="1"
                       ItemsSource="{Binding InventoryItems}">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <SwipeView>
                        <SwipeView.RightItems>
                            <SwipeItems>
                                <SwipeItem Text="Drop"
                                          BackgroundColor="DarkRed"
                                          Command="{Binding DropItemCommand}"
                                          CommandParameter="{Binding .}"/>
                                <SwipeItem Text="Use"
                                          BackgroundColor="DarkGreen"
                                          Command="{Binding UseItemCommand}"
                                          CommandParameter="{Binding .}"/>
                            </SwipeItems>
                        </SwipeView.RightItems>
                        
                        <Border Padding="16"
                                Margin="0,4"
                                StyleClass="ItemCard">
                            <Grid ColumnDefinitions="Auto,*,Auto">
                                <Label Grid.Column="0"
                                       Text="{Binding Icon}"
                                       FontSize="24"
                                       Margin="0,0,12,0"/>
                                <VerticalStackLayout Grid.Column="1">
                                    <Label Text="{Binding Name}"
                                           FontSize="16"
                                           FontAttributes="Bold"/>
                                    <Label Text="{Binding Description}"
                                           FontSize="12"
                                           TextColor="{DynamicResource SecondaryTextColor}"/>
                                </VerticalStackLayout>
                                <Label Grid.Column="2"
                                       Text="{Binding Weight, StringFormat='{0} kg'}"
                                       FontSize="12"
                                       VerticalOptions="Center"/>
                            </Grid>
                        </Border>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>
            
            <CollectionView.EmptyView>
                <VerticalStackLayout Padding="32"
                                    HorizontalOptions="Center"
                                    VerticalOptions="Center">
                    <Label Text="🎒"
                           FontSize="64"
                           HorizontalOptions="Center"/>
                    <Label Text="Your inventory is empty"
                           FontSize="16"
                           HorizontalOptions="Center"
                           Margin="0,16,0,0"/>
                </VerticalStackLayout>
            </CollectionView.EmptyView>
        </CollectionView>
        
        <!-- Close Button -->
        <Button Grid.Row="2"
                Text="Close"
                Command="{Binding CloseCommand}"
                Margin="0,16,0,0"/>
    </Grid>
</ContentPage>
```

### 5. Settings Screen

```xaml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TheCabin.Views.SettingsPage"
             Title="Settings">
    
    <ScrollView Padding="16">
        <VerticalStackLayout Spacing="24">
            
            <!-- Voice Settings -->
            <Border Padding="16" StyleClass="SettingsSection">
                <VerticalStackLayout Spacing="16">
                    <Label Text="Voice Recognition"
                           FontSize="18"
                           FontAttributes="Bold"/>
                    
                    <Grid ColumnDefinitions="*,Auto">
                        <Label Grid.Column="0" 
                               Text="Push-to-Talk Mode"
                               VerticalOptions="Center"/>
                        <Switch Grid.Column="1"
                               IsToggled="{Binding UsePushToTalk}"/>
                    </Grid>
                    
                    <Label Text="Confidence Threshold"/>
                    <Slider Minimum="0.5"
                           Maximum="1.0"
                           Value="{Binding ConfidenceThreshold}"/>
                    
                    <Grid ColumnDefinitions="*,Auto">
                        <Label Grid.Column="0"
                               Text="Offline Mode"
                               VerticalOptions="Center"/>
                        <Switch Grid.Column="1"
                               IsToggled="{Binding OfflineMode}"/>
                    </Grid>
                </VerticalStackLayout>
            </Border>
            
            <!-- Text-to-Speech Settings -->
            <Border Padding="16" StyleClass="SettingsSection">
                <VerticalStackLayout Spacing="16">
                    <Label Text="Text-to-Speech"
                           FontSize="18"
                           FontAttributes="Bold"/>
                    
                    <Grid ColumnDefinitions="*,Auto">
                        <Label Grid.Column="0"
                               Text="Enable Narration"
                               VerticalOptions="Center"/>
                        <Switch Grid.Column="1"
                               IsToggled="{Binding TtsEnabled}"/>
                    </Grid>
                    
                    <Label Text="Speech Rate"/>
                    <Slider Minimum="0.5"
                           Maximum="2.0"
                           Value="{Binding SpeechRate}"/>
                    
                    <Label Text="Voice Pitch"/>
                    <Slider Minimum="0.5"
                           Maximum="2.0"
                           Value="{Binding VoicePitch}"/>
                </VerticalStackLayout>
            </Border>
            
            <!-- Display Settings -->
            <Border Padding="16" StyleClass="SettingsSection">
                <VerticalStackLayout Spacing="16">
                    <Label Text="Display"
                           FontSize="18"
                           FontAttributes="Bold"/>
                    
                    <Label Text="Font Size"/>
                    <Slider Minimum="12"
                           Maximum="24"
                           Value="{Binding FontSize}"/>
                    
                    <Label Text="Theme"/>
                    <Picker ItemsSource="{Binding AvailableThemes}"
                           SelectedItem="{Binding SelectedTheme}"/>
                </VerticalStackLayout>
            </Border>
            
            <!-- About -->
            <Border Padding="16" StyleClass="SettingsSection">
                <VerticalStackLayout Spacing="8">
                    <Label Text="About"
                           FontSize="18"
                           FontAttributes="Bold"/>
                    <Label Text="{Binding AppVersion}"
                           FontSize="12"/>
                    <Label Text="© 2025 The Cabin"
                           FontSize="12"/>
                </VerticalStackLayout>
            </Border>
            
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

## Theme System

### Color Schemes

```xml
<!-- Light Theme -->
<ResourceDictionary>
    <Color x:Key="PageBackgroundColor">#F5F5F5</Color>
    <Color x:Key="PrimaryTextColor">#212121</Color>
    <Color x:Key="SecondaryTextColor">#757575</Color>
    <Color x:Key="PrimaryColor">#2196F3</Color>
    <Color x:Key="AccentColor">#FF5722</Color>
    <Color x:Key="CardBackgroundColor">#FFFFFF</Color>
    <Color x:Key="RecordingColor">#F44336</Color>
</ResourceDictionary>

<!-- Dark Theme -->
<ResourceDictionary>
    <Color x:Key="PageBackgroundColor">#121212</Color>
    <Color x:Key="PrimaryTextColor">#E0E0E0</Color>
    <Color x:Key="SecondaryTextColor">#9E9E9E</Color>
    <Color x:Key="PrimaryColor">#2196F3</Color>
    <Color x:Key="AccentColor">#FF5722</Color>
    <Color x:Key="CardBackgroundColor">#1E1E1E</Color>
    <Color x:Key="RecordingColor">#F44336</Color>
</ResourceDictionary>
```

## Accessibility Features

### 1. Screen Reader Support

```csharp
public void ConfigureAccessibility()
{
    // Set semantic descriptions
    VoiceButton.SetValue(SemanticProperties.DescriptionProperty,
        "Tap to speak a command");
    
    VoiceButton.SetValue(SemanticProperties.HintProperty,
        "Records your voice command for the game");
    
    // Announce state changes
    SemanticScreenReader.Announce(
        "Voice recording started");
}
```

### 2. High Contrast Mode

```csharp
public class AccessibilityService
{
    public bool IsHighContrastEnabled()
    {
        return Preferences.Get("high_contrast", false);
    }
    
    public void ApplyHighContrast()
    {
        Application.Current.Resources["PrimaryTextColor"] = 
            Colors.White;
        Application.Current.Resources["PageBackgroundColor"] = 
            Colors.Black;
    }
}
```

### 3. Font Scaling

```csharp
public double GetScaledFontSize(double baseSize)
{
    var scale = Preferences.Get("font_scale", 1.0);
    return baseSize * scale;
}
```

## Animations and Transitions

### Page Transitions

```csharp
public async Task NavigateWithTransitionAsync(Page page)
{
    page.Opacity = 0;
    await Navigation.PushAsync(page, false);
    await page.FadeTo(1, 250, Easing.CubicInOut);
}
```

### Button Press Feedback

```csharp
private async void OnButtonPressed(object sender, EventArgs e)
{
    var button = (Button)sender;
    await button.ScaleTo(0.95, 50);
    await button.ScaleTo(1.0, 50);
}
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-23  
**Related Documents**: 02-system-architecture.md, 08-maui-implementation.md
