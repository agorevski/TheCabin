# UI Testing & Debugging Guide - The Cabin MAUI App

## Overview

This guide provides a comprehensive approach to testing and debugging The Cabin's MAUI user interface. Use this alongside the Bug Tracking Template and UI Test Checklist.

---

## Quick Start

### 1. Prerequisites
- Android SDK installed
- Android emulator or physical device
- The Cabin MAUI app built and deployed
- Visual Studio or VS Code open with project loaded

### 2. Testing Workflow
```
1. Open UI_TEST_CHECKLIST.md
2. Test each screen systematically
3. For failures: Create bug report using BUG_TRACKING_TEMPLATE.md
4. Share bug report with AI assistant for debugging help
5. Apply fixes and retest
```

---

## Common UI Issues & Solutions

### Issue 1: Element Not Displaying

#### Symptoms
- XAML element defined but not visible on screen
- Empty space where element should be
- Layout looks incomplete

#### Debugging Steps
1. **Check Visibility**
   ```xml
   <!-- Ensure IsVisible is True or not set (defaults to True) -->
   <Label Text="Hello" IsVisible="True" />
   ```

2. **Check Parent Container**
   ```xml
   <!-- Parent must have space for child -->
   <Grid RowDefinitions="Auto,*">
       <Label Grid.Row="0" Text="Header" />
       <!-- If Grid.Row not set, might be hidden behind Row 0 -->
   </Grid>
   ```

3. **Check Opacity**
   ```xml
   <!-- Element might be transparent -->
   <Label Text="Hello" Opacity="1.0" />
   ```

4. **Check Z-Index / Rendering Order**
   ```xml
   <!-- Element might be behind another element -->
   <Grid>
       <BoxView Color="Red" /> <!-- This covers everything below -->
       <Label Text="Hidden!" /> <!-- This is hidden behind BoxView -->
   </Grid>
   ```

5. **Verify in Visual Studio Output**
   - Look for XAML compile errors
   - Check for binding errors
   - Monitor debug output during app start

#### AI Assistant Query Template
```
"I have a [Element Type] that's not displaying on [Page Name].
- XAML file: src/TheCabin.Maui/Views/[PageName].xaml (Line XX)
- Element ID: [x:Name value]
- What I checked:
  - IsVisible="True" ✓
  - Parent container has space ✓
  - No XAML errors ✓
- Visual Studio Output shows: [paste any relevant errors]
Please help debug this."
```

---

### Issue 2: Data Binding Not Working

#### Symptoms
- UI elements show empty/default values
- Changes to ViewModel don't update UI
- CollectionView shows no items despite data in ViewModel

#### Debugging Steps

1. **Verify Binding Path**
   ```xml
   <!-- XAML -->
   <Label Text="{Binding PlayerName}" />
   
   <!-- ViewModel must have EXACT property name -->
   public string PlayerName { get; set; }  // ✓ Correct
   public string playerName { get; set; }  // ✗ Wrong (case matters!)
   ```

2. **Check BindingContext**
   ```csharp
   // MainPage.xaml.cs
   public MainPage(MainViewModel viewModel)
   {
       InitializeComponent();
       BindingContext = viewModel;  // ✓ Must set this!
   }
   ```

3. **Verify INotifyPropertyChanged**
   ```csharp
   // Using CommunityToolkit.Mvvm
   public partial class MainViewModel : ObservableObject
   {
       [ObservableProperty]  // ✓ Auto-generates INotifyPropertyChanged
       private string playerName;
       
       // Or manual implementation:
       private string _playerName;
       public string PlayerName
       {
           get => _playerName;
           set
           {
               _playerName = value;
               OnPropertyChanged();  // ✓ Must call this!
           }
       }
   }
   ```

4. **Check Output Window for Binding Errors**
   ```
   [Binding] Error: PropertyName 'PlayerName' not found on 'MainViewModel'
   ```

5. **Test with Static Value**
   ```xml
   <!-- If this shows, binding syntax is correct -->
   <Label Text="Test" />
   
   <!-- If this doesn't show, binding path or ViewModel is wrong -->
   <Label Text="{Binding PlayerName}" />
   ```

