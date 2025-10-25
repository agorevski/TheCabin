# Bug Tracking Template - The Cabin MAUI App

## How to Use This Template

1. Copy the bug template below for each new issue
2. Fill in all applicable fields
3. Include screenshots/recordings when possible
4. Keep the "For AI Debugging" section detailed - this helps me assist you
5. Update status as the bug progresses

---

## BUG-XXX: [Brief Title]

### Basic Information
- **Bug ID:** BUG-XXX
- **Date Reported:** YYYY-MM-DD
- **Reported By:** [Your Name]
- **Status:** 🔴 Open | 🟡 In Progress | 🟢 Fixed | ✅ Verified | ❌ Closed
- **Priority:** Critical / High / Medium / Low

### Affected Component
- **Screen/Page:** [MainPage | InventoryPage | SettingsPage | etc.]
- **UI Element:** [Button | CollectionView | Label | etc.]
- **XAML File:** `src/TheCabin.Maui/Views/[PageName].xaml` (Line: XX)
- **ViewModel:** `src/TheCabin.Maui/ViewModels/[ViewModelName].cs` (Line: XX)

### Issue Classification
- **Category:** 
  - [ ] Element Not Displaying
  - [ ] Button/Interaction Not Working
  - [ ] Data Binding Issue
  - [ ] Navigation Problem
  - [ ] Layout/Positioning Issue
  - [ ] Performance Issue
  - [ ] Crash/Exception
  - [ ] Other: ___________

### Description
[Clear, concise description of the issue]

### Steps to Reproduce
1. [First step]
2. [Second step]
3. [Third step]
4. ...

### Expected Behavior
[What should happen]

### Actual Behavior
[What actually happens]

### Environment
- **Device Type:** Emulator / Physical Device
- **Device Model:** [e.g., Pixel 5, Samsung Galaxy S21]
- **Android Version:** [e.g., API 33 (Android 13)]
- **App Version:** [e.g., 1.0.0]
- **MAUI Version:** [e.g., .NET 9.0]

### Visual Evidence
- **Screenshot:** [Path or embedded image]
- **Video:** [Path or link]
- **Logs:** [Relevant log output]

### For AI Debugging
> **This section helps me diagnose and fix the issue. Be as detailed as possible!**

#### Code References
```
XAML Element ID: [e.g., StoryFeedCollectionView]
Binding Path: [e.g., {Binding StoryFeed}]
ViewModel Property: [e.g., ObservableCollection<NarrativeEntry> StoryFeed]
```

#### What I've Checked
- [ ] Element exists in XAML
- [ ] Element has x:Name attribute
- [ ] Binding syntax is correct
- [ ] ViewModel property exists
- [ ] Property implements INotifyPropertyChanged
- [ ] Data is present in ViewModel
- [ ] No XAML compilation errors
- [ ] No runtime binding errors in output window

#### Debug Commands for AI
```csharp
// Commands I can run to help debug
// Example: "Check if StoryFeed ObservableCollection is initialized"
// Example: "Verify MainViewModel is set as BindingContext"
// Example: "Look for binding errors in output logs"
```

#### Additional Context
[Any other relevant information, workarounds attempted, related issues, etc.]

### Resolution
- **Root Cause:** [What caused the issue]
- **Fix Applied:** [What was changed to fix it]
- **Files Modified:** [List of files changed]
- **Commit:** [Git commit hash if applicable]

### Verification
- [ ] Fix verified on emulator
- [ ] Fix verified on physical device
- [ ] Automated test added
- [ ] Regression testing passed

---

## Example Bug Report

## BUG-001: Inventory Items Not Displaying in CollectionView

### Basic Information
- **Bug ID:** BUG-001
- **Date Reported:** 2025-10-25
- **Reported By:** Developer
- **Status:** 🟡 In Progress
- **Priority:** High

### Affected Component
- **Screen/Page:** InventoryPage
- **UI Element:** CollectionView (Items list)
- **XAML File:** `src/TheCabin.Maui/Views/InventoryPage.xaml` (Lines 45-120)
- **ViewModel:** `src/TheCabin.Maui/ViewModels/InventoryViewModel.cs` (Line 35)

### Issue Classification
- **Category:** 
  - [x] Element Not Displaying
  - [x] Data Binding Issue

### Description
The InventoryPage CollectionView shows the empty state message even when items exist in the player's inventory. The CollectionView.EmptyView is always visible.

