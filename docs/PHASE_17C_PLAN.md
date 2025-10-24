# Phase 17C: Achievement UI Implementation Plan

## Overview
Phase 17C focuses on creating user interface components to display achievements, show unlock notifications, and track progress.

## Goals
1. Display achievement unlock notifications (toast/popup)
2. Create achievements list page
3. Show achievement progress indicators
4. Integrate with main game flow

## Components to Build

### 1. Achievement View Models

#### AchievementViewModel
**File**: `src/TheCabin.Maui/Models/AchievementViewModel.cs`
- Wraps Achievement model for UI binding
- Properties: Id, Name, Description, Icon, IsUnlocked, Progress, UnlockDate
- Converts from Achievement model

#### AchievementsPageViewModel  
**File**: `src/TheCabin.Maui/ViewModels/AchievementsPageViewModel.cs`
- Manages list of achievements
- Filters by unlocked/locked status
- Sorts achievements
- Handles navigation

### 2. Achievement Notification System

#### AchievementToastService
**File**: `src/TheCabin.Maui/Services/AchievementToastService.cs`
- Shows achievement unlock notifications
- Queue system for multiple unlocks
- Animation support
- Auto-dismiss after timeout

#### Toast UI Component
- Small popup overlay
- Achievement icon + name
- Slide-in animation
- Tap to view details (optional)

### 3. Achievements Page

#### AchievementsPage.xaml
**File**: `src/TheCabin.Maui/Views/AchievementsPage.xaml`
- CollectionView of achievements
- Tabs/segments for All/Unlocked/Locked
- Search/filter functionality
- Progress indicators
- Empty state for no achievements

### 4. Integration Points

#### MainViewModel Updates
- Subscribe to achievement unlock events
- Trigger toast notifications
- Track achievement state

#### AppShell Navigation
- Add achievements page route
- Add navigation menu item

### 5. Visual Design

#### Achievement Card Design
```
┌─────────────────────────────────────┐
│ 🏆  Achievement Name                │
│     Achievement Description         │
│     [████████░░] 80%                │
│     Unlocked: 2025-10-24            │
└─────────────────────────────────────┘
```

#### Toast Notification Design
```
┌─────────────────────────────────┐
│ ✨ Achievement Unlocked!        │
│ 🏆 First Steps                  │
└─────────────────────────────────┘
```

## Implementation Steps

### Step 1: View Models (1-2 hours)
1. Create AchievementViewModel model class
2. Create AchievementsPageViewModel
3. Add achievement-related commands
4. Implement filtering and sorting

### Step 2: Toast Notification System (2-3 hours)
1. Create IAchievementNotificationService interface
2. Implement AchievementToastService
3. Create toast UI component (custom view or use CommunityToolkit.Maui.Alerts)
4. Add animation support
5. Test notification display

### Step 3: Achievements Page (2-3 hours)
1. Create AchievementsPage.xaml
2. Design achievement card template
3. Add filtering UI (All/Unlocked/Locked)
4. Implement progress indicators
5. Add empty state messaging
6. Style with theme colors

### Step 4: Integration (1-2 hours)
1. Update MainViewModel to listen for unlocks
2. Trigger notifications on achievement unlock
3. Add navigation route to AppShell
4. Add menu item to access achievements
5. Update MauiProgram.cs DI registration

### Step 5: Testing & Polish (1-2 hours)
1. Test achievement unlock flow
2. Test notification display
3. Test achievements page filtering
4. Verify on different screen sizes
5. Add haptic feedback (optional)
6. Polish animations

## Technical Decisions

### Achievement Icons
- Use emoji for now (🏆 ⭐ 🎯 🔓 etc.)
- Future: Custom SVG icons per achievement

### Notification Library
- Option 1: Use CommunityToolkit.Maui.Alerts (Toast/Snackbar)
- Option 2: Custom popup implementation
- **Decision**: Use CommunityToolkit.Maui.Alerts for simplicity

### Progress Display
- Linear progress bar for progress-based achievements
- Checkmark icon for completed
- Lock icon for locked

### Data Binding
- Use CommunityToolkit.Mvvm attributes
- ObservableCollection for achievement list
- INotifyPropertyChanged for individual items

## Files to Create

1. `src/TheCabin.Maui/Models/AchievementViewModel.cs`
2. `src/TheCabin.Maui/ViewModels/AchievementsPageViewModel.cs`
3. `src/TheCabin.Maui/Services/IAchievementNotificationService.cs`
4. `src/TheCabin.Maui/Services/AchievementToastService.cs`
5. `src/TheCabin.Maui/Views/AchievementsPage.xaml`
6. `src/TheCabin.Maui/Views/AchievementsPage.xaml.cs`

## Files to Modify

1. `src/TheCabin.Maui/ViewModels/MainViewModel.cs` - Add achievement notifications
2. `src/TheCabin.Maui/AppShell.xaml` - Add achievements route
3. `src/TheCabin.Maui/MauiProgram.cs` - Register new services/views

## Success Criteria

✅ Achievement unlock notifications display automatically  
✅ Achievements page shows all achievements with correct state  
✅ Progress bars display correctly for progress-based achievements  
✅ Filtering works (All/Unlocked/Locked)  
✅ Navigation to achievements page works from menu  
✅ UI is responsive and performs well  
✅ Achievement state persists across app restarts  

## Estimated Time
- Total: 7-12 hours
- Can be completed in 1-2 focused sessions

## Dependencies
- CommunityToolkit.Maui (already included)
- CommunityToolkit.Mvvm (already included)
- IAchievementService (completed in Phase 17B)

## Next Phase
Phase 17D: Enhanced Puzzles (conditional achievements, multi-stage puzzles)

---

**Status**: Ready to Begin  
**Date**: 2025-10-24  
**Prerequisites**: ✅ Phase 17B Complete