#### AI Assistant Query Template
```
"Data binding not working on [Page Name].
- XAML Binding: {Binding PropertyName}
- ViewModel Property: [property declaration]
- BindingContext set: [Yes/No]
- INotifyPropertyChanged: [Yes/No]
- Output Window shows: [paste binding errors]
- Static text displays: [Yes/No]
Please help debug."
```

---

### Issue 3: Button/Command Not Working

#### Symptoms
- Button tap does nothing
- Command doesn't execute
- No visual feedback on button press

#### Debugging Steps

1. **Verify Command Binding**
   ```xml
   <!-- XAML -->
   <Button Text="Save" Command="{Binding SaveCommand}" />
   ```

2. **Check Command Implementation**
   ```csharp
   // Using CommunityToolkit.Mvvm
   [RelayCommand]  // ✓ Auto-generates SaveCommand property
   private async Task SaveAsync()
   {
       // Command logic
   }
   
   // Or manual:
   public ICommand SaveCommand { get; }
   
   public MainViewModel()
   {
       SaveCommand = new Command(async () => await SaveAsync());
   }
   ```

3. **Verify IsEnabled**
   ```xml
   <!-- Button might be disabled -->
   <Button Text="Save" 
           Command="{Binding SaveCommand}"
           IsEnabled="True" />
   ```

4. **Check Command Can Execute**
   ```csharp
   [RelayCommand(CanExecute = nameof(CanSave))]
   private async Task SaveAsync() { }
   
   private bool CanSave() => true;  // Must return true to enable
   ```

5. **Add Debug Output**
   ```csharp
   [RelayCommand]
   private async Task SaveAsync()
   {
       Debug.WriteLine("Save button clicked!");  // Check Output window
       // Rest of logic
   }
   ```

#### AI Assistant Query Template
```
"Button not responding on [Page Name].
- Button XAML: [paste button element]
- Command binding: {Binding CommandName}
- Command implementation: [paste command code]
- IsEnabled: True
- Debug output: [what you see in Output window]
Please help debug."
```

---

### Issue 4: CollectionView Not Showing Items

#### Symptoms
- CollectionView appears empty
- EmptyView always showing
- Items exist in ViewModel but don't display

#### Debugging Steps

1. **Verify ItemsSource Binding**
   ```xml
   <CollectionView ItemsSource="{Binding Items}">
       <!-- ... -->
   </CollectionView>
   ```

2. **Check ViewModel Property**
   ```csharp
   // Must be ObservableCollection for UI updates
   public ObservableCollection<ItemModel> Items { get; set; }
   
   public MainViewModel()
   {
       Items = new ObservableCollection<ItemModel>();  // ✓ Initialize!
   }
   ```

3. **Verify ItemTemplate**
   ```xml
   <CollectionView ItemsSource="{Binding Items}">
       <CollectionView.ItemTemplate>
           <DataTemplate x:DataType="models:ItemModel">
               <Label Text="{Binding Name}" />
           </DataTemplate>
       </CollectionView.ItemTemplate>
   </CollectionView>
   ```

4. **Check Data Population**
   ```csharp
   public async Task LoadItemsAsync()
   {
       Items.Clear();
       var items = await _service.GetItemsAsync();
       foreach (var item in items)
       {
           Items.Add(item);  // Add items to ObservableCollection
       }
       Debug.WriteLine($"Loaded {Items.Count} items");  // Verify count
   }
   ```

5. **Test with Hardcoded Data**
   ```csharp
   public MainViewModel()
   {
       Items = new ObservableCollection<ItemModel>
       {
           new ItemModel { Name = "Test 1" },
           new ItemModel { Name = "Test 2" }
       };
   }
   ```

#### AI Assistant Query Template
```
"CollectionView not displaying items on [Page Name].
- ItemsSource binding: {Binding Items}
- ViewModel property type: [ObservableCollection<T> or List<T>]
- Items.Count: [number]
- ItemTemplate defined: [Yes/No]
- EmptyView showing: [Yes/No]
- Hardcoded items work: [Yes/No]
Please help debug."
```

---

### Issue 5: Navigation Not Working

#### Symptoms
- Button press doesn't navigate to new page
- Back button doesn't work
- Navigation throws exception

#### Debugging Steps

1. **Verify Route Registration**
   ```csharp
   // AppShell.xaml.cs or MauiProgram.cs
   Routing.RegisterRoute(nameof(InventoryPage), typeof(InventoryPage));
   ```