### Steps to Reproduce
1. Launch the app
2. Navigate to MainPage
3. Execute voice command "take lantern"
4. Verify item was added (check story feed for success message)
5. Tap Inventory button (🎒) in bottom navigation
6. Observe InventoryPage

### Expected Behavior
- CollectionView should display item cards for each inventory item
- Each card should show item icon, name, description, and weight
- Empty state should NOT be visible

### Actual Behavior
- CollectionView appears empty
- EmptyView message displays: "Your inventory is empty"
- No item cards render
- Weight display shows "0/20"

### Environment
- **Device Type:** Emulator
- **Device Model:** Pixel 5 API 33
- **Android Version:** API 33 (Android 13)
- **App Version:** 1.0.0-dev
- **MAUI Version:** .NET 9.0

### Visual Evidence
- **Screenshot:** `screenshots/bug-001-empty-inventory.png`
- **Logs:** 
```
[Binding] Binding: 'InventoryItems' property not found on 'InventoryViewModel'
```

### For AI Debugging

#### Code References
```
XAML Element ID: InventoryCollectionView
Binding Path: {Binding InventoryItems}
ViewModel Property: ObservableCollection<GameObject> Items
```

#### What I've Checked
- [x] Element exists in XAML
- [x] Element has x:Name attribute
- [x] Binding syntax looks correct
- [x] ViewModel property exists (but named "Items", not "InventoryItems"!)
- [ ] Property implements INotifyPropertyChanged
- [x] Data is present in ViewModel
- [x] No XAML compilation errors
- [x] Binding error found in output window

#### Debug Commands for AI
```csharp
// 1. Check the ItemsSource binding in InventoryPage.xaml
// 2. Check the property name in InventoryViewModel
// 3. Verify ObservableCollection is initialized in ViewModel constructor
// 4. Check if InventoryViewModel.RefreshInventory() is called on page appearing
```

#### Additional Context
**Issue Found:** The XAML binds to `{Binding InventoryItems}` but the ViewModel property is named `Items`. This is a naming mismatch causing the binding to fail.

**Suspected Fix:** Either:
1. Rename ViewModel property from `Items` to `InventoryItems`, OR
2. Update XAML binding from `InventoryItems` to `Items`

Option 2 is simpler and maintains consistency with other ViewModels.

### Resolution
- **Root Cause:** XAML binding path `InventoryItems` doesn't match ViewModel property name `Items`
- **Fix Applied:** Changed XAML binding to `{Binding Items}` in InventoryPage.xaml line 52
- **Files Modified:** 
  - `src/TheCabin.Maui/Views/InventoryPage.xaml`
- **Commit:** abc123def

### Verification
- [x] Fix verified on emulator
- [ ] Fix verified on physical device
- [ ] Automated test added
- [ ] Regression testing passed

---

## Quick Reference: Common Bug Types

### 1. Element Not Displaying
**Check:**
- Element visibility (`IsVisible="True"`)
- Parent container layout
- Opacity (`Opacity="1"`)
- Z-index / rendering order
- Platform-specific rendering issues

### 2. Button/Interaction Not Working
**Check:**
- Command binding (`Command="{Binding CommandName}"`)
- IsEnabled property
- Command implementation in ViewModel
- Event handlers in code-behind
- Touch/tap target size

### 3. Data Binding Issue
**Check:**
- Binding path matches property name exactly
- BindingContext is set
- Property implements INotifyPropertyChanged
- ObservableCollection for lists
- DataTemplate is defined
- Output window for binding errors

### 4. Navigation Problem
**Check:**
- Routes registered in AppShell
- Navigation command implementation
- Back button behavior
- Shell.Current is available
- Navigation parameters

### 5. Layout/Positioning Issue
**Check:**
- Grid row/column definitions
- HorizontalOptions / VerticalOptions
- Margin and Padding
- Parent container constraints
- Different screen sizes/orientations

---

## Bug Status Definitions

- 🔴 **Open** - Bug reported, not yet investigated
- 🟡 **In Progress** - Currently being debugged/fixed
- 🟢 **Fixed** - Fix implemented, awaiting verification
- ✅ **Verified** - Fix confirmed working
- ❌ **Closed** - Resolved or won't fix

## Priority Definitions

- **Critical** - App crashes, data loss, core functionality broken
- **High** - Major feature not working, significant UX impact
- **Medium** - Minor feature issue, workaround available
- **Low** - Cosmetic issue, enhancement request