2. **Check Navigation Command**
   ```csharp
   [RelayCommand]
   private async Task ShowInventoryAsync()
   {
       await Shell.Current.GoToAsync(nameof(InventoryPage));
   }
   ```

3. **Verify Shell.Current Exists**
   ```csharp
   if (Shell.Current != null)
   {
       await Shell.Current.GoToAsync(nameof(InventoryPage));
   }
   else
   {
       Debug.WriteLine("Shell.Current is null!");
   }
   ```

4. **Check AppShell.xaml**
   ```xml
   <ShellContent
       Title="Inventory"
       ContentTemplate="{DataTemplate views:InventoryPage}"
       Route="InventoryPage"
       IsVisible="False" />
   ```

5. **Monitor Navigation Errors**
   ```csharp
   try
   {
       await Shell.Current.GoToAsync(nameof(InventoryPage));
   }
   catch (Exception ex)
   {
       Debug.WriteLine($"Navigation error: {ex.Message}");
   }
   ```

#### AI Assistant Query Template
```
"Navigation not working from [PageA] to [PageB].
- Route registered: [Yes/No]
- Navigation command: [paste code]
- Shell.Current is null: [Yes/No]
- Error message: [paste exception if any]
- AppShell.xaml route: [paste ShellContent]
Please help debug."
```

---

## Testing Best Practices

### 1. Test in Order
- Test parent elements before children
- Test basic display before interactions
- Test data loading before UI updates

### 2. Isolate Issues
- Test one change at a time
- Comment out complex logic to test basic display
- Use hardcoded data to eliminate service issues

### 3. Use Debug Output
```csharp
Debug.WriteLine($"ViewModel initialized: Items.Count = {Items.Count}");
Debug.WriteLine($"Button clicked at {DateTime.Now}");
Debug.WriteLine($"Navigation to {nameof(InventoryPage)}");
```

### 4. Check Visual Studio Output Window
- XAML compile errors
- Binding errors
- Exception messages
- Debug.WriteLine output

### 5. Test on Multiple Devices
- Different screen sizes
- Different Android versions
- Emulator vs physical device

---

## Performance Testing

### Memory Leaks
```csharp
// Dispose of event handlers
public void OnDisappearing()
{
    _viewModel.PropertyChanged -= OnPropertyChanged;
}
```

### UI Responsiveness
```csharp
// Use async for long operations
[RelayCommand]
private async Task LoadDataAsync()
{
    IsBusy = true;
    await Task.Run(() => 
    {
        // Long-running operation
    });
    IsBusy = false;
}
```

### Collection Performance
```csharp
// Use ObservableCollection.AddRange for bulk updates
Items.AddRange(newItems);  // Better than multiple Add() calls
```

---

## Tools & Resources

### Visual Studio Tools
- **XAML Live Preview** - See changes without rebuilding
- **Hot Reload** - Update running app with code changes
- **Output Window** - View debug messages and errors
- **Breakpoints** - Pause execution to inspect state

### MAUI Community Toolkit
```xml
<!-- Useful for testing -->
<PackageReference Include="CommunityToolkit.Maui" Version="7.0.0" />
```

### Diagnostic Commands
```bash
# Clean build
dotnet clean
dotnet build

# Rebuild MAUI workload
dotnet workload restore

# Clear emulator data
adb shell pm clear com.thecabin.voiceadventure
```

---

## When to Ask for AI Help

Ask for help when:
1. You've followed debugging steps but issue persists
2. Error messages are unclear
3. Binding seems correct but doesn't work
4. Navigation fails unexpectedly
5. Performance issues
6. Complex interactions not working

**Always include:**
- Exact error messages
- Code snippets (XAML and C#)
- What you've already tried
- Visual Studio Output window contents

---

## Testing Checklist Reference

See `UI_TEST_CHECKLIST.md` for:
- 150+ specific test cases
- All 7 screens covered
- Systematic testing approach
- Progress tracking

## Bug Report Template

See `BUG_TRACKING_TEMPLATE.md` for:
- Structured bug reporting format
- AI-friendly debugging information
- Example bug reports
- Common bug type reference

---

**Last Updated:** 2025-10-25  
**Related Documents:** UI_TEST_CHECKLIST.md, BUG_TRACKING_TEMPLATE.md
